using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using FactorApp.UI.Models;
using Brushes = System.Windows.Media.Brushes;
namespace FactorApp.UI.Helpers
{
    // تبدیل وضعیت پرداخت به رنگ (سبز/قرمز)
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPaid && isPaid)
                return Brushes.Green; // پرداخت شده
            return Brushes.Red;       // پرداخت نشده
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // تبدیل وضعیت پرداخت به متن فارسی
    public class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPaid && isPaid)
                return "پرداخت شده";
            return "پرداخت نشده";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    
    // تبدیل Enum روش پرداخت به متن فارسی خوانا
    public class PaymentMethodToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PaymentMethod method)
            {
                switch (method)
                {
                    case PaymentMethod.None: return "---";
                    case PaymentMethod.Cash: return "نقد";
                    case PaymentMethod.Card: return "کارتخوان";
                    case PaymentMethod.CardToCard: return "کارت به کارت";
                    case PaymentMethod.Cheque: return "چک";
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}