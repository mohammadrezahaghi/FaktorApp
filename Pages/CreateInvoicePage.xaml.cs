using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Text.RegularExpressions;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;
using System.Text;

// رفع تداخل‌ها
using MessageBox = System.Windows.MessageBox;
using Clipboard = System.Windows.Clipboard;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using Brushes = System.Windows.Media.Brushes;

namespace FactorApp.UI.Pages
{
    public partial class CreateInvoicePage : Page
    {
        private List<InvoiceItem> _invoiceItems = new List<InvoiceItem>();
        private AppDbContext _context;
        private DateTime _selectedDate = DateTime.Now;
        private bool _isUpdatingText = false;

        public CreateInvoicePage()
        {
            InitializeComponent(); // اگر اینجا ارور دارد، حتما Rebuild کنید
            _context = new AppDbContext();

            SetDateToToday();
            LoadInitialData();
        }

        private void SetDateToToday()
        {
            _selectedDate = DateTime.Now;
            if (TxtInvoiceDate != null)
                TxtInvoiceDate.Text = DateUtils.ToShamsi(_selectedDate);
        }

        private void LoadInitialData()
        {
            var services = _context.Services.ToList();
            CmbServices.ItemsSource = services;
            CmbServices.DisplayMemberPath = "Name";
            CmbServices.SelectedValuePath = "Id";

            ReloadCustomers();

            CmbPaymentMethod.ItemsSource = Enum.GetValues(typeof(PaymentMethod))
                                               .Cast<PaymentMethod>()
                                               .Where(x => x != PaymentMethod.None);
            CmbPaymentMethod.SelectedIndex = 0;
        }

        private void ReloadCustomers()
        {
            var customers = _context.Customers.ToList();
            if (CmbCustomers != null)
            {
                CmbCustomers.ItemsSource = customers;
                CmbCustomers.DisplayMemberPath = "Name";
                CmbCustomers.SelectedValuePath = "Id";
            }
        }

        // --- بخش تاریخ و اعتبارسنجی ---
        private void TxtInvoiceDate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingText) return;
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string originalText = textBox.Text;
            string digitsOnly = new string(originalText.Where(char.IsDigit).ToArray());

            if (digitsOnly.Length > 8) digitsOnly = digitsOnly.Substring(0, 8);

            StringBuilder formatted = new StringBuilder();
            for (int i = 0; i < digitsOnly.Length; i++)
            {
                if (i == 4) formatted.Append("/");
                if (i == 6) formatted.Append("/");
                formatted.Append(digitsOnly[i]);
            }

