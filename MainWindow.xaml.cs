using System;
using System.Collections.Generic; // اضافه شد برای List
using System.Linq; // اضافه شد برای جستجو در لیست
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FactorApp.Models;
using FactorApp.UI.Pages;
using MaterialDesignThemes.Wpf;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace FactorApp.UI
{
    public partial class MainWindow : Window
    {
        // --- تنظیمات سیستم آپدیت ---
        // لینک فایل RAW جیسون
        private const string ChangelogUrl = "https://raw.githubusercontent.com/mohammadrezahaghi/FaktorApp/main/changelog.json";

        public bool CanClose { get; set; } = false;

        // --- متغیرهای کش کردن صفحات ---
        private DashboardPage _dashboardPage;
        private CreateInvoicePage _createInvoicePage;
        private OrdersPage _ordersPage;
        private CustomersPage _customersPage;
        private ServicesPage _servicesPage;
        private SettingsPage _settingsPage;

        public MainWindow()
        {
            InitializeComponent();

            // نمایش نسخه برنامه
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            // فرض بر اینکه TxtVersion دارید
            if (FindName("TxtVersion") is System.Windows.Controls.TextBlock txt)
            {
                txt.Text = $"نسخه {version.Major}.{version.Minor}.{version.Build}";
            }

            // باز کردن داشبورد
            BtnDashboard_Click(null, null);

            // اتصال رویداد لود شدن
            this.Loaded += MainWindow_Loaded;
        }

        // --- رویداد لود شدن پنجره ---
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1500);
            await CheckAndShowChangelog();
        }

        // --- منطق دریافت و نمایش تغییرات ---
        // --- منطق دریافت و نمایش تغییرات ---
        private async Task CheckAndShowChangelog()
        {
            try
            {
                // 1. دریافت نسخه فعلی برنامه به صورت دقیق (3 رقمی)
                var assemblyVer = Assembly.GetExecutingAssembly().GetName().Version;
                // تبدیل 1.0.0.0 به 1.0.0
                var currentVersion = $"{assemblyVer.Major}.{assemblyVer.Minor}.{assemblyVer.Build}";

                // 2. دریافت آخرین نسخه ذخیره شده
                var lastRunVersion = VersionStorage.GetLastRunVersion();

                System.Diagnostics.Debug.WriteLine($"App Version: {currentVersion}");
                System.Diagnostics.Debug.WriteLine($"Last Run: {lastRunVersion}");

                // 3. چک کردن (برای تست فعلا True است)
                if (currentVersion != lastRunVersion) // در نهایت بکنید: if (currentVersion != lastRunVersion)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; FactorApp/1.0)");

                        // دانلود جیسون
                        string jsonContent = await client.GetStringAsync(ChangelogUrl);
                        System.Diagnostics.Debug.WriteLine($"JSON Downloaded: {jsonContent}");

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                        // تبدیل لیست
                        var allChangelogs = JsonSerializer.Deserialize<List<ChangelogModel>>(jsonContent, options);

                        if (allChangelogs != null)
                        {
                            // *** جستجوی نسخه ***
                            // اینجا چک میکنیم کدام آیتم جیسون با نسخه 1.0.0 برابر است
                            // استفاده از Trim برای حذف فاصله‌های احتمالی
                            var targetLog = allChangelogs.FirstOrDefault(x =>
                                x.Version.Trim() == currentVersion.Trim()
                            );
                            if (targetLog != null)
                            {
                                var view = new WhatsNewDialog();
                                view.TxtTitle.Text = targetLog.Title;
                                view.TxtVersion.Text = $"نسخه {targetLog.Version}";
                                view.TxtDescription.Text = targetLog.Description;
                                view.LstFeatures.ItemsSource = targetLog.Features;

                                await DialogHost.Show(view, "RootDialog");
                            }
                            else
                            {
                                // اگر وارد اینجا شد یعنی نسخه اپ شما با هیچکدام از نسخه‌های جیسون یکی نیست
                                System.Diagnostics.Debug.WriteLine("Warning: No changelog found for version " + currentVersion);

                                // (اختیاری برای تست): اگر پیدا نشد، اولی را نشان بده تا مطمئن شی کار میکنه
                                // var fallback = allChangelogs.FirstOrDefault();
                                // if(fallback != null) { ... show fallback ... }
                            }
                        }
                    }

                    // 4. ذخیره نسخه جدید
                    VersionStorage.SaveNewVersion(currentVersion);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CRITICAL ERROR: {ex.Message}");
            }
        }

        // --- مدیریت پنجره (بدون تغییر) ---
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CanClose)
            {
                e.Cancel = true;
                this.Hide();
                var app = System.Windows.Application.Current as App;
                app?.ShowNotification("فاکتور شایان", "برنامه در پس زمینه فعال است.");
            }
        }

        private void ColorZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    BtnMaximize_Click(sender, e);
                    return;
                }
                this.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();


        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
                this.WindowState = WindowState.Maximized;
            else
                this.WindowState = WindowState.Normal;
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((System.Windows.Controls.Button)sender).Foreground = Brushes.Red;
        }

        private void Button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((System.Windows.Controls.Button)sender).Foreground = (Brush)new BrushConverter().ConvertFrom("#FF5555");
        }

        // --- نویگیشن ---
        private void NavigateTo(object page)
        {
            if (PlaceholderGrid != null) PlaceholderGrid.Visibility = Visibility.Collapsed;
            MainContentFrame.Navigate(page);
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (_dashboardPage == null) _dashboardPage = new DashboardPage();
            NavigateTo(_dashboardPage);
        }

        private void BtnNewInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (_createInvoicePage == null) _createInvoicePage = new CreateInvoicePage();
            NavigateTo(_createInvoicePage);
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_ordersPage == null) _ordersPage = new OrdersPage();
            NavigateTo(_ordersPage);
        }

        private void BtnCustomers_Click(object sender, RoutedEventArgs e)
        {
            if (_customersPage == null) _customersPage = new CustomersPage();
            NavigateTo(_customersPage);
        }

        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            if (_servicesPage == null) _servicesPage = new ServicesPage();
            NavigateTo(_servicesPage);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsPage == null) _settingsPage = new SettingsPage();
            NavigateTo(_settingsPage);
        }
    }
}