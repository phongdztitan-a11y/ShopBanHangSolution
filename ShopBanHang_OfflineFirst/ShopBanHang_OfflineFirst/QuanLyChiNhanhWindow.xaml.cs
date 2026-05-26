using ShopBanHang_OfflineFirst.Data;
using ShopBanHang_OfflineFirst.Services;
using ShopBanHang.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ShopBanHang_OfflineFirst
{
    public partial class QuanLyChiNhanhWindow : Window
    {
        public QuanLyChiNhanhWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    // Thêm .Where(x => !x.DaXoa) để chỉ lấy các chi nhánh đang hoạt động
                    dgChiNhanh.ItemsSource = db.ChiNhanhs.Where(x => !x.DaXoa).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private async void btnLuu_Click(object sender, RoutedEventArgs e)
        {
            string ma = txtMaCN.Text.Trim();
            string ten = txtTenCN.Text.Trim();

            if (string.IsNullOrEmpty(ma) || string.IsNullOrEmpty(ten))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!");
                return;
            }

            ChiNhanh? cnMoi = null;
            using (var db = new AppDbContext())
            {
                if (string.Equals(ma, App.MaChiNhanhTong, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Mã {App.MaChiNhanhTong} là chi nhánh tổng hệ thống, không tạo thêm qua màn hình này.", "Thông báo");
                    return;
                }

                // Kiểm tra trùng mã
                if (db.ChiNhanhs.Any(x => x.MaChiNhanh == ma))
                {
                    MessageBox.Show("Mã chi nhánh này đã tồn tại!");
                    return;
                }

                cnMoi = new ChiNhanh
                {
                    // Đồng bộ Id với MaChiNhanh để không lệch FK khi tạo hóa đơn
                    Id = ma,
                    MaChiNhanh = ma,
                    TenChiNhanh = ten,
                    TrangThaiDongBo = 0
                };
                db.ChiNhanhs.Add(cnMoi);

                db.SaveChanges();
            }

            if (cnMoi != null)
            {
                var api = new ApiService();
                var (ok, err) = await api.UpsertChiNhanhsOnServerAsync(new List<ChiNhanh> { cnMoi });
                if (!ok)
                {
                    MessageBox.Show(
                        "Thêm chi nhánh local thành công nhưng chưa đồng bộ được lên server:\n" + (err ?? "Lỗi không xác định"),
                        "Cảnh báo đồng bộ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            MessageBox.Show("Thêm chi nhánh thành công!");
            txtMaCN.Clear();
            txtTenCN.Clear();
            LoadData(); // Tải lại bảng
        }
        private async void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            var selectedCN = dgChiNhanh.SelectedItem as ChiNhanh;

            if (selectedCN == null)
            {
                MessageBox.Show("Vui lòng chọn một chi nhánh trong danh sách để xóa!", "Thông báo");
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa chi nhánh: {selectedCN.TenChiNhanh}?\n\n" +
                "Các tài khoản nhân viên thuộc chi nhánh này (trừ admin) sẽ bị ẩn (xóa mềm). Hóa đơn cũ vẫn giữ Id nhân viên gốc.",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                string ma = selectedCN.MaChiNhanh?.Trim() ?? string.Empty;
                if (ma == App.MaChiNhanhTong || selectedCN.Id == App.MaChiNhanhTong)
                {
                    MessageBox.Show($"Không thể xóa chi nhánh tổng ({App.MaChiNhanhTong}).", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    using (var db = new AppDbContext())
                    {
                        // Mọi bản ghi cùng mã còn hoạt động (tránh FirstOrDefault chỉ trúng bản đã DaXoa)
                        List<ChiNhanh> canXoa = db.ChiNhanhs.Where(x => x.MaChiNhanh == ma && !x.DaXoa).ToList();

                        if (canXoa.Count == 0)
                        {
                            MessageBox.Show("Không tìm thấy chi nhánh trong CSDL (có thể đã bị xóa trước đó).", "Thông báo");
                            LoadData();
                            return;
                        }

                        var nhanVienTheoCN = db.NhanViens
                            .Where(n => n.MaChiNhanh == ma
                                        && !n.DaXoa
                                        && n.Id != "ADMIN_001"
                                        && !(n.TaiKhoan != null && n.TaiKhoan.ToLower() == "admin"))
                            .ToList();

                        var idsGuiLenServer = nhanVienTheoCN.Select(n => n.Id).ToList();
                        bool dongBoNvLenServer = true;
                        if (idsGuiLenServer.Count > 0)
                        {
                            var api = new ApiService();
                            var (ok, err) = await api.DeleteNhanViensOnServerAsync(idsGuiLenServer);
                            dongBoNvLenServer = ok;
                            if (!ok)
                            {
                                MessageBox.Show(
                                    "Không xóa mềm nhân viên trên SQL Server:\n" + (err ?? "Lỗi không xác định") +
                                    "\n\nHệ thống vẫn ẩn tài khoản trên máy này.",
                                    "Cảnh báo đồng bộ", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        if (nhanVienTheoCN.Count > 0)
                        {
                            foreach (var nv in nhanVienTheoCN)
                            {
                                string tenNv = nv.HoTen ?? string.Empty;
                                foreach (var hd in db.HoaDons.Where(h => h.IdNhanVien == nv.Id))
                                {
                                    if (string.IsNullOrWhiteSpace(hd.HoTenNguoiBan))
                                        hd.HoTenNguoiBan = tenNv;
                                }

                                nv.DaXoa = true;
                                nv.NgayCapNhat = DateTime.UtcNow;
                                nv.TrangThaiDongBo = dongBoNvLenServer ? 1 : 0;
                            }
                        }

                        foreach (var item in canXoa)
                        {
                            item.DaXoa = true;
                            item.NgayCapNhat = DateTime.UtcNow;
                        }

                        db.SaveChanges();

                        var apiChiNhanh = new ApiService();
                        var (okCn, errCn) = await apiChiNhanh.UpsertChiNhanhsOnServerAsync(canXoa);
                        if (!okCn)
                        {
                            MessageBox.Show(
                                "Xóa chi nhánh local thành công nhưng chưa đồng bộ trạng thái chi nhánh lên server:\n" + (errCn ?? "Lỗi không xác định"),
                                "Cảnh báo đồng bộ", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }

                        MessageBox.Show("Xóa thành công!");
                        LoadData();
                    }
                }
                catch (Exception ex)
                {
                    string msg = ex.InnerException?.Message ?? ex.Message;
                    MessageBox.Show("Không xóa được chi nhánh: " + msg, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}