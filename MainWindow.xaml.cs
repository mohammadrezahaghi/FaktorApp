using System.Windows;
using FactorApp.UI.Pages;

namespace FactorApp.UI
{
    public partial class MainWindow : Window
    {
        // این متغیر اجازه خروج کامل را می‌دهد
        public bool CanClose { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            NavigateTo(new DashboardPage());
        }

        private void NavigateTo(object page)
        {
            if (PlaceholderGrid != null) PlaceholderGrid.Visibility = Visibility.Collapsed;
            MainContentFrame.Navigate(page);
        }

        // --- رویدادهای منو ---
        private void BtnDashboard_Click(object sender, RoutedEventArgs e) => NavigateTo(new DashboardPage());
        private void BtnNewInvoice_Click(object sender, RoutedEventArgs e) => NavigateTo(new CreateInvoicePage());
        private void BtnOrders_Click(object sender, RoutedEventArgs e) => NavigateTo(new OrdersPage());
        private void BtnCustomers_Click(object sender, RoutedEventArgs e) => NavigateTo(new CustomersPage());
        private void BtnServices_Click(object sender, RoutedEventArgs e) => NavigateTo(new ServicesPage());
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new SettingsPage());
        }

        // --- رویداد مهم بسته شدن ---
        // رویداد بسته شدن پنجره
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CanClose)
            {
                e.Cancel = true;
                this.Hide();

                // ارسال نوتیفیکیشن
                var app = System.Windows.Application.Current as App;
                app?.ShowNotification("چاپخانه پلاس", "برنامه در پس زمینه فعال است.");
            }
        }
    }
}