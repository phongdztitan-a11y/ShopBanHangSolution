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

            optionsBuilder.UseNpgsql(
    "Host=dpg-d8amd8jbc2fs7385p1bg-a.singapore-postgres.render.com;" +
    "Port=5432;" +
    "Database=shopbanhang_db;" +
    "Username=shopbanhang_db_user;" +
    "Password=qnAgnOInBwGwsrbUQ1D0ZWbmwOoElRIT;" +
    "SSL Mode=Require;" +
    "Trust Server Certificate=true");

            return new ServerDbContext(optionsBuilder.Options);
        }
    }
}