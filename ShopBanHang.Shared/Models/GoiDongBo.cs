using System.Collections.Generic;

namespace ShopBanHang.Shared.Models
{
    public class GoiDongBoHoaDonServer
    {
        public ShopBanHang.Shared.HoaDon? HoaDon { get; set; }
        public List<ShopBanHang.Shared.ChiTietHoaDon> ChiTiets { get; set; } = new();
    }

    public class DongBoWrapper
    {
        public List<GoiDongBoHoaDonServer> dsGoi { get; set; } = new();
    }
}
