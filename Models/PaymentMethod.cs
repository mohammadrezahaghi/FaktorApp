namespace FactorApp.UI.Models
{
    public enum PaymentMethod
    {
        None = 0,       // پرداخت نشده
        Cash = 1,       // نقد
        Card = 2,       // کارتخوان
        CardToCard = 3, // کارت به کارت
        Cheque = 4      // چک
    }
}