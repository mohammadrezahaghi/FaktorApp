using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FactorApp.UI.Helpers
{
    public static class CredentialsHelper
    {
        // مسیر ذخیره فایل در AppData کاربر
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FactorApp",
            "user.dat");

        // ذخیره نام کاربری و رمز عبور (رمزنگاری شده)
        public static void SaveCredentials(string username, string password)
        {
            try
            {
                // ساخت پوشه اگر وجود نداشت
                var directory = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                // ترکیب نام کاربری و رمز با یک جداکننده
                string data = $"{username}|{password}";
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);

                // رمزنگاری با استفاده از DPAPI (فقط توسط همین کاربر ویندوز قابل رمزگشایی است)
                byte[] encryptedData = ProtectedData.Protect(dataBytes, null, DataProtectionScope.CurrentUser);

                File.WriteAllBytes(FilePath, encryptedData);
            }
            catch { /* مدیریت خطا (اختیاری) */ }
        }

        // خواندن و بررسی اطلاعات ذخیره شده
        public static bool GetSavedCredentials(out string username, out string password)
        {
            username = null;
            password = null;

            if (!File.Exists(FilePath)) return false;

            try
            {
                byte[] encryptedData = File.ReadAllBytes(FilePath);

                // رمزگشایی
                byte[] dataBytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
                string data = Encoding.UTF8.GetString(dataBytes);

                var parts = data.Split('|');
                if (parts.Length == 2)
                {
                    username = parts[0];
                    password = parts[1];
                    return true;
                }
            }
            catch
            {
                // اگر فایل دستکاری شده باشد یا رمزگشایی نشود، آن را پاک می‌کنیم
                ClearCredentials();
            }
            return false;
        }

        // حذف اطلاعات (برای خروج / Logout)
        public static void ClearCredentials()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }
    }
}