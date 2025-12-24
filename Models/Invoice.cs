using System;
using System.Collections.Generic;

namespace FactorApp.UI.Models // <--- چک کن
{
    public enum InvoiceStatus
    {
        Pending,        // 0 = ثبت شده (در انتظار) -> جدید اضافه شد
        Draft,          // پیش‌فاکتور
        PendingDesign,  // در انتظار طراحی
        Printing,       // در حال چاپ
        ReadyToDeliver, // آماده تحویل
        Delivered,      // تحویل شده
        Canceled        // لغو شده
    }

    public class Invoice
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string InvoiceNumber { get; set; } = string.Empty; // شماره فاکتور دستی یا سیستمی

        // مشتری
        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; } = null!;

        // وضعیت سفارش (خیلی مهم برای چاپخانه)
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        // مالیات و تخفیف
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalAmount { get; set; } // مبلغ نهایی قابل پرداخت

        // آیا پرداخت شده؟
        // --- فیلدهای جدید ---
        public bool IsPaid { get; set; } = false; // وضعیت پرداخت
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.None; // روش پرداخت
        public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
}