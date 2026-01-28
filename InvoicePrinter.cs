using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FactorApp.UI.Models;
using System.Globalization; // اضافه شده برای تاریخ شمسی

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
using Rectangle = System.Windows.Shapes.Rectangle;

namespace FactorApp.UI.Helpers
{
    public class InvoicePrinter
    {
        private Invoice _invoice;
        private const double PageWidth = 794; // استاندارد A5 عرض
        private const double PageHeight = 560; // استاندارد A5 ارتفاع

        private const string CardNumber = "6037 9971 9803 0505";
        private const string CardHolder = "حسین کهنسال";

        public InvoicePrinter(Invoice invoice)
        {
            _invoice = invoice;
        }

        // --- متدهای کمکی داخلی (برای جلوگیری از ارور نبود متد) ---
        private string ToPersian(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Replace("0", "۰").Replace("1", "۱").Replace("2", "۲")
                        .Replace("3", "۳").Replace("4", "۴").Replace("5", "۵")
                        .Replace("6", "۶").Replace("7", "۷").Replace("8", "۸").Replace("9", "۹");
        }

        // متد داخلی تاریخ شمسی (جایگزین DateUtils)
        private string GetPersianDate(DateTime date)
        {
            try
            {
                PersianCalendar pc = new PersianCalendar();
                return $"{pc.GetYear(date):0000}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
            }
            catch { return ""; }
        }

        // متد داخلی تبدیل عدد به حروف (جایگزین NumberToText)
        // اگر کلاس NumberToText دارید، این متد را پاک کنید و از همان استفاده کنید
        private string GetAmountInWords(decimal amount)
        {
            try
            {
                // اینجا فرض کردیم کلاس NumberToText در پروژه شما هست
                // اگر ارور داد، این خط را کامنت کنید یا کلاسش را اضافه کنید
                return NumberToText.ToString((long)amount); 
            }
            catch 
            { 
                return amount.ToString("N0") + " ریال"; // حالت جایگزین در صورت نبود متد
            }
        }

        // =========================================================
        // متد اصلی ساخت دیزاین (اصلاح شده برای پر کردن صفحه)
        // =========================================================
private FlowDocument CreateDesign()
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FlowDirection = FlowDirection.RightToLeft,
                TextAlignment = TextAlignment.Left,
                Background = Brushes.White,
                PagePadding = new Thickness(0),
                PageWidth = PageWidth,
                PageHeight = PageHeight,
                ColumnWidth = PageWidth
            };

            var shayanBlue = new SolidColorBrush(Color.FromRgb(20, 30, 100));
            var alertRed = new SolidColorBrush(Color.FromRgb(200, 0, 0));

        double SafeWidth = PageWidth - 40;   // 20px راست + 20px چپ
double SafeHeight = PageHeight - 1;

Grid pageWrapper = new Grid
{
    Width = SafeWidth,
    Height = SafeHeight,
    Margin = new Thickness(20, 15, 20, 15)
};

