using System;
using System.Collections.Generic;
using System.Text;

namespace ShopBanHang.Shared
{
    // Thừa kế BaseModel để lấy Id, MaChiNhanh, TrangThaiDongBo, NgayCapNhat
    public class TonKhoChiNhanh : BaseModel
    {
        // Liên kết tới sản phẩm nào
        public string IdSanPham { get; set; } = string.Empty;

        // Số lượng thực tế tại chi nhánh đó
        public int SoLuong { get; set; }
    }
}