using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // Thêm thư viện này

namespace ShopBanHang.Shared
{
    public class ChiTietHoaDon : BaseModel
    {
        public string IdHoaDon { get; set; } = string.Empty;
        public string IdSanPham { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public double DonGia { get; set; }

        /// <summary>Ảnh chụp tên SP lúc bán — không đổi khi sửa sản phẩm sau này.</summary>
        [MaxLength(200)]
        public string? TenSanPhamLuu { get; set; }

        /// <summary>Ảnh chụp SKU lúc bán (MaGoc-KichCo-MauSac).</summary>
        [MaxLength(100)]
        public string? SKULuu { get; set; }

        [NotMapped]
        public string TenHienThi =>
            !string.IsNullOrWhiteSpace(TenSanPhamLuu) ? TenSanPhamLuu : (SanPham?.TenSanPham ?? "—");

        [NotMapped]
        public string SKUHienThi =>
            !string.IsNullOrWhiteSpace(SKULuu) ? SKULuu : (SanPham?.SKU ?? "—");

        [ForeignKey("IdHoaDon")]
        [JsonIgnore] // RẤT QUAN TRỌNG: Để khi gửi Chi Tiết, nó không kéo ngược cái HoaDon vào trong JSON
        public virtual HoaDon? HoaDon { get; set; }

        [ForeignKey("IdSanPham")]
        [JsonIgnore] // Tránh lỗi 400 khi không gửi kèm thông tin Sản Phẩm đầy đủ
        public virtual SanPham? SanPham { get; set; }
    }
}