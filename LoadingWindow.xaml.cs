using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace FactorApp.UI
{
    public partial class LoadingWindow : Window
    {
        // ایونت برای اطلاع‌رسانی به App.xaml.cs
        public event Action OperationCompleted;

        public LoadingWindow()
        {
            InitializeComponent();

            // نمایش نسخه برنامه
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            // فرض بر این است که یک TextBlock با نام TxtVersion در XAML دارید
            if (TxtVersion != null)
                TxtVersion.Text = $"نسخه {version.Major}.{version.Minor}.{version.Build}";

            Loaded += LoadingWindow_Loaded;
        }

        private async void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. بررسی و ایجاد دیتابیس (Async)
            // اگر تکست‌باکس وضعیت دارید می‌توانید متن آن را تغییر دهید
            // TxtStatus.Text = "در حال بررسی بانک اطلاعاتی..."; 

            bool dbSuccess = await Task.Run(() => EnsureDatabaseAndAdminUser());

            if (!dbSuccess)
            {
                MessageBox.Show("خطا در ارتباط با بانک اطلاعاتی. برنامه بسته می‌شود.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // 2. بررسی آپدیت
            // TxtStatus.Text = "بررسی نسخه جدید...";
            await Task.Delay(1000); // یک وقفه کوتاه برای زیبایی
            CheckForUpdate();
        }

        // انتقال متد ساخت دیتابیس به اینجا برای اجرای Async
        private bool EnsureDatabaseAndAdminUser()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    // 1. ساخت دیتابیس اگر وجود نداشته باشد
                    // (چون فایل قبلی را پاک کردید، اینجا دیتابیس جدید و سالم ساخته می‌شود)
                    context.Database.EnsureCreated();

                    bool needsSave = false;

                    // 2. بررسی وجود ادمین
                    var adminUser = context.Users.FirstOrDefault(u => u.Username == "admin");

                    if (adminUser == null)
                    {
                        PasswordHelper.CreatePasswordHash("admin", out string hash, out string salt);

                        var admin = new User
                        {
                            Username = "admin",
                            FullName = "مدیر سیستم",
                            PasswordHash = hash,
                            PasswordSalt = salt,
                            IsActive = true
                            // فیلدهای Role و CreatedAt حذف شدند چون در مدل شما نیستند
                        };

                        context.Users.Add(admin);
                        needsSave = true;
                    }

                    // 3. بررسی تنظیمات فروشگاه
                    if (!context.StoreInfos.Any())
                    {
                        context.StoreInfos.Add(new StoreInfo
                        {
                            IsDarkMode = false,
                            StoreName = "فروشگاه من",
                            Address = "آدرس پیش فرض",
                            PhoneNumber = "-",
                            FooterText = "توضیحات فاکتور"
                        });
                        needsSave = true;
                    }

                    // 4. ذخیره تغییرات
                    if (needsSave)
                    {
                        context.SaveChanges();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"خطای دیتابیس:\n{errorMessage}\n\nراه حل: فایل دیتابیس قدیمی را از پوشه bin پاک کنید.",
                                    "Database Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                });

                return false;
            }
        }

        private void CheckForUpdate()
        {
            try
            {
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.DownloadPath = Environment.CurrentDirectory;

                // استفاده از ایونت برای کنترل فلو (چون می‌خواهیم اگر آپدیت نبود، برنامه باز شود)
                AutoUpdater.CheckForUpdateEvent += AutoUpdater_OnCheckForUpdateEvent;

                // لینک فایل XML آپدیت
                string updateUrl = "https://raw.githubusercontent.com/mohammadrezahaghi/FaktorApp/main/update.xml";
                AutoUpdater.Start(updateUrl);
            }
            catch
            {
                // اگر اینترنت نبود یا هر خطایی شد، برنامه را ادامه بده
                FinishLoading();
            }
        }

        private void AutoUpdater_OnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            AutoUpdater.CheckForUpdateEvent -= AutoUpdater_OnCheckForUpdateEvent;

            if (args.Error != null)
            {
                // خطا در چک کردن آپدیت (مثلا قطعی نت) -> ادامه برنامه
                FinishLoading();
                return;
            }

            if (args.IsUpdateAvailable)
            {
                MessageBoxResult dialogResult;
                if (args.Mandatory.Value)
                {
                    dialogResult = MessageBox.Show($"نسخه جدید {args.CurrentVersion} موجود است. برای استفاده باید بروزرسانی کنید.", "بروزرسانی اجباری", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    dialogResult = MessageBox.Show($"نسخه جدید {args.CurrentVersion} موجود است. آیا مایل به دانلود هستید؟", "بروزرسانی", MessageBoxButton.YesNo, MessageBoxImage.Question);
                }

                if (dialogResult == MessageBoxResult.Yes || dialogResult == MessageBoxResult.OK)
                {
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            Application.Current.Shutdown();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "خطا در دانلود", MessageBoxButton.OK, MessageBoxImage.Error);
                        FinishLoading();
                    }
                }
                else
                {
                    // کاربر آپدیت را رد کرد
                    FinishLoading();
                }
            }
            else
            {
                // آپدیتی نیست
                FinishLoading();
            }
        }

        private void FinishLoading()
        {
            // اطلاع به App.xaml.cs
            OperationCompleted?.Invoke();
            this.Close();
        }
    }
}