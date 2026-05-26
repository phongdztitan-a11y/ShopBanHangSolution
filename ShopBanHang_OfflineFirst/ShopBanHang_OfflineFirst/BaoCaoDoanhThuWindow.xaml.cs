using Microsoft.EntityFrameworkCore;
using ShopBanHang_OfflineFirst.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for BaoCaoDoanhThuWindow.xaml
    /// </summary>
    public partial class BaoCaoDoanhThuWindow : Window
    {
        public BaoCaoDoanhThuWindow()
        {
            InitializeComponent();
            dpTu.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpDen.SelectedDate = DateTime.Now;

            LoadDanhSachChiNhanh();
        }
        private void LoadDanhSachChiNhanh()
        {
            using (var db = new AppDbContext())
            {
                // Lấy từ bảng ChiNhanh (những cái chưa xóa)
                var dsChiNhanh = db.ChiNhanhs.Where(x => !x.DaXoa).ToList();

                cbChiNhanh.Items.Clear();
                cbChiNhanh.Items.Add("--- Tất cả chi nhánh ---");

                foreach (var cn in dsChiNhanh)
                {
                    cbChiNhanh.Items.Add(cn.MaChiNhanh); // Hoặc cn.TenChiNhanh tùy bạn muốn lọc theo gì
                }

                if (!App.CoQuyenQuanLyCapCao(App.VaiTro))
                {
                    cbChiNhanh.SelectedItem = App.ChiNhanhHienTai;
                    cbChiNhanh.IsEnabled = false;
                }
                else
                {
                    cbChiNhanh.SelectedIndex = 0;
                }
            }
        }

        private void btnXemBaoCao_Click(object sender, RoutedEventArgs e)
        {
            DateTime tuNgay = dpTu.SelectedDate ?? DateTime.MinValue;
            DateTime denNgay = dpDen.SelectedDate ?? DateTime.MaxValue;
            if (denNgay != DateTime.MaxValue) denNgay = denNgay.Date.AddDays(1).AddTicks(-1);

            using (var db = new AppDbContext())
            {
                // 1. Khởi tạo Query
                var query = db.HoaDons.AsNoTracking().AsQueryable();
                string chiNhanhChon = cbChiNhanh.Text;

                // 2. Lọc theo chi nhánh
                if (!string.IsNullOrEmpty(chiNhanhChon) && chiNhanhChon != "--- Tất cả chi nhánh ---")
                {
                    query = query.Where(h => h.MaChiNhanh == chiNhanhChon);
                }

                // 3. Lọc theo thời gian và lấy danh sách
                var hdTrongKy = query
                     .Where(h => h.NgayLap >= tuNgay && h.NgayLap <= denNgay)
                     .OrderByDescending(h => h.NgayLap) // Mới nhất lên đầu
                     .ToList();

                // 4. Tính toán các con số tổng quát (Top boxes)
                txtTongDoanhThu.Text = hdTrongKy.Sum(h => h.TongTien).ToString("N0") + " đ";
                txtTongDonHang.Text = hdTrongKy.Count.ToString();

                var dsIdHoaDon = hdTrongKy.Select(h => h.Id).ToList();
                var tongSL = db.ChiTietHoaDons
                               .Where(ct => dsIdHoaDon.Contains(ct.IdHoaDon))
                               .Sum(ct => (int?)ct.SoLuong) ?? 0;
                txtTongSanPham.Text = tongSL.ToString("N0");

                // 5. Chuẩn bị dữ liệu hiển thị cho DataGrid (Map tên chi nhánh)
                // 5. Chuẩn bị dữ liệu hiển thị cho DataGrid
                // 5. Chuẩn bị dữ liệu hiển thị cho DataGrid
                // Thay vì dùng FirstOrDefault trong vòng lặp Select:
                // 5. Chuẩn bị dữ liệu hiển thị cho DataGrid (Map tên chi nhánh)
                // Lấy danh sách chi nhánh để map mã -> tên
                // 5. Chuẩn bị dữ liệu hiển thị (Dùng Class rõ ràng thay vì Anonymous Type)
                var dsChiNhanhGoc = db.ChiNhanhs.AsNoTracking().ToList();
                var dictChiNhanh = dsChiNhanhGoc
                    .GroupBy(cn => cn.MaChiNhanh)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .OrderBy(cn => cn.DaXoa) // Ưu tiên chi nhánh còn hoạt động
                            .ThenByDescending(cn => cn.NgayCapNhat)
                            .Select(cn => cn.TenChiNhanh)
                            .FirstOrDefault() ?? g.Key
                    );

                var tableData = hdTrongKy.Select(h => new BaoCaoHoaDonDTO // <--- Dùng class ở đây
                {
                    Id = h.Id,
                    NgayLap = h.NgayLap,
                    TongTien = h.TongTien,
                    MaChiNhanh = h.MaChiNhanh,
                    TrangThaiDongBo = h.TrangThaiDongBo, // <<-- Đã xác định rõ kiểu int
                    TenChiNhanh = dictChiNhanh.ContainsKey(h.MaChiNhanh) ? dictChiNhanh[h.MaChiNhanh] : h.MaChiNhanh,
                    TrangThaiText = h.TrangThaiDongBo == 1 ? "Đã đồng bộ" : "Chờ đồng bộ"
                }).ToList();

                dgHoaDonBaoCao.ItemsSource = tableData;

                // 6. Cảnh báo Offline
                brdCanhBaoOffline.Visibility = hdTrongKy.Any(h => h.TrangThaiDongBo == 0) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        private void dgHoaDonBaoCao_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Lấy dòng dữ liệu đang được chọn
            var selectedItem = dgHoaDonBaoCao.SelectedItem;

            if (selectedItem != null)
            {
                try
                {
                    // Vì ItemsSource dùng Anonymous Type nên ta dùng dynamic để lấy Id
                    dynamic selectedHoaDon = selectedItem;
                    string maHD = selectedHoaDon.Id; // Đây là GUID (khóa chính)

                    // Gọi cửa sổ bạn vừa cung cấp (ChiTietHoaDonWindow)
                    // Lưu ý: ta truyền maHD vào constructor
                    ChiTietHoaDonWindow chiTietWin = new ChiTietHoaDonWindow(maHD);
                    chiTietWin.Owner = this; // Hiển thị giữa màn hình báo cáo
                    chiTietWin.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể mở chi tiết hóa đơn: " + ex.Message);
                }
            }
        }
        
    }
    public class BaoCaoHoaDonDTO
    {
        public string Id { get; set; } = string.Empty;
        public DateTime NgayLap { get; set; }
        public double TongTien { get; set; } // Đổi từ decimal thành double
        public string MaChiNhanh { get; set; } = string.Empty;
        public string TenChiNhanh { get; set; } = string.Empty;
        public int TrangThaiDongBo { get; set; }
        public string TrangThaiText { get; set; } = string.Empty;
    }
}