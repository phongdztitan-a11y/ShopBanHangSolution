using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopBanHang.Shared
{
    public class SanPham : BaseModel
    {
        [Required]
        [MaxLength(200)]
        public string TenSanPham { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MaGoc { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? KichCo { get; set; } // Thêm dấu ? nếu cho phép trống

        [MaxLength(30)]
        public string? MauSac { get; set; } // Thêm dấu ? nếu cho phép trống

        public double GiaBan { get; set; }

        [NotMapped]
        public string SKU => TinhSKU(MaGoc, KichCo, MauSac);

        public static string TinhSKU(string maGoc, string? kichCo, string? mauSac) =>
            $"{maGoc}-{kichCo}-{mauSac}".ToUpper();

        [NotMapped]
        public string DisplayName => $"{TenSanPham} ({KichCo} - {MauSac})";
    }
}