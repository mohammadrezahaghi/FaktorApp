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
using System.Text;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;
using FactorApp.UI.UserControls; // برای دسترسی به MessageDialog و ConfirmDialog
using System.Windows.Data;
// رفع تداخل‌ها
using Clipboard = System.Windows.Clipboard;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using Brushes = System.Windows.Media.Brushes;
using ComboBox = System.Windows.Controls.ComboBox;

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
            InitializeComponent();
            SetDateToToday();
            _context = new AppDbContext();
            this.Loaded += CreateInvoicePage_Loaded;
        }

        private void CreateInvoicePage_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDropdownsPreservingSelection();
            if (CmbPaymentMethod.ItemsSource == null)
            {
                CmbPaymentMethod.ItemsSource = Enum.GetValues(typeof(PaymentMethod))
                                                .Cast<PaymentMethod>()
                                                .Where(x => x != PaymentMethod.None);
                CmbPaymentMethod.SelectedIndex = 0;
            }
        }

        // =========================================================
        // متد نمایش پیام سفارشی
        // =========================================================
        private async void ShowMessage(string message, MessageType type = MessageType.Error)
        {
            var view = new MessageDialog(message, type);
            // نمایش روی دیالوگ اصلی (PageRootDialog)
            await DialogHost.Show(view, "PageRootDialog");
        }

        private void RefreshDropdownsPreservingSelection()
        {
            try
            {
                using (var tempContext = new AppDbContext())
                {
                    var selectedCustomerId = CmbCustomers.SelectedValue;

                    // تغییر: مرتب‌سازی نزولی بر اساس ID (آخرین‌ها اول باشند)
                    var customers = tempContext.Customers.OrderByDescending(c => c.Id).ToList();
                    CmbCustomers.ItemsSource = customers;
                    if (selectedCustomerId != null) CmbCustomers.SelectedValue = selectedCustomerId;

                    if (CmbServices != null)
                    {
                        var selectedServiceId = CmbServices.SelectedValue;

                        // تغییر: مرتب‌سازی نزولی بر اساس ID (آخرین‌ها اول باشند)
                        var services = tempContext.Services.OrderByDescending(s => s.Id).ToList();
                        CmbServices.ItemsSource = services;
                        if (selectedServiceId != null) CmbServices.SelectedValue = selectedServiceId;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("خطا در بارگذاری لیست‌ها: " + ex.Message, MessageType.Error);
            }
        }
        // متد جستجوی پیشرفته برای مشتریان (شامل نام و شماره تماس)
        // متد جستجوی پیشرفته برای مشتریان
        private void CmbCustomers_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var cmb = sender as ComboBox;
            // اگر کلیدهای کنترلی (مثل جهت‌نماها) زده شد، کاری نکنیم تا کاربر بتواند در لیست حرکت کند
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.Tab) return;

            CollectionView itemsViewOriginal = (CollectionView)CollectionViewSource.GetDefaultView(cmb.ItemsSource);

            // اگر متن خالی شد، فیلتر را بردار و انتخاب را پاک کن
            if (string.IsNullOrEmpty(cmb.Text))
            {
                itemsViewOriginal.Filter = null;
                cmb.IsDropDownOpen = true;
                cmb.SelectedIndex = -1; // انتخاب را حذف کن تا کاربر بتواند آزادانه تایپ کند
                return;
            }

            itemsViewOriginal.Filter = ((o) =>
            {
                if (o is Customer customer)
                {
                    string searchText = cmb.Text.Trim().ToLower();
                    if (customer.Name != null && customer.Name.ToLower().Contains(searchText)) return true;
                    if (customer.PhoneNumber != null && customer.PhoneNumber.Contains(searchText)) return true;
                }
                return false;
            });

            itemsViewOriginal.Refresh();

            // اگر آیتمی پیدا شد لیست را باز کن، وگرنه ببند
            if (itemsViewOriginal.Count > 0)
            {
                cmb.IsDropDownOpen = true;
            }
            else
            {
                cmb.IsDropDownOpen = false;
            }
        }

        // متد جستجوی پیشرفته برای خدمات
        private void CmbServices_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var cmb = sender as ComboBox;
            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Enter || e.Key == Key.Tab) return;

            CollectionView itemsViewOriginal = (CollectionView)CollectionViewSource.GetDefaultView(cmb.ItemsSource);

            if (string.IsNullOrEmpty(cmb.Text))
            {
                itemsViewOriginal.Filter = null;
                cmb.IsDropDownOpen = true;
                cmb.SelectedIndex = -1;
                return;
            }

            itemsViewOriginal.Filter = ((o) =>
            {
                if (o is Service service)
                {
                    string searchText = cmb.Text.Trim().ToLower();
                    if (service.Name != null && service.Name.ToLower().Contains(searchText)) return true;
                }
                return false;
            });

            itemsViewOriginal.Refresh();

            if (itemsViewOriginal.Count > 0)
            {
                cmb.IsDropDownOpen = true;
            }
            else
            {
                cmb.IsDropDownOpen = false;
            }
        }
        // ... (متدهای مربوط به تاریخ و تکست باکس‌ها بدون تغییر) ...
        private void SetDateToToday()
        {
            _selectedDate = DateTime.Now;
            if (TxtInvoiceDate != null)
                TxtInvoiceDate.Text = DateUtils.ToShamsi(_selectedDate);
        }

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

        private void NumberValidation(object sender, TextCompositionEventArgs e) => e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        private void DecimalValidation(object sender, TextCompositionEventArgs e) => e.Handled = new Regex("[^0-9.]+").IsMatch(e.Text);

        // --- افزودن آیتم ---
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (CmbServices.SelectedItem is not Service service)
            {
                ShowMessage("لطفا یک کالا/خدمات انتخاب کنید.", MessageType.Warning);
                return;
            }

            int qty = int.TryParse(TxtQty.Text, out int q) ? q : 1;
            double width = double.TryParse(TxtWidth.Text, out double w) ? w : 0;
            double length = double.TryParse(TxtLength.Text, out double l) ? l : 0;

            // ************************************************************
            // *** تغییر جدید: بررسی وجود آیتم تکراری ***
            // ************************************************************

            // جستجو برای آیتمی با نام سرویس، عرض و طول مشابه
            var existingItem = _invoiceItems.FirstOrDefault(x =>
                x.ServiceName == service.Name &&
                Math.Abs(x.Width - width) < 0.01 &&  // مقایسه دابل با تلورانس کوچک
                Math.Abs(x.Length - length) < 0.01
            );

            if (existingItem != null)
            {
                // آیتم تکراری پیدا شد -> تعداد را اضافه کن
                existingItem.Quantity += qty;

                // محاسبه مجدد قیمت کل برای این سطر
                if (service.Method == CalculationMethod.AreaBased)
                    existingItem.TotalPrice = (decimal)(existingItem.Width * existingItem.Length) * existingItem.Quantity * existingItem.UnitPrice;
                else
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                // آیتم جدید است -> اضافه کن
                decimal totalRowPrice = 0;
                if (service.Method == CalculationMethod.AreaBased)
                    totalRowPrice = (decimal)(width * length) * qty * service.UnitPrice;
                else
                    totalRowPrice = qty * service.UnitPrice;

                if (totalRowPrice == 0)
                {
                    ShowMessage("قیمت کل صفر شد! مقادیر را بررسی کنید.", MessageType.Warning);
                    return;
                }

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
            }

            // بازخوانی گرید
            RefreshGrid();

            // پاک کردن فرم
            TxtQty.Text = "1";
            if (TxtWidth.IsEnabled) { TxtWidth.Text = ""; TxtLength.Text = ""; }
        }

        private void RefreshGrid()
        {
            if (DataGridItems == null) return;
            DataGridItems.ItemsSource = null;
            DataGridItems.ItemsSource = _invoiceItems;
            decimal total = _invoiceItems.Sum(x => x.TotalPrice);
            if (TxtSubTotal != null) TxtSubTotal.Text = total.ToString("N0");
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
            else
            {
                // وقتی انتخاب پاک می‌شود (مثلاً هنگام جستجو)
                TxtWidth.IsEnabled = false;
                TxtLength.IsEnabled = false;
                TxtWidth.Text = "";
                TxtLength.Text = "";
            }
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceItem item)
            {
                _invoiceItems.Remove(item);
                RefreshGrid();
            }
        }

        // --- حذف آیتم‌ها با دیالوگ جدید ---
        private async void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var itemsToDelete = _invoiceItems.Where(x => x.IsSelected).ToList();
            if (itemsToDelete.Count == 0) return;

            var dialog = new ConfirmDialog(
                $"آیا از حذف {itemsToDelete.Count} قلم مطمئن هستید؟",
                "حذف اقلام",
                ConfirmType.Delete
            );

            var result = await DialogHost.Show(dialog, "PageRootDialog");

            if (result is bool confirm && confirm)
            {
                foreach (var item in itemsToDelete) _invoiceItems.Remove(item);
                RefreshGrid();
            }
        }

        private void BtnNewCustomerDialog_Click(object sender, RoutedEventArgs e)
        {
            if (QuickCustomerName != null) QuickCustomerName.Clear();
            if (QuickCustomerPhone != null) QuickCustomerPhone.Clear();
            if (QuickCustomerAddress != null) QuickCustomerAddress.Clear();
            if (QuickCustomerBalance != null) QuickCustomerBalance.Text = "0";

            // باز کردن فرم مشتری (دیالوگ داخلی)
            InvoiceRootDialog.IsOpen = true;
        }

        private void BtnQuickSaveCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(QuickCustomerName.Text))
            {
                // اینجا چون دیالوگ داخلی باز است، پیام روی دیالوگ بیرونی باز میشود و مشکلی ندارد
                ShowMessage("نام مشتری الزامی است.", MessageType.Warning);
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

            InvoiceRootDialog.IsOpen = false;

            ReloadCustomers();
            CmbCustomers.SelectedValue = newCustomer.Id;
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
                ShowMessage("لطفا مشتری و اقلام را وارد کنید.", MessageType.Warning);
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
            catch (Exception ex) { ShowMessage("خطا: " + ex.Message, MessageType.Error); }
        }

        private void BtnPrintDirect_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer customer)
            {
                ShowMessage("لطفا مشتری و اقلام را وارد کنید.", MessageType.Warning);
                return;
            }

            var draftInvoice = CreateDraftInvoice(customer);
            try
            {
                var printer = new InvoicePrinter(draftInvoice);
                printer.PrintDirect();
            }
            catch (Exception ex) { ShowMessage("خطا: " + ex.Message, MessageType.Error); }
        }

        // --- ثبت نهایی و چاپ ---
        private async void BtnSaveInvoice_Click(object sender, RoutedEventArgs e)
        {
            // اعتبارسنجی اولیه
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedValue == null)
            {
                ShowMessage("لطفا مشتری و اقلام فاکتور را مشخص کنید.", MessageType.Warning);
                return;
            }

            int customerId = (int)CmbCustomers.SelectedValue;
            UpdateDateFromInput(TxtInvoiceDate.Text);
            bool isPaid = TglIsPaid.IsChecked == true;
            PaymentMethod method = PaymentMethod.None;

            try
            {
                if (isPaid)
                {
                    if (CmbPaymentMethod.SelectedItem == null)
                    {
                        ShowMessage("لطفاً روش پرداخت را انتخاب کنید.", MessageType.Warning);
                        return;
                    }
                    method = (PaymentMethod)CmbPaymentMethod.SelectedItem;
                }

                Invoice newInvoice;

                using (var db = new AppDbContext())
                {
                    var customer = db.Customers.Find(customerId);
                    if (customer == null) throw new Exception("مشتری پیدا نشد.");

                    // ********************************************************
                    // *** بخش تولید شماره فاکتور استاندارد (YY + 0000) ***
                    // ********************************************************

                    // 1. استخراج سال شمسی (مثلاً 1404)
                    System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
                    int currentYear = pc.GetYear(_selectedDate);

                    // 2. ساخت پیشوند (دو رقم آخر سال: 04)
                    string yearPrefix = (currentYear % 100).ToString("00");

                    // 3. پیدا کردن آخرین شماره فاکتور در این سال
                    // شرط: فاکتورهایی که با این پیشوند شروع می‌شوند و طولشان 6 رقم است (2 رقم سال + 4 رقم سریال)
                    var lastInvoice = db.Invoices
                                        .Where(i => i.InvoiceNumber.StartsWith(yearPrefix) && i.InvoiceNumber.Length == 6)
                                        .OrderByDescending(i => i.InvoiceNumber)
                                        .FirstOrDefault();

                    string newInvoiceNumber;
                    if (lastInvoice != null)
                    {
                        // اگر قبلاً فاکتوری بوده، بخش سریال (4 رقم آخر) را بردار و یکی اضافه کن
                        if (int.TryParse(lastInvoice.InvoiceNumber.Substring(2), out int lastSeq))
                        {
                            newInvoiceNumber = yearPrefix + (lastSeq + 1).ToString("0000");
                        }
                        else
                        {
                            // در صورت خطای احتمالی در پارس کردن، یک شماره تصادفی ندهیم، از 1 شروع کنیم
                            newInvoiceNumber = yearPrefix + "0001";
                        }
                    }
                    else
                    {
                        // اولین فاکتور سال
                        newInvoiceNumber = yearPrefix + "0001";
                    }
                    // ********************************************************


                    newInvoice = new Invoice
                    {
                        Customer = customer,
                        Date = _selectedDate,
                        InvoiceNumber = newInvoiceNumber, // استفاده از شماره استاندارد جدید
                        Status = InvoiceStatus.Pending,
                        FinalAmount = _invoiceItems.Sum(x => x.TotalPrice),
                        IsPaid = isPaid,
                        PaymentMethod = method,
                    };

                    // اگر پرداخت نشده، به حساب مشتری اضافه کن
                    if (!isPaid) customer.Balance += newInvoice.FinalAmount;

                    // ذخیره اقلام
                    foreach (var item in _invoiceItems)
                    {
                        var invoiceItem = new InvoiceItem
                        {
                            Invoice = newInvoice,
                            ServiceName = item.ServiceName,
                            Length = item.Length,
                            Width = item.Width,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.TotalPrice
                        };
                        db.InvoiceItems.Add(invoiceItem);
                    }

                    db.Invoices.Add(newInvoice);
                    db.SaveChanges();
                }

                // پر کردن دستی آیتم‌ها برای پرینت (چون شیء newInvoice هنوز آیتم‌هایش لود نشده)
                newInvoice.Items = _invoiceItems;

                // دیالوگ موفقیت و سوال چاپ
                var dialog = new ConfirmDialog($"فاکتور شماره {newInvoice.InvoiceNumber} با موفقیت ثبت شد.\nآیا چاپ مستقیم انجام شود؟", "ثبت موفق", ConfirmType.Success);
                var result = await DialogHost.Show(dialog, "PageRootDialog");

                if (result is bool confirm && confirm)
                {
                    try
                    {
                        var printer = new InvoicePrinter(newInvoice);
                        printer.PrintDirect();
                    }
                    catch (Exception printEx)
                    {
                        ShowMessage("فاکتور ثبت شد اما در چاپ مشکلی پیش آمد:\n" + printEx.Message, MessageType.Warning);
                    }
                }

                // ارسال واتساپ
                if (ChkSendWhatsapp.IsChecked == true)
                {
                    await SendToWhatsapp(newInvoice);
                }

                // پاکسازی فرم برای فاکتور بعدی
                ResetForm();
            }
            catch (Exception ex)
            {
                ShowMessage("خطا در ثبت فاکتور:\n" + ex.Message, MessageType.Error);
            }
        }

        private async void BtnManualWhatsapp_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer customer)
            {
                ShowMessage("اطلاعات ناقص است.", MessageType.Warning); return;
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
                ShowMessage("خطا در پردازش ارسال: " + ex.Message, MessageType.Error);
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

        private void BtnPrintMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void BtnCopyImage_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer customer)
            {
                ShowMessage("لطفا مشتری و اقلام را وارد کنید.", MessageType.Warning);
                return;
            }

            var draftInvoice = CreateDraftInvoice(customer);

            try
            {
                var printer = new InvoicePrinter(draftInvoice);
                var image = printer.GenerateImage();
                Clipboard.SetImage(image);
                ShowMessage("عکس فاکتور کپی شد!\nالان Paste کنید (Ctrl+V).", MessageType.Success);
            }
            catch (Exception ex)
            {
                ShowMessage("خطا در تولید عکس: " + ex.Message, MessageType.Error);
            }
        }

        private void BtnSaveImageFile_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceItems.Count == 0 || CmbCustomers.SelectedItem is not Customer customer)
            {
                ShowMessage("لطفا مشتری و اقلام را وارد کنید.", MessageType.Warning);
                return;
            }

            var draftInvoice = CreateDraftInvoice(customer);

            try
            {
                var printer = new InvoicePrinter(draftInvoice);
                printer.SaveAsImage();
            }
            catch (Exception ex)
            {
                ShowMessage("خطا در ذخیره سازی: " + ex.Message, MessageType.Error);
            }
        }
    }
}