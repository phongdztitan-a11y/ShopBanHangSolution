using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization; // Thêm thư viện này

namespace ShopBanHang.Shared
{
    public class HoaDon : BaseModel
    {
        public string MaHoaDon { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; } = DateTime.Now;
        public double TongTien { get; set; }
        public string? SdtKhachHang { get; set; }

        /// <summary>Họ tên người bán tại thời điểm lập HD (giữ hiển thị lịch sử sau khi xóa tài khoản NV).</summary>
        public string? HoTenNguoiBan { get; set; }

        // Foreign Keys
        public string? IdKhachHang { get; set; }
        public string IdNhanVien { get; set; } = string.Empty;

        // --- CÁC DÒNG CẦN THÊM ĐỂ HẾT LỖI ---

        [ForeignKey("IdKhachHang")]
        [JsonIgnore]
        public virtual KhachHang? KhachHang { get; set; }

        [ForeignKey("IdNhanVien")]
        [JsonIgnore]
        public virtual NhanVien? NhanVien { get; set; }

        // Thuộc tính ChiNhanh (Nếu BaseModel chưa có object này)
        [ForeignKey("MaChiNhanh")] // Ép EF sử dụng cột MaChiNhanh thay vì tự tìm ChiNhanhId
        [JsonIgnore]
        public virtual ChiNhanh? ChiNhanh { get; set; }

        // Đảm bảo tên là ChiTiets (có chữ s ở cuối) khớp với Controller
        [JsonIgnore]
        public virtual ICollection<ChiTietHoaDon>? ChiTiets { get; set; }
    }
}