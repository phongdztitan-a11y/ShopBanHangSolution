using Microsoft.EntityFrameworkCore;
using ShopBanHang_OfflineFirst.Data;
using ShopBanHang_OfflineFirst.Services;
using ShopBanHang.Shared;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ShopBanHang_OfflineFirst
{
    public partial class NhapHangWindow : Window
    {
        private readonly ApiService _apiService = new();
        private string? _idSanPhamDangSua;

        private bool DangSuaSanPham => !string.IsNullOrEmpty(_idSanPhamDangSua);

        public NhapHangWindow()
        {
            InitializeComponent();
            if (!KiemTraQuyenQuanLy())
            {
                Close();
                return;
            }

            CapNhatCheDoForm();
            LoadData();
        }

        private static bool CoQuyenQuanLySanPham() => App.CoQuyenQuanLyCapCao(App.VaiTro);

        private bool KiemTraQuyenQuanLy()
        {
            if (CoQuyenQuanLySanPham())
                return true;

            MessageBox.Show(
                "Chỉ tài khoản admin hoặc quản lý mới được thêm/sửa sản phẩm.",
                "Không đủ quyền",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        public class SanPhamKhoHienThi
        {
            public string Id { get; set; } = string.Empty;
            public string MaGoc { get; set; } = string.Empty;
            public string SKU { get; set; } = string.Empty;
            public string TenSanPham { get; set; } = string.Empty;
            public string KichCo { get; set; } = string.Empty;
            public string MauSac { get; set; } = string.Empty;
            public double GiaBan { get; set; }
            public int SoLuongTon { get; set; }
        }

        private void LoadData()
        {
            using var db = new AppDbContext();
            var query = from sp in db.SanPhams.AsNoTracking()
                        where !sp.DaXoa
                        join tk in db.TonKhoChiNhanhs.Where(t => t.MaChiNhanh == App.ChiNhanhHienTai)
                        on sp.Id equals tk.IdSanPham into groupTK
                        from subTK in groupTK.DefaultIfEmpty()
                        select new SanPhamKhoHienThi
                        {
                            Id = sp.Id,
                            MaGoc = sp.MaGoc,
                            // Compute SKU using concatenation so EF can translate (avoid calling static method TinhSKU in LINQ-to-Entities)
                            SKU = (sp.MaGoc + "-" + (sp.KichCo ?? "") + "-" + (sp.MauSac ?? "")).ToUpper(),
                            TenSanPham = sp.TenSanPham,
                            KichCo = sp.KichCo ?? string.Empty,
                            MauSac = sp.MauSac ?? string.Empty,
                            GiaBan = sp.GiaBan,
                            SoLuongTon = subTK != null ? subTK.SoLuong : 0
                        };

            string tuKhoa = txtTimKiem.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(x => x.TenSanPham.ToLower().Contains(tuKhoa) ||
                                         x.MaGoc.ToLower().Contains(tuKhoa) ||
                                         x.SKU.ToLower().Contains(tuKhoa));
            }

            dgSanPhamKho.ItemsSource = query.OrderByDescending(x => x.Id).ToList();
        }

        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e) => LoadData();

        private void dgSanPhamKho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSanPhamKho.SelectedItem is not SanPhamKhoHienThi sp) return;

            _idSanPhamDangSua = sp.Id;
            txtNewTen.Text = sp.TenSanPham;
            txtNewMaGoc.Text = sp.MaGoc;
            txtNewSize.Text = sp.KichCo;
            txtNewMau.Text = sp.MauSac;
            txtNewGiaBan.Text = sp.GiaBan.ToString();
            txtNewTon.Text = sp.SoLuongTon.ToString();
            CapNhatSkuPreview();
            CapNhatCheDoForm();
        }

        private void CapNhatSkuPreview()
        {
            if (!DangSuaSanPham)
            {
                txtSkuPreview.Text = string.Empty;
                return;
            }

            string skuMoi = SanPham.TinhSKU(txtNewMaGoc.Text.Trim(), txtNewSize.Text.Trim(), txtNewMau.Text.Trim());
            txtSkuPreview.Text = $"SKU sau khi lưu: {skuMoi} (Id sản phẩm giữ nguyên)";
        }

        private void CapNhatCheDoForm()
        {
            bool dangSua = DangSuaSanPham;
            txtTieuDeForm.Text = dangSua ? "2. SỬA SẢN PHẨM" : "2. TẠO SẢN PHẨM MỚI";
            btnThemMo.IsEnabled = !dangSua;
            btnSuaSanPham.IsEnabled = dangSua;
            btnHuyChon.Visibility = dangSua ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnHuyChon_Click(object sender, RoutedEventArgs e)
        {
            dgSanPhamKho.SelectedItem = null;
            ResetFormMoi();
        }

        private void ResetFormMoi()
        {
            _idSanPhamDangSua = null;
            txtNewTen.Clear();
            txtNewMaGoc.Clear();
            txtNewSize.Clear();
            txtNewMau.Clear();
            txtNewGiaBan.Clear();
            txtNewTon.Text = "0";
            txtSkuPreview.Text = string.Empty;
            CapNhatCheDoForm();
        }

        private bool DocForm(out string ten, out string maGoc, out string size, out string mau, out double giaBan, out int tonKho)
        {
            ten = txtNewTen.Text.Trim();
            maGoc = txtNewMaGoc.Text.Trim().ToUpper();
            size = txtNewSize.Text.Trim();
            mau = txtNewMau.Text.Trim();
            giaBan = 0;
            tonKho = 0;

            if (string.IsNullOrWhiteSpace(ten) || string.IsNullOrWhiteSpace(maGoc))
            {
                MessageBox.Show("Vui lòng nhập tên và mã gốc sản phẩm.");
                return false;
            }

            double.TryParse(txtNewGiaBan.Text, out giaBan);
            if (!int.TryParse(txtNewTon.Text, out tonKho) || tonKho < 0)
            {
                MessageBox.Show("Tồn kho phải là số nguyên >= 0.");
                return false;
            }

            return true;
        }

        private void btnThemMo_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraQuyenQuanLy()) return;
            if (DangSuaSanPham) return;
            if (!DocForm(out var ten, out var maGoc, out var size, out var mau, out var giaBan, out var tonKho)) return;

            using var db = new AppDbContext();
            var spMoi = new SanPham
            {
                Id = Guid.NewGuid().ToString(),
                TenSanPham = ten,
                MaGoc = maGoc,
                KichCo = size,
                MauSac = mau,
                GiaBan = giaBan,
                MaChiNhanh = App.ChiNhanhHienTai,
                NgayCapNhat = DateTime.UtcNow,
                TrangThaiDongBo = 0
            };
            db.SanPhams.Add(spMoi);

            if (tonKho > 0)
            {
                db.TonKhoChiNhanhs.Add(new TonKhoChiNhanh
                {
                    Id = Guid.NewGuid().ToString(),
                    IdSanPham = spMoi.Id,
                    MaChiNhanh = App.ChiNhanhHienTai,
                    SoLuong = tonKho,
                    TrangThaiDongBo = 0
                });
            }

            db.SaveChanges();
            _ = DongBoSanPhamAsync(spMoi.Id);
            MessageBox.Show($"Thêm sản phẩm mới thành công.\nSKU: {spMoi.SKU}", "Thành công");
            ResetFormMoi();
            LoadData();
        }

        private async void btnSuaSanPham_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraQuyenQuanLy()) return;
            if (!DangSuaSanPham || string.IsNullOrEmpty(_idSanPhamDangSua)) return;
            if (!DocForm(out var ten, out var maGoc, out var size, out var mau, out var giaBan, out var tonKho)) return;

            try
            {
                using var db = new AppDbContext();
                var sp = await db.SanPhams.FirstOrDefaultAsync(s => s.Id == _idSanPhamDangSua);
                if (sp == null)
                {
                    MessageBox.Show("Không tìm thấy sản phẩm.");
                    return;
                }

                string skuCu = sp.SKU;
                sp.TenSanPham = ten;
                sp.MaGoc = maGoc;
                sp.KichCo = size;
                sp.MauSac = mau;
                sp.GiaBan = giaBan;
                sp.NgayCapNhat = DateTime.UtcNow;
                sp.TrangThaiDongBo = 0;

                var tonKhoRow = await db.TonKhoChiNhanhs.FirstOrDefaultAsync(t =>
                    t.IdSanPham == sp.Id && t.MaChiNhanh == App.ChiNhanhHienTai);

                if (tonKhoRow == null)
                {
                    if (tonKho > 0)
                    {
                        db.TonKhoChiNhanhs.Add(new TonKhoChiNhanh
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdSanPham = sp.Id,
                            MaChiNhanh = App.ChiNhanhHienTai,
                            SoLuong = tonKho,
                            TrangThaiDongBo = 0
                        });
                    }
                }
                else
                {
                    tonKhoRow.SoLuong = tonKho;
                    tonKhoRow.TrangThaiDongBo = 0;
                }

                await db.SaveChangesAsync();

                var (syncOk, syncErr) = await DongBoSanPhamAsync(sp.Id);
                string msg = $"Đã cập nhật sản phẩm (Id giữ nguyên).\nSKU: {skuCu} → {sp.SKU}";
                if (!syncOk)
                    msg += $"\n\nLưu local OK nhưng đồng bộ server thất bại:\n{syncErr}";
                else
                    msg += "\n\nĐã đồng bộ lên server.";

                msg += "\n\nHóa đơn đã thanh toán trước đó vẫn giữ tên/SKU cũ trên chi tiết hóa đơn.";
                MessageBox.Show(msg, syncOk ? "Thành công" : "Cảnh báo");

                LoadData();
                CapNhatSkuPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi sửa: " + ex.Message);
            }
        }

        private async Task<(bool Ok, string? Error)> DongBoSanPhamAsync(string idSanPham)
        {
            try
            {
                using var db = new AppDbContext();
                var sp = await db.SanPhams.AsNoTracking().FirstOrDefaultAsync(s => s.Id == idSanPham);
                if (sp == null) return (false, "Không tìm thấy sản phẩm local.");

                var (postOk, postErr) = await _apiService.PostSanPhamToCatalogAsync(sp);
                if (!postOk) return (false, postErr);

                var (pullOk, dsServer, pullErr) = await _apiService.GetSanPhamCatalogFromServerAsync();
                if (pullOk)
                {
                    var spServer = dsServer.FirstOrDefault(s => s.Id == idSanPham);
                    if (spServer != null)
                    {
                        var spLocal = await db.SanPhams.FirstOrDefaultAsync(s => s.Id == idSanPham);
                        if (spLocal != null && spServer.NgayCapNhat >= spLocal.NgayCapNhat)
                        {
                            spLocal.TenSanPham = spServer.TenSanPham;
                            spLocal.MaGoc = spServer.MaGoc;
                            spLocal.KichCo = spServer.KichCo;
                            spLocal.MauSac = spServer.MauSac;
                            spLocal.GiaBan = spServer.GiaBan;
                            spLocal.NgayCapNhat = spServer.NgayCapNhat;
                            spLocal.TrangThaiDongBo = 1;
                            await db.SaveChangesAsync();
                            return (true, null);
                        }
                    }
                }

                var spMark = await db.SanPhams.FirstOrDefaultAsync(s => s.Id == idSanPham);
                if (spMark != null)
                {
                    spMark.TrangThaiDongBo = 1;
                    spMark.NgayCapNhat = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }

                return (true, pullOk ? null : pullErr);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (!KiemTraQuyenQuanLy()) return;
            if (dgSanPhamKho.SelectedItem is not SanPhamKhoHienThi spChon) return;

            if (MessageBox.Show("Xóa sản phẩm này?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using var db = new AppDbContext();
            var sp = db.SanPhams.Find(spChon.Id);
            if (sp != null)
            {
                sp.DaXoa = true;
                sp.TrangThaiDongBo = 0;
                db.SaveChanges();
                _ = DongBoSanPhamAsync(sp.Id);
            }

            ResetFormMoi();
            LoadData();
        }

        private void btnInMaVach_Click(object sender, RoutedEventArgs e)
        {
            if (dgSanPhamKho.SelectedItem is not SanPhamKhoHienThi spChon)
            {
                MessageBox.Show("Vui lòng chọn sản phẩm để in mã vạch!", "Thông báo");
                return;
            }

            using var db = new AppDbContext();
            var spGoc = db.SanPhams.Find(spChon.Id);
            if (spGoc == null) return;

            var barcodeWin = new BarcodeWindow(spGoc) { Owner = this };
            barcodeWin.ShowDialog();
        }
    }
}
