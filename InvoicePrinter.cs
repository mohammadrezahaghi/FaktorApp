using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FactorApp.UI.Models;

// رفع تداخل‌ها
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using PrintDialog = System.Windows.Controls.PrintDialog;
using MessageBox = System.Windows.MessageBox;
using Size = System.Windows.Size;
using Point = System.Windows.Point;
using FontFamily = System.Windows.Media.FontFamily;
using FlowDirection = System.Windows.FlowDirection;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;

namespace FactorApp.UI.Helpers
{
    public class InvoicePrinter
    {
        private Invoice _invoice;
        private const double PageWidth = 794; // A5 Width (Landscape approx)
        private const double PageHeight = 560; // A5 Height

        public InvoicePrinter(Invoice invoice)
        {
            _invoice = invoice;
        }

        private string ToPersian(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("0", "۰").Replace("1", "۱").Replace("2", "۲")
                        .Replace("3", "۳").Replace("4", "۴").Replace("5", "۵")
                        .Replace("6", "۶").Replace("7", "۷").Replace("8", "۸").Replace("9", "۹");
        }

        // 1. چاپ (Print)
        public void Print()
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                try
                {
                    printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                    printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(System.Printing.PageMediaSizeName.ISOA5);
                }
                catch { }

                FlowDocument doc = CreateDesign();
                doc.PageHeight = PageHeight;
                doc.PageWidth = PageWidth;
                doc.PagePadding = new Thickness(0);
                doc.ColumnWidth = PageWidth;

