using System.Windows;

namespace ClaudeUsage.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            
            // Load existing values if available (handled by MainWindow passing data or reading directly)
            // For simplicity, we assume textboxes are empty initially or user re-enters
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var sessionKey = SessionKeyTextBox.Text.Trim();
            var orgUuid = OrgUuidTextBox.Text.Trim();

            if (string.IsNullOrEmpty(sessionKey))
            {
                MessageBox.Show("Session Key is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MainWindow.SaveCredentials(sessionKey, string.IsNullOrEmpty(orgUuid) ? null : orgUuid);
            
            MessageBox.Show("Settings saved! The usage will update shortly.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}