            if (originalText != formatted.ToString())
            {
                _isUpdatingText = true;
                textBox.Text = formatted.ToString();
                textBox.CaretIndex = textBox.Text.Length;
                _isUpdatingText = false;
            }
            if (digitsOnly.Length == 8) UpdateDateFromInput(textBox.Text);
        }

        private void TxtInvoiceDate_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TxtInvoiceDate == null) return;
            string digitsOnly = new string(TxtInvoiceDate.Text.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length == 8) UpdateDateFromInput(TxtInvoiceDate.Text);
        }

        private void UpdateDateFromInput(string shamsiDate)
        {
            try
            {
                string[] parts = shamsiDate.Split('/');
                if (parts.Length == 3)
                {
                    int year = int.Parse(parts[0]);
                    int month = int.Parse(parts[1]);
                    int day = int.Parse(parts[2]);
                    PersianCalendar pc = new PersianCalendar();
                    DateTime now = DateTime.Now;
                    _selectedDate = pc.ToDateTime(year, month, day, now.Hour, now.Minute, now.Second, 0);
                }
            }
            catch { }
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DecimalValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // --- افزودن آیتم ---
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (CmbServices.SelectedItem is not Service service)
            {
                MessageBox.Show("لطفا یک کالا/خدمات انتخاب کنید.");
                return;
            }

            int qty = int.TryParse(TxtQty.Text, out int q) ? q : 1;
            double width = double.TryParse(TxtWidth.Text, out double w) ? w : 0;
            double length = double.TryParse(TxtLength.Text, out double l) ? l : 0;
            decimal totalRowPrice = 0;

            if (service.Method == CalculationMethod.AreaBased)
                totalRowPrice = (decimal)(width * length) * qty * service.UnitPrice;
            else
                totalRowPrice = qty * service.UnitPrice;

            if (totalRowPrice == 0) { MessageBox.Show("قیمت کل صفر شد! مقادیر را بررسی کنید."); return; }

            _invoiceItems.Add(new InvoiceItem
            {
                ServiceName = service.Name,
                UnitPrice = service.UnitPrice,
                Quantity = qty,
                Width = width,
                Length = length,
                TotalPrice = totalRowPrice,
                IsSelected = false
            });

            RefreshGrid();

            TxtQty.Text = "1";
            if (TxtWidth.IsEnabled) { TxtWidth.Text = ""; TxtLength.Text = ""; }
        }

        private void RefreshGrid()
        {
            if (DataGridItems == null) return;

            DataGridItems.ItemsSource = null;
            DataGridItems.ItemsSource = _invoiceItems;
            decimal total = _invoiceItems.Sum(x => x.TotalPrice);
            if (TxtSubTotal != null)
                TxtSubTotal.Text = total.ToString("N0");
        }

        private void CmbServices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbServices.SelectedItem is Service selectedService)
            {
                bool isAreaBased = selectedService.Method == CalculationMethod.AreaBased;
                TxtWidth.IsEnabled = isAreaBased;
                TxtLength.IsEnabled = isAreaBased;
                if (!isAreaBased) { TxtWidth.Text = ""; TxtLength.Text = ""; }
            }
        }

        // --- حذف آیتم‌ها ---
        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceItem item)
            {
                _invoiceItems.Remove(item);
                RefreshGrid();
            }
        }

        private void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var itemsToDelete = _invoiceItems.Where(x => x.IsSelected).ToList();
            if (itemsToDelete.Count == 0) return;

            if (MessageBox.Show($"آیا از حذف {itemsToDelete.Count} قلم مطمئن هستید؟", "حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                foreach (var item in itemsToDelete) _invoiceItems.Remove(item);
                RefreshGrid();
            }
        }

        // --- دیالوگ مشتری جدید ---
        private void BtnNewCustomerDialog_Click(object sender, RoutedEventArgs e)
        {
            // پاک کردن فیلدها
            if (QuickCustomerName != null) QuickCustomerName.Clear();
            if (QuickCustomerPhone != null) QuickCustomerPhone.Clear();
            if (QuickCustomerAddress != null) QuickCustomerAddress.Clear();
            if (QuickCustomerBalance != null) QuickCustomerBalance.Text = "0";

            // >>>> تغییر اصلی: باز کردن دیالوگ با استفاده از نام <<<<
            if (InvoiceRootDialog != null)
            {
                InvoiceRootDialog.IsOpen = true;
            }
        }

        private void BtnQuickSaveCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(QuickCustomerName.Text))
            {
                MessageBox.Show("نام مشتری الزامی است.");
                return;
            }

            decimal balance = decimal.TryParse(QuickCustomerBalance.Text, out decimal b) ? b : 0;

            var newCustomer = new Customer
            {
                Name = QuickCustomerName.Text,
                PhoneNumber = QuickCustomerPhone.Text,
                Address = QuickCustomerAddress.Text,
                Balance = balance
            };

            _context.Customers.Add(newCustomer);
            _context.SaveChanges();

            // >>>> تغییر اصلی: بستن دیالوگ با استفاده از نام <<<<
            if (InvoiceRootDialog != null)
            {
                InvoiceRootDialog.IsOpen = false;
            }

            ReloadCustomers();
            CmbCustomers.SelectedValue = newCustomer.Id;
        }

        // --- پرداخت و ثبت ---
        private void TglIsPaid_Click(object sender, RoutedEventArgs e)
        {
            bool isPaid = TglIsPaid.IsChecked == true;
            CmbPaymentMethod.IsEnabled = isPaid;

            if (isPaid)
            {
                TxtPaymentStatus.Text = "پرداخت شده";
                TxtPaymentStatus.Foreground = Brushes.Green;
            }
            else
            {
                TxtPaymentStatus.Text = "پرداخت نشده";
                TxtPaymentStatus.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 82, 82));
            }
        }

        private void BtnIssueInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer selectedCustomer)
            {
                MessageBox.Show("لطفا مشتری و اقلام را وارد کنید.");
                return;
            }
            UpdateDateFromInput(TxtInvoiceDate.Text);

            bool isPaid = TglIsPaid.IsChecked == true;
            PaymentMethod method = isPaid ? (PaymentMethod)CmbPaymentMethod.SelectedItem : PaymentMethod.None;

            var draftInvoice = new Invoice
            {
                Date = _selectedDate,
                InvoiceNumber = "پیش‌نمایش",
                Customer = selectedCustomer,
                Items = _invoiceItems,
                FinalAmount = _invoiceItems.Sum(x => x.TotalPrice),
                Status = InvoiceStatus.Printing,
                IsPaid = isPaid,
                PaymentMethod = method
            };

            try
            {
                var printer = new InvoicePrinter(draftInvoice);
                printer.Print();
            }
            catch (Exception ex) { MessageBox.Show("خطا: " + ex.Message); }
        }

        private async void BtnSaveInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer selectedCustomer)
            {
                MessageBox.Show("اطلاعات ناقص است."); return;
            }
            UpdateDateFromInput(TxtInvoiceDate.Text);

            bool isPaid = TglIsPaid.IsChecked == true;
            PaymentMethod method = isPaid ? (PaymentMethod)CmbPaymentMethod.SelectedItem : PaymentMethod.None;

            try
            {
                var newInvoice = new Invoice
                {
                    Date = _selectedDate,
                    CustomerId = selectedCustomer.Id,
                    InvoiceNumber = _selectedDate.ToString("yyyyMMdd") + "-" + _selectedDate.ToString("HHmm"),
                    Status = InvoiceStatus.Pending,
                    FinalAmount = _invoiceItems.Sum(x => x.TotalPrice),
                    IsPaid = isPaid,
                    PaymentMethod = method
                };

                if (!isPaid)
                {
                    var customer = _context.Customers.Find(selectedCustomer.Id);
                    if (customer != null) customer.Balance += newInvoice.FinalAmount;
                }

                foreach (var item in _invoiceItems)
                {
                    _context.InvoiceItems.Add(new InvoiceItem
                    {
                        Invoice = newInvoice,
                        ServiceName = item.ServiceName,
                        Length = item.Length,
                        Width = item.Width,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    });
                }
                _context.Invoices.Add(newInvoice);
                _context.SaveChanges();

                newInvoice.Customer = selectedCustomer;
                newInvoice.Items = _invoiceItems;

                var result = MessageBox.Show($"فاکتور ثبت شد.\nآیا چاپ شود؟", "موفق", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result == MessageBoxResult.Yes)
                {
                    var printer = new InvoicePrinter(newInvoice);
                    printer.Print();
                }

                if (ChkSendWhatsapp.IsChecked == true)
                {
                    await SendToWhatsapp(newInvoice);
                }

                ResetForm();
            }
            catch (Exception ex) { MessageBox.Show("خطا: " + ex.Message); }
        }

        private async void BtnManualWhatsapp_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer customer)
            {
                MessageBox.Show("اطلاعات ناقص"); return;
            }

            var draft = CreateDraftInvoice(customer);
            await SendToWhatsapp(draft);
        }

        private async Task SendToWhatsapp(Invoice invoice)
        {
            try
            {
                var printer = new InvoicePrinter(invoice);
                string imagePath = printer.SaveToTempFile();

                if (imagePath == null) return;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                Clipboard.SetImage(bitmap);

                string phone = invoice.Customer.PhoneNumber;
                if (phone.StartsWith("0")) phone = "98" + phone.Substring(1);
                if (phone.StartsWith("+98")) phone = phone.Substring(1);

                string finalMessage = "";
                string amountStr = invoice.FinalAmount.ToString("N0");

                if (invoice.IsPaid)
                {
                    finalMessage = $"سلام *{invoice.Customer.Name}* عزیز،\n" +
                                   $"فاکتور سفارش شما پیوست شد.\n\n" +
                                   $"وضعیت: *پرداخت شده*\n" +
                                   $"با تشکر از خرید شما.";
                }
                else
                {
                    finalMessage = $"سلام *{invoice.Customer.Name}* عزیز،\n" +
                                   $"فاکتور سفارش شما به مبلغ *{amountStr} ریال* پیوست شد.\n\n" +
                                   $"جهت شروع پردازش سفارش، لطفاً مبلغ مذکور را به شماره کارت زیر واریز نمایید:\n\n" +
                                   $"*6037-9971-9803-0505*\n" +
                                   $"به نام: *حسین کهنسال* (بانک ملی)\n\n" +
                                   $"لطفاً پس از واریز، فیش را ارسال نمایید. با تشکر";
                }

                string msgEncoded = Uri.EscapeDataString(finalMessage);
                string url = $"https://wa.me/{phone}?text={msgEncoded}";

                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در پردازش ارسال: " + ex.Message);
            }

            await Task.CompletedTask;
        }

        private Invoice CreateDraftInvoice(Customer customer)
        {
            UpdateDateFromInput(TxtInvoiceDate.Text);
            bool isPaid = TglIsPaid.IsChecked == true;
            return new Invoice
            {
                Date = _selectedDate,
                InvoiceNumber = "Draft",
                Customer = customer,
                Items = _invoiceItems,
                FinalAmount = _invoiceItems.Sum(x => x.TotalPrice),
                IsPaid = isPaid
            };
        }

        private void ResetForm()
        {
            _invoiceItems.Clear();
            RefreshGrid();
            CmbServices.SelectedIndex = -1;
            TxtQty.Text = "1";
            TxtWidth.Text = "";
            TxtLength.Text = "";
            SetDateToToday();
            TglIsPaid.IsChecked = false;
            TglIsPaid_Click(null, null);
            ChkSendWhatsapp.IsChecked = false;
        }
        // --- 1. باز کردن منوی دکمه ---
        private void BtnPrintMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
                btn.ContextMenu.IsOpen = true;
            }
        }

        // --- 2. کپی عکس در کلیپ‌بورد ---
        private void BtnCopyImage_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer customer)
            {
                MessageBox.Show("لطفا مشتری و اقلام را وارد کنید.");
                return;
            }

            var draftInvoice = CreateDraftInvoice(customer);

            try
            {
                var printer = new InvoicePrinter(draftInvoice);

                // 
                // این متد الان در InvoicePrinter وجود دارد
                var image = printer.GenerateImage();

                System.Windows.Clipboard.SetImage(image);

                MessageBox.Show("عکس فاکتور کپی شد!\nالان Paste کنید (Ctrl+V).", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در تولید عکس: " + ex.Message);
            }
        }
        // --- 3. ذخیره عکس فاکتور در فایل (Save As) ---
        private void BtnSaveImageFile_Click(object sender, RoutedEventArgs e)
        {
            // 1. اعتبارسنجی ورودی‌ها
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer customer)
            {
                MessageBox.Show("لطفا مشتری و اقلام را وارد کنید.");
                return;
            }

            // 2. ساخت فاکتور پیش‌نویس (بدون ذخیره در دیتابیس)
            var draftInvoice = CreateDraftInvoice(customer);

            try
            {
                // 3. استفاده از کلاس پرینتر
                var printer = new InvoicePrinter(draftInvoice);

                // 4. فراخوانی متد ذخیره (که دیالوگ باز می‌کند و ذخیره می‌کند)
                // این متد را در مرحله قبل در InvoicePrinter.cs اضافه کردیم
                printer.SaveAsImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در ذخیره سازی: " + ex.Message);
            }
        }
    }
}