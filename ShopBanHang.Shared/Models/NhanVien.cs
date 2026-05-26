using System;

// Trong file Models/NhanVien.cs
namespace ShopBanHang.Shared
{
    public class NhanVien : BaseModel
    {
        public string HoTen { get; set; } = string.Empty;
        public string MaNhanVien { get; set; } = string.Empty; // Thêm dòng này
        public string TaiKhoan { get; set; } = string.Empty;
        public string MatKhau { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public DateTime? LanDangNhapOnlineGanNhat { get; set; }
    }
}