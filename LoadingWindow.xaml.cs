using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using FactorApp.UI.Data;
using FactorApp.UI.Models;
using FactorApp.UI.Helpers;
using System.Linq;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace FactorApp.UI
{
    public enum LoadingResult
    {
        ShowLogin,
        ShowMain,
        Shutdown
    }

    public partial class LoadingWindow : Window
    {
        // رفع خطای CS8618: اضافه کردن علامت سوال (Nullable)
        public event Action<LoadingResult, User?>? OperationCompleted;

        // >>> رفع خطای CS0103: تعریف متغیر در سطح کلاس <<<
        private User? _loggedInUser = null;

        public LoadingWindow()
        {
            InitializeComponent();

            // رفع خطای CS8602 (احتمال نال بودن ورژن)
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                TxtVersion.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
            }
            else
            {
                TxtVersion.Text = "v1.0.0";
            }

            Loaded += LoadingWindow_Loaded;
        }

        private async void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "در حال بارگذاری...";
            await Task.Delay(500);

            TxtStatus.Text = "بررسی اطلاعات پایه...";
            bool dbSuccess = await Task.Run(() => EnsureDatabaseAndAdminUser());

            if (!dbSuccess)
            {
                Application.Current.Shutdown();
                return;
            }

            TxtStatus.Text = "بررسی نسخه نرم‌افزار...";
            CheckForUpdate();
        }

        private void CheckForUpdate()
        {
            try
            {
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.CheckForUpdateEvent += AutoUpdater_OnCheckForUpdateEvent;
                string updateUrl = "https://raw.githubusercontent.com/mohammadrezahaghi/FaktorApp/main/update.xml";
                AutoUpdater.Start(updateUrl);
            }
            catch
            {
                PerformAutoLoginAndFinish();
            }
        }

        private async void AutoUpdater_OnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            AutoUpdater.CheckForUpdateEvent -= AutoUpdater_OnCheckForUpdateEvent;

            if (args.Error != null)
            {
                PerformAutoLoginAndFinish();
                return;
            }

            if (args.IsUpdateAvailable)
            {
                TxtStatus.Text = "در حال بروزرسانی...";
                TxtStatus.Foreground = System.Windows.Media.Brushes.Cyan;
                StartCustomDownload(args.DownloadURL);
            }
            else
            {
                TxtStatus.Text = "آماده‌سازی محیط کاربری...";
                TxtStatus.Foreground = System.Windows.Media.Brushes.White;
                await Task.Delay(800);
                PerformAutoLoginAndFinish();
            }
        }

        private void StartCustomDownload(string url)
        {
            PrgUpdate.Visibility = Visibility.Visible;
            TxtPercent.Visibility = Visibility.Visible;

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "FactorApp_Setup.exe");
                if (File.Exists(tempPath)) File.Delete(tempPath);

                // رفع هشدار WebClient Obsolete با نادیده گرفتن آن (ساده‌ترین راه برای الان)
#pragma warning disable SYSLIB0014
                WebClient webClient = new WebClient();
#pragma warning restore SYSLIB0014

                webClient.DownloadProgressChanged += (s, e) =>
                {
                    PrgUpdate.Value = e.ProgressPercentage;
                    TxtPercent.Text = $"{e.ProgressPercentage}%";
                    TxtStatus.Text = $"در حال دانلود بروزرسانی... ({e.ProgressPercentage}%)";

                    if (e.ProgressPercentage > 95)
                        TxtStatus.Text = "در حال آماده‌سازی نصب...";
                };

                webClient.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null)
                    {
                        PerformAutoLoginAndFinish();
                        return;
                    }
                    RunInstaller(tempPath);
                };

                webClient.DownloadFileAsync(new Uri(url), tempPath);
            }
            catch
            {
                PerformAutoLoginAndFinish();
            }
        }

        private void RunInstaller(string path)
        {
            TxtStatus.Text = "نصب بروز رسانی...";
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                Application.Current.Shutdown();
            }
            catch
            {
                PerformAutoLoginAndFinish();
            }
        }

        private async void PerformAutoLoginAndFinish()
        {
            TxtStatus.Text = "بررسی ورود خودکار...";

            bool loginSuccess = false;

            await Task.Run(() =>
            {
                if (CredentialsHelper.GetSavedCredentials(out string savedUser, out string savedPass))
                {
                    // حالا متغیر _loggedInUser که بالا تعریف کردیم اینجا استفاده می‌شود
                    loginSuccess = TryAutoLogin(savedUser, savedPass, out _loggedInUser);
                }
            });

            TxtStatus.Text = "اجرای برنامه...";
            await Task.Delay(300);

            if (loginSuccess && _loggedInUser != null)
            {
                OperationCompleted?.Invoke(LoadingResult.ShowMain, _loggedInUser);
            }
            else
            {
                OperationCompleted?.Invoke(LoadingResult.ShowLogin, null);
            }

            this.Close();
        }

        private bool TryAutoLogin(string username, string password, out User? authenticatedUser)
        {
            authenticatedUser = null;
            try
            {
                using (var context = new AppDbContext())
                {
                    var user = context.Users.SingleOrDefault(u => u.Username.ToLower() == username.ToLower());
                    if (user != null && user.IsActive)
                    {
                        if (PasswordHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                        {
                            authenticatedUser = user;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AutoLogin Error: {ex.Message}");
            }
            return false;
        }

        private bool EnsureDatabaseAndAdminUser()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    context.Database.EnsureCreated();
                    if (!context.StoreInfos.Any())
                    {
                        context.StoreInfos.Add(new StoreInfo
                        {
                            StoreName = "فروشگاه من",
                            IsDarkMode = false,
                            Address = "آدرس پیش فرض",
                            PhoneNumber = "-",
                            FooterText = "توضیحات فاکتور"
                        });
                        context.SaveChanges();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطای پایگاه داده: {ex.Message}\n\nلطفا با پشتیبانی تماس بگیرید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return false;
            }
        }
    }
}