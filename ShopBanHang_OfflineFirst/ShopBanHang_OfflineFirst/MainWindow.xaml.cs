using Microsoft.EntityFrameworkCore;
using ShopBanHang.Shared;
using ShopBanHang.Shared.Models;
using ShopBanHang.Shared.Security;
using ShopBanHang_OfflineFirst.Data;
using ShopBanHang_OfflineFirst.Services;
using ShopBanHang_OfflineFirst.Views;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace ShopBanHang_OfflineFirst
{
    public partial class MainWindow : Window
    {
        private readonly ApiService _apiService = new ApiService();
        private readonly AppDbContext _localDb = new AppDbContext();
        private DispatcherTimer? _timerDongBo;

        // --- CẬP NHẬT GIÁ TRỊ MẶC ĐỊNH ĐỂ FIX LỖI FOREIGN KEY ---
        private string _idNhanVienHienTai = "ADMIN_001"; // Gán ID cố định ở đây
        private string _tenNhanVien = "Quản trị viên";
        private string _chiNhanh = "CN_GOC_ID";
        private string _vaiTro = "Admin";

        // TẠO GIỎ HÀNG 
        private ObservableCollection<CartItem> _gioHang = new ObservableCollection<CartItem>();
        private string _idKhachHangHienTai = "KHACH_LE"; // Mặc định là khách lẻ

        public MainWindow()
        {
            InitializeComponent();

            // Đảm bảo khởi tạo dữ liệu hệ thống TRƯỚC khi load sản phẩm
            KhoiTaoDuLieuHeThong();

            dgGioHang.ItemsSource = _gioHang;
            LoadDanhSachSanPham();
            CapNhatTrangThaiDongBo();
            _timerDongBo = new DispatcherTimer();
            _ = LamMoiEndpointAsync();
        }


        public MainWindow(string idNV, string tenNV, string chiNhanh, string vaiTro)
        {
            InitializeComponent();

            // 1. Gán các biến toàn cục
            _idNhanVienHienTai = idNV;
            _tenNhanVien = tenNV;
            _chiNhanh = chiNhanh;
            _vaiTro = vaiTro;

            // 2. Hiển thị thông tin lên giao diện
            using (var db = new AppDbContext())
            {
                var khachLe = db.KhachHangs.FirstOrDefault(k => k.Id == "KHACH_LE");
                if (khachLe == null)
                {
                    db.KhachHangs.Add(new KhachHang
                    {
                        Id = "KHACH_LE",
                        HoTen = "Khách bán lẻ",
                        SoDienThoai = "0000000000",
                        DiaChi = "", // THÊM DÒNG NÀY: Không được để null
                        NgayCapNhat = DateTime.UtcNow,
                        TrangThaiDongBo = 1,
                        MaChiNhanh = _chiNhanh,
                        DaXoa = false
                    });
                    db.SaveChanges();
                }
            }
            using (var db = new AppDbContext())
            {
                var chiNhanhObj = db.ChiNhanhs.AsNoTracking().FirstOrDefault(x => x.MaChiNhanh == chiNhanh);
                string tenHienThiCN = chiNhanhObj?.TenChiNhanh ?? chiNhanh;
                txtThongTinNhanVien.Text = $"NV: {tenNV} | CN: {tenHienThiCN} ({vaiTro})";
            }

            // 3. Phân quyền (NV không được xem quản lý)
            ApDungPhanQuyen();

            // 4. Khởi tạo DataGrid và Timer
            dgGioHang.ItemsSource = _gioHang;
            LoadDanhSachSanPham();
            CapNhatTrangThaiDongBo();

            // --- TÍCH HỢP KIỂM TRA MẠNG ---
            KiemTraVaCapNhatMang();
            NetworkChange.NetworkAddressChanged += (s, e) => KiemTraVaCapNhatMang();

            // --- Tìm đoạn này trong Constructor ---
            _timerDongBo = new DispatcherTimer();
            _timerDongBo.Interval = TimeSpan.FromSeconds(30);
            _timerDongBo.Tick += async (s, e) => {
                if (txtStatus.Text.Contains("Online"))
                    await LamMoiEndpointVaDongBoAsync();
            };
            _timerDongBo.Start();
            _ = LamMoiEndpointAsync();
        }

        private async Task LamMoiEndpointAsync()
        {
            try
            {
                await _apiService.RefreshEndpointAsync();
                Dispatcher.Invoke(CapNhatHienThiApiEndpoint);
            }
            catch { /* giữ URL cũ */ }
        }

        private bool CoQuyenQuanLyHienTai() => App.CoQuyenQuanLyCapCao(_vaiTro);

        private bool YeuCauQuyenQuanLy(string hanhDong)
        {
            if (CoQuyenQuanLyHienTai())
                return true;

            MessageBox.Show(
                $"Chỉ tài khoản admin hoặc quản lý mới được {hanhDong}.",
                "Không đủ quyền",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        private void ApDungPhanQuyen()
        {
            bool coQuyenQuanLy = CoQuyenQuanLyHienTai();
            btnNhapHang.Visibility = coQuyenQuanLy ? Visibility.Visible : Visibility.Collapsed;
            btnQuanLyNV.Visibility = coQuyenQuanLy ? Visibility.Visible : Visibility.Collapsed;
            btnQuanLyChiNhanh.Visibility = coQuyenQuanLy ? Visibility.Visible : Visibility.Collapsed;
            btnBaoCao.Visibility = coQuyenQuanLy ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CapNhatHienThiApiEndpoint()
        {
            if (txtApiEndpoint == null) return;
            string url = _apiService.BaseUrl;
            txtApiEndpoint.Text = url.Length > 48 ? "API: …" + url[^40..] : "API: " + url;
            txtApiEndpoint.ToolTip = url;
        }

        private async void btnCauHinhServer_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ServerConfigDialog { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                await LamMoiEndpointAsync();
                MessageBox.Show($"Đã lưu. URL hiện tại:\n{_apiService.BaseUrl}", "Cấu hình server");
            }
        }

        // ================= TRẠNG THÁI MẠNG =================
        private void KiemTraVaCapNhatMang()
        {
            bool isOnline = false;
            try
            {
                using (Ping p = new Ping())
                {
                    PingReply reply = p.Send("8.8.8.8", 1000);
                    isOnline = (reply.Status == IPStatus.Success);
                }
            }
            catch { isOnline = false; }

            Dispatcher.Invoke(() => {
                if (isOnline)
                {
                    iconStatus.Fill = System.Windows.Media.Brushes.Green;
                    txtStatus.Text = "Trạng thái: Online (Đã kết nối)";
                    btnDongBo.IsEnabled = true;

                    // Ép đồng bộ ngay khi vừa có mạng (hóa đơn + danh mục hai chiều)
                    _ = LamMoiEndpointVaDongBoAsync();
                }
                else
                {
                    iconStatus.Fill = System.Windows.Media.Brushes.Red;
                    txtStatus.Text = "Trạng thái: Offline (Mất kết nối)";
                    btnDongBo.IsEnabled = false;
                }
            });
        }

        private void LoadDanhSachSanPham()
        {
            using (var db = new AppDbContext())
            {
                var query = from sp in db.SanPhams.AsNoTracking()
                            where !sp.DaXoa
                            // Lọc tồn kho của chi nhánh hiện tại TRƯỚC khi join
                            join tk in db.TonKhoChiNhanhs.Where(t => t.MaChiNhanh == App.ChiNhanhHienTai)
                            on sp.Id equals tk.IdSanPham into tkGroup
                            from tk in tkGroup.DefaultIfEmpty()
                            select new SanPhamHienThi
                            {
                                Id = sp.Id,
                                SKU = sp.SKU,
                                TenSanPham = sp.TenSanPham,
                                KichCo = sp.KichCo ?? string.Empty,
                                MauSac = sp.MauSac ?? string.Empty,
                                GiaBan = sp.GiaBan,
                                // Nếu tk null (không có dòng tồn kho), lấy giá trị mặc định là 0
                                SoLuongTon = tk != null ? tk.SoLuong : 0
                            };

                dgSanPham.ItemsSource = query.ToList();
            }
        }

        private void dgSanPham_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedSp = dgSanPham.SelectedItem as SanPhamHienThi;
            if (selectedSp == null || selectedSp.SoLuongTon <= 0) return;

            // Kiểm tra xem sản phẩm đã có trong giỏ hàng (ObservableCollection<CartItem> _gioHang) chưa
            var itemTrongGio = _gioHang.FirstOrDefault(x => x.IdSanPham == selectedSp.Id);

            if (itemTrongGio != null)
            {
                if (itemTrongGio.SoLuong < selectedSp.SoLuongTon)
                    itemTrongGio.SoLuong++; // Tự động cập nhật UI nhờ INotifyPropertyChanged
                else
                    MessageBox.Show("Số lượng trong giỏ đã đạt giới hạn tồn kho!");
            }
            else
            {
                _gioHang.Add(new CartItem
                {
                    IdSanPham = selectedSp.Id,
                    TenSanPham = selectedSp.TenSanPham,
                    SKU = selectedSp.SKU,
                    DonGia = selectedSp.GiaBan,
                    SoLuong = 1,
                    SoLuongTon = selectedSp.SoLuongTon
                });
            }
            CapNhatTongTien();
        }

        private void CapNhatTongTien()
        {
            double tong = _gioHang.Sum(x => x.ThanhTien);
            txtTongTien.Text = string.Format("{0:N0} đ", tong);
        }
        private async void btnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            if (_gioHang.Count == 0)
            {
                MessageBox.Show("Giỏ hàng đang trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Xác nhận thanh toán đơn hàng này?", "Xác nhận", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            using (var db = new AppDbContext())
            {
                // Kiểm tra nhân viên hiện tại có tồn tại không
                bool coNhanVien = db.NhanViens.Any(n => n.Id == _idNhanVienHienTai);
                if (!coNhanVien)
                {
                    MessageBox.Show($"Thiếu Nhân viên trong SQLite (Id: {_idNhanVienHienTai})!\nVui lòng đồng bộ hoặc thêm nhân viên này trước khi thanh toán.", "Lỗi khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Kiểm tra chi nhánh hiện tại có tồn tại không
                DamBaoChiNhanhHopLeChoHoaDon(db, _chiNhanh);
                bool coChiNhanh = db.ChiNhanhs.Any(cn => cn.MaChiNhanh == _chiNhanh || cn.Id == _chiNhanh);
                if (!coChiNhanh)
                {
                    MessageBox.Show($"Thiếu Chi nhánh trong SQLite (Id: {_chiNhanh})!\nVui lòng đồng bộ hoặc thêm chi nhánh này trước khi thanh toán.", "Lỗi khóa ngoại", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. Lấy số điện thoại từ TextBox hoặc logic tìm khách
                        string sdt = txtSoDienThoai.Text.Trim();

                        // Nếu là placeholder hoặc trống, gán giá trị mặc định cho khách lẻ
                        if (string.IsNullOrEmpty(sdt) || sdt == "Nhập SĐT khách...")
                        {
                            sdt = "0000000000";
                        }
                        // Loại bỏ khoảng trắng định dạng (ví dụ "0908 123 456" -> "0908123456")
                        sdt = new string(sdt.Where(char.IsDigit).ToArray());

                        // 2. Tạo đối tượng hóa đơn
                        // Kiểm tra xem _idKhachHangHienTai có thực sự tồn tại trong DB không
                        var checkKhach = db.KhachHangs.Any(k => k.Id == _idKhachHangHienTai);

                        var hoaDonMoi = new HoaDon
                        {
                            Id = Guid.NewGuid().ToString(),
                            MaHoaDon = "HD" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                            NgayLap = DateTime.UtcNow,
                            TongTien = _gioHang.Sum(x => x.ThanhTien),
                            IdNhanVien = _idNhanVienHienTai,
                            HoTenNguoiBan = _tenNhanVien,
                            MaChiNhanh = _chiNhanh,
                            TrangThaiDongBo = 0,
                            // Nếu khách hàng hiện tại không tồn tại (chưa lưu), ép về KHACH_LE
                            IdKhachHang = checkKhach ? _idKhachHangHienTai : "KHACH_LE",
                            SdtKhachHang = sdt,
                            NgayCapNhat = DateTime.UtcNow,
                            DaXoa = false
                        };


                        db.HoaDons.Add(hoaDonMoi);
                        // Lưu hóa đơn trước để đảm bảo tồn tại khi thêm chi tiết hóa đơn (tránh lỗi FK)
                        await db.SaveChangesAsync();

                        // 2. Xử lý chi tiết và trừ kho
                        foreach (var item in _gioHang)
                        {
                            db.ChiTietHoaDons.Add(new ChiTietHoaDon
                            {
                                Id = Guid.NewGuid().ToString(),
                                IdHoaDon = hoaDonMoi.Id,
                                IdSanPham = item.IdSanPham,
                                SoLuong = item.SoLuong,
                                DonGia = item.DonGia,
                                TenSanPhamLuu = item.TenSanPham,
                                SKULuu = string.IsNullOrWhiteSpace(item.SKU)
                                    ? null
                                    : item.SKU,
                                TrangThaiDongBo = 0,
                                MaChiNhanh = _chiNhanh
                            });

                            var tonKho = db.TonKhoChiNhanhs.FirstOrDefault(tk =>
                                                 tk.IdSanPham == item.IdSanPham &&
                                                 tk.MaChiNhanh == _chiNhanh);

                            if (tonKho != null)
                            {
                                if (tonKho.SoLuong < item.SoLuong)
                                    throw new Exception($"Sản phẩm {item.TenSanPham} đã hết hàng!");

                                tonKho.SoLuong -= item.SoLuong;
                                tonKho.NgayCapNhat = DateTime.UtcNow;
                            }
                        }

                        await db.SaveChangesAsync();
                        transaction.Commit();

                        MessageBox.Show("Thanh toán thành công!", "Thông báo");

                        // Mở cửa sổ in hóa đơn
                        ChiTietHoaDonWindow winIn = new ChiTietHoaDonWindow(hoaDonMoi.Id, true);
                        winIn.Owner = this;
                        winIn.ShowDialog();

                        LàmMoiGiaoDienSauThanhToan();
                        CapNhatTrangThaiDongBo();
                        _ = TuDongDongBo();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        string msg = ex.Message;
                        if (ex.InnerException != null) msg = ex.InnerException.Message;

                        // Đoạn code thám tử:
                        using (var checkDb = new AppDbContext())
                        {
                            bool coNhanVien2 = checkDb.NhanViens.Any(n => n.Id == _idNhanVienHienTai);
                            bool coKhachHang = checkDb.KhachHangs.Any(k => k.Id == _idKhachHangHienTai || k.Id == "KHACH_LE");

                            string huongDan = "Gợi ý sửa lỗi:\n";
                            if (!coNhanVien2) huongDan += "- Thiếu Nhân viên trong SQLite (Id: " + _idNhanVienHienTai + ")\n";
                            if (!coKhachHang) huongDan += "- Thiếu Khách hàng mồi (KHACH_LE)\n";

                            MessageBox.Show($"{huongDan}\nChi tiết lỗi: {msg}", "Phát hiện lỗi khóa ngoại");
                        }
                    }
                }
            }
        }

        private void DamBaoChiNhanhHopLeChoHoaDon(AppDbContext db, string maChiNhanh)
        {
            // Trường hợp chuẩn: đã có bản ghi Id == mã chi nhánh
            if (db.ChiNhanhs.Any(cn => !cn.DaXoa && cn.Id == maChiNhanh))
            {
                return;
            }

            // Trường hợp dữ liệu cũ bị lệch: Id là GUID nhưng MaChiNhanh = HN01
            var chiNhanhTheoMa = db.ChiNhanhs.FirstOrDefault(cn => !cn.DaXoa && cn.MaChiNhanh == maChiNhanh);
            if (chiNhanhTheoMa == null)
            {
                return;
            }

            if (!db.ChiNhanhs.Any(cn => cn.Id == maChiNhanh))
            {
                db.ChiNhanhs.Add(new ChiNhanh
                {
                    Id = maChiNhanh,
                    MaChiNhanh = maChiNhanh,
                    TenChiNhanh = chiNhanhTheoMa.TenChiNhanh,
                    NgayCapNhat = DateTime.UtcNow,
                    TrangThaiDongBo = chiNhanhTheoMa.TrangThaiDongBo,
                    DaXoa = false
                });
            }

            // Ẩn bản ghi lệch để tránh hiển thị trùng chi nhánh trên combobox
            chiNhanhTheoMa.DaXoa = true;
            chiNhanhTheoMa.NgayCapNhat = DateTime.UtcNow;
            db.SaveChanges();
        }

        private void LàmMoiGiaoDienSauThanhToan()
        {
            _gioHang.Clear();
            txtSoDienThoai.Text = "Nhập SĐT khách...";
            txtTenKhachHang.Text = "Khách lẻ (Chưa có thông tin)";
            txtTongTien.Text = "0 đ";
            LoadDanhSachSanPham(); // Để cập nhật lại số lượng tồn kho mới trên bảng bên trái
        }

        private void CapNhatTrangThaiDongBo()
        {
            using (var db = new AppDbContext())
            {
                // Đếm lại chính xác số hóa đơn chưa đồng bộ của chi nhánh hiện tại
                int soLuong = db.HoaDons.AsNoTracking()
                    .Count(h => h.TrangThaiDongBo == 0 && h.MaChiNhanh == App.ChiNhanhHienTai);

                txtSoLuongDongBo.Text = $"Hóa đơn chờ đồng bộ: {soLuong}";

                if (soLuong > 0)
                    txtSoLuongDongBo.Foreground = System.Windows.Media.Brushes.Red;
                else
                    txtSoLuongDongBo.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void txtSoDienThoai_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSoDienThoai.Text == "Nhập SĐT khách...")
            {
                txtSoDienThoai.Text = "";
                txtSoDienThoai.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void btnTimKhach_Click(object sender, RoutedEventArgs e)
        {
            string sdtGoc = txtSoDienThoai.Text.Trim();
            string sdtDeTim = new string(sdtGoc.Where(char.IsDigit).ToArray());
            var regexPhone = new System.Text.RegularExpressions.Regex(@"^(03|05|07|08|09)\d{8}$");

            if (!regexPhone.IsMatch(sdtDeTim))
            {
                MessageBox.Show("Số điện thoại không hợp lệ! (Phải bắt đầu bằng 03, 05, 07, 08, 09 và đủ 10 số)",
                                "Lỗi định dạng", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtSoDienThoai.Focus();
                return;
            }

            if (string.IsNullOrEmpty(sdtDeTim) || sdtGoc == "Nhập SĐT khách...")
            {
                MessageBox.Show("Vui lòng nhập số điện thoại khách hàng!", "Thông báo");
                txtSoDienThoai.Focus();
                return;
            }

            using (var db = new AppDbContext())
            {
                var kh = db.KhachHangs.FirstOrDefault(k => k.SoDienThoai == sdtDeTim);

                if (kh != null)
                {
                    _idKhachHangHienTai = kh.Id;
                    txtTenKhachHang.Text = $"Khách: {kh.HoTen}";
                    txtTenKhachHang.Foreground = System.Windows.Media.Brushes.Blue;
                    txtSoDienThoai.Text = DinhDangSoDienThoai(kh.SoDienThoai);
                }
                else
                {
                    var confirm = MessageBox.Show("Khách hàng này chưa có trong hệ thống. Bạn có muốn thêm mới không?",
                                                  "Khách hàng mới", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (confirm == MessageBoxResult.Yes)
                    {
                        ThemKhachHangWindow win = new ThemKhachHangWindow(sdtDeTim);
                        win.Owner = this;

                        if (win.ShowDialog() == true)
                        {
                            var khMoi = win.KhachHangMoi;
                            if (khMoi != null)
                            {
                                _idKhachHangHienTai = khMoi.Id;
                                txtTenKhachHang.Text = $"Khách: {khMoi.HoTen}";
                                txtTenKhachHang.Foreground = System.Windows.Media.Brushes.Green;
                                txtSoDienThoai.Text = DinhDangSoDienThoai(khMoi.SoDienThoai);
                            }
                        }
                    }
                }
            }
        }

        private void btnLichSu_Click(object sender, RoutedEventArgs e)
        {
            LichSuHoaDonWindow lichSuWin = new LichSuHoaDonWindow();
            lichSuWin.ShowDialog();
        }

        private async Task LamMoiEndpointVaDongBoAsync()
        {
            await LamMoiEndpointAsync();
            await TuDongDongBo();
            await DongBoDanhMuc();
        }

        private async void btnDongBo_Click(object sender, RoutedEventArgs e)
        {
            if (txtStatus.Text.Contains("Offline"))
            {
                MessageBox.Show("Không có kết nối mạng!", "Cảnh báo");
                return;
            }

            try
            {
                btnDongBo.IsEnabled = false;
                btnDongBo.Content = "Đang gửi...";

                await LamMoiEndpointAsync();
                bool thanhCong = await TuDongDongBo();

                if (thanhCong)
                {
                    await DongBoDanhMuc(hienThiCanhBaoLoi: true);
                    MessageBox.Show("Tất cả hóa đơn đã được đồng bộ thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    string chiTiet = string.IsNullOrWhiteSpace(_lastSyncErrorMessage)
                        ? "Có thể do server không phản hồi."
                        : _lastSyncErrorMessage;
                    MessageBox.Show($"Đồng bộ thất bại!\n{chiTiet}", "Thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}");
            }
            finally
            {
                btnDongBo.IsEnabled = true;
                btnDongBo.Content = "Đồng bộ ngay";
                CapNhatTrangThaiDongBo();
            }
        }
        private bool _isSyncing = false;
        private string? _lastSyncErrorMessage;

        private async Task<bool> TuDongDongBo()
        {
            if (_isSyncing)
            {
                Dispatcher.Invoke(() => CapNhatTrangThaiDongBo());
                return false;
            }
            _lastSyncErrorMessage = null;

            bool dangOnline = false;
            Dispatcher.Invoke(() => {
                dangOnline = txtStatus.Text.Contains("Online");
            });

            if (!dangOnline) return false;

            _isSyncing = true;
            try
            {
                using (var db = new AppDbContext())
                {
                    var ds = db.HoaDons
                        .Where(h => h.TrangThaiDongBo == 0 && h.MaChiNhanh == App.ChiNhanhHienTai)
                        .ToList();
                    if (ds.Count == 0) return true;

                    var ketQuaGui = await GuiDuLieuLenServerThat(ds);
                    bool ketQuaServer = ketQuaGui.Success;
                    _lastSyncErrorMessage = ketQuaGui.ErrorMessage;

                    if (ketQuaServer)
                    {
                        foreach (var hd in ds)
                        {
                            hd.TrangThaiDongBo = 1;
                            var chiTietsLocal = db.ChiTietHoaDons.Where(ct => ct.IdHoaDon == hd.Id).ToList();
                            foreach (var ct in chiTietsLocal) ct.TrangThaiDongBo = 1;
                        }
                        await db.SaveChangesAsync();

                        return true; // <-- QUAN TRỌNG: Thêm dòng này để hết lỗi code path
                    }
                    return false; // Trả về false nếu ketQuaServer thất bại
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đồng bộ: " + ex.Message);
                _lastSyncErrorMessage = ex.Message;
                return false;
            }
            finally
            {
                _isSyncing = false;
                Dispatcher.Invoke(() => CapNhatTrangThaiDongBo());
            }
        }
        private void KhoiTaoDuLieuHeThong()
        {
            using (var db = new AppDbContext())
            {
                // 1. Kiểm tra chi nhánh (Phải khớp với ID bạn dùng)
                var cn = db.ChiNhanhs.FirstOrDefault(x => x.MaChiNhanh == "CN_GOC");
                if (cn == null)
                {
                    db.ChiNhanhs.Add(new ChiNhanh { Id = "CN_GOC", MaChiNhanh = "CN_GOC", TenChiNhanh = "Chi nhánh chính" });
                    db.SaveChanges();
                }

                // 2. CẬP NHẬT LẠI CHI NHÁNH CHO NHÂN VIÊN
                var nv = db.NhanViens.Find("ADMIN_001");
                if (nv != null)
                {
                    nv.MaChiNhanh = "CN_GOC"; // Đồng nhất theo mã chi nhánh
                }
                else
                {
                    db.NhanViens.Add(new NhanVien
                    {
                        Id = App.IdNhanVienAdminTong,
                        TaiKhoan = App.TaiKhoanAdminTong,
                        MatKhau = PasswordHasher.Hash("123"),
                        HoTen = "Quản Trị Viên",
                        VaiTro = App.VaiTroAdminHeThong,
                        MaChiNhanh = App.MaChiNhanhTong,
                        MaNhanVien = "NV001"
                    });
                }

                // 3. CẬP NHẬT LẠI CHI NHÁNH CHO KHÁCH LẺ
                var kl = db.KhachHangs.Find("KHACH_LE");
                if (kl != null)
                {
                    kl.MaChiNhanh = "CN_GOC"; // Đồng nhất theo mã chi nhánh
                }
                else
                {
                    db.KhachHangs.Add(new KhachHang { Id = "KHACH_LE", HoTen = "Khách lẻ", MaChiNhanh = "CN_GOC", SoDienThoai = "0000000000" });
                }

                db.SaveChanges();
            }
        }
        private async Task<(bool Success, string? ErrorMessage)> GuiDuLieuLenServerThat(List<HoaDon> dsHoaDon)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var hoaDonIds = dsHoaDon.Select(h => h.Id).ToList();
                    var idSanPhamCanDongBo = db.ChiTietHoaDons
                        .Where(ct => hoaDonIds.Contains(ct.IdHoaDon))
                        .Select(ct => ct.IdSanPham)
                        .Distinct()
                        .ToList();

                    var ketQuaSanPham = await DamBaoSanPhamTonTaiTrenServer(db, idSanPhamCanDongBo);
                    if (!ketQuaSanPham.Success)
                    {
                        return ketQuaSanPham;
                    }

                    var danhSachGoi = new List<object>();

                    foreach (var hd in dsHoaDon)
                    {
                        // Đảm bảo không gửi navigation property lên server
                        var hdSend = new HoaDon
                        {
                            Id = hd.Id,
                            MaHoaDon = hd.MaHoaDon,
                            NgayLap = hd.NgayLap,
                            TongTien = hd.TongTien,
                            SdtKhachHang = hd.SdtKhachHang,
                            IdKhachHang = hd.IdKhachHang,
                            IdNhanVien = hd.IdNhanVien,
                            HoTenNguoiBan = hd.HoTenNguoiBan,
                            MaChiNhanh = hd.MaChiNhanh,
                            TrangThaiDongBo = hd.TrangThaiDongBo,
                            NgayCapNhat = hd.NgayCapNhat,
                            DaXoa = hd.DaXoa
                        };

                        var chiTiets = db.ChiTietHoaDons
                                         .Where(ct => ct.IdHoaDon == hd.Id)
                                         .Select(ct => new ChiTietHoaDon
                                         {
                                             Id = ct.Id,
                                             IdHoaDon = ct.IdHoaDon,
                                             IdSanPham = ct.IdSanPham,
                                             SoLuong = ct.SoLuong,
                                             DonGia = ct.DonGia,
                                             TenSanPhamLuu = ct.TenSanPhamLuu,
                                             SKULuu = ct.SKULuu,
                                             TrangThaiDongBo = ct.TrangThaiDongBo,
                                             MaChiNhanh = ct.MaChiNhanh
                                         })
                                         .ToList();

                        danhSachGoi.Add(new { HoaDon = hdSend, ChiTiets = chiTiets });
                    }

                    var (ok, err) = await _apiService.PostSyncHoaDonsAsync(new { dsGoi = danhSachGoi });
                    if (!ok)
                    {
                        System.Diagnostics.Debug.WriteLine($"Server từ chối: {err}");
                        return (false, $"Server trả về: {err}");
                    }

                    return (true, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi Client: {ex.Message}");
                return (false, $"Lỗi kết nối tới server: {ex.Message}");
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> DamBaoSanPhamTonTaiTrenServer(AppDbContext db, List<string> idSanPhamCanDongBo)
        {
            if (idSanPhamCanDongBo.Count == 0) return (true, null);

            var (layOk, dsSanPhamServer, layErr) = await _apiService.GetSanPhamCatalogFromServerAsync();
            if (!layOk)
                return (false, layErr ?? "Không lấy được danh sách sản phẩm từ server.");

            var idServer = dsSanPhamServer.Select(sp => sp.Id).ToHashSet();

            var dsSanPhamCanDay = db.SanPhams.AsNoTracking()
                .Where(sp => idSanPhamCanDongBo.Contains(sp.Id) && !idServer.Contains(sp.Id))
                .ToList();

            foreach (var sp in dsSanPhamCanDay)
            {
                var (postOk, postErr) = await _apiService.PostSanPhamToCatalogAsync(sp);
                if (!postOk)
                    return (false, $"Không thể đẩy sản phẩm `{sp.TenSanPham}` lên server. Chi tiết: {postErr}");
            }

            return (true, null);
        }

        private async Task DongBoDanhMuc(bool hienThiCanhBaoLoi = false)
        {
            var loiDayLen = new List<string>();
            var loiKeoVe = new List<string>();

            void GhiLoiDayLen(string muc, string? chiTiet)
            {
                if (string.IsNullOrWhiteSpace(chiTiet)) return;
                var dong = $"{muc}: {chiTiet}";
                loiDayLen.Add(dong);
                System.Diagnostics.Debug.WriteLine("Lỗi đẩy " + dong);
            }

            void GhiLoiKeoVe(string muc, string chiTiet)
            {
                if (string.IsNullOrWhiteSpace(chiTiet)) return;
                var dong = $"{muc}: {chiTiet}";
                loiKeoVe.Add(dong);
                System.Diagnostics.Debug.WriteLine("Lỗi kéo " + dong);
            }

            try
            {
                // --- Đẩy lên server: thử hết từng nhóm; một nhóm lỗi không chặn nhóm sau ---
                var ketQuaDayChiNhanh = await DongBoChiNhanhDangChoLenServer();
                if (!ketQuaDayChiNhanh.Success) GhiLoiDayLen("chi nhánh", ketQuaDayChiNhanh.ErrorMessage);

                var ketQuaDayTonKho = await DongBoTonKhoDangChoLenServer();
                if (!ketQuaDayTonKho.Success) GhiLoiDayLen("tồn kho", ketQuaDayTonKho.ErrorMessage);

                var ketQuaDaySanPham = await DongBoSanPhamDangChoLenServer();
                if (!ketQuaDaySanPham.Success) GhiLoiDayLen("sản phẩm", ketQuaDaySanPham.ErrorMessage);

                var ketQuaDayKhach = await DongBoKhachHangDangChoLenServer();
                if (!ketQuaDayKhach.Success) GhiLoiDayLen("khách hàng", ketQuaDayKhach.ErrorMessage);

                var ketQuaDayNv = await DongBoNhanVienDangChoLenServer();
                if (!ketQuaDayNv.Success) GhiLoiDayLen("nhân viên", ketQuaDayNv.ErrorMessage);

                if (loiDayLen.Count > 0)
                    _lastSyncErrorMessage = string.Join(" | ", loiDayLen);

                // --- Kéo về máy: luôn chạy; lỗi một bước không chặn các bước sau ---
                try
                {
                    var dsChiNhanhServer = await _apiService.GetChiNhanhsFromServerAsync();
                    if (dsChiNhanhServer.Count > 0)
                    {
                        using (var db = new AppDbContext())
                        {
                            foreach (var cnServer in dsChiNhanhServer)
                            {
                                var cnLocal = db.ChiNhanhs.FirstOrDefault(c => c.Id == cnServer.Id || c.MaChiNhanh == cnServer.MaChiNhanh);
                                if (cnLocal == null)
                                {
                                    db.ChiNhanhs.Add(cnServer);
                                    continue;
                                }

                                if (cnLocal.TrangThaiDongBo == 0) continue;
                                if (cnServer.NgayCapNhat < cnLocal.NgayCapNhat) continue;

                                cnLocal.MaChiNhanh = cnServer.MaChiNhanh;
                                cnLocal.TenChiNhanh = cnServer.TenChiNhanh;
                                cnLocal.DaXoa = cnServer.DaXoa;
                                cnLocal.NgayCapNhat = cnServer.NgayCapNhat;
                                cnLocal.TrangThaiDongBo = 1;
                            }

                            await db.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    GhiLoiKeoVe("chi nhánh", ex.Message);
                    System.Diagnostics.Debug.WriteLine("Lỗi kéo chi nhánh: " + ex.Message);
                }

                try
                {
                    var dsTonKhoServer = await _apiService.GetTonKhoFromServerAsync();
                    if (dsTonKhoServer.Count > 0)
                    {
                        using (var db = new AppDbContext())
                        {
                            foreach (var tkServer in dsTonKhoServer)
                            {
                                var tkLocal = db.TonKhoChiNhanhs.FirstOrDefault(t =>
                                    t.Id == tkServer.Id || (t.IdSanPham == tkServer.IdSanPham && t.MaChiNhanh == tkServer.MaChiNhanh));

                                if (tkLocal == null)
                                {
                                    db.TonKhoChiNhanhs.Add(tkServer);
                                    continue;
                                }

                                if (tkLocal.TrangThaiDongBo == 0) continue;
                                if (tkServer.NgayCapNhat < tkLocal.NgayCapNhat) continue;

                                tkLocal.IdSanPham = tkServer.IdSanPham;
                                tkLocal.MaChiNhanh = tkServer.MaChiNhanh;
                                tkLocal.SoLuong = tkServer.SoLuong;
                                tkLocal.DaXoa = tkServer.DaXoa;
                                tkLocal.NgayCapNhat = tkServer.NgayCapNhat;
                                tkLocal.TrangThaiDongBo = 1;
                            }

                            await db.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    GhiLoiKeoVe("tồn kho", ex.Message);
                    System.Diagnostics.Debug.WriteLine("Lỗi kéo tồn kho: " + ex.Message);
                }

                try
                {
                    var dsKhachServer = await _apiService.GetKhachHangsFromServerAsync();
                    if (dsKhachServer.Count > 0)
                    {
                        using (var db = new AppDbContext())
                        {
                            foreach (var khServer in dsKhachServer)
                            {
                                var khLocal = db.KhachHangs.FirstOrDefault(k => k.Id == khServer.Id);
                                if (khLocal == null)
                                {
                                    db.KhachHangs.Add(khServer);
                                    continue;
                                }

                                if (khLocal.TrangThaiDongBo == 0) continue;
                                if (khServer.NgayCapNhat < khLocal.NgayCapNhat) continue;

                                khLocal.HoTen = khServer.HoTen;
                                khLocal.SoDienThoai = khServer.SoDienThoai;
                                khLocal.DiaChi = khServer.DiaChi;
                                khLocal.DiemTichLuy = khServer.DiemTichLuy;
                                khLocal.MaChiNhanh = khServer.MaChiNhanh;
                                khLocal.DaXoa = khServer.DaXoa;
                                khLocal.NgayCapNhat = khServer.NgayCapNhat;
                                khLocal.TrangThaiDongBo = 1;
                            }

                            await db.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    GhiLoiKeoVe("khách hàng", ex.Message);
                    System.Diagnostics.Debug.WriteLine("Lỗi kéo khách hàng: " + ex.Message);
                }

                try
                {
                    await MergeNhanViensTuServerVaoMayAsync();
                }
                catch (Exception ex)
                {
                    GhiLoiKeoVe("nhân viên", ex.Message);
                    System.Diagnostics.Debug.WriteLine("Lỗi kéo nhân viên: " + ex.Message);
                }

                try
                {
                    var (spOk, dsSanPhamServer, spErr) = await _apiService.GetSanPhamsSyncFromServerAsync();
                    if (!spOk)
                    {
                        GhiLoiKeoVe("sản phẩm (HTTP)", spErr ?? "Không kéo được sản phẩm từ server.");
                    }
                    else if (dsSanPhamServer.Count > 0)
                    {
                        using (var db = new AppDbContext())
                        {
                            foreach (var spServer in dsSanPhamServer)
                            {
                                var spLocal = db.SanPhams.FirstOrDefault(x => x.Id == spServer.Id);

                                if (spLocal == null)
                                {
                                    db.SanPhams.Add(spServer);
                                }
                                else
                                {
                                    if (spLocal.TrangThaiDongBo == 0) continue;

                                    spLocal.TenSanPham = spServer.TenSanPham;
                                    spLocal.GiaBan = spServer.GiaBan;
                                    spLocal.MaGoc = spServer.MaGoc;
                                    spLocal.KichCo = spServer.KichCo;
                                    spLocal.MauSac = spServer.MauSac;
                                    spLocal.DaXoa = spServer.DaXoa;
                                    spLocal.NgayCapNhat = DateTime.UtcNow;
                                }
                            }

                            await db.SaveChangesAsync();
                        }

                        Dispatcher.Invoke(() => LoadDanhSachSanPham());
                    }
                }
                catch (Exception ex)
                {
                    GhiLoiKeoVe("sản phẩm", ex.Message);
                    System.Diagnostics.Debug.WriteLine("Lỗi kéo sản phẩm: " + ex.Message);
                }

                try
                {
                    await KeoHoaDonTuServerTheoChiNhanhAsync();
                }
                catch (Exception ex)
                {
                    GhiLoiKeoVe("hóa đơn", ex.Message);
                    System.Diagnostics.Debug.WriteLine("Lỗi kéo hóa đơn: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                GhiLoiKeoVe("đồng bộ danh mục", ex.Message);
                System.Diagnostics.Debug.WriteLine("Lỗi đồng bộ danh mục: " + ex.Message);
            }
            finally
            {
                if (hienThiCanhBaoLoi && (loiDayLen.Count > 0 || loiKeoVe.Count > 0))
                {
                    string msg = "";
                    if (loiDayLen.Count > 0)
                    {
                        msg += "Không đẩy được một số dữ liệu lên server:\n";
                        msg += string.Join("\n", loiDayLen.Select(x => "• " + x));
                    }

                    if (loiKeoVe.Count > 0)
                    {
                        if (msg.Length > 0) msg += "\n\n";
                        msg += "Không kéo được một số dữ liệu về máy:\n";
                        msg += string.Join("\n", loiKeoVe.Select(x => "• " + x));
                    }

                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(msg.TrimEnd(), "Cảnh báo đồng bộ danh mục", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }
            }
        }

        private async Task MergeNhanViensTuServerVaoMayAsync()
        {
            var dsServer = await _apiService.GetNhanViensFromServerAsync();
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

                if (nvLocal.TrangThaiDongBo == 0) continue;
                if (nvServer.NgayCapNhat < nvLocal.NgayCapNhat) continue;

                bool laAdminTong = App.LaTaiKhoanAdminTong(nvServer.Id, nvServer.TaiKhoan);
                nvLocal.HoTen = nvServer.HoTen;
                if (!string.IsNullOrWhiteSpace(nvServer.MatKhau))
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

        private async Task KeoHoaDonTuServerTheoChiNhanhAsync()
        {
            // NV: chỉ chi nhánh đang làm việc. Admin / QL (báo cáo toàn hệ): kéo tất cả — API không lọc khi maChiNhanh == null.
            string? maLoc = App.CoQuyenQuanLyCapCao(_vaiTro) ? null : App.ChiNhanhHienTai;

            var dsGoi = await _apiService.GetHoaDonsForDongBoAsync(maLoc);
            if (dsGoi.Count == 0) return;

            using var db = new AppDbContext();
            foreach (var goi in dsGoi)
            {
                if (goi?.HoaDon == null) continue;
                var s = goi.HoaDon;

                var hdLocal = db.HoaDons.FirstOrDefault(h => h.Id == s.Id);
                if (hdLocal == null)
                {
                    s.ChiTiets = null;
                    db.HoaDons.Add(s);
                    if (goi.ChiTiets != null)
                    {
                        foreach (var ct in goi.ChiTiets)
                        {
                            ct.HoaDon = null;
                            db.ChiTietHoaDons.Add(ct);
                        }
                    }

                    continue;
                }

                if (hdLocal.TrangThaiDongBo == 0) continue;
                if (s.NgayCapNhat < hdLocal.NgayCapNhat) continue;

                hdLocal.MaHoaDon = s.MaHoaDon;
                hdLocal.NgayLap = s.NgayLap;
                hdLocal.TongTien = s.TongTien;
                hdLocal.SdtKhachHang = s.SdtKhachHang;
                hdLocal.HoTenNguoiBan = s.HoTenNguoiBan;
                hdLocal.IdKhachHang = s.IdKhachHang;
                hdLocal.IdNhanVien = s.IdNhanVien;
                hdLocal.MaChiNhanh = s.MaChiNhanh;
                hdLocal.TrangThaiDongBo = 1;
                hdLocal.NgayCapNhat = s.NgayCapNhat;
                hdLocal.DaXoa = s.DaXoa;

                var olds = db.ChiTietHoaDons.Where(c => c.IdHoaDon == hdLocal.Id).ToList();
                db.ChiTietHoaDons.RemoveRange(olds);

                if (goi.ChiTiets != null)
                {
                    foreach (var ct in goi.ChiTiets)
                    {
                        ct.HoaDon = null;
                        db.ChiTietHoaDons.Add(ct);
                    }
                }
            }

            await db.SaveChangesAsync();

            Dispatcher.Invoke(CapNhatTrangThaiDongBo);
        }

        private async Task<(bool Success, string? ErrorMessage)> DongBoNhanVienDangChoLenServer()
        {
            try
            {
                using var db = new AppDbContext();
                var ds = db.NhanViens.Where(n => n.TrangThaiDongBo == 0).ToList();
                if (ds.Count == 0) return (true, null);

                var (ok, err) = await _apiService.UpsertNhanViensOnServerAsync(ds);
                if (!ok) return (false, err);

                foreach (var nv in ds)
                {
                    nv.TrangThaiDongBo = 1;
                    nv.NgayCapNhat = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> DongBoChiNhanhDangChoLenServer()
        {
            try
            {
                using var db = new AppDbContext();
                var ds = db.ChiNhanhs.Where(c => c.TrangThaiDongBo == 0).ToList();
                if (ds.Count == 0) return (true, null);

                var (ok, err) = await _apiService.UpsertChiNhanhsOnServerAsync(ds);
                if (!ok) return (false, err);

                foreach (var cn in ds)
                {
                    cn.TrangThaiDongBo = 1;
                    cn.NgayCapNhat = DateTime.UtcNow;
                }
                await db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> DongBoTonKhoDangChoLenServer()
        {
            try
            {
                using var db = new AppDbContext();
                var ds = db.TonKhoChiNhanhs.Where(t => t.TrangThaiDongBo == 0).ToList();
                if (ds.Count == 0) return (true, null);

                var (ok, err) = await _apiService.UpsertTonKhoOnServerAsync(ds);
                if (!ok) return (false, err);

                foreach (var tk in ds)
                {
                    tk.TrangThaiDongBo = 1;
                    tk.NgayCapNhat = DateTime.UtcNow;
                }
                await db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> DongBoKhachHangDangChoLenServer()
        {
            try
            {
                using var db = new AppDbContext();
                var ds = db.KhachHangs.Where(k => k.TrangThaiDongBo == 0).ToList();
                if (ds.Count == 0) return (true, null);

                var (ok, err) = await _apiService.UpsertKhachHangsOnServerAsync(ds);
                if (!ok) return (false, err);

                foreach (var kh in ds)
                {
                    kh.TrangThaiDongBo = 1;
                    kh.NgayCapNhat = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<(bool Success, string? ErrorMessage)> DongBoSanPhamDangChoLenServer()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var dsSanPhamChoDongBo = db.SanPhams
                        .Where(sp => sp.TrangThaiDongBo == 0)
                        .ToList();

                    if (dsSanPhamChoDongBo.Count == 0) return (true, null);

                    foreach (var sp in dsSanPhamChoDongBo)
                    {
                        var (postOk, postErr) = await _apiService.PostSanPhamToCatalogAsync(sp);
                        if (!postOk)
                            return (false, $"Không đồng bộ được sản phẩm {sp.TenSanPham}: {postErr}");

                        sp.TrangThaiDongBo = 1;
                        sp.NgayCapNhat = DateTime.UtcNow;
                    }

                    await db.SaveChangesAsync();
                    return (true, null);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private void btnQuanLyNV_Click(object sender, RoutedEventArgs e)
        {
            if (!YeuCauQuyenQuanLy("quản lý nhân viên")) return;

            QuanLyNhanVienWindow qlNV = new QuanLyNhanVienWindow(_chiNhanh);
            qlNV.ShowDialog();
        }

        private void btnDoiMatKhau_Click(object sender, RoutedEventArgs e)
        {
            DoiMatKhauWindow doiMkWin = new DoiMatKhauWindow(App.TaiKhoanHienTai);
            doiMkWin.ShowDialog();
        }

        private void btnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            var ketQua = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ketQua == MessageBoxResult.Yes)
            {
                App.TaiKhoanHienTai = string.Empty;
                App.VaiTro = string.Empty;
                App.ChiNhanhHienTai = string.Empty;
                LoginWindow loginWin = new LoginWindow();
                loginWin.Show();
                this.Close();
            }
        }

        private void btnNhapHang_Click(object sender, RoutedEventArgs e)
        {
            if (!YeuCauQuyenQuanLy("thêm/sửa sản phẩm")) return;

            NhapHangWindow nhapHangWin = new NhapHangWindow();
            nhapHangWin.ShowDialog();
            LoadDanhSachSanPham();
        }

        private void txtTimKiem_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtTimKiem.Text == "Nhập mã hoặc tên sản phẩm...")
            {
                txtTimKiem.Text = "";
                txtTimKiem.FontStyle = FontStyles.Normal;
                txtTimKiem.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void txtTimKiem_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTimKiem.Text))
            {
                txtTimKiem.Text = "Nhập mã hoặc tên sản phẩm...";
                txtTimKiem.FontStyle = FontStyles.Italic;
                txtTimKiem.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            string key = txtTimKiem.Text.Trim().ToLower();
            if (key == "nhập mã hoặc tên sản phẩm...") return;

            using (var db = new AppDbContext())
            {
                var query = from sp in db.SanPhams.AsNoTracking()
                            where !sp.DaXoa && (
                                sp.TenSanPham.ToLower().Contains(key) ||
                                sp.MaGoc.ToLower().Contains(key) ||
                                (sp.KichCo ?? "").ToLower().Contains(key) ||
                                (sp.MauSac ?? "").ToLower().Contains(key))
                            join tk in db.TonKhoChiNhanhs.Where(t => t.MaChiNhanh == App.ChiNhanhHienTai)
                            on sp.Id equals tk.IdSanPham into tkGroup
                            from tk in tkGroup.DefaultIfEmpty()
                            select new SanPhamHienThi
                            {
                                Id = sp.Id,
                                SKU = sp.SKU,
                                TenSanPham = sp.TenSanPham,
                                GiaBan = sp.GiaBan,
                                SoLuongTon = tk != null ? tk.SoLuong : 0
                            };
                dgSanPham.ItemsSource = query.ToList();
            }
        }
        private void btnTimKiem_Click(object sender, RoutedEventArgs e)
        {
            txtTimKiem_TextChanged(sender, e as TextChangedEventArgs ?? new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
        }

        private void btnTangSL_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: CartItem item })
            {
                if (item.SoLuong < item.SoLuongTon)
                {
                    item.SoLuong++;
                    CapNhatTongTien();
                }
                else
                {
                    MessageBox.Show("Đã đạt giới hạn tồn kho!");
                }
            }
        }

        private void btnGiamSL_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: CartItem item } && item.SoLuong > 1)
            {
                item.SoLuong--;
                CapNhatTongTien();
            }
        }

        private void btnXoaDong_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is CartItem item)
            {
                _gioHang.Remove(item);
                CapNhatTongTien();
            }
        }

        private string DinhDangSoDienThoai(string input)
        {

            string digits = new string(input.Where(char.IsDigit).ToArray());


            if (digits.Length > 0 && digits[0] != '0')
            {
                digits = "";
            }

            if (digits.Length > 10) digits = digits.Substring(0, 10);

            if (digits.Length <= 4)
                return digits;
            else if (digits.Length <= 7)
                return digits.Insert(4, " ");
            else
                return digits.Insert(4, " ").Insert(8, " ");

        }

        private void txtSoDienThoai_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Tránh vòng lặp vô tận khi gán lại Text
            txtSoDienThoai.TextChanged -= txtSoDienThoai_TextChanged;

            int selectionStart = txtSoDienThoai.SelectionStart;
            string originalText = txtSoDienThoai.Text;

            // Chỉ lấy các ký số
            string cleaned = new string(originalText.Where(char.IsDigit).ToArray());

            // Định dạng đơn giản: 0908 123 456
            string formatted = cleaned;
            if (cleaned.Length > 4 && cleaned.Length <= 7)
                formatted = cleaned.Insert(4, " ");
            else if (cleaned.Length > 7)
                formatted = cleaned.Insert(4, " ").Insert(8, " ");

            if (originalText != formatted)
            {
                txtSoDienThoai.Text = formatted;

                // Tính toán lại vị trí con trỏ (quan trọng)
                int newPosition = selectionStart + (formatted.Length - originalText.Length);
                txtSoDienThoai.SelectionStart = Math.Max(0, Math.Min(newPosition, formatted.Length));
            }

            txtSoDienThoai.TextChanged += txtSoDienThoai_TextChanged;
        }

        private void btnBaoCao_Click(object sender, RoutedEventArgs e)
        {
            if (!YeuCauQuyenQuanLy("xem báo cáo")) return;

            BaoCaoDoanhThuWindow baoCaoWin = new BaoCaoDoanhThuWindow();
            baoCaoWin.Owner = this;

            if (txtStatus.Text.Contains("Offline"))
            {
                baoCaoWin.brdCanhBaoOffline.Visibility = Visibility.Visible;
            }

            baoCaoWin.ShowDialog();
        }
        // Class phụ trợ để gộp dữ liệu Sản Phẩm và Tồn Kho hiển thị lên DataGrid
        public class SanPhamHienThi
        {
            public string Id { get; set; } = string.Empty;
            public string SKU { get; set; } = string.Empty;
            public string TenSanPham { get; set; } = string.Empty;
            public string KichCo { get; set; } = string.Empty;
            public string MauSac { get; set; } = string.Empty;
            public double GiaBan { get; set; }
            public int SoLuongTon { get; set; }
        }
        // Thêm vào MainWindow.xaml.cs
        private void btnQuanLyChiNhanh_Click(object sender, RoutedEventArgs e)
        {
            if (!YeuCauQuyenQuanLy("quản lý chi nhánh")) return;

            QuanLyChiNhanhWindow qlCN = new QuanLyChiNhanhWindow();
            qlCN.Owner = this;
            qlCN.ShowDialog();
        }

    }
}
