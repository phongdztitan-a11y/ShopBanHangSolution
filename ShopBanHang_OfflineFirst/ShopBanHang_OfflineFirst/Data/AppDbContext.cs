using Microsoft.EntityFrameworkCore;
using ShopBanHang.Shared;
using System;

namespace ShopBanHang_OfflineFirst.Data
{
    public class AppDbContext : DbContext
    {
        private static bool _daKiemTraCotBoSung;

        public AppDbContext()
        {
            // Tự tạo DB nếu chưa có và nạp dữ liệu mẫu
            Database.EnsureCreated();
            DamBaoSchemaBoSung();
        }

        /// <summary>SQLite EnsureCreated không thêm cột mới khi DB đã tồn tại — bổ sung cột lịch sử tên người bán.</summary>
        public static void DamBaoCotHoTenNguoiBanSqlite()
        {
            try
            {
                using var db = new AppDbContext();
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""HoaDons"" ADD COLUMN ""HoTenNguoiBan"" TEXT NULL;");
            }
            catch
            {
                // Cột đã có hoặc bảng chưa tồn tại
            }
        }

        public static void DamBaoCotLanDangNhapOnlineGanNhatSqlite()
        {
            try
            {
                using var db = new AppDbContext();
                db.Database.ExecuteSqlRaw(@"ALTER TABLE ""NhanViens"" ADD COLUMN ""LanDangNhapOnlineGanNhat"" TEXT NULL;");
            }
            catch
            {
                // Cột đã có hoặc bảng chưa tồn tại
            }
        }

        private void DamBaoSchemaBoSung()
        {
            if (_daKiemTraCotBoSung) return;

            try
            {
                Database.ExecuteSqlRaw(@"ALTER TABLE ""HoaDons"" ADD COLUMN ""HoTenNguoiBan"" TEXT NULL;");
            }
            catch { }

            try
            {
                Database.ExecuteSqlRaw(@"ALTER TABLE ""NhanViens"" ADD COLUMN ""LanDangNhapOnlineGanNhat"" TEXT NULL;");
            }
            catch { }

            try
            {
                Database.ExecuteSqlRaw(@"ALTER TABLE ""ChiTietHoaDons"" ADD COLUMN ""TenSanPhamLuu"" TEXT NULL;");
            }
            catch { }

            try
            {
                Database.ExecuteSqlRaw(@"ALTER TABLE ""ChiTietHoaDons"" ADD COLUMN ""SKULuu"" TEXT NULL;");
            }
            catch { }

            _daKiemTraCotBoSung = true;
        }

        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<TonKhoChiNhanh> TonKhoChiNhanhs { get; set; }
        public DbSet<ChiNhanh> ChiNhanhs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=shop.db");
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Seed Chi Nhánh (Gộp lại thành 1 lần gọi duy nhất)
            modelBuilder.Entity<ChiNhanh>().HasData(
                new ChiNhanh
                {
                    Id = "CN_GOC",
                    MaChiNhanh = "CN_GOC",
                    TenChiNhanh = "Chi Nhánh Tổng",
                    DaXoa = false,
                    TrangThaiDongBo = 1,
                    NgayCapNhat = DateTime.UtcNow
                }
            );

            // 2. Seed Nhân Viên Admin (Gộp lại và điền đủ thông tin)
            modelBuilder.Entity<NhanVien>().HasData(
                new NhanVien
                {
                    Id = "ADMIN_001",
                    TaiKhoan = "admin",
                    MatKhau = "123",
                    HoTen = "Quản Trị Viên",
                    VaiTro = "QuanLy",
                    MaChiNhanh = "CN_GOC",
                    TrangThaiDongBo = 1,
                    DaXoa = false,
                    NgayCapNhat = DateTime.UtcNow
                }
            );

            // 3. Cấu hình mối quan hệ giữa Hóa đơn và Chi tiết
            modelBuilder.Entity<ChiTietHoaDon>()
                .HasOne(ct => ct.HoaDon)
                .WithMany(h => h.ChiTiets)
                .HasForeignKey(ct => ct.IdHoaDon)
                .OnDelete(DeleteBehavior.Cascade); // Xóa hóa đơn thì xóa luôn chi tiết
        }
    }
}