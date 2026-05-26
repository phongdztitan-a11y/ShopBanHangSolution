using System;

using System.IO;

using System.Net.Http;

using System.Text.Json;

using System.Threading.Tasks;



namespace ShopBanHang_OfflineFirst.Services

{

    /// <summary>Đọc URL API từ file cạnh .exe (đổi ngrok/LAN không cần build lại).</summary>

    public static class ApiEndpointConfig

    {

        private const string DefaultBaseUrl = "http://localhost:5191/api/";



        public static string AppBaseDirectory => AppDomain.CurrentDomain.BaseDirectory;



        public static string ServerUrlPath => Path.Combine(AppBaseDirectory, "server.url");

        public static string DiscoveryUrlPath => Path.Combine(AppBaseDirectory, "discovery.url");



        public static string ResolveBaseUrl()

        {

            string? fromUrlFile = ReadFirstNonCommentLine(ServerUrlPath);

            if (!string.IsNullOrWhiteSpace(fromUrlFile))

                return NormalizeApiBaseUrl(fromUrlFile);



            string? fromAppSettings = ReadAppSettings(Path.Combine(AppBaseDirectory, "appsettings.json"));

            if (!string.IsNullOrWhiteSpace(fromAppSettings))

                return NormalizeApiBaseUrl(fromAppSettings);



            return DefaultBaseUrl;

        }



        /// <summary>

        /// Nếu có discovery.url (URL cố định, không phải ngrok), tải JSON/text để lấy apiBaseUrl mới

        /// và ghi vào server.url — dùng khi ngrok free đổi subdomain.

        /// </summary>

        public static async Task<(bool Ok, string? BaseUrl, string? Error)> TryRefreshFromDiscoveryAsync()

        {

            string? discoveryUrl = ReadFirstNonCommentLine(DiscoveryUrlPath);

            if (string.IsNullOrWhiteSpace(discoveryUrl))

                return (false, null, null);



            try

            {

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

                string body = await http.GetStringAsync(discoveryUrl.Trim());

                string? apiUrl = ParseDiscoveryPayload(body);

                if (string.IsNullOrWhiteSpace(apiUrl))

                    return (false, null, "discovery.url không trả về apiBaseUrl hợp lệ.");



                apiUrl = NormalizeApiBaseUrl(apiUrl);

                File.WriteAllText(ServerUrlPath, apiUrl + Environment.NewLine);

                return (true, apiUrl, null);

            }

            catch (Exception ex)

            {

                return (false, null, ex.Message);

            }

        }



        public static void SaveServerUrl(string url)

        {

            File.WriteAllText(ServerUrlPath, NormalizeApiBaseUrl(url) + Environment.NewLine);

        }



        public static void SaveDiscoveryUrl(string url)

        {

            File.WriteAllText(DiscoveryUrlPath, url.Trim() + Environment.NewLine);

        }



        public static bool ShouldUseNgrokHeaders(string baseUrl) =>

            baseUrl.Contains("ngrok", StringComparison.OrdinalIgnoreCase);



        private static string? ReadFirstNonCommentLine(string path)

        {

            if (!File.Exists(path)) return null;



            foreach (var rawLine in File.ReadAllLines(path))

            {

                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//"))

                    continue;

                return line;

            }



            return null;

        }



        private static string? ParseDiscoveryPayload(string body)

        {

            body = body.Trim();

            if (body.Length == 0) return null;



            if (body.StartsWith("{", StringComparison.Ordinal))

            {

                try

                {

                    using var doc = JsonDocument.Parse(body);

                    var root = doc.RootElement;

                    foreach (var name in new[] { "apiBaseUrl", "ApiBaseUrl", "url", "Url", "endpoint" })

                    {

                        if (root.TryGetProperty(name, out var prop) &&

                            prop.ValueKind == JsonValueKind.String)

                        {

                            var s = prop.GetString();

                            if (!string.IsNullOrWhiteSpace(s)) return s;

                        }

                    }

                }

                catch

                {

                    return null;

                }



                return null;

            }



            return body.Split('\n', '\r')[0].Trim();

        }



        private static string? ReadAppSettings(string path)

        {

            if (!File.Exists(path)) return null;



            try

            {

                using var doc = JsonDocument.Parse(File.ReadAllText(path));

                var root = doc.RootElement;



                if (root.TryGetProperty("ApiBaseUrl", out var direct) &&

                    direct.ValueKind == JsonValueKind.String)

                    return direct.GetString();



                if (root.TryGetProperty("Api", out var api) &&

                    api.TryGetProperty("BaseUrl", out var nested) &&

                    nested.ValueKind == JsonValueKind.String)

                    return nested.GetString();

            }

            catch

            {

                // Bỏ qua file JSON lỗi, dùng mặc định hoặc server.url

            }



            return null;

        }



        /// <summary>Luôn trả về dạng https://host/api/</summary>

        public static string NormalizeApiBaseUrl(string url)

        {

            url = url.Trim();

            if (url.Length == 0) return DefaultBaseUrl;



            if (!url.EndsWith('/'))

                url += "/";



            if (url.Contains("/api/", StringComparison.OrdinalIgnoreCase))

                return url;



            if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))

                return url + "/";



            return url.TrimEnd('/') + "/api/";

        }

    }

}


