using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;
using System.Timers;

namespace ClaudeUsage.Windows
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new();
        private TaskbarIcon? _trayIcon;
        private System.Timers.Timer? _refreshTimer;
        private string? _sessionKey;
        private string? _orgUuid;
        private double _usagePercent;
        private DateTime _resetTime;

        public MainWindow()
        {
            InitializeComponent();
            LoadCredentials();
            InitializeTrayIcon();
            InitializeTimer();
            
            if (!string.IsNullOrEmpty(_sessionKey))
            {
                _ = RefreshUsageAsync();
            }
            else
            {
                ShowSettings();
            }
        }

        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                IconSource = new System.Drawing.Icon("Icons/app.ico"),
                ToolTipText = "Claude Usage: Loading..."
            };

            _trayIcon.TrayMouseDoubleClick += (s, e) => ShowDetails();
            _trayIcon.ContextMenu = (System.Windows.Controls.ContextMenu)FindResource("TrayContextMenu");
        }

        private void InitializeTimer()
        {
            _refreshTimer = new System.Timers.Timer(30000); // 30 seconds
            _refreshTimer.Elapsed += async (s, e) => await Dispatcher.InvokeAsync(async () => await RefreshUsageAsync());
            _refreshTimer.Start();
        }

        private async Task RefreshUsageAsync()
        {
            if (string.IsNullOrEmpty(_sessionKey)) return;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://claude.ai/api/organizations");
                request.Headers.Add("Cookie", $"sessionKey={_sessionKey}");
                
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return;

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.GetArrayLength() == 0) return;

                JsonElement orgElement;
                if (!string.IsNullOrEmpty(_orgUuid))
                {
                    foreach (var elem in root.EnumerateArray())
                    {
                        if (elem.GetProperty("uuid").GetString() == _orgUuid)
                        {
                            orgElement = elem;
                            goto found;
                        }
                    }
                    orgElement = root[0];
                }
                else
                {
                    orgElement = root[0];
                    _orgUuid = orgElement.GetProperty("uuid").GetString();
                }

                found:
                var usage = orgElement.GetProperty("usage");
                var maxTokens = usage.GetProperty("max_tokens");
                var usedTokens = usage.GetProperty("tokens_used_in_all_projects");
                
                _usagePercent = maxTokens.GetInt32() > 0 
                    ? (double)usedTokens.GetInt64() / maxTokens.GetInt32() * 100 
                    : 0;

                var resetStr = orgElement.GetProperty("usage_limit_reset_at").GetString();
                if (DateTime.TryParse(resetStr, out var reset))
                    _resetTime = reset.ToLocalTime();

                UpdateTrayInfo();
            }
            catch { /* Ignore errors on background refresh */ }
        }

        private void UpdateTrayInfo()
        {
            if (_trayIcon == null) return;

            var percentStr = _usagePercent.ToString("F1");
            var now = DateTime.Now;
            var remaining = _resetTime - now;
            
            string timeStr;
            if (remaining.TotalHours >= 1)
                timeStr = $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
            else if (remaining.TotalMinutes >= 1)
                timeStr = $"{(int)remaining.TotalMinutes}m {remaining.Seconds}s";
            else
                timeStr = $"{remaining.Seconds}s";

            _trayIcon.ToolTipText = $"Claude Usage: {percentStr}%\nResets in: {timeStr}";
            
            // Update title if window is open (optional)
            Title = $"Claude Usage ({percentStr}%)";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Hide(); // Keep main window hidden, only show tray
        }

        private void NotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowDetails();
        }

        private void ShowDetails_Click(object sender, RoutedEventArgs e)
        {
            ShowDetails();
        }

        private void ShowDetails()
        {
            MessageBox.Show(
                $"Usage: {_usagePercent:F1}%\n" +
                $"Resets in: {(_resetTime - DateTime.Now):hh\\:mm\\:ss}\n\n" +
                "Double-click tray icon to open Settings.",
                "Claude Usage",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ShowSettings();
        }

        private void ShowSettings()
        {
            var settingsWindow = new SettingsWindow(this);
            settingsWindow.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            _trayIcon?.Dispose();
            Application.Current.Shutdown();
        }

        // --- Credential Management (Static helpers) ---

        public static void SaveCredentials(string sessionKey, string? orgUuid)
        {
            try
            {
                var encrypted = Encrypt(sessionKey);
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\ClaudeUsage", "SessionKey", Convert.ToBase64String(encrypted));
                
                if (!string.IsNullOrEmpty(orgUuid))
                {
                    var encryptedOrg = Encrypt(orgUuid);
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\ClaudeUsage", "OrgUuid", Convert.ToBase64String(encryptedOrg));
                }
                else
                {
                    Registry.CurrentUser.CreateSubKey(@"Software\ClaudeUsage").DeleteValue("OrgUuid", false);
                }
            }
            catch { /* Ignore registry errors */ }
        }

        public void LoadCredentials()
        {
            try
            {
                var keyVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\ClaudeUsage", "SessionKey", null);
                if (keyVal is string keyStr)
                {
                    _sessionKey = Decrypt(Convert.FromBase64String(keyStr));
                }

                var orgVal = Registry.GetValue(@"HKEY_CURRENT_USER\Software\ClaudeUsage", "OrgUuid", null);
                if (orgVal is string orgStr)
                {
                    _orgUuid = Decrypt(Convert.FromBase64String(orgStr));
                }
            }
            catch { /* Ignore errors */ }
        }

        private static byte[] Encrypt(string plainText)
        {
            return ProtectedData.Protect(Encoding.UTF8.GetBytes(plainText), null, DataProtectionScope.CurrentUser);
        }

        private static string Decrypt(byte[] encryptedData)
        {
            var bytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}