using System;
using System.Threading.Tasks;
using System.Windows;
using AutoUpdaterDotNET;

namespace FactorApp.UI
{
    public partial class LoadingWindow : Window
    {
        // 1. تعریف یک رویداد (Event) برای اینکه به App خبر بدیم کارمون تمومه
        public event Action OperationCompleted;

        public LoadingWindow()
        {
            InitializeComponent();
            Loaded += LoadingWindow_Loaded;
        }

        private async void LoadingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1500); // وقفه نمایشی
            CheckForUpdate();
        }

        private void CheckForUpdate()
        {
            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_OnCheckForUpdateEvent;

            // لینک خودتان را اینجا بگذارید
            string updateUrl = "https://raw.githubusercontent.com/mohammadrezahaghi/FaktorApp/refs/heads/main/update.xml";
            AutoUpdater.Start(updateUrl);
        }

        private void AutoUpdater_OnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            // قطع ایونت برای جلوگیری از تکرار
            AutoUpdater.CheckForUpdateEvent -= AutoUpdater_OnCheckForUpdateEvent;

            // اگر خطا بود یا آپدیتی نبود، کار تمام است
            if (args.Error != null || !args.IsUpdateAvailable)
            {
                FinishLoading();
                return;
            }

            // اگر آپدیت موجود بود
            if (args.IsUpdateAvailable)
            {
                MessageBoxResult dialogResult;
                if (args.Mandatory.Value)
                {
                    dialogResult = System.Windows.MessageBox.Show($"نسخه جدید {args.CurrentVersion} موجود است...", "آپدیت اجباری", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    dialogResult = System.Windows.MessageBox.Show($"نسخه جدید {args.CurrentVersion} موجود است. دانلود؟", "آپدیت", MessageBoxButton.YesNo, MessageBoxImage.Question);
                }

                if (dialogResult == MessageBoxResult.Yes || dialogResult == MessageBoxResult.OK)
                {
                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            System.Windows.Application.Current.Shutdown(); // بستن برای نصب
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show(ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                        FinishLoading();
                    }
                }
                else
                {
                    // کاربر گفت نه
                    FinishLoading();
                }
            }
        }

        // متد نهایی که به App خبر می‌دهد
        private void FinishLoading()
        {
            // صدا زدن ایونت برای App.xaml.cs
            OperationCompleted?.Invoke();

            // بستن خود پنجره لودینگ
            this.Close();
        }
    }
}