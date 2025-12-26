using System;
using System.IO;

namespace FactorApp
{
    public static class VersionStorage
    {
        // مسیر فایل ذخیره نسخه (در پوشه AppData ویندوز ذخیره می‌شود تا امن باشد)
        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "FactorApp_Version.txt");

        // متد خواندن آخرین نسخه
        public static string GetLastRunVersion()
        {
            if (File.Exists(FilePath))
            {
                return File.ReadAllText(FilePath);
            }
            return "0.0.0.0"; // اگر فایل نبود (اولین اجرای برنامه)
        }

        // متد ذخیره نسخه جدید
        public static void SaveNewVersion(string version)
        {
            try
            {
                File.WriteAllText(FilePath, version);
            }
            catch
            {
                // اگر خطایی رخ داد (نادیده بگیر)
            }
        }
    }
}