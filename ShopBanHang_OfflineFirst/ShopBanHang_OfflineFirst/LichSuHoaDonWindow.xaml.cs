using ShopBanHang_OfflineFirst.Data;
using ShopBanHang.Shared; // Đảm bảo đã using Models để nhận dạng HoaDon
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore; // Dòng này chính là "chìa khóa"

namespace ShopBanHang_OfflineFirst
{
    public partial class LichSuHoaDonWindow : Window
    {
        public LichSuHoaDonWindow()
        {
            InitializeComponent();
            dpTuNgay.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpDenNgay.SelectedDate = DateTime.Now;
            LoadLichSuHoaDon();
        }

        private void LoadLichSuHoaDon()
        {
            using (var db = new AppDbContext())
            {
                // 1. Khởi tạo query với AsNoTracking để tối ưu tốc độ đọc
                var query = db.HoaDons.AsNoTracking().AsQueryable();
                // Đảm bảo vẫn còn các dòng này để lọc dữ liệu thực tế
                query = query.Where(h => h.MaChiNhanh == App.ChiNhanhHienTai);

                if (!string.IsNullOrEmpty(txtTimKiemHĐ.Text))
                    query = query.Where(h => h.MaHoaDon.Contains(txtTimKiemHĐ.Text));

                if (dpTuNgay.SelectedDate.HasValue)
                    query = query.Where(h => h.NgayLap >= dpTuNgay.SelectedDate.Value.Date);

                if (dpDenNgay.SelectedDate.HasValue)
                {
                    var denNgay = dpDenNgay.SelectedDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(h => h.NgayLap <= denNgay);
                }

                // === 2. BỘ LỌC CHI NHÁNH (Áp dụng cho TẤT CẢ mọi người) ===
                // Admin hay nhân viên khi vào đây đều chỉ thấy dữ liệu của chi nhánh đã chọn lúc Login
                // 5. Thực thi truy vấn (Join với Khách hàng để lấy tên)
                var result = (from h in query
                              join k in db.KhachHangs on h.IdKhachHang equals k.Id into kGroup
                              from k in kGroup.DefaultIfEmpty()
                              select new
                              {
                                  h.Id,
                                  h.MaHoaDon,
                                  h.NgayLap,
                                  TenKhachHang = k != null ? k.HoTen : "Khách lẻ",
                                  h.TongTien,
                                  TrangThaiDongBoText = h.TrangThaiDongBo == 1 ? "Đã đồng bộ" : "Chờ đồng bộ",
                                  h.MaChiNhanh
                              }).OrderByDescending(h => h.NgayLap).ToList();

                dgHoaDon.ItemsSource = result;
            }
        }

        private void txtTimKiemHĐ_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadLichSuHoaDon();
        }

        private void btnLoc_Click(object sender, RoutedEventArgs e)
        {
            LoadLichSuHoaDon();
        }

        private void btnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiemHĐ.Text = "";
            // Quay lại mặc định đầu tháng thay vì null
            dpTuNgay.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            dpDenNgay.SelectedDate = DateTime.Now;
            LoadLichSuHoaDon();
        }

        private void dgLichSuHoaDon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid?.SelectedItem != null)
            {
                // Sử dụng dynamic để đọc thuộc tính Id từ Anonymous Type
                dynamic selectedItem = grid.SelectedItem;
                string idSelected = selectedItem.Id;

                ChiTietHoaDonWindow detailWin = new ChiTietHoaDonWindow(idSelected);
                detailWin.Owner = this;
                detailWin.ShowDialog();
            }
        }

    }
}