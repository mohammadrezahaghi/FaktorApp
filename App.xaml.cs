using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf; // حتما این باشد

// آلیاس‌ها
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace FactorApp.UI
{
    public partial class App : System.Windows.Application
    {
        private Forms.NotifyIcon _notifyIcon;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 1. اطمینان از وجود دیتابیس (یک بار کافی است)
            EnsureDatabaseAndAdminUser();

            // 2. اعمال تم ذخیره شده (قبل از نمایش هر پنجره‌ای)
            ApplySavedTheme();

            // 3. نمایش لودینگ
            var loadingWindow = new LoadingWindow();
            loadingWindow.Show();
            await Task.Delay(2000);
            loadingWindow.Close();

            // 4. بررسی لاگین خودکار
            bool autoLoginSuccess = false;

            if (CredentialsHelper.GetSavedCredentials(out string savedUser, out string savedPass))
            {
                if (TryAutoLogin(savedUser, savedPass))
                {
                    autoLoginSuccess = true;
                }
            }

            if (!autoLoginSuccess)
            {
                var loginWindow = new LoginWindow();
                loginWindow.ShowDialog();

                if (!loginWindow.IsLoggedIn)
                {
                    Shutdown();
                    return;
                }
            }

            // 5. ورود موفقیت آمیز
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();

            SetupTrayIcon();

            if (!autoLoginSuccess)
                ShowNotification("خوش آمدید", "ورود موفقیت آمیز بود.");
        }

        // --- متد اعمال تم از دیتابیس ---
        // --- متد اصلاح شده و نهایی برای اعمال تم ---
        private void ApplySavedTheme()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var info = context.StoreInfos.FirstOrDefault();

                    // بررسی اینکه آیا باید تم تاریک شود؟
                    if (info != null && info.IsDarkMode)
                    {
                        var paletteHelper = new PaletteHelper();

                        // دریافت تم فعلی
                        var theme = paletteHelper.GetTheme();

                        // تغییر تم پایه به دارک (این خط ارور را رفع می‌کند)
                        theme.SetBaseTheme(BaseTheme.Dark);

                        // اعمال تغییرات
                        paletteHelper.SetTheme(theme);
                    }
                }
            }
            catch
            {
                // در صورت خطا، با تم پیش‌فرض ادامه بده
            }
        }

        // --- متد چک کردن لاگین ---
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
            catch { }
            return false;
        }

        // --- ساخت دیتابیس ---
        private void EnsureDatabaseAndAdminUser()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    context.Database.EnsureCreated();
                    if (!context.Users.Any())
                    {
                        PasswordHelper.CreatePasswordHash("admin", out string hash, out string salt);
                        var admin = new User
                        {
                            Username = "admin",
                            FullName = "مدیر سیستم",
                            PasswordHash = hash,
                            PasswordSalt = salt,
                            IsActive = true
                        };
                        context.Users.Add(admin);
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطا در ارتباط با دیتابیس: " + ex.Message);
            }
        }

        // --- تنظیمات سینی ویندوز (Tray Icon) ---
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

            if (MainWindow is MainWindow myWindow)
            {
                myWindow.CanClose = true;
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