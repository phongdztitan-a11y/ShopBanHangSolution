using Microsoft.EntityFrameworkCore;
using ShopBanHang.Shared;
using ShopBanHang_OfflineFirst;
using ShopBanHang_OfflineFirst.Data;
using System.Diagnostics;

namespace SeedPi42TestData;

internal static class Program
{
    private static int Main(string[] args)
    {
        var options = ParseArgs(args);
        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(options.DbDirectory))
        {
            Directory.SetCurrentDirectory(Path.GetFullPath(options.DbDirectory));
        }

        string dbPath = Path.GetFullPath("shop.db");
        Console.WriteLine($"=== Seed PI 4.2 (test data) ===");
        Console.WriteLine($"  Working directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"  shop.db: {dbPath}");
        Console.WriteLine($"  MaChiNhanh: {options.MaChiNhanh}");
        Console.WriteLine($"  Target: {options.SoSanPham} SP, {options.SoHoaDon} HĐ (TrangThaiDongBo=0)");
        Console.WriteLine();

        if (options.WhatIf)
        {
            Console.WriteLine("[WhatIf] Không ghi dữ liệu.");
            return 0;
        }

        if (IsShopBanHangRunning())
        {
            Console.Error.WriteLine("Lỗi: ShopBanHang_OfflineFirst đang chạy — đóng app trước (tránh khóa shop.db).");
            return 2;
        }

        try
        {
            using var db = new AppDbContext();
            DamBaoCoSo(db, options.MaChiNhanh);

            int spHienCo = db.SanPhams.Count(s => s.MaChiNhanh == options.MaChiNhanh && !s.DaXoa);
            int hdHienCo = db.HoaDons.Count(h => h.MaChiNhanh == options.MaChiNhanh);

            if (options.SkipIfEnough && spHienCo >= options.SoSanPham && hdHienCo >= options.SoHoaDon)
            {
                Console.WriteLine($"Đã đủ dữ liệu (SP={spHienCo}, HĐ={hdHienCo}). Bỏ qua.");
                return 0;
            }

            string nvId = db.NhanViens
                .Where(n => n.MaChiNhanh == options.MaChiNhanh && !n.DaXoa)
                .Select(n => n.Id)
                .FirstOrDefault() ?? App.IdNhanVienAdminTong;

            string nvTen = db.NhanViens.Where(n => n.Id == nvId).Select(n => n.HoTen).FirstOrDefault() ?? "Seed NV";

            int spCanThem = Math.Max(0, options.SoSanPham - spHienCo);
            int hdCanThem = Math.Max(0, options.SoHoaDon - hdHienCo);

            var sanPhamIds = db.SanPhams.AsNoTracking()
                .Where(s => s.MaChiNhanh == options.MaChiNhanh && !s.DaXoa)
                .Select(s => s.Id)
                .ToList();

            if (spCanThem > 0)
            {
                Console.WriteLine($"Đang thêm {spCanThem} sản phẩm...");
                sanPhamIds.AddRange(ThemSanPham(db, options.MaChiNhanh, spCanThem, options.BatchSize));
            }

            if (sanPhamIds.Count == 0)
            {
                Console.Error.WriteLine("Không có sản phẩm để gắn chi tiết hóa đơn.");
                return 3;
            }

            if (hdCanThem > 0)
            {
                Console.WriteLine($"Đang thêm {hdCanThem} hóa đơn (chờ đồng bộ)...");
                ThemHoaDon(db, options.MaChiNhanh, hdCanThem, sanPhamIds, nvId, nvTen, options.BatchSize);
            }

            spHienCo = db.SanPhams.Count(s => s.MaChiNhanh == options.MaChiNhanh && !s.DaXoa);
            hdHienCo = db.HoaDons.Count(h => h.MaChiNhanh == options.MaChiNhanh);
            int choSync = db.HoaDons.Count(h => h.MaChiNhanh == options.MaChiNhanh && h.TrangThaiDongBo == 0);

            Console.WriteLine();
            Console.WriteLine("=== Hoàn tất ===");
            Console.WriteLine($"  SanPham (CN): {spHienCo}");
            Console.WriteLine($"  HoaDon (CN):  {hdHienCo}");
            Console.WriteLine($"  HĐ chờ sync:  {choSync}");
            Console.WriteLine("  Lưu ý: Chỉ chạy trên máy dev/test. Đừng đóng gói shop.db đã seed vào setup.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Lỗi: {ex.Message}");
            if (ex.InnerException != null)
                Console.Error.WriteLine($"  → {ex.InnerException.Message}");
            return 1;
        }
    }

