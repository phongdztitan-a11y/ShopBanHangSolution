using ShopBanHang_OfflineFirst.Data;
using ShopBanHang_OfflineFirst.Services;
using ShopBanHang.Shared;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHang_OfflineFirst
{
    public partial class LoginWindow : Window
    {
        private const int SoNgayChoPhepDangNhapOffline = 7;

        public LoginWindow()
        {
            InitializeComponent();
            // Gọi hàm nạp chi nhánh ngay khi khởi tạo
            LoadDanhSachChiNhanh();
        }

        private async Task DongBoTaiKhoanTuServerNeuCoMang()
        {
            var api = new ApiService();
            var dsServer = await api.GetNhanViensFromServerAsync();
            if (dsServer.Count == 0) return;

            using var db = new AppDbContext();
            foreach (var nvServer in dsServer)
            {
                var nvLocal = db.NhanViens.FirstOrDefault(x => x.Id == nvServer.Id);
                if (nvLocal == null)
                {
                    db.NhanViens.Add(new NhanVien
                    {
                        Id = nvServer.Id,
                        HoTen = nvServer.HoTen,
                        TaiKhoan = nvServer.TaiKhoan,
                        MatKhau = nvServer.MatKhau,
                        VaiTro = nvServer.VaiTro,
                        MaNhanVien = nvServer.MaNhanVien,
                        MaChiNhanh = nvServer.MaChiNhanh,
                        LanDangNhapOnlineGanNhat = nvServer.LanDangNhapOnlineGanNhat,
                        DaXoa = nvServer.DaXoa,
                        TrangThaiDongBo = 1,
                        NgayCapNhat = nvServer.NgayCapNhat
                    });
                    continue;
                }

                if (nvServer.NgayCapNhat < nvLocal.NgayCapNhat) continue;

                bool laAdminTong = App.LaTaiKhoanAdminTong(nvServer.Id, nvServer.TaiKhoan);
                nvLocal.HoTen = nvServer.HoTen;
                nvLocal.MatKhau = nvServer.MatKhau;
                nvLocal.MaNhanVien = nvServer.MaNhanVien;
                nvLocal.LanDangNhapOnlineGanNhat = nvServer.LanDangNhapOnlineGanNhat;
                nvLocal.NgayCapNhat = nvServer.NgayCapNhat;
                nvLocal.TrangThaiDongBo = 1;

                if (laAdminTong)
                {
                    nvLocal.TaiKhoan = App.TaiKhoanAdminTong;
                    nvLocal.VaiTro = App.VaiTroAdminHeThong;
                    nvLocal.MaChiNhanh = App.MaChiNhanhTong;
                    nvLocal.DaXoa = false;
                }
                else
                {
                    nvLocal.TaiKhoan = nvServer.TaiKhoan;
                    nvLocal.VaiTro = nvServer.VaiTro;
                    nvLocal.MaChiNhanh = nvServer.MaChiNhanh;
                    nvLocal.DaXoa = nvServer.DaXoa;
                }
            }
            await db.SaveChangesAsync();
        }

        private static bool LaTaiKhoanQuanLyKhongPhaiAdminTong(NhanVien user) =>
            !App.LaTaiKhoanAdminTong(user.Id, user.TaiKhoan) && App.CoQuyenQuanLyCapCao(user.VaiTro);

        private void DangNhapThanhCong(NhanVien user, string maChiNhanhSelected)
        {
            string vaiTroDangNhap = App.LaTaiKhoanAdminTong(user.Id, user.TaiKhoan)
                ? App.VaiTroAdminHeThong
                : user.VaiTro;

            App.TaiKhoanHienTai = user.TaiKhoan;
            App.VaiTro = vaiTroDangNhap;

            if (vaiTroDangNhap == "NV" && user.MaChiNhanh != maChiNhanhSelected)
            {
                MessageBox.Show($"Bạn thuộc {user.MaChiNhanh}. Hệ thống tự động chuyển vùng.");
                App.ChiNhanhHienTai = user.MaChiNhanh;
            }
            else
            {
                App.ChiNhanhHienTai = maChiNhanhSelected;
            }

            MainWindow main = new MainWindow(user.Id, user.HoTen, App.ChiNhanhHienTai, App.VaiTro);
            main.Show();
            Close();
        }

        private void LoadDanhSachChiNhanh()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // SỬA DÒNG NÀY: Thêm .Where(x => !x.DaXoa)
                    var dsChiNhanh = db.ChiNhanhs
                                       .AsNoTracking()
                                       .Where(x => !x.DaXoa) // Chỉ lấy những ông chưa bị xóa
                                       .ToList();

                    if (dsChiNhanh.Count > 0)
                    {
                        cbChiNhanh.ItemsSource = dsChiNhanh;
                        cbChiNhanh.DisplayMemberPath = "TenChiNhanh";
                        cbChiNhanh.SelectedValuePath = "MaChiNhanh";
                        cbChiNhanh.SelectedIndex = 0;
                    }
                    else
                    {
                        // Thông báo nếu không có chi nhánh nào khả dụng
                        cbChiNhanh.ItemsSource = null;
                        MessageBox.Show("Hiện không có chi nhánh nào đang hoạt động!");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi tải chi nhánh: " + ex.Message);
            }
        }

        private async void BtnDangNhap_Click(object sender, RoutedEventArgs e)
        {
            // Lấy mã chi nhánh từ ComboBox
            string? maChiNhanhSelected = cbChiNhanh.SelectedValue?.ToString();
            string taiKhoan = txtTaiKhoan.Text.Trim();
            string matKhau = txtMatKhau.Password;

            if (string.IsNullOrEmpty(maChiNhanhSelected))
            {
                MessageBox.Show("Vui lòng chọn một chi nhánh để làm việc!");
                return;
            }

            var api = new ApiService();
            var onlineLogin = await api.LoginNhanVienOnlineAsync(taiKhoan, matKhau);

            if (onlineLogin.IsSuccess && onlineLogin.User != null)
            {
                var userOnline = onlineLogin.User;
                await DongBoTaiKhoanTuServerNeuCoMang();

                using (var db = new AppDbContext())
                {
                    var local = await db.NhanViens.FirstOrDefaultAsync(x => x.Id == userOnline.Id);
                    if (local == null)
                    {
                        local = new NhanVien
                        {
                            Id = userOnline.Id,
                            HoTen = userOnline.HoTen,
                            TaiKhoan = userOnline.TaiKhoan,
                            MatKhau = userOnline.MatKhau,
                            VaiTro = userOnline.VaiTro,
                            MaNhanVien = userOnline.MaNhanVien,
                            MaChiNhanh = userOnline.MaChiNhanh,
                            DaXoa = userOnline.DaXoa
                        };
                        db.NhanViens.Add(local);
                    }
                    else
                    {
                        local.HoTen = userOnline.HoTen;
                        local.TaiKhoan = userOnline.TaiKhoan;
                        local.MatKhau = userOnline.MatKhau;
                        local.VaiTro = userOnline.VaiTro;
                        local.MaNhanVien = userOnline.MaNhanVien;
                        local.MaChiNhanh = userOnline.MaChiNhanh;
                        local.DaXoa = userOnline.DaXoa;
                    }

                    if (App.LaTaiKhoanAdminTong(local.Id, local.TaiKhoan))
                    {
                        local.TaiKhoan = App.TaiKhoanAdminTong;
                        local.VaiTro = App.VaiTroAdminHeThong;
                        local.MaChiNhanh = App.MaChiNhanhTong;
                        local.DaXoa = false;
                    }

                    local.LanDangNhapOnlineGanNhat = DateTime.Now;
                    local.TrangThaiDongBo = 1;
                    local.NgayCapNhat = DateTime.Now;
                    await db.SaveChangesAsync();

                    DangNhapThanhCong(local, maChiNhanhSelected);
                    return;
                }
            }

            if (onlineLogin.IsInvalidCredential)
            {
                MessageBox.Show("Tài khoản hoặc mật khẩu không đúng!");
                return;
            }

            using (var db = new AppDbContext())
            {
                var hopLe = await db.NhanViens
                    .Where(u => !u.DaXoa && u.TaiKhoan == taiKhoan && u.MatKhau == matKhau)
                    .ToListAsync();

                // Ưu tiên bản ADMIN_001 nếu trùng tài khoản (dữ liệu lỗi thời)
                var user = hopLe.FirstOrDefault(u => u.Id == App.IdNhanVienAdminTong) ?? hopLe.FirstOrDefault();

                if (user != null)
                {
                    if (App.LaTaiKhoanAdminTong(user.Id, user.TaiKhoan))
                    {
                        MessageBox.Show("Tài khoản admin tổng yêu cầu online để đăng nhập.", "Yêu cầu kết nối");
                        return;
                    }

                    if (LaTaiKhoanQuanLyKhongPhaiAdminTong(user))
                    {
                        var moc = user.LanDangNhapOnlineGanNhat;
                        if (!moc.HasValue || moc.Value < DateTime.Now.AddDays(-SoNgayChoPhepDangNhapOffline))
                        {
                            MessageBox.Show(
                                $"Tài khoản quản lý/NV cần đăng nhập online ít nhất 1 lần trong {SoNgayChoPhepDangNhapOffline} ngày gần đây để được dùng offline.",
                                "Hết hạn offline");
                            return;
                        }
                    }

                    DangNhapThanhCong(user, maChiNhanhSelected);
                }
                else
                {
                    MessageBox.Show("Không thể đăng nhập online và tài khoản offline không hợp lệ.\nChi tiết: " + (onlineLogin.ErrorMessage ?? "Mất kết nối server."));
                }
            }
        }
    }
}