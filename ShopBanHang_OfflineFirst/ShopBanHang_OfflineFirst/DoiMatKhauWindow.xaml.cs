using ShopBanHang_OfflineFirst.Data;
using System;
using System.Linq;
using System.Windows;

namespace ShopBanHang_OfflineFirst
{
    public partial class DoiMatKhauWindow : Window
    {
        private string _taiKhoanDangNhap;

        public DoiMatKhauWindow(string taiKhoan)
        {
            InitializeComponent();
            _taiKhoanDangNhap = taiKhoan;
        }

        private void btnLuuMatKhau_Click(object sender, RoutedEventArgs e)
        {
            string passCu = txtMatKhauCu.Password;
            string passMoi = txtMatKhauMoi.Password;
            string xacNhan = txtXacNhan.Password;

            // 1. Kiểm tra rỗng
            if (string.IsNullOrEmpty(passCu) || string.IsNullOrEmpty(passMoi))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ mật khẩu!");
                return;
            }

            using (var db = new AppDbContext())
            {
                var dsTrungTk = db.NhanViens.Where(x => x.TaiKhoan == _taiKhoanDangNhap).ToList();
                var nv = dsTrungTk.FirstOrDefault(x => x.Id == App.IdNhanVienAdminTong) ?? dsTrungTk.FirstOrDefault();

                // KIỂM TRA LỖI Ở ĐÂY:
                if (nv == null)
                {
                    MessageBox.Show("Không tìm thấy tài khoản này trong hệ thống!");
                    return; // <--- CỰC KỲ QUAN TRỌNG: Phải có return để dừng hàm tại đây
                }

                // 3. Kiểm tra mật khẩu cũ
                if (nv.MatKhau != passCu)
                {
                    MessageBox.Show("Mật khẩu cũ không chính xác!");
                    return; // Dừng nếu sai pass cũ
                }

                // 4. Nếu mọi thứ OK mới chạy xuống đây
                nv.MatKhau = passMoi;
                nv.TrangThaiDongBo = 0;
                db.SaveChanges();

                MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo");
                this.Close();
            }
        }
    }
}