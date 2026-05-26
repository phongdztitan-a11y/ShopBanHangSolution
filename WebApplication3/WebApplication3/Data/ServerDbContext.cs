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
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Thiếu DATABASE_URL hoặc ConnectionStrings__DefaultConnection để chạy EF migration.");
            }

            optionsBuilder.UseNpgsql(ToNpgsqlConnectionString(connectionString));

            return new ServerDbContext(optionsBuilder.Options);
        }

        private static string ToNpgsqlConnectionString(string raw)
        {
            if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
                !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return raw;
            }

            var databaseUri = new Uri(raw);
            var userParts = databaseUri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userParts[0]);
            var password = userParts.Length > 1 ? Uri.UnescapeDataString(userParts[1]) : string.Empty;
            var port = databaseUri.Port > 0 ? databaseUri.Port : 5432;
            var database = databaseUri.AbsolutePath.TrimStart('/');

            return
                $"Host={databaseUri.Host};" +
                $"Port={port};" +
                $"Database={database};" +
                $"Username={username};" +
                $"Password={password};" +
                "SSL Mode=Require;Trust Server Certificate=true";
        }
    }
}
