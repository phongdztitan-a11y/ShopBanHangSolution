using ShopBanHang.Shared;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ZXing;

namespace ShopBanHang_OfflineFirst
{
    public partial class BarcodeWindow : Window
    {
        public BarcodeWindow(SanPham sp)
        {
            InitializeComponent();
            LoadBarcode(sp);
        }

        private void LoadBarcode(SanPham sp)
        {
            txtTenSanPham.Text = sp.TenSanPham;
            txtSKU.Text = sp.MaGoc;
            txtGiaBan.Text = string.Format("{0:N0} VNĐ", sp.GiaBan);

            // 1. Sử dụng BarcodeWriterPixelData thay cho BarcodeWriter
            var writer = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Width = 200,
                    Height = 60,
                    Margin = 1,
                    PureBarcode = true
                }
            };

            string maCanIn = string.IsNullOrEmpty(sp.MaGoc) ? sp.Id : sp.MaGoc;

            // 2. Tạo ra dữ liệu điểm ảnh (PixelData)
            var pixelData = writer.Write(maCanIn);

            // 3. Chuyển đổi trực tiếp PixelData sang BitmapSource của WPF
            var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(
                pixelData.Width,
                pixelData.Height,
                96, // dpi X
                96, // dpi Y
                System.Windows.Media.PixelFormats.Bgra32, // Định dạng màu
                null,
                pixelData.Pixels,
                pixelData.Width * 4 // stride (byte per row)
            );

            // 4. Gán vào giao diện
            imgBarcode.Source = bitmap;
        }

        private void btnXacNhanIn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.PrintDialog pd = new System.Windows.Controls.PrintDialog();
            if (pd.ShowDialog() == true)
            {
                // Chỉ in khu vực tem nhãn (PrintArea), không in nút bấm
                pd.PrintVisual(PrintArea, "In tem nhãn sản phẩm");
            }
        }
    }
}