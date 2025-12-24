using System.Windows;
using System.Windows.Documents;

namespace FactorApp.UI
{
    public partial class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow(IDocumentPaginatorSource doc)
        {
            InitializeComponent();
            DocViewer.Document = doc;
        }
    }
}