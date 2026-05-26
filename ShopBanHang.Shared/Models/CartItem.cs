using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShopBanHang.Shared
{
    public class CartItem : INotifyPropertyChanged
    {
        private int _soLuong;

        public string IdSanPham { get; set; } = string.Empty;
        public string TenSanPham { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public double DonGia { get; set; }
        public int SoLuongTon { get; set; } // Thuộc tính quan trọng để chặn giới hạn

        public bool CanIncrease => SoLuong < SoLuongTon;

        public int SoLuong
        {
            get => _soLuong;
            set
            {
                if (_soLuong != value)
                {
                    _soLuong = value;
                    OnPropertyChanged();
                    // Thông báo cho tất cả các thuộc tính phụ thuộc
                    OnPropertyChanged(nameof(ThanhTien));
                    OnPropertyChanged(nameof(CanIncrease));
                    OnPropertyChanged(nameof(IsHetHang));
                }
            }
        }

        public double ThanhTien => SoLuong * DonGia;

        // Logic để XAML biết khi nào cần Disable nút +
        public bool IsHetHang => SoLuong >= SoLuongTon;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}