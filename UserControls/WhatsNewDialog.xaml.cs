using System.Windows.Controls;

namespace FactorApp.UI // دقت کنید Namespace درست باشد
{
    public partial class WhatsNewDialog : System.Windows.Controls.UserControl
    {
        public WhatsNewDialog()
        {
            InitializeComponent(); // این خط حیاتی است!
        }
        // این متد حیاتی است برای اینکه بتوانیم از MainWindow اطلاعات را پر کنیم
        public void Setup(string title, string version, string description, List<string> features)
        {
            if (TxtTitle != null) TxtTitle.Text = title;
            if (TxtVersion != null) TxtVersion.Text = version;
            if (TxtDescription != null) TxtDescription.Text = description;
            if (LstFeatures != null) LstFeatures.ItemsSource = features;
        }
    }
}