using System;

namespace FactorApp.UI.Helpers
{
    public static class NumberToText
    {
        private static readonly string[] Yakan = { "", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه" };
        private static readonly string[] Dahgan = { "", "ده", "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود" };
        private static readonly string[] Dahyek = { "ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده" };
        private static readonly string[] Sadgan = { "", "صد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد" };
        private static readonly string[] Basex = { "", "هزار", "میلیون", "میلیارد", "تریلیون" };

        public static string ToString(long number)
        {
            if (number == 0) return "صفر";

            string fullNumber = number.ToString("000000000000");
            string result = "";

            int i = 0;
            while (i < 4)
            {
                int threeDigit = int.Parse(fullNumber.Substring(3 * i, 3));
                if (threeDigit > 0)
                {
                    string threeDigitText = GetThreeDigitText(threeDigit);
                    string scale = Basex[3 - i];

                    if (!string.IsNullOrEmpty(result))
                    {
                        result += " و ";
                    }

                    result += threeDigitText + (string.IsNullOrEmpty(scale) ? "" : " " + scale);
                }
                i++;
            }

            return result;
        }

        public static string ToString(decimal number)
        {
            return ToString((long)number);
        }

        private static string GetThreeDigitText(int number)
        {
            string result = "";

            int sadgan = number / 100;
            int baghimandeSadgan = number % 100;

            if (sadgan > 0)
            {
                result = Sadgan[sadgan];
                if (baghimandeSadgan > 0) result += " و ";
            }

            if (baghimandeSadgan > 0)
            {
                if (baghimandeSadgan < 10)
                {
                    result += Yakan[baghimandeSadgan];
                }
                else if (baghimandeSadgan >= 10 && baghimandeSadgan < 20)
                {
                    result += Dahyek[baghimandeSadgan - 10];
                }
                else
                {
                    int dahgan = baghimandeSadgan / 10;
                    int yakan = baghimandeSadgan % 10;

                    result += Dahgan[dahgan];
                    if (yakan > 0) result += " و " + Yakan[yakan];
                }
            }

            return result;
        }
    }
}