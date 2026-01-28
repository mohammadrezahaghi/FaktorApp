using System;

namespace FactorApp.UI.Helpers
{
    public static class NumberToText
    {
        private static readonly string[] Yakan = { "", "یــــــک", "دو", "ســـــه", "چهــــــار", "پنــــــج", "شــــش", "هفـــــت", "هشــــــت", "نـــه" };
        private static readonly string[] Dahgan = { "", "ده", "بیســــت", "ســـــی", "چـــــهل", "پنــــجاه", "شصـــــت", "هفـــتاد", "هشـــتاد", "نـــود" };
        private static readonly string[] Dahyek = { "ده", "یــــازده", "دوازده", "ســـیزده", "چهـــــارده", "پانـــزده", "شانــــزده", "هفــــده", "هجــــده", "نـــوزده" };
        private static readonly string[] Sadgan = { "", "صــــد", "دویــــست", "ســــیصـد", "چـهارصــد", "پانـــصـد", "ششصـــد", "هفتصـــد", "هشتصـــد", "نهصـــد" };
        private static readonly string[] Basex = { "", "هــــزار", "مــیلیـون", "میـلیـارد", "تریلیـــون" };

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