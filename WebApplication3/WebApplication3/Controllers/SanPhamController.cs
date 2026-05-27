using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Data;
using ShopBanHang.Shared;
using WebApplication3.Security;

namespace WebApplication3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SanPhamController : ControllerBase
    {
        private readonly ServerDbContext _context;

        public SanPhamController(ServerDbContext context)
        {
            _context = context;
        }

        // GET: api/SanPham
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SanPham>>> GetSanPhams()
        {
            return await _context.SanPhams.ToListAsync();
        }

        // POST: api/SanPham
        [HttpPost]
        public async Task<ActionResult<SanPham>> PostSanPham(SanPham sanPham)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Thiếu hoặc sai token đăng nhập.");

            if (!CoQuyenQuanLySanPham(currentUser))
                return StatusCode(403, "Chỉ tài khoản admin hoặc quản lý mới được thêm/sửa sản phẩm.");

            var sanPhamDaCo = await _context.SanPhams.FirstOrDefaultAsync(sp => sp.Id == sanPham.Id);
            if (sanPhamDaCo != null)
            {
                sanPhamDaCo.TenSanPham = sanPham.TenSanPham;
                sanPhamDaCo.MaGoc = sanPham.MaGoc;
                sanPhamDaCo.KichCo = sanPham.KichCo;
                sanPhamDaCo.MauSac = sanPham.MauSac;
                sanPhamDaCo.GiaBan = sanPham.GiaBan;
                sanPhamDaCo.NgayCapNhat = DateTime.UtcNow;
                sanPhamDaCo.DaXoa = sanPham.DaXoa;
                sanPhamDaCo.MaChiNhanh = sanPham.MaChiNhanh;
                await _context.SaveChangesAsync();
                return Ok(sanPhamDaCo);
            }

            _context.SanPhams.Add(sanPham);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSanPhams), new { id = sanPham.Id }, sanPham);
        }

        private async Task<NhanVien?> GetCurrentUserAsync()
        {
            if (!ApiTokenService.TryGetUserId(Request, out var userId))
                return null;

            return await _context.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(n => !n.DaXoa && n.Id == userId);
        }

        private static bool CoQuyenQuanLySanPham(NhanVien user) =>
            user.Id == "ADMIN_001"
            || string.Equals(user.TaiKhoan, "admin", StringComparison.OrdinalIgnoreCase)
            || user.VaiTro == "QuanLy"
            || user.VaiTro == "QL"
            || string.Equals(user.VaiTro, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
