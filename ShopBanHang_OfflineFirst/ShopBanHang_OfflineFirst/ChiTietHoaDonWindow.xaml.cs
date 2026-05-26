using Microsoft.EntityFrameworkCore;
using ShopBanHang_OfflineFirst.Data;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZXing;

namespace ShopBanHang_OfflineFirst
{
    public partial class ChiTietHoaDonWindow : Window
    {
        public ChiTietHoaDonWindow(string idHoaDon, bool tuDongIn = false)
        {
            InitializeComponent();
            LoadChiTiet(idHoaDon);

            // Nếu được yêu cầu tự động in, ta sẽ đợi giao diện render xong rồi gọi nút In
            if (tuDongIn)
            {
                this.Loaded += (sender, e) =>
                {
                    // Tự động kích hoạt sự kiện click của nút In
                    btnIn_Click(this, new RoutedEventArgs());
                };
            }
        }

        private void LoadChiTiet(string idHoaDon)
        {
            using (var db = new AppDbContext())
            {
                // 1. Tìm hóa đơn cũ đã lưu trong máy
                var hd = db.HoaDons.AsNoTracking().FirstOrDefault(h => h.Id == idHoaDon);

                if (hd != null)
                {
                    txtMaHoaDon.Text = $"MÃ ĐƠN HÀNG: {hd.MaHoaDon}";

                    // Tìm tên khách hàng
                    var kh = db.KhachHangs.AsNoTracking().FirstOrDefault(k => k.Id == hd.IdKhachHang);
                    txtTenKhach.Text = $"Khách hàng: {(kh != null ? kh.HoTen : "Khách lẻ")}";
                    txtSdtKhach.Text = $"SĐT: {hd.SdtKhachHang}";
                    txtNgayTao.Text = $"Ngày bán: {hd.NgayLap:dd/MM/yyyy HH:mm}";

                    string? tenBan = hd.HoTenNguoiBan?.Trim();
                    if (string.IsNullOrEmpty(tenBan))
                    {
                        var nv = db.NhanViens.AsNoTracking().FirstOrDefault(n => n.Id == hd.IdNhanVien);
                        tenBan = nv?.HoTen ?? hd.IdNhanVien;
                    }
                    txtNhanVien.Text = $"Người bán: {tenBan}";

                    // 2. LẤY TÊN CHI NHÁNH CHUẨN (Tìm theo MaChiNhanh có sẵn trong hd)
                    var cn = db.ChiNhanhs.AsNoTracking().FirstOrDefault(c => c.MaChiNhanh == hd.MaChiNhanh);
                    txtChiNhanh.Text = $"Chi nhánh: {(cn != null ? cn.TenChiNhanh : hd.MaChiNhanh)}";

                    txtTongTien.Text = $"TỔNG CỘNG: {hd.TongTien:N0} đ";

                    // Tạo mã vạch dựa trên mã đơn hàng hiển thị
                    TaoMaVachHoaDon(hd.MaHoaDon);

                    // 3. Lấy danh sách sản phẩm (Join bảng để có tên SP)
                    var dsSanPham = (from ct in db.ChiTietHoaDons.AsNoTracking()
                                     join sp in db.SanPhams.AsNoTracking() on ct.IdSanPham equals sp.Id into spJoin
                                     from sp in spJoin.DefaultIfEmpty()
                                     where ct.IdHoaDon == idHoaDon
                                     select new
                                     {
                                         ct.IdSanPham,
                                         SKU = !string.IsNullOrWhiteSpace(ct.SKULuu) ? ct.SKULuu! : (sp != null ? sp.SKU : "—"),
                                         TenSanPham = !string.IsNullOrWhiteSpace(ct.TenSanPhamLuu) ? ct.TenSanPhamLuu! : (sp != null ? sp.TenSanPham : "—"),
                                         ct.SoLuong,
                                         ct.DonGia,
                                         ThanhTien = ct.SoLuong * ct.DonGia
                                     }).ToList();

                    dgChiTiet.ItemsSource = dsSanPham;
                }
                else
                {
                    MessageBox.Show("Không tìm thấy dữ liệu hóa đơn!");
                    this.Close();
                }
            }
        }

        private void TaoMaVachHoaDon(string maHD)
        {
            if (string.IsNullOrEmpty(maHD)) return;

            try
            {
                var writer = new ZXing.BarcodeWriterPixelData
                {
                    Format = ZXing.BarcodeFormat.CODE_128,
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = 250,
                        Height = 50,
                        Margin = 0,
                        PureBarcode = true // Vẽ vạch đen (không kèm chữ bên dưới)
                    }
                };

                var pixelData = writer.Write(maHD);

                var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(
                    pixelData.Width, pixelData.Height,
                    96, 96,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    pixelData.Pixels,
                    pixelData.Width * 4
                );

                // Đổ hình ảnh vạch đen vào thẻ Image
                imgBarcodeHoaDon.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tạo mã vạch: " + ex.Message);
            }
        }

        private void btnIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(grdInNoiDung, "In Hoa Don " + txtMaHoaDon.Text);
                    MessageBox.Show("Đã gửi lệnh in!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi in: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDong_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}