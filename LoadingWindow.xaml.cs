using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
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
        public event Action<LoadingResult, User?>? OperationCompleted;
        private User? _loggedInUser = null;
        private CancellationTokenSource _cts;

        public LoadingWindow()
        {
            InitializeComponent();
            _cts = new CancellationTokenSource();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            TxtVersion.Text = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";

            Loaded += LoadingWindow_Loaded;
        }

        private async void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // مرحله 1: بررسی لایسنس
                TxtStatus.Text = "بررسی اعتبار لایسنس...";
                await Task.Delay(100); // وقفه کوتاه برای دیدن متن توسط کاربر

                if (!CheckLicense())
                {
                    this.Hide();
                    var activation = new ActivationWindow();
                    activation.ShowDialog();

                    if (activation.IsActivated)
                    {
                        this.Show();
                    }
                    else
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                }

                // مرحله 2: بررسی دیتابیس (جداگانه نمایش داده شود)
                TxtStatus.Text = "بررسی و اتصال به پایگاه داده...";
                bool dbSuccess = await EnsureDatabaseAndAdminUserAsync();
                
                if (!dbSuccess)
                {
                    Application.Current.Shutdown();
                    return;
                }

                // مرحله 3: بررسی آپدیت (جداگانه نمایش داده شود)
                TxtStatus.Text = "بررسی نسخه اپلیکیشن...";
                await CheckForUpdateAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Loading Critical Error: " + ex.Message);
                PerformAutoLoginAndFinish();
            }
        }

        // --- متد بررسی آپدیت با تایم‌اوت و پینگ ---
        private async Task CheckForUpdateAsync(CancellationToken token)
        {
            // تست سریع اینترنت (زیر 1 ثانیه)
            // اگر نت نباشد، سریع رد میشود تا کاربر معطل نشود
            if (!IsInternetAvailable())
            {
                Debug.WriteLine("No Internet. Skipping Update.");
                PerformAutoLoginAndFinish();
                return;
            }

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                AutoUpdater.RunUpdateAsAdmin = false;
                
                // هندل کردن ایونت آپدیت
                void Handler(UpdateInfoEventArgs args)
                {
                    // بلافاصله ایونت را جدا میکنیم
                    AutoUpdater.CheckForUpdateEvent -= Handler;

                    if (args.Error == null && args.IsUpdateAvailable)
                    {
                        // آپدیت پیدا شد -> دانلود شروع شود
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TxtStatus.Text = "نسخه جدید یافت شد...";
                            TxtStatus.Foreground = System.Windows.Media.Brushes.Cyan;
                            StartCustomDownload(args.DownloadURL);
                        });
                        // اینجا Task تمام میشود اما متد دانلود ادامه میدهد
                        tcs.TrySetResult(true); 
                    }
                    else
                    {
                        // آپدیت نیست -> ادامه به لاگین
                        tcs.TrySetResult(false);
                    }
                }

                AutoUpdater.CheckForUpdateEvent += Handler;

                string baseUpdateUrl = "https://raw.githubusercontent.com/mohammadrezahaghi/FaktorApp/main/update.xml";
                string updateUrl = $"{baseUpdateUrl}?t={DateTime.Now.Ticks}";

                AutoUpdater.Start(updateUrl);

                // *** تایم‌اوت هوشمند 4 ثانیه‌ای ***
                var timeoutTask = Task.Delay(4000, token);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // اگر طول کشید (اینترنت کند)، بیخیال شو و برو تو برنامه
                    Debug.WriteLine("Update Check Timed Out.");
                    AutoUpdater.CheckForUpdateEvent -= Handler;
                    PerformAutoLoginAndFinish();
                }
                else
                {
                    // نتیجه آمد
                    bool updateFound = await tcs.Task;
                    if (!updateFound)
                    {
                        // آپدیتی نبود، برو تو برنامه
                        PerformAutoLoginAndFinish();
                    }
                    // اگر آپدیت بود، متد دانلود اجرا شده و صفحه باز میماند
                }
            }
            catch
            {
                PerformAutoLoginAndFinish();
            }
        }

        // پینگ فوق سریع (تایم اوت 1.5 ثانیه)
        private bool IsInternetAvailable()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send("8.8.8.8", 1500); 
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        // متد خالی برای جلوگیری از ارور کامپایل احتمالی
        private void AutoUpdater_OnCheckForUpdateEvent(UpdateInfoEventArgs args) { }

        private void StartCustomDownload(string url)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                PrgUpdate.Visibility = Visibility.Visible;
                TxtPercent.Visibility = Visibility.Visible;
                TxtStatus.Text = "در حال دانلود آپدیت...";
            });

            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), "FactorApp_Setup.exe");
                if (File.Exists(tempPath)) File.Delete(tempPath);

#pragma warning disable SYSLIB0014
                WebClient webClient = new WebClient();
#pragma warning restore SYSLIB0014

                webClient.DownloadProgressChanged += (s, e) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PrgUpdate.Value = e.ProgressPercentage;
                        TxtPercent.Text = $"{e.ProgressPercentage}%";
                        TxtStatus.Text = $"دانلود... ({e.ProgressPercentage}%)";
                    });
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
            Application.Current.Dispatcher.Invoke(() => TxtStatus.Text = "نصب نسخه جدید...");
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

        // --- ورود خودکار ---
        private async void PerformAutoLoginAndFinish()
        {
            if (_cts.IsCancellationRequested) return;
            _cts.Cancel();

            Application.Current.Dispatcher.Invoke(() => 
            {
                TxtStatus.Text = "ورود به سیستم..."; // متن نهایی
                PrgUpdate.Visibility = Visibility.Collapsed;
                TxtPercent.Visibility = Visibility.Collapsed;
            });

            bool loginSuccess = false;

            await Task.Run(() =>
            {
                if (CredentialsHelper.GetSavedCredentials(out string savedUser, out string savedPass))
                {
                    loginSuccess = TryAutoLogin(savedUser, savedPass, out _loggedInUser);
                }
            });

            // مکث خیلی کوتاه برای زیبایی UI
            await Task.Delay(150);

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

        // چک کردن دیتابیس (Asynchronous)
        private Task<bool> EnsureDatabaseAndAdminUserAsync()
        {
            return Task.Run(() =>
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
                        }

                        if (!context.Users.Any())
                        {
                            string passwordHash;
                            string passwordSalt;
                            PasswordHelper.CreatePasswordHash("admin", out passwordHash, out passwordSalt);

                            context.Users.Add(new User
                            {
                                Username = "admin",
                                PasswordHash = passwordHash,
                                PasswordSalt = passwordSalt,
                                FullName = "مدیر سیستم",
                                IsActive = true,
                            });
                        }
                        context.SaveChanges();
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
            });
        }

        private bool CheckLicense()
        {
            try
            {
                if (File.Exists("license.key"))
                {
                    string savedKey = File.ReadAllText("license.key").Trim();
                    string systemId = LicenseHelper.GetSystemId();
                    return LicenseHelper.ValidateLicense(systemId, savedKey);
                }
            }
            catch { }
            return false;
        }
    }
}