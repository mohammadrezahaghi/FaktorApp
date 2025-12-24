using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;

// رفع تداخل‌ها
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Application = System.Windows.Application;
using WinFormsApp = System.Windows.Forms.Application;

namespace FactorApp.UI.Pages
{
    public partial class SettingsPage : Page
    {
        private readonly PaletteHelper _paletteHelper = new PaletteHelper();

        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
            // نکته: کد تنظیم اولیه تم را از اینجا برداشتیم و به متد LoadSettings بردیم
        }

        // --- بارگذاری تنظیمات اولیه ---
        private void LoadSettings()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var info = context.StoreInfos.FirstOrDefault();
                    if (info != null)
                    {
                        TxtStoreName.Text = info.StoreName;
                        TxtPhone.Text = info.PhoneNumber;
                        TxtAddress.Text = info.Address;
                        TxtFooter.Text = info.FooterText;

                        // >>>> بارگذاری وضعیت تم از دیتابیس <<<<
                        TglDarkMode.IsChecked = info.IsDarkMode;

                        // اعمال تم بر اساس دیتابیس (برای اطمینان)
                        var theme = _paletteHelper.GetTheme();
                        theme.SetBaseTheme(info.IsDarkMode ? BaseTheme.Dark : BaseTheme.Light);
                        _paletteHelper.SetTheme(theme);
                    }
                }
            }
            catch { }
        }

        // --- تغییر تم (دارک / لایت) و ذخیره در دیتابیس ---
        private void TglDarkMode_Click(object sender, RoutedEventArgs e)
        {
            bool isDark = TglDarkMode.IsChecked == true;

            // 1. اعمال تغییرات ظاهری
            var theme = _paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            _paletteHelper.SetTheme(theme);

            // 2. ذخیره در دیتابیس
            try
            {
                using (var context = new AppDbContext())
                {
                    var info = context.StoreInfos.FirstOrDefault();
                    if (info == null)
                    {
                        info = new StoreInfo();
                        context.StoreInfos.Add(info);
                    }

                    info.IsDarkMode = isDark;
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // در صورت خطا در ذخیره، پیام ندهیم بهتر است تا تجربه کاربری خراب نشود
                // یا می‌توان در کنسول لاگ کرد
                System.Diagnostics.Debug.WriteLine("Error saving theme: " + ex.Message);
            }
        }

        // --- ذخیره اطلاعات فروشگاه ---
        private void BtnSaveInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var info = context.StoreInfos.FirstOrDefault();
                    if (info == null)
                    {
                        info = new StoreInfo();
                        context.StoreInfos.Add(info);
                    }

                    info.StoreName = TxtStoreName.Text;
                    info.PhoneNumber = TxtPhone.Text;
                    info.Address = TxtAddress.Text;
                    info.FooterText = TxtFooter.Text;
                    // نکته: IsDarkMode را اینجا دست نمیزنیم تا تغییر نکند

                    context.SaveChanges();
                    MessageBox.Show("اطلاعات با موفقیت ذخیره شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در ذخیره سازی: " + ex.Message);
            }
        }

        // ... (بقیه متدها مثل تغییر رمز، خروج و بکاپ بدون تغییر باقی می‌مانند) ...

        private void BtnChangePass_Click(object sender, RoutedEventArgs e)
        {
            // کدهای قبلی خودتان را اینجا بگذارید
            // ...
            string oldPass = TxtOldPass.Password;
            string newPass = TxtNewPass.Password;

            if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
            {
                MessageBox.Show("لطفا هر دو فیلد رمز را پر کنید.");
                return;
            }

            string currentUsername = "admin";

            using (var context = new AppDbContext())
            {
                var user = context.Users.SingleOrDefault(u => u.Username == currentUsername);
                if (user != null)
                {
                    if (PasswordHelper.VerifyPasswordHash(oldPass, user.PasswordHash, user.PasswordSalt))
                    {
                        PasswordHelper.CreatePasswordHash(newPass, out string hash, out string salt);
                        user.PasswordHash = hash;
                        user.PasswordSalt = salt;
                        context.SaveChanges();

                        MessageBox.Show("رمز عبور با موفقیت تغییر کرد.");
                        TxtOldPass.Clear();
                        TxtNewPass.Clear();
                    }
                    else
                    {
                        MessageBox.Show("رمز عبور فعلی اشتباه است.");
                    }
                }
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("آیا مطمئن هستید که می‌خواهید خارج شوید؟", "خروج", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                CredentialsHelper.ClearCredentials();
                WinFormsApp.Restart();
                Application.Current.Shutdown();
            }
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "Database Backup (*.db)|*.db",
                FileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    if (System.IO.File.Exists("factor.db"))
                    {
                        System.IO.File.Copy("factor.db", saveDialog.FileName, true);
                        MessageBox.Show("پشتیبان‌گیری انجام شد.");
                    }
                    else
                    {
                        MessageBox.Show("فایل دیتابیس یافت نشد.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطا در پشتیبان‌گیری: " + ex.Message);
                }
            }
        }
    }
}