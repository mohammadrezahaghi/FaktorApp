using System;
using System.IO;
using System.Linq;
using System.Windows;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using MaterialDesignThemes.Wpf;

// مدیریت تداخل‌ها با Alias
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace FactorApp.UI
{
    public partial class App : System.Windows.Application
    {
        private Forms.NotifyIcon? _notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // جلوگیری از بسته شدن برنامه هنگام بسته شدن آخرین پنجره (برای Tray Icon)
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. اعمال تم ذخیره شده (سبک اجرا می‌شود)
            ApplySavedTheme();

            // 2. نمایش پنجره لودینگ برای انجام کارهای سنگین (دیتابیس و آپدیت)
            var loadingWindow = new LoadingWindow();
            loadingWindow.OperationCompleted += ContinueStartup; // وقتی کارش تمام شد این متد صدا زده می‌شود
            loadingWindow.Show();
        }

        // این متد زمانی اجرا می‌شود که LoadingWindow کارش تمام شده باشد
        private void ContinueStartup()
        {
            bool autoLoginSuccess = false;

            // تلاش برای لاگین خودکار
            if (CredentialsHelper.GetSavedCredentials(out string savedUser, out string savedPass))
            {
                if (TryAutoLogin(savedUser, savedPass))
                {
                    autoLoginSuccess = true;
                }
            }

            // اگر لاگین خودکار موفق نبود، پنجره لاگین را نشان بده
            if (!autoLoginSuccess)
            {
                var loginWindow = new LoginWindow();
                bool? result = loginWindow.ShowDialog();

                if (loginWindow.IsLoggedIn != true)
                {
                    // اگر کاربر لاگین نکرد و پنجره را بست، برنامه بسته شود
                    Shutdown();
                    return;
                }
            }

            // نمایش پنجره اصلی
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();

            // فعال‌سازی آیکون کنار ساعت
            SetupTrayIcon();

            if (!autoLoginSuccess)
                ShowNotification("خوش آمدید", "ورود به سیستم با موفقیت انجام شد.");
        }

        private bool TryAutoLogin(string username, string password)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var user = context.Users.SingleOrDefault(u => u.Username.ToLower() == username.ToLower());
                    if (user != null && user.IsActive)
                    {
                        return PasswordHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoLogin Error: {ex.Message}");
            }
            return false;
        }

        public void ApplySavedTheme()
        {
            try
            {
                bool isDark = false;
                // خواندن تنظیمات تم از دیتابیس (با هندل کردن خطای احتمالی نبود دیتابیس در لحظه اول)
                try 
                {
                    using (var context = new AppDbContext())
                    {
                        if (context.Database.CanConnect())
                        {
                            var info = context.StoreInfos.FirstOrDefault();
                            if (info != null) isDark = info.IsDarkMode;
                        }
                    }
                }
                catch { /* در اجرای اول ممکن است دیتابیس نباشد، پیش‌فرض لایت است */ }

                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                BaseTheme baseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;
                theme.SetBaseTheme(baseTheme);

                if (isDark)
                {
                    theme.SetPrimaryColor((MediaColor)MediaColorConverter.ConvertFromString("#FFD700"));
                    theme.SetSecondaryColor((MediaColor)MediaColorConverter.ConvertFromString("#A9A9A9"));
                    theme.Background = (MediaColor)MediaColorConverter.ConvertFromString("#121212");

                    Application.Current.Resources["SidebarBackgroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#1E1E1E"));
                    Application.Current.Resources["SidebarForegroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#E0E0E0"));
                }
                else
                {
                    theme.SetPrimaryColor((MediaColor)MediaColorConverter.ConvertFromString("#3F51B5"));
                    theme.SetSecondaryColor((MediaColor)MediaColorConverter.ConvertFromString("#FF4081"));
                    theme.Background = (MediaColor)MediaColorConverter.ConvertFromString("#fbfbfb");

                    Application.Current.Resources["SidebarBackgroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#f5f5f5"));
                    Application.Current.Resources["SidebarForegroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#212121"));
                }

                paletteHelper.SetTheme(theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Theme Error: " + ex.Message);
            }
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            try
            {
                var iconUri = new Uri("pack://application:,,,/logo.ico");
                Stream iconStream = GetResourceStream(iconUri).Stream;
                _notifyIcon.Icon = new Drawing.Icon(iconStream);
            }
            catch
            {
                _notifyIcon.Icon = Drawing.SystemIcons.Application;
            }

            _notifyIcon.Visible = true;
            _notifyIcon.Text = "چاپخانه پلاس";

            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("باز کردن برنامه", null, (sender, args) => ShowMainWindow());
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            contextMenu.Items.Add("خروج کامل", null, (sender, args) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;

            _notifyIcon.DoubleClick += (sender, args) => ShowMainWindow();
        }

        public void ShowNotification(string title, string message)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(3000, title, message, Forms.ToolTipIcon.Info);
            }
        }

        public void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                if (MainWindow.WindowState == WindowState.Minimized)
                    MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }

        public void ExitApplication()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            // بستن پنجره اصلی اگر باز است
            if (MainWindow is MainWindow myWindow)
            {
                myWindow.CanClose = true; // فرض بر این است که پراپرتی CanClose در MainWindow دارید
                myWindow.Close();
            }
            
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null) _notifyIcon.Dispose();
            base.OnExit(e);
        }
    }
}