namespace ShopBanHang.Shared
{
    public class KhachHang : BaseModel
    {
        public string HoTen { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string DiaChi { get; set; } = string.Empty;

        // Tích điểm để khách hàng quay lại (đúng chuẩn shop quần áo)
        public int DiemTichLuy { get; set; } = 0;

        // Để thuận tiện cho việc hiển thị trên giao diện tìm kiếm
        public string ThongTinHienThi => $"{HoTen} - {SoDienThoai}";
    }
}