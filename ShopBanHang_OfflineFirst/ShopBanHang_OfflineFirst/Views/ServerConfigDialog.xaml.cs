using ShopBanHang_OfflineFirst.Services;
using System.Windows;

namespace ShopBanHang_OfflineFirst.Views
{
    public partial class ServerConfigDialog : Window
    {
        public ServerConfigDialog()
        {
            InitializeComponent();
            txtServerUrl.Text = ApiEndpointConfig.ResolveBaseUrl();
            txtDiscoveryUrl.Text = ReadOptionalFile(ApiEndpointConfig.DiscoveryUrlPath);
        }

        private static string ReadOptionalFile(string path)
        {
            if (!System.IO.File.Exists(path)) return string.Empty;
            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                var t = line.Trim();
                if (t.Length > 0 && !t.StartsWith('#') && !t.StartsWith("//"))
                    return t;
            }
            return string.Empty;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtServerUrl.Text))
                ApiEndpointConfig.SaveServerUrl(txtServerUrl.Text);

            if (!string.IsNullOrWhiteSpace(txtDiscoveryUrl.Text))
                ApiEndpointConfig.SaveDiscoveryUrl(txtDiscoveryUrl.Text);
            else if (System.IO.File.Exists(ApiEndpointConfig.DiscoveryUrlPath))
                System.IO.File.Delete(ApiEndpointConfig.DiscoveryUrlPath);

            DialogResult = true;
            Close();
        }
    }
}
