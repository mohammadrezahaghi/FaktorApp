using System.Windows.Controls; 

namespace FactorApp.UI.UserControls
{
    // از System.Windows.Controls.UserControl ارث‌بری می‌کنیم تا با WinForms تداخل نکند
    public partial class ConfirmDialog : System.Windows.Controls.UserControl
    {
        public ConfirmDialog(string message)
        {
            InitializeComponent();
            TxtMessage.Text = message;
        }
    }
}