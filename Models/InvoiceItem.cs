using System.ComponentModel.DataAnnotations.Schema;

namespace FactorApp.UI.Models // <--- چک کن
{
    public class InvoiceItem
    {
        public int Id { get; set; }

        // ارتباط با فاکتور اصلی
        public int InvoiceId { get; set; }
        public virtual Invoice Invoice { get; set; } = null!;

        // نام خدمات در لحظه ثبت (شاید بعداً نام اصلی سرویس تغییر کند، اما فاکتور قدیم نباید عوض شود)
        public string ServiceName { get; set; } = string.Empty;

        // ابعاد (برای بنر، فلکس، استیکر و...)
        // اگر کارت ویزیت بود، این‌ها 0 یا 1 در نظر گرفته می‌شوند
        public double Width { get; set; } = 0; // متر
        public double Length { get; set; } = 0; // متر

        public int Quantity { get; set; } = 1; // تعداد

        public decimal UnitPrice { get; set; } // قیمت واحد
        public decimal TotalPrice { get; set; } // قیمت نهایی این ردیف (تعداد * متر * قیمت)
        [NotMapped] // یعنی در دیتابیس ذخیره نشود
        public bool IsSelected { get; set; }
    }
}