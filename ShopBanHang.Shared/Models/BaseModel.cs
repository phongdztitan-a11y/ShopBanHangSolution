using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBanHang.Shared
{
    public class BaseModel
    {
        // 1. UUID để tránh trùng lặp khi gộp dữ liệu từ nhiều máy về Server
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // 2. Trạng thái đồng bộ: 0 (Chờ), 1 (Xong), 2 (Lỗi)
        public int TrangThaiDongBo { get; set; } = 0;

        // 3. Thời gian cập nhật để Server biết bản ghi nào mới hơn
        public DateTime NgayCapNhat { get; set; } = DateTime.Now;

        // 4. Mã chi nhánh để phân loại dữ liệu
        public string MaChiNhanh { get; set; } = string.Empty;

        // 5. THÊM DÒNG NÀY: Để fix lỗi ở lớp HoaDon và hỗ trợ xóa mềm
        public bool DaXoa { get; set; } = false;
    }
}