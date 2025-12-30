using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace FactorApp.UI.UserControls
{
    // 1. تعریف نوع پیام (Enum)
    // این همان چیزی است که SettingsPage دنبالش می‌گردد
    public enum MessageType
    {
        Info,
        Success,
        Error,
        Warning
    }

    public partial class MessageDialog : System.Windows.Controls.UserControl
    {
        // 2. متد سازنده که پیام و نوع آن را می‌گیرد
        public MessageDialog(string message, MessageType type = MessageType.Info)
        {
            InitializeComponent();
            
            // ست کردن متن
            TxtMessage.Text = message;
            
            // ست کردن آیکون و رنگ
            SetTheme(type);
        }

        // 3. متد تغییر ظاهر بر اساس نوع پیام
        private void SetTheme(MessageType type)
        {
            switch (type)
            {
                case MessageType.Success:
                    MsgIcon.Kind = PackIconKind.CheckCircle;
                    MsgIcon.Foreground = Brushes.Green;
                    MsgTitle.Text = "موفقیت";
                    break;

                case MessageType.Error:
                    MsgIcon.Kind = PackIconKind.CloseCircle;
                    MsgIcon.Foreground = Brushes.Red;
                    MsgTitle.Text = "خطا";
                    break;

                case MessageType.Warning:
                    MsgIcon.Kind = PackIconKind.AlertCircle;
                    MsgIcon.Foreground = Brushes.Orange;
                    MsgTitle.Text = "هشدار";
                    break;

                case MessageType.Info:
                default:
                    MsgIcon.Kind = PackIconKind.Information;
                    MsgIcon.Foreground = (Brush)FindResource("PrimaryHueMidBrush"); // رنگ اصلی تم
                    MsgTitle.Text = "اطلاعیه";
                    break;
            }
        }
    }
}