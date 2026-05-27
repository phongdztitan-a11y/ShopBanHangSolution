using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopBanHang.Shared;
using ShopBanHang.Shared.Models;
using ShopBanHang.Shared.Security;
using ShopBanHang.Shared.Utilities;
using WebApplication3.Security;

using WebApplication3.Data; // <--- Dòng này sẽ fix lỗi CS0103/CS0246 của bạn

namespace WebApplication3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly ServerDbContext _context;
        private const string AdminId = "ADMIN_001";
        private const string AdminTaiKhoan = "admin";
        private const string AdminMatKhauMacDinh = "123";
        private const string AdminVaiTro = "QuanLy";
        private const string AdminChiNhanh = "CN_GOC";
        private const string KhachLeId = "KHACH_LE";

        public SyncController(ServerDbContext context)
        {
            _context = context;
        }

        private static NhanVien ToPublicNhanVien(NhanVien source) => new()
        {
            Id = source.Id,
            HoTen = source.HoTen,
            MaNhanVien = source.MaNhanVien,
            TaiKhoan = source.TaiKhoan,
            MatKhau = string.Empty,
            VaiTro = source.VaiTro,
            MaChiNhanh = source.MaChiNhanh,
            LanDangNhapOnlineGanNhat = source.LanDangNhapOnlineGanNhat,
            TrangThaiDongBo = source.TrangThaiDongBo,
            NgayCapNhat = source.NgayCapNhat,
            DaXoa = source.DaXoa
        };

        private static NhanVien ToLoginNhanVien(NhanVien source)
        {
            var user = ToPublicNhanVien(source);
            user.MatKhau = source.MatKhau;
            return user;
        }

        private async Task<NhanVien?> GetCurrentUserAsync()
        {
            if (!ApiTokenService.TryGetUserId(Request, out var userId))
                return null;

            return await _context.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(n => !n.DaXoa && n.Id == userId);
        }

        private static bool CoQuyenQuanLy(NhanVien user) =>
            user.Id == AdminId
            || string.Equals(user.TaiKhoan, AdminTaiKhoan, StringComparison.OrdinalIgnoreCase)
            || user.VaiTro == AdminVaiTro
            || user.VaiTro == "QL"
            || string.Equals(user.VaiTro, "Admin", StringComparison.OrdinalIgnoreCase);

        private static void NormalizeBaseDates(BaseModel model)
        {
            model.NgayCapNhat = DateTimeUtc.Normalize(model.NgayCapNhat);
        }

        private static void NormalizeNhanVienDates(NhanVien model)
        {
            NormalizeBaseDates(model);
            model.LanDangNhapOnlineGanNhat = DateTimeUtc.Normalize(model.LanDangNhapOnlineGanNhat);
        }

        private static void NormalizeHoaDonDates(HoaDon model)
        {
            NormalizeBaseDates(model);
            model.NgayLap = DateTimeUtc.Normalize(model.NgayLap);
        }

        private async Task EnsureAdminTongAsync()
        {
            var admin = await _context.NhanViens.FirstOrDefaultAsync(n =>
                n.Id == AdminId || (n.TaiKhoan != null && n.TaiKhoan.ToLower() == AdminTaiKhoan));

            if (admin == null)
            {
                _context.NhanViens.Add(new NhanVien
                {
                    Id = AdminId,
                    TaiKhoan = AdminTaiKhoan,
                    MatKhau = PasswordHasher.Hash(AdminMatKhauMacDinh),
                    HoTen = "Quản Trị Viên",
                    VaiTro = AdminVaiTro,
                    MaNhanVien = "NV001",
                    MaChiNhanh = AdminChiNhanh,
                    DaXoa = false,
                    TrangThaiDongBo = 1,
                    NgayCapNhat = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                return;
            }

            admin.Id = AdminId;
            admin.TaiKhoan = AdminTaiKhoan;
            admin.MaChiNhanh = AdminChiNhanh;
            admin.VaiTro = AdminVaiTro;
            admin.DaXoa = false;
            if (string.IsNullOrWhiteSpace(admin.MatKhau))
                admin.MatKhau = PasswordHasher.Hash(AdminMatKhauMacDinh);
            else if (!PasswordHasher.IsHashed(admin.MatKhau))
                admin.MatKhau = PasswordHasher.Hash(admin.MatKhau);
            if (string.IsNullOrWhiteSpace(admin.HoTen))
                admin.HoTen = "Quản Trị Viên";
            admin.NgayCapNhat = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        // POS gửi hóa đơn lên Server
        [HttpPost("PostHoaDon")]
        public async Task<IActionResult> PostHoaDon([FromBody] DongBoWrapper wrapper)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            if (wrapper == null) return BadRequest("Payload rỗng.");
            // Lấy danh sách ra từ wrapper
            var dsGoi = wrapper.dsGoi;

            if (dsGoi == null || !dsGoi.Any()) return BadRequest("Không có dữ liệu gửi lên.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Trong SyncController.cs
                foreach (var goi in dsGoi)
                {
                    if (goi?.HoaDon == null) continue;

                    if (await _context.HoaDons.AnyAsync(h =>
                        h.Id == goi.HoaDon.Id || h.MaHoaDon == goi.HoaDon.MaHoaDon))
                    {
                        continue;
                    }

                    // --- ĐẢM BẢO DỮ LIỆU THAM CHIẾU (FK) TỒN TẠI ---
                    // Server DB hiện đang có FK bắt buộc: HoaDon -> ChiNhanh, NhanVien và (thực tế) IdKhachHang không được NULL.
                    // Vì POS offline-first có thể gửi lên trước khi đồng bộ danh mục, ta tự tạo bản ghi tối thiểu để không 500.
                    var maChiNhanh = string.IsNullOrWhiteSpace(goi.HoaDon.MaChiNhanh) ? "CN_GOC" : goi.HoaDon.MaChiNhanh.Trim();
                    goi.HoaDon.MaChiNhanh = maChiNhanh;
                    NormalizeHoaDonDates(goi.HoaDon);

                    // HoaDon.MaChiNhanh currently references ChiNhanh.Id in the existing migration.
                    // Always ensure a canonical branch row whose Id equals the invoice branch code.
                    var chiNhanh = _context.ChiNhanhs.Local.FirstOrDefault(c => c.Id == maChiNhanh);
                    if (chiNhanh == null)
                    {
                        chiNhanh = await _context.ChiNhanhs.FirstOrDefaultAsync(c => c.Id == maChiNhanh);
                        if (chiNhanh == null)
                        {
                            var chiNhanhTheoMa = await _context.ChiNhanhs
                                .AsNoTracking()
                                .Where(c => c.MaChiNhanh == maChiNhanh)
                                .OrderByDescending(c => c.NgayCapNhat)
                                .FirstOrDefaultAsync();

                            var newChi = new ChiNhanh
                            {
                                Id = maChiNhanh,
                                MaChiNhanh = maChiNhanh,
                                TenChiNhanh = string.IsNullOrWhiteSpace(chiNhanhTheoMa?.TenChiNhanh)
                                    ? maChiNhanh
                                    : chiNhanhTheoMa.TenChiNhanh,
                                DaXoa = false,
                                TrangThaiDongBo = 1,
                                NgayCapNhat = DateTime.UtcNow
                            };
                            _context.ChiNhanhs.Add(newChi);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(goi.HoaDon.IdKhachHang))
                        goi.HoaDon.IdKhachHang = "KHACH_LE";

                    // Avoid tracking duplicate KhachHang instances
                    var khach = _context.KhachHangs.Local.FirstOrDefault(k => k.Id == goi.HoaDon.IdKhachHang);
                    if (khach == null)
                    {
                        khach = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Id == goi.HoaDon.IdKhachHang);
                        if (khach == null)
                        {
                            if (goi.HoaDon.IdKhachHang == "KHACH_LE")
                            {
                                _context.KhachHangs.Add(new KhachHang
                                {
                                    Id = "KHACH_LE",
                                    HoTen = "Khách bán lẻ",
                                    SoDienThoai = "0000000000",
                                    DiaChi = "",
                                    DiemTichLuy = 0,
                                    MaChiNhanh = maChiNhanh,
                                    DaXoa = false,
                                    TrangThaiDongBo = 1,
                                    NgayCapNhat = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                var sdt = string.IsNullOrWhiteSpace(goi.HoaDon.SdtKhachHang)
                                    ? "0000000000"
                                    : goi.HoaDon.SdtKhachHang.Trim();
                                _context.KhachHangs.Add(new KhachHang
                                {
                                    Id = goi.HoaDon.IdKhachHang,
                                    HoTen = $"Khách ({sdt})",
                                    SoDienThoai = sdt,
                                    DiaChi = "",
                                    DiemTichLuy = 0,
                                    MaChiNhanh = maChiNhanh,
                                    DaXoa = false,
                                    TrangThaiDongBo = 1,
                                    NgayCapNhat = DateTime.UtcNow
                                });
                            }
                        }
                    }

                    // Avoid duplicate tracking for NhanVien
                    var nv = _context.NhanViens.Local.FirstOrDefault(n => n.Id == goi.HoaDon.IdNhanVien);
                    if (nv == null)
                    {
                        nv = await _context.NhanViens.FirstOrDefaultAsync(n => n.Id == goi.HoaDon.IdNhanVien);
                        if (nv == null)
                        {
                            var tenNguoiBan = string.IsNullOrWhiteSpace(goi.HoaDon.HoTenNguoiBan)
                                ? goi.HoaDon.IdNhanVien
                                : goi.HoaDon.HoTenNguoiBan.Trim();

                            _context.NhanViens.Add(new NhanVien
                            {
                                Id = goi.HoaDon.IdNhanVien,
                                TaiKhoan = "sync_" + goi.HoaDon.IdNhanVien,
                                MatKhau = "",
                                HoTen = tenNguoiBan ?? "",
                                VaiTro = "NV",
                                MaNhanVien = "",
                                MaChiNhanh = maChiNhanh,
                                DaXoa = false,
                                TrangThaiDongBo = 1,
                                NgayCapNhat = DateTime.UtcNow
                            });
                        }
                    }

                    // --- NGẮT KẾT NỐI OBJECT ---
                    goi.HoaDon.ChiNhanh = null;   // Quan trọng: Ngắt để không tìm ChiNhanhId
                    goi.HoaDon.KhachHang = null;  // Ngắt để không tìm KhachHangId
                    goi.HoaDon.NhanVien = null;   // Ngắt để không tìm NhanVienId
                    goi.HoaDon.ChiTiets = null;   // Ngắt vì chúng ta AddRange riêng ở dưới

                    // POS gửi lúc còn "chờ đồng bộ" (0); sau khi lưu lên DB server = đã tiếp nhận đồng bộ
                    goi.HoaDon.TrangThaiDongBo = 1;

                    _context.HoaDons.Add(goi.HoaDon);

                    if (goi.ChiTiets != null)
                    {
                        foreach (var ct in goi.ChiTiets)
                        {
                            ct.IdHoaDon = goi.HoaDon.Id;
                            ct.HoaDon = null;
                            ct.SanPham = null;
                            ct.TrangThaiDongBo = 1;
                            NormalizeBaseDates(ct);
                            // Nếu ChiTietHoaDon cũng có ChiNhanh, hãy gán null nốt:
                            // ct.ChiNhanh = null; 
                        }
                        _context.ChiTietHoaDons.AddRange(goi.ChiTiets);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var rootMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, rootMessage);
            }
        }

        /// <summary>POS kéo hóa đơn + chi tiết từ server (đồng bộ đa máy). Lọc theo MaChiNhanh nếu có.</summary>
        [HttpGet("GetHoaDonsForDongBo")]
        public async Task<IActionResult> GetHoaDonsForDongBo([FromQuery] string? maChiNhanh)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            var q = _context.HoaDons.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(maChiNhanh))
                q = q.Where(h => h.MaChiNhanh == maChiNhanh);

            var raw = await q
                .Include(h => h.ChiTiets)
                .OrderByDescending(h => h.NgayLap)
                .ToListAsync();

            var result = new List<GoiDongBoHoaDonServer>();
            foreach (var h in raw)
            {
                var chiTs = h.ChiTiets?.Select(ct => new ChiTietHoaDon
                {
                    Id = ct.Id,
                    IdHoaDon = ct.IdHoaDon,
                    IdSanPham = ct.IdSanPham,
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    TenSanPhamLuu = ct.TenSanPhamLuu,
                    SKULuu = ct.SKULuu,
                    MaChiNhanh = ct.MaChiNhanh,
                    TrangThaiDongBo = ct.TrangThaiDongBo,
                    NgayCapNhat = ct.NgayCapNhat,
                    DaXoa = ct.DaXoa
                }).ToList() ?? new List<ChiTietHoaDon>();

                result.Add(new GoiDongBoHoaDonServer
                {
                    HoaDon = new HoaDon
                    {
                        Id = h.Id,
                        MaHoaDon = h.MaHoaDon,
                        NgayLap = h.NgayLap,
                        TongTien = h.TongTien,
                        SdtKhachHang = h.SdtKhachHang,
                        HoTenNguoiBan = h.HoTenNguoiBan,
                        IdKhachHang = h.IdKhachHang,
                        IdNhanVien = h.IdNhanVien,
                        MaChiNhanh = h.MaChiNhanh,
                        TrangThaiDongBo = h.TrangThaiDongBo,
                        NgayCapNhat = h.NgayCapNhat,
                        DaXoa = h.DaXoa
                    },
                    ChiTiets = chiTs
                });
            }

            return Ok(result);
        }

        // POS lấy sản phẩm từ Server về
        [HttpGet("GetSanPhams")]
        public async Task<IActionResult> GetSanPhams()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            var data = await _context.SanPhams.AsNoTracking().ToListAsync();
            return Ok(data);
        }

        [HttpGet("GetChiNhanhs")]
        public async Task<IActionResult> GetChiNhanhs()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            var data = await _context.ChiNhanhs
                .AsNoTracking()
                .OrderByDescending(c => c.NgayCapNhat)
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost("UpsertChiNhanhs")]
        public async Task<IActionResult> UpsertChiNhanhs([FromBody] UpsertChiNhanhRequest? req)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");
            if (!CoQuyenQuanLy(currentUser))
                return StatusCode(403, "Chỉ tài khoản admin hoặc quản lý mới được thêm/sửa chi nhánh.");

            if (req?.ChiNhanhs == null || req.ChiNhanhs.Count == 0)
                return BadRequest("Không có dữ liệu chi nhánh.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in req.ChiNhanhs)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.MaChiNhanh))
                        continue;

                    NormalizeBaseDates(item);

                    var current = await _context.ChiNhanhs.FirstOrDefaultAsync(c =>
                        c.Id == item.Id || c.MaChiNhanh == item.MaChiNhanh);

                    if (current == null)
                    {
                        if (string.IsNullOrWhiteSpace(item.Id))
                            item.Id = item.MaChiNhanh;
                        item.TrangThaiDongBo = 1;
                        if (item.NgayCapNhat == default) item.NgayCapNhat = DateTime.UtcNow;
                        _context.ChiNhanhs.Add(item);
                        continue;
                    }

                    if (item.NgayCapNhat < current.NgayCapNhat)
                        continue;

                    current.Id = string.IsNullOrWhiteSpace(current.Id) ? item.MaChiNhanh : current.Id;
                    current.MaChiNhanh = item.MaChiNhanh;
                    current.TenChiNhanh = item.TenChiNhanh;
                    current.DaXoa = item.DaXoa;
                    current.TrangThaiDongBo = 1;
                    current.NgayCapNhat = item.NgayCapNhat == default ? DateTime.UtcNow : item.NgayCapNhat;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var rootMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, rootMessage);
            }
        }

        [HttpGet("GetTonKhoChiNhanhs")]
        public async Task<IActionResult> GetTonKhoChiNhanhs()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            var data = await _context.TonKhoChiNhanhs
                .AsNoTracking()
                .OrderByDescending(t => t.NgayCapNhat)
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>POS kéo danh sách khách hàng (kể cả đã xóa mềm) để đồng bộ đa máy.</summary>
        [HttpGet("GetKhachHangs")]
        public async Task<IActionResult> GetKhachHangs()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            var data = await _context.KhachHangs
                .AsNoTracking()
                .OrderByDescending(k => k.NgayCapNhat)
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost("UpsertKhachHangs")]
        public async Task<IActionResult> UpsertKhachHangs([FromBody] UpsertKhachHangRequest? req)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            if (req?.KhachHangs == null || req.KhachHangs.Count == 0)
                return BadRequest("Không có dữ liệu khách hàng.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in req.KhachHangs)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Id))
                        continue;

                    NormalizeBaseDates(item);

                    if (string.IsNullOrWhiteSpace(item.DiaChi))
                        item.DiaChi = "";

                    bool laKhachLe = item.Id == KhachLeId;

                    var current = await _context.KhachHangs.FirstOrDefaultAsync(k => k.Id == item.Id);
                    if (current == null)
                    {
                        if (laKhachLe)
                        {
                            item.DaXoa = false;
                            item.HoTen = string.IsNullOrWhiteSpace(item.HoTen) ? "Khách lẻ" : item.HoTen;
                            if (string.IsNullOrWhiteSpace(item.SoDienThoai))
                                item.SoDienThoai = "0000000000";
                        }

                        item.TrangThaiDongBo = 1;
                        if (item.NgayCapNhat == default) item.NgayCapNhat = DateTime.UtcNow;
                        _context.KhachHangs.Add(item);
                        continue;
                    }

                    if (item.NgayCapNhat < current.NgayCapNhat)
                        continue;

                    current.HoTen = item.HoTen;
                    current.SoDienThoai = string.IsNullOrWhiteSpace(item.SoDienThoai) ? current.SoDienThoai : item.SoDienThoai;
                    current.DiaChi = item.DiaChi;
                    current.DiemTichLuy = item.DiemTichLuy;
                    current.MaChiNhanh = string.IsNullOrWhiteSpace(item.MaChiNhanh) ? current.MaChiNhanh : item.MaChiNhanh;
                    current.DaXoa = laKhachLe ? false : item.DaXoa;
                    current.TrangThaiDongBo = 1;
                    current.NgayCapNhat = item.NgayCapNhat == default ? DateTime.UtcNow : item.NgayCapNhat;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var rootMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, rootMessage);
            }
        }

        [HttpPost("UpsertTonKhoChiNhanhs")]
        public async Task<IActionResult> UpsertTonKhoChiNhanhs([FromBody] UpsertTonKhoRequest? req)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            if (req?.TonKhos == null || req.TonKhos.Count == 0)
                return BadRequest("Không có dữ liệu tồn kho.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in req.TonKhos)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.IdSanPham) || string.IsNullOrWhiteSpace(item.MaChiNhanh))
                        continue;

                    NormalizeBaseDates(item);

                    var current = await _context.TonKhoChiNhanhs.FirstOrDefaultAsync(t =>
                        t.Id == item.Id || (t.IdSanPham == item.IdSanPham && t.MaChiNhanh == item.MaChiNhanh));

                    if (current == null)
                    {
                        if (string.IsNullOrWhiteSpace(item.Id))
                            item.Id = Guid.NewGuid().ToString();
                        item.TrangThaiDongBo = 1;
                        if (item.NgayCapNhat == default) item.NgayCapNhat = DateTime.UtcNow;
                        _context.TonKhoChiNhanhs.Add(item);
                        continue;
                    }

                    if (item.NgayCapNhat < current.NgayCapNhat)
                        continue;

                    current.IdSanPham = item.IdSanPham;
                    current.MaChiNhanh = item.MaChiNhanh;
                    current.SoLuong = item.SoLuong;
                    current.DaXoa = item.DaXoa;
                    current.TrangThaiDongBo = 1;
                    current.NgayCapNhat = item.NgayCapNhat == default ? DateTime.UtcNow : item.NgayCapNhat;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var rootMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, rootMessage);
            }
        }

        /// <summary>POS kéo danh sách tài khoản nhân viên từ server (kể cả đã xóa mềm để đồng bộ trạng thái).</summary>
        [HttpGet("GetNhanViens")]
        public async Task<IActionResult> GetNhanViens()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");
            if (!CoQuyenQuanLy(currentUser))
                return StatusCode(403, "Chỉ tài khoản admin hoặc quản lý mới được xem danh sách nhân viên.");

            await EnsureAdminTongAsync();
            var data = await _context.NhanViens
                .AsNoTracking()
                .OrderByDescending(n => n.NgayCapNhat)
                .ToListAsync();
            return Ok(data.Select(ToPublicNhanVien).ToList());
        }

        /// <summary>Đăng nhập online trực tiếp qua server (central-auth).</summary>
        [HttpPost("LoginNhanVien")]
        public async Task<IActionResult> LoginNhanVien([FromBody] LoginNhanVienRequest? req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.TaiKhoan) || string.IsNullOrWhiteSpace(req.MatKhau))
                return BadRequest("Thiếu tài khoản hoặc mật khẩu.");

            await EnsureAdminTongAsync();
            var user = await _context.NhanViens.FirstOrDefaultAsync(u =>
                !u.DaXoa && u.TaiKhoan == req.TaiKhoan);

            if (user == null || !PasswordHasher.Verify(req.MatKhau, user.MatKhau))
                return Unauthorized("Tài khoản hoặc mật khẩu không đúng.");

            if (!PasswordHasher.IsHashed(user.MatKhau))
                user.MatKhau = PasswordHasher.Hash(req.MatKhau);

            // Chuẩn hóa tài khoản admin tổng trên server.
            bool laAdminTong = user.Id == AdminId || string.Equals(user.TaiKhoan, AdminTaiKhoan, StringComparison.OrdinalIgnoreCase);
            if (laAdminTong)
            {
                user.Id = AdminId;
                user.TaiKhoan = AdminTaiKhoan;
                user.MaChiNhanh = AdminChiNhanh;
                user.VaiTro = AdminVaiTro;
                user.DaXoa = false;
            }

            user.LanDangNhapOnlineGanNhat = DateTime.UtcNow;
            user.NgayCapNhat = DateTime.UtcNow;
            user.TrangThaiDongBo = 1;
            await _context.SaveChangesAsync();

            return Ok(new LoginNhanVienResponse
            {
                Success = true,
                NhanVien = ToLoginNhanVien(user),
                Token = ApiTokenService.CreateToken(user.Id)
            });
        }

        /// <summary>POS đẩy danh sách tài khoản tạo/sửa lên server (upsert theo Id).</summary>
        [HttpPost("UpsertNhanViens")]
        public async Task<IActionResult> UpsertNhanViens([FromBody] UpsertNhanVienRequest? req)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");
            if (!CoQuyenQuanLy(currentUser))
                return StatusCode(403, "Chỉ tài khoản admin hoặc quản lý mới được thêm/sửa nhân viên.");

            if (req?.NhanViens == null || req.NhanViens.Count == 0)
                return BadRequest("Không có dữ liệu nhân viên.");

            await EnsureAdminTongAsync();
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in req.NhanViens)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Id))
                        continue;

                    NormalizeNhanVienDates(item);

                    bool laAdminTong = item.Id == AdminId
                        || string.Equals(item.TaiKhoan, AdminTaiKhoan, StringComparison.OrdinalIgnoreCase);

                    var current = await _context.NhanViens.FirstOrDefaultAsync(n => n.Id == item.Id);
                    if (current == null)
                    {
                        if (string.IsNullOrWhiteSpace(item.TaiKhoan)) continue;

                        if (laAdminTong)
                        {
                            item.Id = AdminId;
                            item.TaiKhoan = AdminTaiKhoan;
                            item.MaChiNhanh = AdminChiNhanh;
                            item.VaiTro = AdminVaiTro;
                            item.DaXoa = false;
                            if (string.IsNullOrWhiteSpace(item.MatKhau))
                                item.MatKhau = PasswordHasher.Hash(AdminMatKhauMacDinh);
                        }
                        item.MatKhau = PasswordHasher.HashIfNeeded(item.MatKhau);
                        item.TrangThaiDongBo = 1;
                        if (item.NgayCapNhat == default) item.NgayCapNhat = DateTime.UtcNow;
                        _context.NhanViens.Add(item);
                        continue;
                    }

                    // Chỉ nhận bản ghi mới hơn hoặc bằng để tránh ghi đè ngược bởi dữ liệu cũ.
                    if (item.NgayCapNhat < current.NgayCapNhat)
                        continue;

                    if (laAdminTong)
                    {
                        current.TaiKhoan = AdminTaiKhoan;
                        current.MaChiNhanh = AdminChiNhanh;
                        current.VaiTro = AdminVaiTro;
                        current.DaXoa = false;
                        if (string.IsNullOrWhiteSpace(current.MatKhau))
                            current.MatKhau = PasswordHasher.Hash(AdminMatKhauMacDinh);
                        else if (!PasswordHasher.IsHashed(current.MatKhau))
                            current.MatKhau = PasswordHasher.Hash(current.MatKhau);
                    }
                    else
                    {
                        current.TaiKhoan = item.TaiKhoan;
                        current.MaChiNhanh = item.MaChiNhanh;
                        current.VaiTro = item.VaiTro;
                        current.DaXoa = item.DaXoa;
                    }

                    current.HoTen = item.HoTen;
                    if (!string.IsNullOrWhiteSpace(item.MatKhau))
                        current.MatKhau = PasswordHasher.HashIfNeeded(item.MatKhau);
                    current.MaNhanVien = item.MaNhanVien;
                    current.TrangThaiDongBo = 1;
                    current.NgayCapNhat = item.NgayCapNhat == default ? DateTime.UtcNow : item.NgayCapNhat;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var rootMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, rootMessage);
            }
        }

        /// <summary>POS đánh dấu xóa mềm nhân viên trên SQL Server (giữ IdNhanVien trên hóa đơn lịch sử).</summary>
        [HttpPost("DeleteNhanViens")]
        public async Task<IActionResult> DeleteNhanViens([FromBody] XoaNhanVienRequest? req)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");
            if (!CoQuyenQuanLy(currentUser))
                return StatusCode(403, "Chỉ tài khoản admin hoặc quản lý mới được xóa nhân viên.");

            if (req?.Ids == null || req.Ids.Count == 0)
                return BadRequest("Không có Id nhân viên.");

            var ids = req.Ids
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .Where(id => id != AdminId)
                .ToList();

            if (ids.Count == 0)
                return Ok(new { Success = true, Message = "Không có Id hợp lệ để xóa." });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var canXoa = await _context.NhanViens
                    .Where(n => ids.Contains(n.Id) && !n.DaXoa)
                    .ToListAsync();

                foreach (var nv in canXoa)
                {
                    if (string.Equals(nv.TaiKhoan, AdminTaiKhoan, StringComparison.OrdinalIgnoreCase))
                        continue;

                    nv.DaXoa = true;
                    nv.TrangThaiDongBo = 1;
                    nv.NgayCapNhat = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var rootMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, rootMessage);
            }
        }
    }

    public class XoaNhanVienRequest
    {
        public List<string> Ids { get; set; } = new();
    }

    public class UpsertNhanVienRequest
    {
        public List<NhanVien> NhanViens { get; set; } = new();
    }

    public class UpsertChiNhanhRequest
    {
        public List<ChiNhanh> ChiNhanhs { get; set; } = new();
    }

    public class UpsertTonKhoRequest
    {
        public List<TonKhoChiNhanh> TonKhos { get; set; } = new();
    }

    public class UpsertKhachHangRequest
    {
        public List<KhachHang> KhachHangs { get; set; } = new();
    }

    public class LoginNhanVienRequest
    {
        public string TaiKhoan { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
    }

    public class LoginNhanVienResponse
    {
        public bool Success { get; set; }
        public NhanVien? NhanVien { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
