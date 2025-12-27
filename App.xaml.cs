using System;
using System.IO;
using System.Linq;
using System.Windows;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
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

            // 1. اعمال تم ذخیره شده (همینجا بماند بهتر است تا پنجره‌ها با تم درست باز شوند)
            ApplySavedTheme();

            // 2. نمایش پنجره لودینگ
            var loadingWindow = new LoadingWindow();

            // گوش دادن به نتیجه‌ای که لودینگ اعلام می‌کند
            loadingWindow.OperationCompleted += OnLoadingFinished;

            loadingWindow.Show();
        }

        // این متد زمانی اجرا می‌شود که LoadingWindow کارش تمام شده باشد
        // و نتیجه (لاگین شده یا نشده) را به ما می‌دهد
        private void OnLoadingFinished(LoadingResult result, User? user)
        {
            if (result == LoadingResult.ShowMain && user != null)
            {
                // *** حالت 1: لاگین خودکار در لودینگ موفق بوده ***
                ShowMainWindow(user);
                ShowNotification("خوش آمدید", $"ورود خودکار با موفقیت انجام شد.\nکاربر: {user.FullName}");
            }
            else if (result == LoadingResult.ShowLogin)
            {
                // *** حالت 2: نیاز به لاگین دستی ***
                var loginWindow = new LoginWindow();
                bool? dialogResult = loginWindow.ShowDialog();

                if (loginWindow.IsLoggedIn && loginWindow.User != null)
                {
                    // کاربر دستی لاگین کرد
                    ShowMainWindow(loginWindow.User);
                }
                else
                {
                    // کاربر پنجره لاگین را بست یا کنسل کرد -> خروج کامل
                    ExitApplication();
                }
            }
            else
            {
                // حالت Shutdown (مثلا خطای دیتابیس در لودینگ)
                ExitApplication();
            }
        }

        private void ShowMainWindow(User user)
        {
            // ایجاد پنجره اصلی (می‌توانید آبجکت User را به سازنده آن پاس دهید)
            var mainWindow = new MainWindow();
            // اگر MainWindow شما ورودی User می‌گیرد: new MainWindow(user);

            this.MainWindow = mainWindow;
            mainWindow.Show();

            // فعال‌سازی آیکون کنار ساعت
            SetupTrayIcon();
        }

        // --- متدهای مربوط به تم (بدون تغییر) ---
        public void ApplySavedTheme()
        {
            try
            {
                bool isDark = false;
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
                catch { }

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

        // --- متدهای مربوط به Tray Icon (بدون تغییر) ---
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
            // اگر آیکون نابود شده یا نال است، هیچ کاری نکن (جلوگیری از ارور)
            if (_notifyIcon == null || _notifyIcon.Icon == null) return;

            try
            {
                _notifyIcon.Visible = true;
                _notifyIcon.ShowBalloonTip(3000, title, message, Forms.ToolTipIcon.Info);
            }
            catch
            {
                // نادیده گرفتن خطا در لحظه خروج
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
            // اول: پنجره اصلی را ببندیم (بدون اینکه نوتیفیکیشن بدهد)
            if (MainWindow is MainWindow myWindow)
            {
                // به پنجره میگوییم که اجازه دارد بسته شود
                myWindow.CanClose = true;
                myWindow.Close();
            }

            // دوم: حالا که پنجره بسته شد، آیکون را نابود کن
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null; // نال کردن برای اطمینان
            }

            // سوم: خروج نهایی
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null) _notifyIcon.Dispose();
            base.OnExit(e);
        }
    }
}