using ShopBanHang_OfflineFirst.Data;
using ShopBanHang.Shared.Security;
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

            if (string.IsNullOrEmpty(passCu) || string.IsNullOrEmpty(passMoi))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ mật khẩu!");
                return;
            }

            if (passMoi != xacNhan)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!");
                return;
            }

            using (var db = new AppDbContext())
            {
                var dsTrungTk = db.NhanViens.Where(x => x.TaiKhoan == _taiKhoanDangNhap).ToList();
                var nv = dsTrungTk.FirstOrDefault(x => x.Id == App.IdNhanVienAdminTong) ?? dsTrungTk.FirstOrDefault();

                if (nv == null)
                {
                    MessageBox.Show("Không tìm thấy tài khoản này trong hệ thống!");
                    return;
                }

                if (!PasswordHasher.Verify(passCu, nv.MatKhau))
                {
                    MessageBox.Show("Mật khẩu cũ không chính xác!");
                    return;
                }

                nv.MatKhau = PasswordHasher.Hash(passMoi);
                nv.TrangThaiDongBo = 0;
                nv.NgayCapNhat = DateTime.UtcNow;
                db.SaveChanges();

                MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo");
                Close();
            }
        }
    }
}
