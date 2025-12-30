using System;
using System.Management; // نیاز به رفرنس System.Management دارد
using System.Security.Cryptography;
using System.Text;

namespace FactorApp.UI.Helpers
{
    public static class LicenseHelper
    {
        // این کلید مخفی فقط دست شماست (تغییرش دهید و به کسی ندهید)
        private const string SecretKey = "MysuperSecretKey_ShayanPrint_2025"; 

        // دریافت شناسه یکتای سخت‌افزار (CPU + Motherboard)
        public static string GetSystemId()
        {
            try
            {
                string cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");
                string boardId = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
                
                // ترکیب و ساده‌سازی کد برای نمایش به کاربر
                string rawId = $"{cpuId}{boardId}";
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                    return BitConverter.ToString(hash).Replace("-", "").Substring(0, 12);
                }
            }
            catch
            {
                return "UNKNOWN-ID";
            }
        }

        private static string GetWmiProperty(string className, string propertyName)
        {
            try
            {
                // نکته: پکیج System.Management باید نصب باشد (در دات نت کور/5+ شاید نیاز به نصب پکیج ناگت باشد)
                // اگر روی .NET Framework هستید، رفرنس آن را Add Reference کنید.
                using (var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var val = obj[propertyName]?.ToString();
                        if (!string.IsNullOrEmpty(val)) return val;
                    }
                }
            }
            catch { }
            return "00000000";
        }

        // تولید لایسنس بر اساس کد سیستم (این متد در برنامه KeyGen شما استفاده می‌شود)
        public static string GenerateLicenseKey(string systemId)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SecretKey)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(systemId));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
            }
        }

        // بررسی صحت لایسنس
        public static bool ValidateLicense(string systemId, string inputLicense)
        {
            string expectedLicense = GenerateLicenseKey(systemId);
            return string.Equals(expectedLicense, inputLicense, StringComparison.OrdinalIgnoreCase);
        }
    }
}