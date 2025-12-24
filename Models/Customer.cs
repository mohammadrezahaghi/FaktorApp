namespace FactorApp.UI.Models // <--- این خط باید دقیقاً همین باشد
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // نام مشتری یا نام شرکت
        public string PhoneNumber { get; set; } = string.Empty; // کلید اصلی شناسایی مشتری در اکثر چاپخانه‌ها
        public string? Address { get; set; }

        // مانده حساب (مثبت: بستانکار، منفی: بدهکار)
        public decimal Balance { get; set; } = 0;

        // لیست فاکتورهای این مشتری
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}