    private static void DamBaoCoSo(AppDbContext db, string maChiNhanh)
    {
        if (!db.ChiNhanhs.Any(c => c.MaChiNhanh == maChiNhanh))
        {
            db.ChiNhanhs.Add(new ChiNhanh
            {
                Id = maChiNhanh,
                MaChiNhanh = maChiNhanh,
                TenChiNhanh = $"Chi nhánh {maChiNhanh}",
                TrangThaiDongBo = 1,
                NgayCapNhat = DateTime.Now,
                DaXoa = false
            });
        }

        if (!db.KhachHangs.Any(k => k.Id == "KHACH_LE"))
        {
            db.KhachHangs.Add(new KhachHang
            {
                Id = "KHACH_LE",
                HoTen = "Khách lẻ",
                SoDienThoai = "0000000000",
                DiaChi = "",
                MaChiNhanh = maChiNhanh,
                TrangThaiDongBo = 1,
                NgayCapNhat = DateTime.Now,
                DaXoa = false
            });
        }

        db.SaveChanges();
    }

    private static List<string> ThemSanPham(AppDbContext db, string maChiNhanh, int soLuong, int batchSize)
    {
        var sizes = new[] { "S", "M", "L", "XL" };
        var colors = new[] { "Trắng", "Đen", "Xanh" };
        var ids = new List<string>(soLuong);
        var rng = new Random(42);
        int baseIndex = db.SanPhams.Count() + 1;

        for (int i = 0; i < soLuong; i++)
        {
            int idx = baseIndex + i;
            string maGoc = $"SP-SEED-{idx:D5}";
            string size = sizes[i % sizes.Length];
            string color = colors[i % colors.Length];
            string spId = Guid.NewGuid().ToString();

            var sp = new SanPham
            {
                Id = spId,
                TenSanPham = $"Sản phẩm thử {idx:D5}",
                MaGoc = maGoc,
                KichCo = size,
                MauSac = color,
                GiaBan = 100_000 + (idx % 20) * 10_000,
                MaChiNhanh = maChiNhanh,
                TrangThaiDongBo = 0,
                NgayCapNhat = DateTime.Now,
                DaXoa = false
            };

            db.SanPhams.Add(sp);
            db.TonKhoChiNhanhs.Add(new TonKhoChiNhanh
            {
                Id = Guid.NewGuid().ToString(),
                IdSanPham = spId,
                MaChiNhanh = maChiNhanh,
                SoLuong = 5000,
                TrangThaiDongBo = 0,
                NgayCapNhat = DateTime.Now,
                DaXoa = false
            });

            ids.Add(spId);

            if ((i + 1) % batchSize == 0)
            {
                db.SaveChanges();
                Console.WriteLine($"  ... {i + 1}/{soLuong} SP");
            }
        }

        db.SaveChanges();
        return ids;
    }

