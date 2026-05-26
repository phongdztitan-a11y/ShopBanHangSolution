using ShopBanHang_OfflineFirst.Data;
using ShopBanHang.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShopBanHang_OfflineFirst
{
    /// <summary>
    /// Interaction logic for ThemKhachHangWindow.xaml
    /// </summary>
    public partial class ThemKhachHangWindow : Window
    {
        public KhachHang? KhachHangMoi { get; set; }

        public ThemKhachHangWindow(string sdtMacDinh)
        {
            InitializeComponent();
            txtSdt.Text = sdtMacDinh;
            txtHoTen.Focus();

            // Nhấn Enter ở ô tên thì tự bấm nút Lưu
            txtHoTen.KeyDown += (s, e) => {
                if (e.Key == System.Windows.Input.Key.Enter) btnLuu_Click(this, new RoutedEventArgs());
            };
        }

        private void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            string hoTen = txtHoTen.Text.Trim();

            if (string.IsNullOrWhiteSpace(hoTen))
            {
                MessageBox.Show("Vui lòng nhập Họ tên khách hàng!", "Thông báo");
                txtHoTen.Focus();
                return;
            }

            try
            {
                using (var db = new AppDbContext())
                {
                    KhachHangMoi = new KhachHang
                    {
                        Id = Guid.NewGuid().ToString(), // Đảm bảo luôn có Id mới
                        SoDienThoai = txtSdt.Text.Trim(),
                        HoTen = hoTen,

                        // 1. Fix lỗi NOT NULL constraint failed: KhachHangs.DiaChi
                        DiaChi = "",

                        // 2. Các trường của KhachHang
                        DiemTichLuy = 0,

                        // 3. Các trường từ BaseModel
                        TrangThaiDongBo = 0,
                        NgayCapNhat = DateTime.UtcNow,  // <--- Thêm dòng này
                        MaChiNhanh = App.ChiNhanhHienTai,
                        DaXoa = false                // <--- Thêm dòng này cho chắc chắn
                    };

                    db.KhachHangs.Add(KhachHangMoi);
                    db.SaveChanges();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                // Nếu vẫn lỗi, nó sẽ hiện chi tiết lỗi SQLite tại đây
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                MessageBox.Show("Không thể lưu khách hàng: " + errorMsg, "Lỗi");
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            // Đóng cửa sổ và hủy bỏ thao tác
            this.DialogResult = false;
            this.Close();
        }
    }
}