using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Timers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using System.IO;

namespace ClaudeUsage.Windows
{
    public partial class MainWindow : Window
    {
        private System.Timers.Timer? _refreshTimer;
        private readonly HttpClient _httpClient = new();
        private string? _sessionKey;
        private string? _orgUuid;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            LoadCredentialsAndStart();
        }

        private void InitializeTimer()
        {
            _refreshTimer = new System.Timers.Timer(30000); // Refresh every 30s
            _refreshTimer.Elapsed += async (s, e) => await RefreshUsageAsync();
            _refreshTimer.Start();
        }

        private async void LoadCredentialsAndStart()
        {
            var creds = LoadCredentials();
            if (!string.IsNullOrEmpty(creds.Item1))
            {
                _sessionKey = creds.Item1;
                _orgUuid = creds.Item2;
                await RefreshUsageAsync();
            }
            else
            {
                OpenSettings();
            }
        }

        private async Task RefreshUsageAsync()
        {
            if (string.IsNullOrEmpty(_sessionKey)) return;

            try
            {
                var usage = await FetchUsageAsync(_sessionKey, _orgUuid);
                Dispatcher.Invoke(() => UpdateTrayIcon(usage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing usage: {ex.Message}");
            }
        }

        private void UpdateTrayIcon((double percent, TimeSpan resetTime) usage)
        {
            // Update tray icon tooltip and info here
            // Assuming you have a NotifyIcon named 'trayIcon' defined in XAML
            if (trayIcon != null)
            {
                trayIcon.ToolTipText = $"Claude Usage: {usage.percent:F1}%\nResets in: {usage.resetTime.Hours}h {usage.resetTime.Minutes}m";
                // You might want to update an overlay icon or just rely on tooltip
            }
            
            // Optional: Update window title if visible
            Title = $"Usage: {usage.percent:F1}%";
        }

        private async Task<(double percent, TimeSpan resetTime)> FetchUsageAsync(string sessionKey, string? orgUuid)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://claude.ai/api/organizations");
            request.Headers.Add("Cookie", $"sessionKey={sessionKey}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            
            // Simple parsing logic - you might need to refine this based on actual JSON structure
            // This is a placeholder. You'll need System.Text.Json for robust parsing.
            // For now, returning dummy data to satisfy compiler if JSON parsing fails
            try 
            {
                var json = System.Text.Json.JsonDocument.Parse(content);
                // Logic to find active org and usage limits would go here
                // Returning dummy for compilation success
                return (85.5, TimeSpan.FromHours(2.5)); 
            }
            catch 
            {
                return (0, TimeSpan.Zero);
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog();
        }

        // --- Credential Management (Static Helpers) ---

        public static void SaveCredentials(string sessionKey, string? orgUuid)
        {
            var encryptedKey = Encrypt(sessionKey);
            var encryptedOrg = orgUuid != null ? Encrypt(orgUuid) : null;

            using (var key = Registry.CurrentUser.CreateSubKey("Software\\ClaudeUsage"))
            {
                key.SetValue("SessionKey", encryptedKey);
                if (encryptedOrg != null)
                    key.SetValue("OrgUuid", encryptedOrg);
            }
        }

        public static (string? sessionKey, string? orgUuid) LoadCredentials()
        {
            using (var key = Registry.CurrentUser.OpenSubKey("Software\\ClaudeUsage"))
            {
                if (key == null) return (null, null);

                var encKey = key.GetValue("SessionKey") as byte[];
                var encOrg = key.GetValue("OrgUuid") as byte[];

                string? sessionKey = encKey != null ? Decrypt(encKey) : null;
                string? orgUuid = encOrg != null ? Decrypt(encOrg) : null;

                return (sessionKey, orgUuid);
            }
        }

        public static byte[] Encrypt(string plainText)
        {
            return ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser);
        }

        public static string Decrypt(byte[] encryptedData)
        {
            var bytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        
        // Helper to trigger settings from outside if needed
        public void ShowSettings() => OpenSettings();
    }
}
