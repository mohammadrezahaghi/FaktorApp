using System.Linq;
using System.Windows;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;

namespace FactorApp.UI
{
    public partial class LoginWindow : Window
    {
        public bool IsLoggedIn { get; private set; } = false; // نتیجه لاگین

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
                System.Windows.MessageBox.Show("لطفا نام کاربری و رمز عبور را وارد کنید.");
                return;
            }

            using (var context = new AppDbContext())
            {
                // جستجوی کاربر (به حروف بزرگ و کوچک حساس نباشد)
                var user = context.Users.SingleOrDefault(u => u.Username.ToLower() == username.ToLower());

                if (user != null)
                {
                    // بررسی رمز عبور
                    if (PasswordHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                    {
                        if (!user.IsActive)
                        {
                            System.Windows.MessageBox.Show("حساب کاربری شما غیرفعال شده است.");
                            return;
                        }
                        // ... (داخل شرط موفقیت آمیز بودن لاگین) ...
                        if (PasswordHelper.VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
                        {
                            if (!user.IsActive) { /* ... */ return; }

                            // >>>> بخش جدید: ذخیره اطلاعات اگر تیک زده بود <<<<
                            if (ChkRememberMe.IsChecked == true)
                            {
                                // نام کاربری و رمز واقعی را ذخیره می‌کنیم (رمزنگاری شده)
                                // تا دفعه بعد بتوانیم دوباره لاگین کنیم
                                CredentialsHelper.SaveCredentials(username, password);
                            }
                            else
                            {
                                // اگر تیک را برداشت، اطلاعات قبلی را پاک کن
                                CredentialsHelper.ClearCredentials();
                            }

                            IsLoggedIn = true;
                            this.Close();
                        }
                        IsLoggedIn = true;
                        this.Close(); // بستن پنجره لاگین
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("نام کاربری یا رمز عبور اشتباه است.");
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("نام کاربری یا رمز عبور اشتباه است.");
                }
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}