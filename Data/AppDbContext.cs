using FactorApp.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace FactorApp.UI.Data
{
    public class AppDbContext : DbContext
    {
        // تبدیل کلاس‌ها به جداول دیتابیس
        public DbSet<Service> Services { get; set; } 
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<User> Users { get; set; } // این خط را اضافه کنید
        public DbSet<StoreInfo> StoreInfos { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // 1. مسیر پوشه AppData ویندوز را پیدا میکنیم (جایی که اجازه نوشتن داریم)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 2. یک پوشه به نام برنامه خودمان در آنجا در نظر میگیریم
            string folderPath = System.IO.Path.Combine(appData, "FactorApp");

            // 3. اگر پوشه وجود نداشت، آن را میسازیم (خیلی مهم)
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            // 4. مسیر نهایی فایل دیتابیس
            string dbPath = System.IO.Path.Combine(folderPath, "FactorApp.db");

            // 5. اتصال به دیتابیس در مسیر جدید
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // تنظیمات حرفه‌ای برای نوع داده‌های پول (Decimal)
            // این کار باعث می‌شود در SQL دقیقاً با فرمت پول ذخیره شوند و وارنینگ نگیریم
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18,2)");
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}