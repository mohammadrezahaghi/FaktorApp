using System.IO;
using System.Windows;
using FactorApp.UI.Helpers;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace FactorApp.UI
{
    public partial class ActivationWindow : Window
    {
        public bool IsActivated { get; private set; } = false;

        public ActivationWindow()
        {
            InitializeComponent();
            TxtSystemId.Text = LicenseHelper.GetSystemId();
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TxtSystemId.Text);
            MessageBox.Show("کد سیستم کپی شد.");
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            string systemId = TxtSystemId.Text;
            string inputKey = TxtLicenseKey.Text.Trim();

            if (LicenseHelper.ValidateLicense(systemId, inputKey))
            {
                // ذخیره لایسنس در یک فایل (کنار فایل exe)
                File.WriteAllText("license.key", inputKey);
                
                MessageBox.Show("نرم‌افزار با موفقیت فعال شد!", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                IsActivated = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("کد فعال‌سازی نامعتبر است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}