using System.ComponentModel.DataAnnotations;

namespace ShopBanHang.Shared
{
    // Kế thừa BaseModel để có sẵn Id, TrangThaiDongBo, NgayCapNhat
    public class ChiNhanh : BaseModel
    {
        // Id từ BaseModel sẽ đóng vai trò khóa chính duy nhất toàn hệ thống
        // Đây là mã hiển thị (VD: CN_HANOI)
        public string TenChiNhanh { get; set; } = string.Empty;

        // XÓA BỎ: TrangThaiDongBo và DaXoa ở đây!
    }
}