    private static void ThemHoaDon(
        AppDbContext db,
        string maChiNhanh,
        int soLuong,
        List<string> sanPhamIds,
        string idNhanVien,
        string hoTenNguoiBan,
        int batchSize)
    {
        var rng = new Random(99);
        var now = DateTime.Now;

        for (int i = 0; i < soLuong; i++)
        {
            string hdId = Guid.NewGuid().ToString();
            string spId = sanPhamIds[rng.Next(sanPhamIds.Count)];
            var sp = db.SanPhams.AsNoTracking().First(s => s.Id == spId);
            int sl = 1 + rng.Next(3);
            double donGia = sp.GiaBan;

            var hd = new HoaDon
            {
                Id = hdId,
                MaHoaDon = $"HD-SEED-{i + 1:D5}",
                NgayLap = now.AddMinutes(-i),
                TongTien = donGia * sl,
                IdNhanVien = idNhanVien,
                HoTenNguoiBan = hoTenNguoiBan,
                IdKhachHang = "KHACH_LE",
                SdtKhachHang = "0000000000",
                MaChiNhanh = maChiNhanh,
                TrangThaiDongBo = 0,
                NgayCapNhat = now.AddMinutes(-i),
                DaXoa = false
            };

            db.HoaDons.Add(hd);
            db.ChiTietHoaDons.Add(new ChiTietHoaDon
            {
                Id = Guid.NewGuid().ToString(),
                IdHoaDon = hdId,
                IdSanPham = spId,
                SoLuong = sl,
                DonGia = donGia,
                TenSanPhamLuu = sp.TenSanPham,
                SKULuu = SanPham.TinhSKU(sp.MaGoc, sp.KichCo, sp.MauSac),
                MaChiNhanh = maChiNhanh,
                TrangThaiDongBo = 0,
                NgayCapNhat = hd.NgayCapNhat,
                DaXoa = false
            });

            if ((i + 1) % batchSize == 0)
            {
                db.SaveChanges();
                Console.WriteLine($"  ... {i + 1}/{soLuong} HĐ");
            }
        }

        db.SaveChanges();
    }

    private static bool IsShopBanHangRunning()
    {
        string name = "ShopBanHang_OfflineFirst";
        return Process.GetProcessesByName(name).Length > 0;
    }

    private static SeedOptions ParseArgs(string[] args)
    {
        var o = new SeedOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            switch (a)
            {
                case "-h":
                case "--help":
                    o.ShowHelp = true;
                    break;
                case "--whatif":
                    o.WhatIf = true;
                    break;
                case "--skip-if-enough":
                    o.SkipIfEnough = true;
                    break;
                case "-m":
                case "--ma-chi-nhanh":
                    o.MaChiNhanh = NextArg(args, ref i, o.MaChiNhanh);
                    break;
                case "--so-san-pham":
                    o.SoSanPham = int.Parse(NextArg(args, ref i, "500"));
                    break;
                case "--so-hoa-don":
                    o.SoHoaDon = int.Parse(NextArg(args, ref i, "500"));
                    break;
                case "--db-dir":
                    o.DbDirectory = NextArg(args, ref i, "");
                    break;
                case "--batch-size":
                    o.BatchSize = Math.Max(10, int.Parse(NextArg(args, ref i, "100")));
                    break;
            }
        }

        return o;
    }

    private static string NextArg(string[] args, ref int i, string defaultValue)
    {
        if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
            return args[++i];
        return defaultValue;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            SeedPi42TestData — tạo dữ liệu thử PI 4.2 (không đụng setup.exe)

            Cách chạy (từ repo):
              dotnet run --project tools/SeedPi42TestData -- --db-dir "<thư mục chứa shop.db>"

            Tham số:
              --ma-chi-nhanh CN_GOC   Chi nhánh (mặc định CN_GOC)
              --so-san-pham 500
              --so-hoa-don 500
              --db-dir <path>       Thư mục làm việc (file shop.db tạo/ghi tại đây)
              --batch-size 100      Lưu theo lô
              --skip-if-enough      Bỏ qua nếu đã đủ số lượng
              --whatif              Chỉ in thông tin
              -h, --help
            """);
    }

    private sealed class SeedOptions
    {
        public bool ShowHelp { get; set; }
        public bool WhatIf { get; set; }
        public bool SkipIfEnough { get; set; }
        public string MaChiNhanh { get; set; } = "CN_GOC";
        public int SoSanPham { get; set; } = 500;
        public int SoHoaDon { get; set; } = 500;
        public string? DbDirectory { get; set; }
        public int BatchSize { get; set; } = 100;
    }
}
