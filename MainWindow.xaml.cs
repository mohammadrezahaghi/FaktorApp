using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation; // این برای NavigationEventArgs ضروری است
using FactorApp.Models;
using FactorApp.UI.Pages;
using MaterialDesignThemes.Wpf;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace FactorApp.UI
{
    public partial class MainWindow : Window
    {
        private const string ChangelogUrl = "https://raw.githubusercontent.com/mohammadrezahaghi/FaktorApp/main/changelog.json";
        public bool CanClose { get; set; } = false;
        private List<Button> _menuButtons;

        // صفحات کش شده (ممکن است در ابتدا نال باشند)
        private DashboardPage? _dashboardPage;
        private CreateInvoicePage? _createInvoicePage;
        private OrdersPage? _ordersPage;
        private ServicesPage? _servicesPage;
        private CustomersPage? _customersPage;
        private SettingsPage? _settingsPage;

        public MainWindow()
        {
            InitializeComponent(); // اگر XAML درست باشد، این خطا رفع می‌شود

            // لیست دکمه‌های منو
            _menuButtons = new List<Button>
            {
                BtnDashboard, BtnNewInvoice, BtnOrders, BtnServices, BtnCustomers, BtnSettings
            };

            // انتخاب پیش‌فرض داشبورد
            BtnDashboard_Click(BtnDashboard, null);

            this.Loaded += MainWindow_Loaded;
        }

        // --- متد جدید و بهینه تغییر وضعیت ---
        private void UpdateSidebarUI(Button? activeButton)
        {
            if (activeButton == null) return;

            // 1. همه دکمه‌ها را غیرفعال کن (Uid را خالی کن)
            foreach (var btn in _menuButtons)
            {
                btn.Uid = "";
            }

            // 2. دکمه کلیک شده را فعال کن
            activeButton.Uid = "Active";
        }

        private void MainContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content == null) return;

            // چک کردن نوع صفحه‌ی باز شده
            var pageContent = e.Content;

            if (pageContent is DashboardPage)
            {
                // در داشبورد هدر را مخفی کن
                PageHeaderArea.Visibility = Visibility.Collapsed;
            }
            else
            {
                // در بقیه صفحات هدر را نشان بده و اطلاعاتش را پر کن
                PageHeaderArea.Visibility = Visibility.Visible;

                if (pageContent is CreateInvoicePage)
                {
                    SetPageHeader("ثبت فاکتور جدید", "صدور فاکتور فروش کالا و خدمات", PackIconKind.FileDocumentPlusOutline);
                }
                else if (pageContent is OrdersPage)
                {
                    SetPageHeader("لیست سفارشات", "مدیریت و پیگیری فاکتورهای ثبت شده", PackIconKind.FormatListBulleted);
                }
                else if (pageContent is CustomersPage)
                {
                    SetPageHeader("مدیریت مشتریان", "افزودن و ویرایش لیست مشتریان", PackIconKind.AccountGroupOutline);
                }
                else if (pageContent is ServicesPage)
                {
                    SetPageHeader("مدیریت خدمات", "تعریف تعرفه‌ها و خدمات چاپ", PackIconKind.PrinterSettings);
                }
                else if (pageContent is SettingsPage)
                {
                    SetPageHeader("تنظیمات سیستم", "مدیریت مشخصات فروشگاه، ظاهر و امنیت", PackIconKind.CogOutline);
                }
            }
        }

        // تابع کمکی برای تنظیم سریع هدر
        private void SetPageHeader(string title, string subtitle, PackIconKind icon)
        {
            TxtPageTitle.Text = title;
            TxtPageSubtitle.Text = subtitle;
            PageHeaderIcon.Kind = icon;
        }

        private void NavigateTo(object page)
        {
            if (PlaceholderGrid != null) PlaceholderGrid.Visibility = Visibility.Collapsed;
            MainContentFrame.Navigate(page);
        }

        // --- رویدادهای کلیک منو ---
        private void BtnDashboard_Click(object? sender, RoutedEventArgs? e)
        {
            UpdateSidebarUI(sender as Button);
            if (_dashboardPage == null) _dashboardPage = new DashboardPage();
            NavigateTo(_dashboardPage);
        }

        private void BtnNewInvoice_Click(object sender, RoutedEventArgs e)
        {
            UpdateSidebarUI(sender as Button);
            if (_createInvoicePage == null) _createInvoicePage = new CreateInvoicePage();
            NavigateTo(_createInvoicePage);
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            UpdateSidebarUI(sender as Button);

            // تغییر مهم: به جای استفاده از نمونه کش شده، هر بار یک نمونه جدید بساز
            // این باعث می‌شود سازنده (Constructor) و متد Loaded صفحه دوباره اجرا شوند و اطلاعات تازه شود.
            _ordersPage = new OrdersPage();

            NavigateTo(_ordersPage);
        }

        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            UpdateSidebarUI(sender as Button);
            if (_servicesPage == null) _servicesPage = new ServicesPage();
            NavigateTo(_servicesPage);
        }

        private void BtnCustomers_Click(object sender, RoutedEventArgs e)
        {
            UpdateSidebarUI(sender as Button);
            if (_customersPage == null) _customersPage = new CustomersPage();
            NavigateTo(_customersPage);
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            UpdateSidebarUI(sender as Button);
            if (_settingsPage == null) _settingsPage = new SettingsPage();
            NavigateTo(_settingsPage);
        }

        // --- سایر متدها ---
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1500);
            await CheckAndShowChangelog();
        }

        private async Task CheckAndShowChangelog()
        {
            try
            {
                var assemblyVer = Assembly.GetExecutingAssembly().GetName().Version;
                if (assemblyVer == null) return;

                var currentVersion = $"{assemblyVer.Major}.{assemblyVer.Minor}.{assemblyVer.Build}";
                var lastRunVersion = VersionStorage.GetLastRunVersion();
                //currentVersion != lastRunVersion
                if (currentVersion != lastRunVersion)
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; FactorApp/1.0)");
                        string jsonContent = await client.GetStringAsync(ChangelogUrl);
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var allChangelogs = JsonSerializer.Deserialize<List<ChangelogModel>>(jsonContent, options);

                        if (allChangelogs != null)
                        {
                            var targetLog = allChangelogs.FirstOrDefault(x => x.Version.Trim() == currentVersion.Trim());
                            if (targetLog != null)
                            {
                                var view = new WhatsNewDialog();
                                view.TxtTitle.Text = targetLog.Title;
                                view.TxtVersion.Text = $"نسخه {targetLog.Version}";
                                view.TxtDescription.Text = targetLog.Description;
                                view.LstFeatures.ItemsSource = targetLog.Features;
                                await DialogHost.Show(view, "RootDialog");
                            }
                        }
                    }
                    VersionStorage.SaveNewVersion(currentVersion);
                }
            }
            catch { }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (CanClose) return;
            e.Cancel = true;
            this.Hide();
            (Application.Current as FactorApp.UI.App)?.ShowNotification("چاپخانه پلاس", "برنامه در پس‌زمینه فعال است.");
        }

        private void ColorZone_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2) { BtnMaximize_Click(sender, e); return; }
                this.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) => this.WindowState = (this.WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
    }
}