                IDocumentPaginatorSource idp = doc;
                printDialog.PrintDocument(idp.DocumentPaginator, "Factor_" + _invoice.InvoiceNumber);
            }
        }

        // 2. ساخت عکس (خروجی برای کلیپ‌بورد و ذخیره)
        public RenderTargetBitmap GenerateImage()
        {
            // ساخت داکیومنت
            FlowDocument doc = CreateDesign();
            doc.PageHeight = PageHeight;
            doc.PageWidth = PageWidth;
            doc.PagePadding = new Thickness(0);
            doc.ColumnWidth = PageWidth;

            // آماده‌سازی صفحه‌بندی
            DocumentPaginator paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            paginator.PageSize = new Size(PageWidth, PageHeight);

            // رندر کردن صفحه اول به عکس
            using (var page = paginator.GetPage(0))
            {
                var visual = page.Visual;

                // کیفیت عکس (1.5 برابر برای وضوح بهتر)
                double scale = 1.5;

                RenderTargetBitmap renderTarget = new RenderTargetBitmap(
                    (int)(PageWidth * scale),
                    (int)(PageHeight * scale),
                    96 * scale,
                    96 * scale,
                    PixelFormats.Pbgra32);

                renderTarget.Render(visual);
                return renderTarget;
            }
        }

        // 3. ذخیره عکس در فایل (Save As)
        public void SaveAsImage()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                FileName = $"Factor_{_invoice.InvoiceNumber}_{DateTime.Now:yyyyMMdd-HHmm}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = GenerateImage(); // استفاده از متد بالا
                    BitmapEncoder encoder;

                    if (saveDialog.FileName.ToLower().EndsWith(".jpg"))
                        encoder = new JpegBitmapEncoder();
                    else
                        encoder = new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(bitmap));

                    using (FileStream stream = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(stream);
                    }
                    MessageBox.Show("فایل با موفقیت ذخیره شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("خطا در ذخیره عکس:\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 4. ذخیره در فایل موقت (برای واتساپ)
        public string SaveToTempFile()
        {
            try
            {
                string fileName = $"Factor_{_invoice.InvoiceNumber}_{DateTime.Now.Ticks}.png";
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);

                var bitmap = GenerateImage(); // استفاده از متد بالا
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using (FileStream stream = new FileStream(tempPath, FileMode.Create))
                {
                    encoder.Save(stream);
                }
                return tempPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در ساخت عکس موقت: " + ex.Message);
                return null;
            }
        }

        // --- طراحی گرافیکی فاکتور (FlowDocument) ---
        private FlowDocument CreateDesign()
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FlowDirection = FlowDirection.RightToLeft,
                TextAlignment = TextAlignment.Left,
                Background = Brushes.White
            };

            var shayanBlue = new SolidColorBrush(Color.FromRgb(20, 30, 100));

            Grid wrapperGrid = new Grid();
            Grid invoiceLayer = new Grid { Margin = new Thickness(30) };
            invoiceLayer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.3, GridUnitType.Star) });
            invoiceLayer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // >>> سایدبار
            StackPanel sidebar = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            Image logoImage = new Image { Width = 80, Height = 80, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            try { logoImage.Source = new BitmapImage(new Uri("pack://application:,,,/logo.png")); } catch { }
            sidebar.Children.Add(logoImage);

            sidebar.Children.Add(new TextBlock { Text = "مرکز چاپ شایان (۱)", FontSize = 16, FontWeight = FontWeights.ExtraBold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center });
            sidebar.Children.Add(new TextBlock { Text = "Digital Printing Center", FontSize = 11, Foreground = shayanBlue, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            sidebar.Children.Add(new TextBlock { Text = "Indoor - Outdoor", FontSize = 10, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center });
            sidebar.Children.Add(new TextBlock { Text = "مجری کلیه امور چاپی", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 15, 0, 0) });
            sidebar.Children.Add(new TextBlock { Text = "افست - دیجیتال", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 20) });
            sidebar.Children.Add(new TextBlock { Text = $"تاریخ: {ToPersian(DateUtils.ToShamsi(_invoice.Date))}", FontSize = 11, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) });

            StackPanel footerInfo = new StackPanel { Margin = new Thickness(0, 50, 0, 0) };
            footerInfo.Children.Add(new TextBlock { Text = "تهران، خیابان دکتر فاطمی، روبروی سازمان آب، پلاک ۲۰۵", FontSize = 8, TextWrapping = TextWrapping.Wrap, Foreground = shayanBlue, TextAlignment = TextAlignment.Center });
            footerInfo.Children.Add(new TextBlock { Text = "تلفن : " + ToPersian("88954562 - 88960183"), FontSize = 9, FontWeight = FontWeights.Bold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 0) });
            footerInfo.Children.Add(new TextBlock { Text = "تلگرام، واتساپ: " + ToPersian("09196847300"), FontSize = 8, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
            footerInfo.Children.Add(new TextBlock { Text = "Email: shayandigital@yahoo.com", FontSize = 8, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
            sidebar.Children.Add(footerInfo);
            Grid.SetColumn(sidebar, 1);
            invoiceLayer.Children.Add(sidebar);

            // >>> محتوا
            StackPanel contentPanel = new StackPanel();
            StackPanel headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            headerPanel.Children.Add(new TextBlock { Text = "مشتری محترم: ", FontSize = 10, Foreground = shayanBlue, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 5, 0) });
            Grid nameContainer = new Grid();
            nameContainer.Children.Add(new TextBlock { Text = "...............................................................................", FontSize = 10, Foreground = shayanBlue, VerticalAlignment = VerticalAlignment.Bottom });
            nameContainer.Children.Add(new TextBlock { Text = _invoice.Customer.Name, FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 7) });
            headerPanel.Children.Add(nameContainer);
            contentPanel.Children.Add(headerPanel);

            // جدول
            Grid tableGrid = new Grid();
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.3, GridUnitType.Star) });
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2.5, GridUnitType.Star) });
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.7, GridUnitType.Star) });
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddHeaderCell(tableGrid, "ردیف", 0, 0);
            AddHeaderCell(tableGrid, "موضوع", 1, 0);
            AddHeaderCell(tableGrid, "تعداد", 2, 0);
            AddHeaderCell(tableGrid, "فـی", 3, 0);
            AddHeaderCell(tableGrid, "جمع کـل", 4, 0);

            int rowIndex = 1;
            int itemCounter = 1;
            var rowHeight = new GridLength(29);

            foreach (var item in _invoice.Items)
            {
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = rowHeight });
                string desc = item.ServiceName + (item.Width > 0 ? $" ({ToPersian(item.Width.ToString())}x{ToPersian(item.Length.ToString())})" : "");
                AddBodyCell(tableGrid, ToPersian(itemCounter.ToString()), 0, rowIndex);
                AddBodyCell(tableGrid, desc, 1, rowIndex);
                AddBodyCell(tableGrid, ToPersian(item.Quantity.ToString()), 2, rowIndex);
                AddBodyCell(tableGrid, ToPersian(item.UnitPrice.ToString("N0")), 3, rowIndex);
                AddBodyCell(tableGrid, ToPersian(item.TotalPrice.ToString("N0")), 4, rowIndex);
                rowIndex++; itemCounter++;
            }

            while (rowIndex <= 11)
            {
                tableGrid.RowDefinitions.Add(new RowDefinition { Height = rowHeight });
                for (int i = 0; i < 5; i++) AddBodyCell(tableGrid, "", i, rowIndex);
                rowIndex++;
            }

            tableGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) });
            Border totalLabelBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5), Padding = new Thickness(5) };
            TextBlock totalLabelTxt = new TextBlock { Text = "جمع کـل:", FontWeight = FontWeights.Bold, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            totalLabelBorder.Child = totalLabelTxt;
            Grid.SetColumn(totalLabelBorder, 0); Grid.SetRow(totalLabelBorder, rowIndex); Grid.SetColumnSpan(totalLabelBorder, 4);
            tableGrid.Children.Add(totalLabelBorder);
            AddBodyCell(tableGrid, ToPersian(_invoice.FinalAmount.ToString("N0")), 4, rowIndex, true);
            contentPanel.Children.Add(tableGrid);

            // مبلغ به حروف
            Border wordsBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5, 0, 0.5, 0.5), Padding = new Thickness(5) };
            StackPanel wordsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            wordsPanel.Children.Add(new TextBlock { Text = "به حروف: ", FontWeight = FontWeights.Bold, FontSize = 10 });
            try
            {
                string amountInWords = NumberToText.ToString(_invoice.FinalAmount);
                wordsPanel.Children.Add(new TextBlock { Text = $"{amountInWords} ریال", FontSize = 10, Margin = new Thickness(5, 0, 0, 0) });
            }
            catch { }
            wordsBorder.Child = wordsPanel;
            contentPanel.Children.Add(wordsBorder);

            Grid footerGrid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition());
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition());
            footerGrid.Children.Add(new TextBlock { Text = "خـریـدار", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 40, 0), FontSize = 11, FontWeight = FontWeights.Bold });
            TextBlock sellerTxt = new TextBlock { Text = "فـروشـنـده", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(40, 0, 0, 0), FontSize = 11, FontWeight = FontWeights.Bold };
            Grid.SetColumn(sellerTxt, 1);
            footerGrid.Children.Add(sellerTxt);
            contentPanel.Children.Add(footerGrid);

            Grid.SetColumn(contentPanel, 0);
            invoiceLayer.Children.Add(contentPanel);
            wrapperGrid.Children.Add(invoiceLayer);

            // مهر
            Image stampImage = new Image { Width = 250, Height = 150, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, IsHitTestVisible = false };
            stampImage.RenderTransformOrigin = new Point(0.5, 0.5);
            stampImage.RenderTransform = new RotateTransform(20);
            try
            {
                if (_invoice.IsPaid) stampImage.Source = new BitmapImage(new Uri("pack://application:,,,/paid.png"));
                else stampImage.Source = new BitmapImage(new Uri("pack://application:,,,/unpaid.png"));
            }
            catch { }
            wrapperGrid.Children.Add(stampImage);

            doc.Blocks.Add(new BlockUIContainer(wrapperGrid));
            return doc;
        }

        private void AddHeaderCell(Grid grid, string text, int col, int row)
        {
            Border b = new Border { Background = new SolidColorBrush(Color.FromRgb(230, 231, 232)), BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5), Padding = new Thickness(2) };
            b.Child = new TextBlock { Text = text, FontWeight = FontWeights.Bold, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(b, col); Grid.SetRow(b, row); grid.Children.Add(b);
        }

        private void AddBodyCell(Grid grid, string text, int col, int row, bool isBold = false)
        {
            Border b = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5), Padding = new Thickness(2) };
            b.Child = new TextBlock { Text = text, FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(b, col); Grid.SetRow(b, row); grid.Children.Add(b);
        }
    }
}