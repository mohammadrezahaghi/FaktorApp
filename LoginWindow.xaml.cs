using System.Windows;
using FactorApp.UI.Data;
using FactorApp.UI.Models;
using FactorApp.UI.Helpers; // حتما این namespace باشد
using System.Linq;
using MessageBox = System.Windows.MessageBox;

namespace FactorApp.UI
{
    public partial class LoginWindow : Window
    {
        // پراپرتی برای انتقال کاربر به App.xaml.cs
        public User? User { get; private set; }
        public bool IsLoggedIn { get; private set; } = false;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text;
            string password = TxtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("لطفا نام کاربری و رمز عبور را وارد کنید.");
                return;
            }

            try
            {
                using (var context = new AppDbContext())
                {
                    var user = context.Users.SingleOrDefault(u => u.Username.ToLower() == username.ToLower());

                    if (user != null && user.IsActive)
                    {
                        if (PasswordHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                        {
                            // *** لاگین موفق ***

                            // 1. بررسی تیک "به یاد بسپار" و ذخیره اطلاعات
                            // فرض بر این است که نام چک‌باکس در فایل XAML شما 'chkRememberMe' است
                            if (ChkRememberMe.IsChecked == true)
                            {
                                CredentialsHelper.SaveCredentials(username, password);
                            }
                            else
                            {
                                // اگر تیک را برداشته بود، اطلاعات قبلی را پاک کن
                                CredentialsHelper.ClearCredentials();
                            }

                            // 2. تنظیم پراپرتی‌ها و بستن پنجره
                            this.User = user;
                            this.IsLoggedIn = true;
                            this.DialogResult = true;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("نام کاربری یا رمز عبور اشتباه است.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("کاربری یافت نشد یا غیرفعال است.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("خطا در برقراری ارتباط با دیتابیس:\n" + ex.Message);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}