            Grid invoiceLayer = new Grid
            {
                Margin = new Thickness(15), 
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // ستون 0: محتوا (3.2) - ستون 1: سایدبار (1)
            invoiceLayer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3.2, GridUnitType.Star) }); 
            invoiceLayer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   

            // ردیف 0: هدر (Auto)
            invoiceLayer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            // ردیف 1: بدنه (Star)
            invoiceLayer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // ============================================================
            // 1. هدر
            // ============================================================
            Grid headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // --- مشتری ---
            Grid customerContainer = new Grid();
            customerContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            customerContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            TextBlock lblCustomer = new TextBlock 
            { 
                Text = "مشتری محترم: ", FontSize = 10, Foreground = shayanBlue, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 5, 4) 
            };
            Grid.SetColumn(lblCustomer, 0);
            customerContainer.Children.Add(lblCustomer);

            Grid nameOverlay = new Grid();
            TextBlock txtDots = new TextBlock 
            { 
                Text = "...........................................................................................................................................................................................................................................", 
                FontSize = 10, Foreground = shayanBlue, VerticalAlignment = VerticalAlignment.Bottom, Opacity = 0.5, Margin = new Thickness(0, 0, 0, 2), TextTrimming = TextTrimming.None,
                HorizontalAlignment = HorizontalAlignment.Stretch, TextAlignment = TextAlignment.Center 
            };
            Border dotsClipper = new Border { ClipToBounds = true, Child = txtDots, VerticalAlignment = VerticalAlignment.Bottom };
            nameOverlay.Children.Add(dotsClipper);

            TextBlock txtName = new TextBlock 
            { 
                Text = " " + _invoice.Customer.Name + " ", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, Background = Brushes.Transparent, Margin = new Thickness(0, 0, 0, 6)
            };
            nameOverlay.Children.Add(txtName);

            Grid.SetColumn(nameOverlay, 1);
            customerContainer.Children.Add(nameOverlay);
            Grid.SetColumn(customerContainer, 0);
            headerGrid.Children.Add(customerContainer);

            // --- شماره ---
            if (!string.IsNullOrWhiteSpace(_invoice.InvoiceNumber) && _invoice.InvoiceNumber != "0")
            {
                TextBlock invoiceNumTxt = new TextBlock
                {
                    Text = $"شماره: {ToPersian(_invoice.InvoiceNumber)}", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = alertRed, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(15, 0, 0, 5)
                };
                Grid.SetColumn(invoiceNumTxt, 1);
                headerGrid.Children.Add(invoiceNumTxt);
            }

            Grid.SetRow(headerGrid, 0);
            Grid.SetColumn(headerGrid, 0);
            invoiceLayer.Children.Add(headerGrid);


            // ============================================================
            // 2. سایدبار (با منطق شرطی برای جایگذاری)
            // ============================================================
            StackPanel sidebar = new StackPanel 
            { 
                Margin = new Thickness(35, 0, 0, 0), 
                HorizontalAlignment = HorizontalAlignment.Center,
                // VerticalAlignment اینجا ست نمیشه، پایین ست میکنیم
            };

            Image logoImage = new Image { Width = 75, Height = 75, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 5) };
            try { logoImage.Source = new BitmapImage(new Uri("pack://application:,,,/logo.png")); } catch { }
            sidebar.Children.Add(logoImage);

            sidebar.Children.Add(new TextBlock { Text = "مرکز چاپ شایان (۱)", FontSize = 15, FontWeight = FontWeights.ExtraBold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center });
            sidebar.Children.Add(new TextBlock { Text = "Digital Printing Center", FontSize = 10, Foreground = shayanBlue, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            sidebar.Children.Add(new TextBlock { Text = "Indoor - Outdoor", FontSize = 9, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center });
            sidebar.Children.Add(new TextBlock { Text = "مجری کلیه امور چاپی", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 5) });
            sidebar.Children.Add(new TextBlock { Text = $"تاریخ: {ToPersian(GetPersianDate(_invoice.Date))}", FontSize = 10, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center });

            if (!_invoice.IsPaid)
            {
                Border cardBorder = new Border
                {
                    BorderBrush = shayanBlue, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(2, 15, 2, 0), Padding = new Thickness(5), Background = new SolidColorBrush(Color.FromRgb(245, 245, 255))
                };
                StackPanel cardStack = new StackPanel();
                cardStack.Children.Add(new TextBlock { Text = "شماره کارت:", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center });
                cardStack.Children.Add(new TextBlock { Text = ToPersian(CardNumber), FontSize = 13, FontWeight = FontWeights.Bold, FlowDirection = FlowDirection.LeftToRight, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 2) });
                cardStack.Children.Add(new TextBlock { Text = CardHolder, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center });
                cardBorder.Child = cardStack;
                sidebar.Children.Add(cardBorder);
            }

            StackPanel footerInfo = new StackPanel { Margin = new Thickness(0, 20, 0, 0) };
            footerInfo.Children.Add(new TextBlock { Text = "تهران، خیابان دکتر فاطمی، روبروی سازمان آب، پلاک ۲۰۵", FontSize = 9, TextWrapping = TextWrapping.Wrap, Foreground = shayanBlue, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center });
            footerInfo.Children.Add(new TextBlock { Text = "تلفن : " + ToPersian("88954562 - 88960183"), FontSize = 9, FontWeight = FontWeights.Bold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 0) });
            footerInfo.Children.Add(new TextBlock { Text = "تلگرام، واتساپ: " + ToPersian("7300 684 0919"), FontSize = 9, FontWeight = FontWeights.Bold, Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
            footerInfo.Children.Add(new TextBlock { Text = "Email: shayandigital@yahoo.com", FontSize = 9, FontWeight = FontWeights.Bold , Foreground = shayanBlue, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 2, 0, 0) });
            sidebar.Children.Add(footerInfo);

            // ************************************************************
            // *** منطق شرطی برای جایگذاری سایدبار ***
            // ************************************************************
            if (!_invoice.IsPaid)
            {
                // حالت پرداخت نشده: شروع از ردیف 0 (بالا)، گرفتن 2 ردیف، وسط‌چین عمودی
                Grid.SetRow(sidebar, 0);
                Grid.SetRowSpan(sidebar, 2);
                sidebar.VerticalAlignment = VerticalAlignment.Center;
            }
            else
            {
                // حالت پرداخت شده: شروع از ردیف 1 (جدول)، چسبیده به بالا
                Grid.SetRow(sidebar, 1);
                Grid.SetRowSpan(sidebar, 1);
                sidebar.VerticalAlignment = VerticalAlignment.Top;
            }

            Grid.SetColumn(sidebar, 1);
            invoiceLayer.Children.Add(sidebar);


            // ============================================================
            // 3. بدنه اصلی
            // ============================================================
            StackPanel contentPanel = new StackPanel();

            // جدول
            Grid tableGrid = new Grid();
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.4, GridUnitType.Star) });
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3.5, GridUnitType.Star) });
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            tableGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

            tableGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            AddHeaderCell(tableGrid, "ردیف", 0, 0);
            AddHeaderCell(tableGrid, "موضوع", 1, 0);
            AddHeaderCell(tableGrid, "تعداد", 2, 0);
            AddHeaderCell(tableGrid, "فـی", 3, 0);
            AddHeaderCell(tableGrid, "جمع کـل", 4, 0);

            int rowIndex = 1;
            int itemCounter = 1;
            var rowHeight = new GridLength(25);

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
            TextBlock totalLabelTxt = new TextBlock { Text = "جمع کـل:", FontWeight = FontWeights.Bold, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            totalLabelBorder.Child = totalLabelTxt;
            Grid.SetColumn(totalLabelBorder, 0); Grid.SetRow(totalLabelBorder, rowIndex); Grid.SetColumnSpan(totalLabelBorder, 4);
            tableGrid.Children.Add(totalLabelBorder);
            AddBodyCell(tableGrid, ToPersian(_invoice.FinalAmount.ToString("N0")), 4, rowIndex, true);
            contentPanel.Children.Add(tableGrid);

            // حروف
            Border wordsBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5, 0, 0.5, 0.5), Padding = new Thickness(5) };
            StackPanel wordsPanel = new StackPanel { Orientation = Orientation.Horizontal };
            wordsPanel.Children.Add(new TextBlock { Text = "به حروف: ", FontWeight = FontWeights.Bold, FontSize = 11 });
            string amountInWords = GetAmountInWords(_invoice.FinalAmount);
            wordsPanel.Children.Add(new TextBlock { Text = $"{amountInWords} ریال", FontSize = 11, Margin = new Thickness(5, 0, 0, 0) });
            wordsBorder.Child = wordsPanel;
            contentPanel.Children.Add(wordsBorder);

            // فوتر
            Border footerBorder = new Border { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5), Margin = new Thickness(0, 10, 0, 0), Padding = new Thickness(0), Background = Brushes.White, SnapsToDevicePixels = true, Height = 85 };
            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Rectangle verticalLine = new Rectangle { Width = 0.5, Fill = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Stretch, Margin = new Thickness(0, 2, 0, 2) };
            // Grid.SetColumn(verticalLine, 0);
            // footerGrid.Children.Add(verticalLine);

            StackPanel buyerPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            buyerPanel.Children.Add(new TextBlock { Text = "خـریـدار", FontSize = 12, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
            Grid.SetColumn(buyerPanel, 0); footerGrid.Children.Add(buyerPanel);

            Grid sellerContainer = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            sellerContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            sellerContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock sellerText = new TextBlock { Text = "فـروشـنـده", FontSize = 12, FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 15, 0) };
            Grid.SetColumn(sellerText, 0); sellerContainer.Children.Add(sellerText);

            Grid imageGroup = new Grid { Width = 90, Height = 80 };
            Image signatureImg = new Image { Width = 80, Height = 60, Stretch = Stretch.Uniform };
            signatureImg.RenderTransform = new TranslateTransform(10, 10);
            try { signatureImg.Source = new BitmapImage(new Uri("pack://application:,,,/shop_signature.png")); } catch { }
            imageGroup.Children.Add(signatureImg);

            Image stampShopImg = new Image { Width = 100, Height = 100, Stretch = Stretch.Uniform, Opacity = 0.9 };
            stampShopImg.RenderTransform = new TranslateTransform(0, -10);
            try { stampShopImg.Source = new BitmapImage(new Uri("pack://application:,,,/shop_stamp.png")); } catch { }
            imageGroup.Children.Add(stampShopImg);

            Grid.SetColumn(imageGroup, 1); sellerContainer.Children.Add(imageGroup);
            Grid.SetColumn(sellerContainer, 1); footerGrid.Children.Add(sellerContainer);
            footerBorder.Child = footerGrid;
            contentPanel.Children.Add(footerBorder);

            Grid.SetRow(contentPanel, 1);
            Grid.SetColumn(contentPanel, 0);
            invoiceLayer.Children.Add(contentPanel);

            pageWrapper.Children.Add(invoiceLayer);

            // واترمارک
            Image statusStamp = new Image { Width = 180, Height = 130, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Opacity = 0.7, IsHitTestVisible = false };
            statusStamp.RenderTransformOrigin = new Point(0.5, 0.5);
            statusStamp.RenderTransform = new RotateTransform(20);
            try
            {
                if (_invoice.IsPaid) statusStamp.Source = new BitmapImage(new Uri("pack://application:,,,/paid.png"));
                else statusStamp.Source = new BitmapImage(new Uri("pack://application:,,,/unpaid.png"));
            }
            catch { }
            pageWrapper.Children.Add(statusStamp);

            doc.Blocks.Add(new BlockUIContainer(pageWrapper));
            return doc;
        }

        public void PrintDirect()
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Landscape;
                printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(System.Printing.PageMediaSizeName.ISOA5);
                printDialog.PrintTicket.CopyCount = 1;

                FlowDocument doc = CreateDesign();
                IDocumentPaginatorSource idp = doc;
                printDialog.PrintDocument(idp.DocumentPaginator, "Factor_Direct_" + _invoice.InvoiceNumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در چاپ مستقیم:\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                IDocumentPaginatorSource idp = doc;
                printDialog.PrintDocument(idp.DocumentPaginator, "Factor_" + _invoice.InvoiceNumber);
            }
        }

        public RenderTargetBitmap GenerateImage()
        {
            FlowDocument doc = CreateDesign();
            DocumentPaginator paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
            paginator.PageSize = new Size(PageWidth, PageHeight);

            using (var page = paginator.GetPage(0))
            {
                var visual = page.Visual;
                double scale = 1.5;
                RenderTargetBitmap renderTarget = new RenderTargetBitmap((int)(PageWidth * scale), (int)(PageHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
                renderTarget.Render(visual);
                return renderTarget;
            }
        }

        public void SaveAsImage()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg",
                FileName = $"Invoice-{_invoice.InvoiceNumber}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = GenerateImage();
                    BitmapEncoder encoder;
                    if (saveDialog.FileName.ToLower().EndsWith(".jpg")) encoder = new JpegBitmapEncoder();
                    else encoder = new PngBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using (FileStream stream = new FileStream(saveDialog.FileName, FileMode.Create)) encoder.Save(stream);
                    MessageBox.Show("فایل با موفقیت ذخیره شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show("خطا در ذخیره عکس:\n" + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        public string SaveToTempFile()
        {
            try
            {
                string fileName = $"Factor_{_invoice.InvoiceNumber}_{DateTime.Now.Ticks}.png";
                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
                var bitmap = GenerateImage();
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (FileStream stream = new FileStream(tempPath, FileMode.Create)) encoder.Save(stream);
                return tempPath;
            }
            catch (Exception ex) { MessageBox.Show("خطا در ساخت عکس موقت: " + ex.Message); return null; }
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