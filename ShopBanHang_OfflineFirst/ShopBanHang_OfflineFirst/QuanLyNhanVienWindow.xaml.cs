using ShopBanHang_OfflineFirst.Data;
using ShopBanHang_OfflineFirst.Services;
using ShopBanHang.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace ShopBanHang_OfflineFirst
{
    public partial class QuanLyNhanVienWindow : Window
    {
        private string _chiNhanhHienTaiCuaQuanLy;
        private string? _idNhanVienDangChon;

        public QuanLyNhanVienWindow(string chiNhanh)
        {
            InitializeComponent();
            _chiNhanhHienTaiCuaQuanLy = chiNhanh;

            // Khởi tạo dữ liệu
            LoadDanhSachChiNhanh();
            LoadDanhSachNhanVien();
            PhanQuyenGiaoDien();
        }

        // 1. Tải danh sách chi nhánh vào ComboBox (chỉ chi nhánh đang hoạt động — trùng mã thì lấy bản mới nhất)
        private void LoadDanhSachChiNhanh()
        {
            using (var db = new AppDbContext())
            {
                var dsChiNhanh = db.ChiNhanhs.AsNoTracking()
                    .Where(x => !x.DaXoa)
                    .AsEnumerable()
                    .GroupBy(x => x.MaChiNhanh, StringComparer.Ordinal)
                    .Select(g => g.OrderByDescending(x => x.NgayCapNhat).First())
                    .OrderBy(x => x.TenChiNhanh)
                    .ToList();

                cmbChiNhanh.ItemsSource = dsChiNhanh;
                cmbChiNhanh.DisplayMemberPath = "TenChiNhanh";
                cmbChiNhanh.SelectedValuePath = "MaChiNhanh";

                // Mặc định chọn chi nhánh của người đang đăng nhập (nếu còn trong danh sách)
                if (dsChiNhanh.Any(c => c.MaChiNhanh == _chiNhanhHienTaiCuaQuanLy))
                    cmbChiNhanh.SelectedValue = _chiNhanhHienTaiCuaQuanLy;
                else if (dsChiNhanh.Count > 0)
                    cmbChiNhanh.SelectedIndex = 0;
            }
        }

        // 2. Phân quyền: Nếu không phải admin thì không được chọn chi nhánh khác
        private void PhanQuyenGiaoDien()
        {
            if (!App.TaiKhoanHienTai.Equals(App.TaiKhoanAdminTong, StringComparison.OrdinalIgnoreCase))
                cmbChiNhanh.IsEnabled = false;
        }

        private static bool LaVaiTroLaQuanLyTrongHeThong(string? vaiTro) =>
            vaiTro == "QL" || vaiTro == App.VaiTroAdminHeThong
            || string.Equals(vaiTro, "Admin", StringComparison.OrdinalIgnoreCase);

        private void LoadDanhSachNhanVien()
        {
            using (var db = new AppDbContext())
            {
                var maChiNhanhHoatDong = db.ChiNhanhs.AsNoTracking()
                    .Where(c => !c.DaXoa)
                    .Select(c => c.MaChiNhanh)
                    .Distinct()
                    .ToHashSet(StringComparer.Ordinal);

                // Không dùng string.Equals(..., StringComparison) — EF Core SQLite không dịch được
                if (App.TaiKhoanHienTai.Equals(App.TaiKhoanAdminTong, StringComparison.OrdinalIgnoreCase))
                {
                    dgNhanVien.ItemsSource = db.NhanViens.AsNoTracking()
                        .Where(nv => !nv.DaXoa)
                        .Where(nv => maChiNhanhHoatDong.Contains(nv.MaChiNhanh)
                                     || (nv.TaiKhoan != null && nv.TaiKhoan.ToLower() == "admin"))
                        .ToList();
                }
                else
                {
                    dgNhanVien.ItemsSource = db.NhanViens.AsNoTracking()
                        .Where(nv => !nv.DaXoa)
                        .Where(nv => nv.MaChiNhanh == _chiNhanhHienTaiCuaQuanLy
                                     || nv.TaiKhoan == App.TaiKhoanHienTai)
                        .Where(nv => maChiNhanhHoatDong.Contains(nv.MaChiNhanh)
                                     || (nv.TaiKhoan != null && nv.TaiKhoan.ToLower() == "admin"))
                        .ToList();
                }
            }
        }

        private async Task DongBoNhanVienLenServer(NhanVien nv)
        {
            var api = new ApiService();
            var (ok, err) = await api.UpsertNhanViensOnServerAsync(new List<NhanVien> { nv });
            if (!ok)
            {
                MessageBox.Show(
                    "Không đồng bộ được tài khoản nhân viên lên SQL Server:\n" + (err ?? "Lỗi không xác định"),
                    "Cảnh báo đồng bộ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void btnThemNV_Click(object sender, RoutedEventArgs e)
        {
            string taiKhoan = txtTaiKhoan.Text.Trim();
            string matKhau = txtMatKhau.Text.Trim();
            string hoTen = txtHoTen.Text.Trim();
            string maCN = cmbChiNhanh.SelectedValue?.ToString() ?? string.Empty;

            if (string.IsNullOrEmpty(taiKhoan) || string.IsNullOrEmpty(matKhau) ||
                string.IsNullOrEmpty(hoTen) || string.IsNullOrEmpty(maCN))
            {
                MessageBox.Show("Vui lòng nhập đủ thông tin và chọn chi nhánh!", "Cảnh báo");
                return;
            }

            using (var db = new AppDbContext())
            {
                bool cnHopLe = db.ChiNhanhs.Any(c => c.MaChiNhanh == maCN && !c.DaXoa);
                if (!cnHopLe)
                {
                    MessageBox.Show("Chi nhánh đã chọn không còn hoạt động. Vui lòng chọn chi nhánh khác hoặc làm mới danh sách.", "Cảnh báo");
                    return;
                }

                if (taiKhoan.Equals(App.TaiKhoanAdminTong, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Không được đặt tài khoản trùng với admin tổng.", "Cảnh báo");
                    return;
                }

                if (db.NhanViens.Any(nv => nv.TaiKhoan == taiKhoan))
                {
                    MessageBox.Show("Tài khoản đã tồn tại!", "Lỗi");
                    return;
                }

                var nvMoi = new NhanVien
                {
                    Id = Guid.NewGuid().ToString(),
                    TaiKhoan = taiKhoan,
                    MatKhau = matKhau,
                    HoTen = hoTen,
                    VaiTro = cmbVaiTro.Text.Contains("QL") ? "QL" : "NV",
                    MaChiNhanh = maCN, // Lưu mã chi nhánh từ ComboBox
                    TrangThaiDongBo = 0,
                    NgayCapNhat = DateTime.Now
                };

                db.NhanViens.Add(nvMoi);
                db.SaveChanges();
                await DongBoNhanVienLenServer(nvMoi);
            }

            MessageBox.Show("Thêm nhân viên thành công!");
            DatLaiForm();
            LoadDanhSachNhanVien();
        }

        private void dgNhanVien_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (dgNhanVien.SelectedItem is NhanVien nv)
            {
                _idNhanVienDangChon = nv.Id;
                txtTaiKhoan.Text = nv.TaiKhoan;
                txtTaiKhoan.IsReadOnly = true;
                txtMatKhau.Text = nv.MatKhau;
                txtHoTen.Text = nv.HoTen;

                bool laAdminTong = App.LaTaiKhoanAdminTong(nv.Id, nv.TaiKhoan);
                cmbVaiTro.Text = (laAdminTong || LaVaiTroLaQuanLyTrongHeThong(nv.VaiTro))
                    ? "Quản lý (QL)"
                    : "Nhân viên (NV)";

                cmbChiNhanh.SelectedValue = nv.MaChiNhanh;

                btnThemNV.IsEnabled = false;
                bool choPhepSuaXoa = !laAdminTong;
                btnSuaNV.IsEnabled = choPhepSuaXoa;
                btnXoaNV.IsEnabled = choPhepSuaXoa;
                cmbVaiTro.IsEnabled = choPhepSuaXoa;
                if (laAdminTong)
                    cmbChiNhanh.IsEnabled = false;
                else
                    cmbChiNhanh.IsEnabled = App.TaiKhoanHienTai.Equals(App.TaiKhoanAdminTong, StringComparison.OrdinalIgnoreCase);
            }
        }

        private async void btnSuaNV_Click(object sender, RoutedEventArgs e)
        {
            if (_idNhanVienDangChon == null) return;

            using (var db = new AppDbContext())
            {
                var nv = db.NhanViens.FirstOrDefault(x => x.Id == _idNhanVienDangChon);
                if (nv != null)
                {
                    if (App.LaTaiKhoanAdminTong(nv.Id, nv.TaiKhoan))
                    {
                        MessageBox.Show("Không được sửa tài khoản admin tổng tại đây (vai trò, chi nhánh). Đổi mật khẩu dùng chức năng Đổi mật khẩu trên màn hình bán hàng.", "Thông báo");
                        return;
                    }

                    nv.MatKhau = txtMatKhau.Text.Trim();
                    nv.HoTen = txtHoTen.Text.Trim();
                    nv.VaiTro = cmbVaiTro.Text.Contains("QL") ? "QL" : "NV";

                    // Chỉ Admin mới được phép đổi chi nhánh cho nhân viên
                    if (App.TaiKhoanHienTai.Equals(App.TaiKhoanAdminTong, StringComparison.OrdinalIgnoreCase))
                    {
                        string maMoi = cmbChiNhanh.SelectedValue?.ToString() ?? nv.MaChiNhanh;
                        if (!db.ChiNhanhs.Any(c => c.MaChiNhanh == maMoi && !c.DaXoa))
                        {
                            MessageBox.Show("Chi nhánh đã chọn không còn hoạt động.", "Cảnh báo");
                            return;
                        }
                        nv.MaChiNhanh = maMoi;
                    }

                    nv.TrangThaiDongBo = 0;
                    nv.NgayCapNhat = DateTime.Now;

                    db.SaveChanges();
                    await DongBoNhanVienLenServer(nv);
                    MessageBox.Show("Cập nhật thành công!");
                    LoadDanhSachNhanVien();
                    DatLaiForm();
                }
            }
        }

        private void btnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            LoadDanhSachChiNhanh();
            LoadDanhSachNhanVien();
            DatLaiForm();
        }

        private void DatLaiForm()
        {
            _idNhanVienDangChon = null;
            txtTaiKhoan.Clear();
            txtTaiKhoan.IsReadOnly = false;
            txtMatKhau.Clear();
            txtHoTen.Clear();

            // Reset ComboBox về chi nhánh mặc định
            cmbChiNhanh.SelectedValue = _chiNhanhHienTaiCuaQuanLy;

            btnThemNV.IsEnabled = true;
            btnSuaNV.IsEnabled = false;
            btnXoaNV.IsEnabled = false;
            cmbVaiTro.IsEnabled = true;
            cmbChiNhanh.IsEnabled = App.TaiKhoanHienTai.Equals(App.TaiKhoanAdminTong, StringComparison.OrdinalIgnoreCase);
            dgNhanVien.SelectedItem = null;
        }

        private async void btnXoaNV_Click(object sender, RoutedEventArgs e)
        {
            if (_idNhanVienDangChon == null) return;
            if (MessageBox.Show("Xóa nhân viên này?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using (var db = new AppDbContext())
            {
                var nv = db.NhanViens.FirstOrDefault(x => x.Id == _idNhanVienDangChon);
                if (nv == null) return;
                if (App.LaTaiKhoanAdminTong(nv.Id, nv.TaiKhoan))
                {
                    MessageBox.Show("Không thể xóa tài khoản admin tổng.", "Thông báo");
                    return;
                }

                var api = new ApiService();
                var (ok, err) = await api.DeleteNhanViensOnServerAsync(new List<string> { nv.Id });
                if (!ok)
                {
                    MessageBox.Show(
                        "Không xóa mềm nhân viên trên SQL Server:\n" + (err ?? "Lỗi không xác định") +
                        "\n\nVẫn ẩn tài khoản trên máy này.",
                        "Cảnh báo đồng bộ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                string tenNvXoa = nv.HoTen ?? string.Empty;
                foreach (var hd in db.HoaDons.Where(h => h.IdNhanVien == nv.Id))
                {
                    if (string.IsNullOrWhiteSpace(hd.HoTenNguoiBan))
                        hd.HoTenNguoiBan = tenNvXoa;
                }

                nv.DaXoa = true;
                nv.NgayCapNhat = DateTime.Now;
                nv.TrangThaiDongBo = ok ? 1 : 0;
                db.SaveChanges();
                LoadDanhSachNhanVien();
                DatLaiForm();
            }
        }
    }
}