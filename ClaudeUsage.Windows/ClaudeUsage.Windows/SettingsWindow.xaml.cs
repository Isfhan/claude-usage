using System;
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
            
            // Load existing credentials if any
            var creds = MainWindow.LoadCredentials();
            if (!string.IsNullOrEmpty(creds.sessionKey))
            {
                SessionKeyTextBox.Text = creds.sessionKey;
            }
            if (!string.IsNullOrEmpty(creds.orgUuid))
            {
                OrgUuidTextBox.Text = creds.orgUuid;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var sessionKey = SessionKeyTextBox.Text.Trim();
            var orgUuid = string.IsNullOrWhiteSpace(OrgUuidTextBox.Text) ? null : OrgUuidTextBox.Text.Trim();

            if (string.IsNullOrEmpty(sessionKey))
            {
                MessageBox.Show("Please enter a valid Session Key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Call static method
            MainWindow.SaveCredentials(sessionKey, orgUuid);
            
            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Trigger refresh in main window
            // We can't directly access private methods, so we might need a public trigger or just restart logic
            // For simplicity, let's close and let MainWindow handle reload on next tick or add a public Reload method
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
