using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design; // Thêm dòng này
using ShopBanHang.Shared;

namespace WebApplication3.Data
{
    public class ServerDbContext : DbContext
    {
        public ServerDbContext(DbContextOptions<ServerDbContext> options) : base(options) { }

        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<KhachHang> KhachHangs { get; set; }
        public DbSet<ChiNhanh> ChiNhanhs { get; set; }
        public DbSet<TonKhoChiNhanh> TonKhoChiNhanhs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HoaDon>().HasIndex(h => h.MaHoaDon).IsUnique();
            base.OnModelCreating(modelBuilder);
        }
    }

    // THÊM ĐOẠN NÀY ĐỂ FIX LỖI KẾT NỐI KHI CHẠY MIGRATION
    public class ServerDbContextFactory : IDesignTimeDbContextFactory<ServerDbContext>
    {
        public ServerDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ServerDbContext>();
            // Chuỗi kết nối có TrustServerCertificate=True để fix lỗi SSL ban nãy
            optionsBuilder.UseSqlServer("Server=TRAN-PHONG\\SQLEXPRESS;Database=ShopBanHang_DoAn;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

            return new ServerDbContext(optionsBuilder.Options);
        }
    }
}