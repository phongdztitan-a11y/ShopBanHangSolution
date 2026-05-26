using ShopBanHang.Shared;
using ShopBanHang.Shared.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShopBanHang_OfflineFirst.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private string _baseUrl;

        /// <summary>URL API đang dùng (đọc từ server.url hoặc appsettings.json cạnh .exe).</summary>
        public string BaseUrl => _baseUrl;

        private static readonly JsonSerializerOptions JsonInsensitive = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions SyncHoaDonJsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            PropertyNamingPolicy = null
        };

        public ApiService()
        {
            _baseUrl = ApiEndpointConfig.ResolveBaseUrl();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            ApplyNgrokHeadersIfNeeded();
        }

        /// <summary>Cập nhật URL sau khi đọc discovery.url hoặc sửa server.url.</summary>
        public void UpdateBaseUrl(string baseUrl)
        {
            _baseUrl = ApiEndpointConfig.NormalizeApiBaseUrl(baseUrl);
            ApplyNgrokHeadersIfNeeded();
        }

        /// <summary>Thử tải URL mới từ discovery.url; trả về URL đang dùng.</summary>
        public async Task<string> RefreshEndpointAsync()
        {
            var (ok, url, _) = await ApiEndpointConfig.TryRefreshFromDiscoveryAsync();
            if (ok && !string.IsNullOrWhiteSpace(url))
                UpdateBaseUrl(url);
            else
                UpdateBaseUrl(ApiEndpointConfig.ResolveBaseUrl());

            return _baseUrl;
        }

        private void ApplyNgrokHeadersIfNeeded()
        {
            _httpClient.DefaultRequestHeaders.Remove("ngrok-skip-browser-warning");
            if (ApiEndpointConfig.ShouldUseNgrokHeaders(_baseUrl))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(
                    "ngrok-skip-browser-warning", "true");
            }
        }

        public async Task<(bool Ok, string? ErrorMessage)> PostSyncHoaDonsAsync(object dongBoWrapper)
        {
            try
            {
                var json = JsonSerializer.Serialize(dongBoWrapper, SyncHoaDonJsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Sync/PostHoaDon", content);

                if (response.IsSuccessStatusCode)
                    return (true, null);

                string error = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(error) ? response.ReasonPhrase : error);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>Danh mục sản phẩm qua controller SanPham (CRUD catalog).</summary>
        public async Task<(bool Ok, List<SanPham> Data, string? ErrorMessage)> GetSanPhamCatalogFromServerAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}SanPham");
                if (!response.IsSuccessStatusCode)
                    return (false, new List<SanPham>(), "Không lấy được danh sách sản phẩm từ server.");

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<List<SanPham>>(json, JsonInsensitive) ?? new List<SanPham>();
                return (true, data, null);
            }
            catch (Exception ex)
            {
                return (false, new List<SanPham>(), ex.Message);
            }
        }

        public async Task<(bool Ok, string? ErrorMessage)> PostSanPhamToCatalogAsync(SanPham sp)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}SanPham", sp);
                if (response.IsSuccessStatusCode)
                    return (true, null);

                string body = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>Đồng bộ danh mục sản phẩm qua Sync/GetSanPhams.</summary>
        public async Task<(bool Ok, List<SanPham> Data, string? ErrorMessage)> GetSanPhamsSyncFromServerAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}Sync/GetSanPhams");
                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    return (false, new List<SanPham>(),
                        $"{(int)response.StatusCode} {response.ReasonPhrase} {body}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<List<SanPham>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.IgnoreCycles
                }) ?? new List<SanPham>();
                return (true, data, null);
            }
            catch (Exception ex)
            {
                return (false, new List<SanPham>(), ex.Message);
            }
        }

        public async Task<List<SanPham>> GetSanPhamsAsync() =>
            (await GetSanPhamCatalogFromServerAsync()).Data;

        public async Task<bool> PostSanPhamAsync(SanPham sp) =>
            (await PostSanPhamToCatalogAsync(sp)).Ok;

        public async Task<(bool Ok, string? ErrorMessage)> DeleteNhanViensOnServerAsync(IReadOnlyList<string> ids)
        {
            if (ids == null || ids.Count == 0)
                return (true, null);

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}Sync/DeleteNhanViens", new { ids });
                if (response.IsSuccessStatusCode)
                    return (true, null);

                string body = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool Ok, string? ErrorMessage)> UpsertNhanViensOnServerAsync(IReadOnlyList<NhanVien> nhanViens)
        {
            if (nhanViens == null || nhanViens.Count == 0)
                return (true, null);

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}Sync/UpsertNhanViens", new { nhanViens });
                if (response.IsSuccessStatusCode)
                    return (true, null);

                string body = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<List<NhanVien>> GetNhanViensFromServerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<NhanVien>>($"{_baseUrl}Sync/GetNhanViens") ?? new List<NhanVien>();
            }
            catch
            {
                return new List<NhanVien>();
            }
        }

        public async Task<(bool Ok, string? ErrorMessage)> UpsertChiNhanhsOnServerAsync(IReadOnlyList<ChiNhanh> chiNhanhs)
        {
            if (chiNhanhs == null || chiNhanhs.Count == 0)
                return (true, null);

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}Sync/UpsertChiNhanhs", new { chiNhanhs });
                if (response.IsSuccessStatusCode) return (true, null);
                string body = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<List<ChiNhanh>> GetChiNhanhsFromServerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ChiNhanh>>($"{_baseUrl}Sync/GetChiNhanhs") ?? new List<ChiNhanh>();
            }
            catch
            {
                return new List<ChiNhanh>();
            }
        }

        public async Task<(bool Ok, string? ErrorMessage)> UpsertTonKhoOnServerAsync(IReadOnlyList<TonKhoChiNhanh> tonKhos)
        {
            if (tonKhos == null || tonKhos.Count == 0)
                return (true, null);

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}Sync/UpsertTonKhoChiNhanhs", new { tonKhos });
                if (response.IsSuccessStatusCode) return (true, null);
                string body = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<List<TonKhoChiNhanh>> GetTonKhoFromServerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TonKhoChiNhanh>>($"{_baseUrl}Sync/GetTonKhoChiNhanhs") ?? new List<TonKhoChiNhanh>();
            }
            catch
            {
                return new List<TonKhoChiNhanh>();
            }
        }

        public async Task<(bool Ok, string? ErrorMessage)> UpsertKhachHangsOnServerAsync(IReadOnlyList<KhachHang> khachHangs)
        {
            if (khachHangs == null || khachHangs.Count == 0)
                return (true, null);

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}Sync/UpsertKhachHangs", new { khachHangs });
                if (response.IsSuccessStatusCode) return (true, null);
                string body = await response.Content.ReadAsStringAsync();
                return (false, string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : body);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<List<KhachHang>> GetKhachHangsFromServerAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<KhachHang>>($"{_baseUrl}Sync/GetKhachHangs") ?? new List<KhachHang>();
            }
            catch
            {
                return new List<KhachHang>();
            }
        }

        public async Task<List<GoiDongBoHoaDonServer>> GetHoaDonsForDongBoAsync(string? maChiNhanh)
        {
            try
            {
                string url = $"{_baseUrl}Sync/GetHoaDonsForDongBo";
                if (!string.IsNullOrWhiteSpace(maChiNhanh))
                    url += "?maChiNhanh=" + Uri.EscapeDataString(maChiNhanh);

                return await _httpClient.GetFromJsonAsync<List<GoiDongBoHoaDonServer>>(url)
                       ?? new List<GoiDongBoHoaDonServer>();
            }
            catch
            {
                return new List<GoiDongBoHoaDonServer>();
            }
        }

        public async Task<OnlineLoginResult> LoginNhanVienOnlineAsync(string taiKhoan, string matKhau)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}Sync/LoginNhanVien", new
                {
                    taiKhoan,
                    matKhau
                });

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<LoginNhanVienResponse>();
                    if (body?.NhanVien != null) return OnlineLoginResult.Success(body.NhanVien);
                    return OnlineLoginResult.NetworkError("Server không trả dữ liệu tài khoản.");
                }

                if ((int)response.StatusCode == 401 || (int)response.StatusCode == 400)
                    return OnlineLoginResult.InvalidCredential();

                string err = await response.Content.ReadAsStringAsync();
                return OnlineLoginResult.NetworkError(string.IsNullOrWhiteSpace(err) ? response.ReasonPhrase : err);
            }
            catch (Exception ex)
            {
                return OnlineLoginResult.NetworkError(ex.Message);
            }
        }
    }

    public class LoginNhanVienResponse
    {
        public bool Success { get; set; }
        public NhanVien? NhanVien { get; set; }
    }

    public class OnlineLoginResult
    {
        public bool IsSuccess { get; private set; }
        public bool IsInvalidCredential { get; private set; }
        public NhanVien? User { get; private set; }
        public string? ErrorMessage { get; private set; }

        public static OnlineLoginResult Success(NhanVien user) =>
            new OnlineLoginResult { IsSuccess = true, User = user };

        public static OnlineLoginResult InvalidCredential() =>
            new OnlineLoginResult { IsInvalidCredential = true };

        public static OnlineLoginResult NetworkError(string? message) =>
            new OnlineLoginResult { ErrorMessage = message };
    }
}
