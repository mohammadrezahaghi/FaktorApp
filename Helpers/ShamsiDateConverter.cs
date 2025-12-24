using System;
using System.Globalization;
using System.Windows.Data;

namespace FactorApp.UI.Helpers
{
    public class ShamsiDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return DateUtils.ToShamsi(date);
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}