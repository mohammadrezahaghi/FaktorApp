using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace FactorApp.UI.UserControls
{
    // تعریف انواع دیالوگ تایید
    public enum ConfirmType
    {
        Info,       // معمولی (سبز/آبی)
        Warning,    // هشدار (زرد/نارنجی)
        Delete,     // حذف (قرمز)
        Logout,     // خروج (قرمز تیره)
        Question    // سوالی (پیش‌فرض)
    }

    public partial class ConfirmDialog : System.Windows.Controls.UserControl
    {
        public ConfirmDialog(string message, string title = "تایید عملیات", ConfirmType type = ConfirmType.Question)
        {
            InitializeComponent();

            TxtMessage.Text = message;
            TxtTitle.Text = title;

            ApplyTheme(type);
        }

        private void ApplyTheme(ConfirmType type)
        {
            var brushConverter = new BrushConverter();
            SolidColorBrush mainColor;
            PackIconKind iconKind;
            string confirmText = "بله";

            switch (type)
            {
                case ConfirmType.Delete:
                    mainColor = (SolidColorBrush)brushConverter.ConvertFrom("#FF5252"); // قرمز
                    iconKind = PackIconKind.DeleteForever;
                    confirmText = "حذف کن";
                    break;

                case ConfirmType.Logout:
                    mainColor = (SolidColorBrush)brushConverter.ConvertFrom("#D32F2F"); // قرمز تیره
                    iconKind = PackIconKind.Logout;
                    confirmText = "خروج";
                    break;

                case ConfirmType.Warning:
                    mainColor = (SolidColorBrush)brushConverter.ConvertFrom("#FFB300"); // زرد/نارنجی
                    iconKind = PackIconKind.AlertOutline;
                    confirmText = "متوجه شدم";
                    break;

                case ConfirmType.Info:
                    mainColor = (SolidColorBrush)brushConverter.ConvertFrom("#00C49F"); // سبز (تم اصلی)
                    iconKind = PackIconKind.InformationVariant;
                    confirmText = "تایید";
                    break;

                default: // Question
                    mainColor = (SolidColorBrush)brushConverter.ConvertFrom("#00C49F"); // سبز
                    iconKind = PackIconKind.QuestionMark;
                    confirmText = "بله";
                    break;
            }

            // اعمال رنگ‌ها به المان‌ها
            GlowBorder.Background = mainColor;
            RingBorder.BorderBrush = mainColor;
            IconDisplay.Foreground = mainColor;
            IconDisplay.Kind = iconKind;

            // استایل دکمه تایید (همرنگ با تم انتخاب شده)
            BtnConfirm.Background = mainColor;
            BtnConfirm.BorderBrush = mainColor;
            BtnConfirm.Content = confirmText;
        }
    }
}