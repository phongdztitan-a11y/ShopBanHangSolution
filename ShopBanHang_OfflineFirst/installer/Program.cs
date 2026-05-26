using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace ShopBanHang.Setup;

internal static class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== ShopBanHang - Cài đặt client (không cần .NET trên máy đích) ===\n");

        try
        {
            string zipPath = ResolveClientZipPath();
            if (zipPath == null)
            {
                Console.WriteLine("Không tìm thấy gói client (client.zip).");
                Console.WriteLine("Chạy build-setup.ps1 trên máy dev để tạo setup.exe đầy đủ.");
                Pause();
                return 1;
            }

            string defaultDest = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "ShopBanHang");
            Console.Write($"Thư mục cài đặt [{defaultDest}]: ");
            string? destInput = Console.ReadLine();
            string dest = string.IsNullOrWhiteSpace(destInput) ? defaultDest : destInput.Trim();

            Console.WriteLine("\nChọn cách kết nối server trên máy A:");
            Console.WriteLine("  1) URL ngrok/API trực tiếp (dán link hiện tại, ví dụ https://xxx.ngrok-free.dev/api/)");
            Console.WriteLine("  2) URL discovery cố định (khuyên dùng khi ngrok free hay đổi subdomain)");
            Console.WriteLine("  3) Cả hai (discovery + URL dự phòng)");
            Console.Write("Lựa chọn [1]: ");
            string choice = Console.ReadLine()?.Trim() ?? "1";

            string? serverUrl = null;
            string? discoveryUrl = null;

            if (choice is "2" or "3")
            {
                Console.Write("\nURL discovery (JSON chứa apiBaseUrl, URL không đổi): ");
                discoveryUrl = Console.ReadLine()?.Trim();
            }

            if (choice is "1" or "3")
            {
                Console.Write("\nURL server API (ngrok hoặc LAN, có thể bỏ trống nếu chỉ dùng discovery): ");
                serverUrl = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrWhiteSpace(serverUrl) && string.IsNullOrWhiteSpace(discoveryUrl))
            {
                Console.WriteLine("Cần ít nhất một URL.");
                Pause();
                return 1;
            }

            if (Directory.Exists(dest))
            {
                Console.Write($"\nThư mục đã tồn tại. Ghi đè? (y/N): ");
                if (!Console.ReadLine()?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) ?? true)
                {
                    Console.WriteLine("Đã hủy.");
                    Pause();
                    return 0;
                }
                Directory.Delete(dest, true);
            }

            Directory.CreateDirectory(dest);
            Console.WriteLine($"\nĐang giải nén vào {dest} ...");
            ZipFile.ExtractToDirectory(zipPath, dest, true);

            if (!string.IsNullOrWhiteSpace(serverUrl))
            {
                serverUrl = NormalizeApiUrl(serverUrl);
                File.WriteAllText(Path.Combine(dest, "server.url"), serverUrl + Environment.NewLine);
                Console.WriteLine($"  -> server.url");
            }

            if (!string.IsNullOrWhiteSpace(discoveryUrl))
            {
                File.WriteAllText(Path.Combine(dest, "discovery.url"), discoveryUrl.Trim() + Environment.NewLine);
                Console.WriteLine($"  -> discovery.url");
            }

            string? exe = Directory.GetFiles(dest, "ShopBanHang_OfflineFirst.exe", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (exe == null)
                exe = Directory.GetFiles(dest, "*.exe").FirstOrDefault(f =>
                    !f.Contains("setup", StringComparison.OrdinalIgnoreCase));

            CreateDesktopShortcut(exe);

            Console.WriteLine("\n=== Cài đặt xong ===");
            if (!string.IsNullOrWhiteSpace(discoveryUrl))
                Console.WriteLine("Khi ngrok trên máy A đổi URL: chạy scripts\\update-ngrok-url.ps1 trên máy A.");
            if (exe != null)
                Console.WriteLine($"Chạy: {exe}");
            else
                Console.WriteLine($"Mở thư mục: {dest}");

            Pause();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi: " + ex.Message);
            Pause();
            return 2;
        }
    }

    private static string? ResolveClientZipPath()
    {
        string sidecar = Path.Combine(AppContext.BaseDirectory, "client.zip");
        if (File.Exists(sidecar)) return sidecar;

        var asm = Assembly.GetExecutingAssembly();
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (name.EndsWith("client.zip", StringComparison.OrdinalIgnoreCase))
            {
                string temp = Path.Combine(Path.GetTempPath(), "shopbanhang-client-" + Guid.NewGuid().ToString("N") + ".zip");
                using var stream = asm.GetManifestResourceStream(name)!;
                using var fs = File.Create(temp);
                stream.CopyTo(fs);
                return temp;
            }
        }

        return null;
    }

    private static string NormalizeApiUrl(string url)
    {
        url = url.Trim();
        if (!url.EndsWith('/')) url += "/";
        if (!url.Contains("/api/", StringComparison.OrdinalIgnoreCase))
        {
            if (url.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                url += "/";
            else
                url = url.TrimEnd('/') + "/api/";
        }
        return url;
    }

    private static void CreateDesktopShortcut(string? targetExe)
    {
        if (string.IsNullOrWhiteSpace(targetExe) || !File.Exists(targetExe)) return;

        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string lnk = Path.Combine(desktop, "ShopBanHang.lnk");
            string vbs = Path.Combine(Path.GetTempPath(), "mkshortcut.vbs");
            string script = $"""
                Set WshShell = WScript.CreateObject("WScript.Shell")
                Set lnk = WshShell.CreateShortcut("{lnk.Replace("\\", "\\\\")}")
                lnk.TargetPath = "{targetExe.Replace("\\", "\\\\")}"
                lnk.WorkingDirectory = "{Path.GetDirectoryName(targetExe)!.Replace("\\", "\\\\")}"
                lnk.Description = "ShopBanHang Client"
                lnk.Save
                """;
            File.WriteAllText(vbs, script);
            var psi = new System.Diagnostics.ProcessStartInfo("wscript.exe", $"//nologo \"{vbs}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit(3000);
            File.Delete(vbs);
            Console.WriteLine($"  -> Shortcut: {lnk}");
        }
        catch
        {
            Console.WriteLine("  (Không tạo được shortcut desktop — chạy .exe trong thư mục cài đặt)");
        }
    }

    private static void Pause()
    {
        Console.WriteLine("\nNhấn Enter để thoát...");
        Console.ReadLine();
    }
}
