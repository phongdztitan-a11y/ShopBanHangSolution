using ShopBanHang_OfflineFirst.Data;
using ShopBanHang.Shared.Security;
using System;
using System.Linq;
using System.Windows;

namespace ShopBanHang_OfflineFirst
{
    public partial class App : Application
    {
        public const string IdNhanVienAdminTong = "ADMIN_001";
        public const string MaChiNhanhTong = "CN_GOC";
        public const string TaiKhoanAdminTong = "admin";
        public const string VaiTroAdminHeThong = "QuanLy";

        public static string ChiNhanhHienTai { get; set; } = string.Empty;
        public static string TaiKhoanHienTai { get; set; } = string.Empty;
        public static string VaiTro { get; set; } = string.Empty;

        /// <summary>Tài khoản admin tổng / Id cố định — không được xóa hay hạ quyền.</summary>
        public static bool LaTaiKhoanAdminTong(string? idNhanVien, string? taiKhoan) =>
            idNhanVien == IdNhanVienAdminTong
            || (taiKhoan != null && taiKhoan.Equals(TaiKhoanAdminTong, StringComparison.OrdinalIgnoreCase));

        /// <summary>Quyền mở báo cáo toàn hệ / quản lý (không phải NV bán hàng thuần).</summary>
        public static bool CoQuyenQuanLyCapCao(string? vaiTro) =>
            vaiTro == VaiTroAdminHeThong || vaiTro == "QL"
            || string.Equals(vaiTro, "Admin", StringComparison.OrdinalIgnoreCase);

        // Hàm sinh mã tự động (Hóa đơn, Mã sản phẩm...)
        public static string SinhMaHienThi(string tienTo)
        {
            // Nếu ChiNhanhHienTai là "CN-HN" -> lấy "HN"
            // Nếu là "CN_GOC" -> lấy "GOC"
            string ma = "CH";
            if (!string.IsNullOrEmpty(ChiNhanhHienTai))
            {
                var parts = ChiNhanhHienTai.Split('_', '-');
                ma = parts.Last().ToUpper();
            }

            string thoiGian = DateTime.UtcNow.ToString("yyMMddHHmmss");
            return $"{tienTo}-{ma}-{thoiGian}";
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Lưu ý quan trọng: 
            // Nếu bạn dùng Database.EnsureCreated() trong AppDbContext, 
            // thì KHÔNG ĐƯỢC dùng db.Database.Migrate() ở đây. 
            // Hai ông này khắc khẩu nhau, dùng chung sẽ báo lỗi Migration pending.

            using (var db = new AppDbContext())
            {
                // Chỉ cần dòng này để đảm bảo DB được tạo và Seed Data từ AppDbContext được nạp
                db.Database.EnsureCreated();
                DamBaoAdminTongVaChiNhanhGoc(db);
            }

            AppDbContext.DamBaoCotHoTenNguoiBanSqlite();
            AppDbContext.DamBaoCotLanDangNhapOnlineGanNhatSqlite();

            LoginWindow login = new LoginWindow();
            login.Show();
        }

        /// <summary>Luôn khôi phục chi nhánh gốc và admin tổng (không đụng mật khẩu đã đổi).</summary>
        private static void DamBaoAdminTongVaChiNhanhGoc(AppDbContext db)
        {
            foreach (var cn in db.ChiNhanhs.Where(c => c.MaChiNhanh == MaChiNhanhTong || c.Id == MaChiNhanhTong))
                cn.DaXoa = false;

            if (!db.ChiNhanhs.Any(c => c.Id == MaChiNhanhTong))
            {
                db.ChiNhanhs.Add(new ShopBanHang.Shared.ChiNhanh
                {
                    Id = MaChiNhanhTong,
                    MaChiNhanh = MaChiNhanhTong,
                    TenChiNhanh = "Chi Nhánh Tổng",
                    DaXoa = false,
                    TrangThaiDongBo = 1,
                    NgayCapNhat = DateTime.UtcNow
                });
            }

            var admin = db.NhanViens.FirstOrDefault(n => n.Id == IdNhanVienAdminTong);
            if (admin == null)
                return;

            admin.DaXoa = false;
            admin.TaiKhoan = TaiKhoanAdminTong;
            admin.MaChiNhanh = MaChiNhanhTong;
            admin.VaiTro = VaiTroAdminHeThong;
            if (string.IsNullOrWhiteSpace(admin.HoTen))
                admin.HoTen = "Quản Trị Viên";
            if (string.IsNullOrWhiteSpace(admin.MatKhau))
                admin.MatKhau = PasswordHasher.Hash("123");
            else if (!PasswordHasher.IsHashed(admin.MatKhau))
                admin.MatKhau = PasswordHasher.Hash(admin.MatKhau);

            db.SaveChanges();
        }
    }
}
