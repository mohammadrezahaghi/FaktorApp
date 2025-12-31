using System;
using System.Linq;
using System.Windows; 
using System.Windows.Controls;
using MaterialDesignThemes.Wpf; // برای DialogHost
using Microsoft.Win32; // برای SaveFileDialog

// *** هماهنگی نام‌ها با FactorApp ***
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using FactorApp.UI.UserControls; // برای دسترسی به MessageDialog و ConfirmDialog

// تعریف نام مستعار فقط برای دسترسی به متد Restart
using WinForms = System.Windows.Forms;

namespace FactorApp.UI.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
        }

        // --- بارگذاری تنظیمات ---
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
                        TglDarkMode.IsChecked = info.IsDarkMode;
                    }
                }
            }
            catch { }
        }

        // --- متد کمکی برای نمایش پیام مدرن (جایگزین MessageBox) ---
        private async void ShowAlert(string message, MessageType type)
        {
            // ساخت دیالوگ جدید با استایل متریال
            var view = new MessageDialog(message, type);
            
            // نمایش روی RootDialog (که در MainWindow تعریف کردیم)
            await DialogHost.Show(view, "RootDialog");
        }

        // --- تغییر تم ---
        private void TglDarkMode_Click(object sender, RoutedEventArgs e)
        {
            bool isDark = TglDarkMode.IsChecked == true;

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

                // اعمال تم (اشاره به نسخه WPF کلاس Application)
                if (System.Windows.Application.Current is App myApp)
                {
                    myApp.ApplySavedTheme();
                }
            }
            catch (Exception ex)
            {
                ShowAlert("خطا در ذخیره تم: " + ex.Message, MessageType.Error);
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

                    context.SaveChanges();

                    // نمایش پیام موفقیت مدرن
                    ShowAlert("اطلاعات فروشگاه با موفقیت ذخیره شد.", MessageType.Success);
                }
            }
            catch (Exception ex)
            {
                // نمایش پیام خطا مدرن
                ShowAlert("خطا در ذخیره سازی: " + ex.Message, MessageType.Error);
            }
        }

        // --- تغییر رمز عبور ---
        private void BtnChangePass_Click(object sender, RoutedEventArgs e)
        {
            string oldPass = TxtOldPass.Password;
            string newPass = TxtNewPass.Password;

            if (string.IsNullOrWhiteSpace(oldPass) || string.IsNullOrWhiteSpace(newPass))
            {
                ShowAlert("لطفا هر دو فیلد رمز را پر کنید.", MessageType.Warning);
                return;
            }

            string currentUsername = "admin"; 

            try
            {
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

                            ShowAlert("رمز عبور با موفقیت تغییر کرد.", MessageType.Success);
                            TxtOldPass.Clear();
                            TxtNewPass.Clear();
                        }
                        else
                        {
                            ShowAlert("رمز عبور فعلی اشتباه است.", MessageType.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert("خطا: " + ex.Message, MessageType.Error);
            }
        }

        // --- خروج از حساب (با دیالوگ تایید مدرن) ---
    private async void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // استفاده از نوع Logout برای نمایش قرمز و آیکون خروج
            var dialog = new ConfirmDialog(
                message: "آیا مطمئن هستید که می‌خواهید از حساب کاربری خارج شوید؟",
                title: "خروج از حساب",
                type: ConfirmType.Logout
            );

            var result = await DialogHost.Show(dialog, "RootDialog");

            if (result is bool booleanResult && booleanResult == true)
            {
                CredentialsHelper.ClearCredentials();
                WinForms.Application.Restart();
                System.Windows.Application.Current.Shutdown();
            }
        }

        // --- پشتیبان‌گیری ---
        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Database Backup (*.db)|*.db",
                FileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    string dbName = "FactorApp.db"; 
                    
                    if (System.IO.File.Exists(dbName))
                    {
                        System.IO.File.Copy(dbName, saveDialog.FileName, true);
                        ShowAlert("پشتیبان‌گیری انجام شد.", MessageType.Success);
                    }
                    else
                    {
                        ShowAlert("فایل دیتابیس یافت نشد.", MessageType.Error);
                    }
                }
                catch (Exception ex)
                {
                    ShowAlert("خطا در پشتیبان‌گیری: " + ex.Message, MessageType.Error);
                }
            }
        }
    }
}