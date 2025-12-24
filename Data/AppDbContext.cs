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
            // تنظیمات اتصال به دیتابیس
            // از LocalDB استفاده می‌کنیم که همراه ویژوال استودیو نصب است و نیازی به نصب SQL Server سنگین نیست
            // نام دیتابیس را PrintShopDb گذاشتم
            optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=PrintShopDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");
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