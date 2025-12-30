using System;
using System.Globalization;

namespace FactorApp.UI.Helpers
{
    public static class DateUtils
    {
        public static string ToShamsi(DateTime date)
        {
            PersianCalendar pc = new PersianCalendar();
            return $"{pc.GetYear(date):0000}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
        }
    }
}