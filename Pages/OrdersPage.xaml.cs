using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.RegularExpressions;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using MenuItem = System.Windows.Controls.MenuItem;
using TextBox = System.Windows.Controls.TextBox;
using Brushes = System.Windows.Media.Brushes;

namespace FactorApp.UI.Pages
{
    public partial class OrdersPage : Page
    {
        private AppDbContext _context;
        private Invoice? _editingInvoice;
        private List<InvoiceItem> _tempItems;
        private List<Service> _allServices;
        private decimal _originalTotalAmount = 0; // متغیر برای نگهداری مبلغ اولیه

        public OrdersPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadOrders();
            LoadServicesForEdit();
        }

        private void LoadServicesForEdit()
        {
            using (var db = new AppDbContext())
            {
                _allServices = db.Services.OrderBy(s => s.Name).ToList();
                CmbEditServices.ItemsSource = _allServices;
            }
        }

        // --- بارگذاری لیست اصلی ---
        private void LoadOrders()
        {
            if (_context == null || TxtSearch == null) return;
            
            var query = _context.Invoices
                                .Include(i => i.Customer)
                                .AsNoTracking() 
                                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                string search = TxtSearch.Text.Trim();
                query = query.Where(i => i.Customer.Name.Contains(search) || i.InvoiceNumber.Contains(search));
            }

            if (CmbStatusFilter.SelectedIndex > 0)
            {
                switch (CmbStatusFilter.SelectedIndex)
                {
                    case 1: query = query.Where(i => i.Status == InvoiceStatus.Pending); break;
                    case 2: query = query.Where(i => i.Status == InvoiceStatus.Printing); break;
                    case 3: query = query.Where(i => i.Status == InvoiceStatus.ReadyToDeliver); break;
                    case 4: query = query.Where(i => i.Status == InvoiceStatus.Delivered); break;
                }
            }

            if (CmbPaymentFilter.SelectedIndex > 0)
            {
                switch (CmbPaymentFilter.SelectedIndex)
                {
                    case 1: query = query.Where(i => i.IsPaid == true); break;
                    case 2: query = query.Where(i => i.IsPaid == false); break;
                }
            }

            DataGridOrders.ItemsSource = query.OrderByDescending(i => i.Id).ToList();
        }

        private void FilterChanged(object sender, RoutedEventArgs e) => LoadOrders();
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadOrders();

        // --- باز کردن دیالوگ جزئیات ---
        private void BtnDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Invoice invoiceRow)
            {
                _editingInvoice = _context.Invoices
                                          .Include(i => i.Items)
                                          .Include(i => i.Customer)
                                          .FirstOrDefault(i => i.Id == invoiceRow.Id);

                if (_editingInvoice == null) return;

                // ذخیره مبلغ اولیه برای مقایسه
                _originalTotalAmount = _editingInvoice.FinalAmount;
                TxtOriginalTotal.Text = _originalTotalAmount.ToString("N0");

                // کپی کردن آیتم‌ها
                _tempItems = _editingInvoice.Items.Select(x => new InvoiceItem
                {
                    Id = x.Id,
                    InvoiceId = x.InvoiceId,
                    ServiceName = x.ServiceName,
                    Quantity = x.Quantity,
                    Width = x.Width,
                    Length = x.Length,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.TotalPrice
                }).ToList();

                TxtDetailInvoiceNum.Text = $"شماره فاکتور: {_editingInvoice.InvoiceNumber} | مشتری: {_editingInvoice.Customer.Name}";
                
                RefreshEditGridAndTotal();
                DetailsDialog.IsOpen = true;
            }
        }

        // >>> تغییر مهم: محاسبه اختلاف قیمت و رنگ‌بندی <<<
        private void RefreshEditGridAndTotal()
        {
            // محاسبه مجدد قیمت‌ها
            foreach (var item in _tempItems)
            {
                CalculateRowTotal(item);
            }

            decimal newTotal = _tempItems.Sum(i => i.TotalPrice);
            TxtEditTotal.Text = newTotal.ToString("N0");

            // محاسبه اختلاف
            decimal diff = newTotal - _originalTotalAmount;
            
            if (diff > 0)
            {
                // افزایش قیمت (سبز)
                TxtDiffAmount.Text = "+" + diff.ToString("N0");
                TxtDiffAmount.Foreground = Brushes.Green;
                IconDiffArrow.Kind = PackIconKind.ArrowUp;
                IconDiffArrow.Foreground = Brushes.Green;
            }
            else if (diff < 0)
            {
                // کاهش قیمت (قرمز)
                TxtDiffAmount.Text = diff.ToString("N0"); // خودش منفی دارد
                TxtDiffAmount.Foreground = Brushes.Red;
                IconDiffArrow.Kind = PackIconKind.ArrowDown;
                IconDiffArrow.Foreground = Brushes.Red;
            }
            else
            {
                // بدون تغییر (خاکستری)
                TxtDiffAmount.Text = "0";
                TxtDiffAmount.Foreground = (System.Windows.Media.Brush)FindResource("SidebarForegroundBrush"); // رنگ پیش‌فرض تم
                IconDiffArrow.Kind = PackIconKind.Minus;
                IconDiffArrow.Foreground = (System.Windows.Media.Brush)FindResource("SidebarForegroundBrush");
            }

            DataGridEditItems.ItemsSource = null;
            DataGridEditItems.ItemsSource = _tempItems;
        }

        private void CalculateRowTotal(InvoiceItem item)
        {
            bool isAreaBased = IsServiceAreaBased(item.ServiceName);
            
            if (isAreaBased && item.Width > 0 && item.Length > 0)
                item.TotalPrice = (decimal)(item.Width * item.Length) * item.Quantity * item.UnitPrice;
            else
                item.TotalPrice = item.Quantity * item.UnitPrice;
        }

        private bool IsServiceAreaBased(string serviceName)
        {
            var service = _allServices.FirstOrDefault(s => s.Name == serviceName);
            return service != null && service.Method == CalculationMethod.AreaBased;
        }

        // --- رویدادهای محاسبه لایو ---
        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is InvoiceItem item)
            {
                var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();

                CalculateRowTotal(item);
                
                // آپدیت کردن فوتر (بدون رفرش کامل گرید برای حفظ فوکوس)
                decimal newTotal = _tempItems.Sum(i => i.TotalPrice);
                TxtEditTotal.Text = newTotal.ToString("N0");
                
                // محاسبه اختلاف لایو
                decimal diff = newTotal - _originalTotalAmount;
                if (diff > 0) { TxtDiffAmount.Text = "+" + diff.ToString("N0"); TxtDiffAmount.Foreground = Brushes.Green; IconDiffArrow.Kind = PackIconKind.ArrowUp; IconDiffArrow.Foreground = Brushes.Green; }
                else if (diff < 0) { TxtDiffAmount.Text = diff.ToString("N0"); TxtDiffAmount.Foreground = Brushes.Red; IconDiffArrow.Kind = PackIconKind.ArrowDown; IconDiffArrow.Foreground = Brushes.Red; }
                else { TxtDiffAmount.Text = "0"; TxtDiffAmount.Foreground = (System.Windows.Media.Brush)FindResource("SidebarForegroundBrush"); IconDiffArrow.Kind = PackIconKind.Minus; IconDiffArrow.Foreground = (System.Windows.Media.Brush)FindResource("SidebarForegroundBrush"); }
            }
        }

        private void Price_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is InvoiceItem item)
            {
                string rawText = textBox.Text.Replace(",", "");
                if (decimal.TryParse(rawText, out decimal price))
                {
                    item.UnitPrice = price;
                    
                    int caretIndex = textBox.CaretIndex;
                    string formatted = price.ToString("N0");
                    
                    if (textBox.Text != formatted)
                    {
                        textBox.Text = formatted;
                        int diff = formatted.Length - rawText.Length;
                        textBox.CaretIndex = Math.Max(0, Math.Min(formatted.Length, caretIndex + (formatted.Length - textBox.Text.Length)));
                        textBox.CaretIndex = formatted.Length;
                    }
                }

                CalculateRowTotal(item);
                
                // آپدیت لایو فوتر
                decimal newTotal = _tempItems.Sum(i => i.TotalPrice);
                TxtEditTotal.Text = newTotal.ToString("N0");
                
                decimal diffVal = newTotal - _originalTotalAmount;
                if (diffVal > 0) { TxtDiffAmount.Text = "+" + diffVal.ToString("N0"); TxtDiffAmount.Foreground = Brushes.Green; IconDiffArrow.Kind = PackIconKind.ArrowUp; IconDiffArrow.Foreground = Brushes.Green; }
                else if (diffVal < 0) { TxtDiffAmount.Text = diffVal.ToString("N0"); TxtDiffAmount.Foreground = Brushes.Red; IconDiffArrow.Kind = PackIconKind.ArrowDown; IconDiffArrow.Foreground = Brushes.Red; }
                else { TxtDiffAmount.Text = "0"; TxtDiffAmount.Foreground = (System.Windows.Media.Brush)FindResource("SidebarForegroundBrush"); IconDiffArrow.Kind = PackIconKind.Minus; IconDiffArrow.Foreground = (System.Windows.Media.Brush)FindResource("SidebarForegroundBrush"); }
            }
        }

        private void Dimensions_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is InvoiceItem item)
            {
                bool isArea = IsServiceAreaBased(item.ServiceName);
                textBox.IsEnabled = isArea;
            }
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e) => e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
        private void DecimalValidation(object sender, TextCompositionEventArgs e) => e.Handled = new Regex("[^0-9.]+").IsMatch(e.Text);

        private void BtnAddEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (CmbEditServices.SelectedItem is Service service)
            {
                var newItem = new InvoiceItem
                {
                    Id = 0,
                    InvoiceId = _editingInvoice?.Id ?? 0,
                    ServiceName = service.Name,
                    UnitPrice = service.UnitPrice,
                    Quantity = 1,
                    Width = 0, Length = 0,
                    TotalPrice = service.UnitPrice
                };
                
                _tempItems.Add(newItem);
                RefreshEditGridAndTotal();
            }
        }

        private void BtnDeleteEditItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceItem item)
            {
                _tempItems.Remove(item);
                RefreshEditGridAndTotal();
            }
        }

        private void BtnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_editingInvoice == null) return;

            try
            {
                var dbItems = _context.InvoiceItems.Where(x => x.InvoiceId == _editingInvoice.Id).ToList();
                var uiIds = _tempItems.Select(x => x.Id).ToList();
                var itemsToDelete = dbItems.Where(x => !uiIds.Contains(x.Id)).ToList();

                if (itemsToDelete.Any()) _context.InvoiceItems.RemoveRange(itemsToDelete);

                foreach (var tempItem in _tempItems)
                {
                    CalculateRowTotal(tempItem);

                    if (tempItem.Id == 0)
                    {
                        var newItem = new InvoiceItem
                        {
                            InvoiceId = _editingInvoice.Id,
                            ServiceName = tempItem.ServiceName,
                            Quantity = tempItem.Quantity,
                            Width = tempItem.Width,
                            Length = tempItem.Length,
                            UnitPrice = tempItem.UnitPrice,
                            TotalPrice = tempItem.TotalPrice
                        };
                        _context.InvoiceItems.Add(newItem);
                    }
                    else
                    {
                        var existingItem = dbItems.FirstOrDefault(x => x.Id == tempItem.Id);
                        if (existingItem != null)
                        {
                            existingItem.ServiceName = tempItem.ServiceName;
                            existingItem.Quantity = tempItem.Quantity;
                            existingItem.Width = tempItem.Width;
                            existingItem.Length = tempItem.Length;
                            existingItem.UnitPrice = tempItem.UnitPrice;
                            existingItem.TotalPrice = tempItem.TotalPrice;
                        }
                    }
                }

                decimal newTotal = _tempItems.Sum(i => i.TotalPrice);
                _editingInvoice.FinalAmount = newTotal;

                if (!_editingInvoice.IsPaid)
                {
                    decimal diff = newTotal - _originalTotalAmount; // از متغیر اصلی استفاده می‌کنیم
                    var customer = _context.Customers.Find(_editingInvoice.CustomerId);
                    if (customer != null) customer.Balance += diff;
                }

                _context.SaveChanges();
                DetailsDialog.IsOpen = false;
                LoadOrders();
                MessageBox.Show("تغییرات با موفقیت اعمال شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در ذخیره تغییرات: " + ex.Message);
            }
        }

        // --- متدهای دیگر بدون تغییر ---
        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridOrders.SelectedItem is not Invoice selectedInvoice) return;
            if (sender is not MenuItem menuItem) return;
            string tag = menuItem.Tag.ToString();
            var invoice = _context.Invoices.Find(selectedInvoice.Id);
            if (invoice != null && Enum.TryParse(tag, out InvoiceStatus newStatus))
            {
                invoice.Status = newStatus;
                _context.SaveChanges();
                LoadOrders();
            }
        }

        private void ChangePayment_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridOrders.SelectedItem is not Invoice selectedInvoice) return;
            if (sender is not MenuItem menuItem) return;
            string tag = menuItem.Tag.ToString();
            var invoice = _context.Invoices.Include(i => i.Customer).FirstOrDefault(i => i.Id == selectedInvoice.Id);
            
            if (invoice != null && invoice.Customer != null)
            {
                bool oldIsPaid = invoice.IsPaid;
                bool newIsPaid = (tag != "None");

                if (!oldIsPaid && newIsPaid) invoice.Customer.Balance -= invoice.FinalAmount;
                else if (oldIsPaid && !newIsPaid) invoice.Customer.Balance += invoice.FinalAmount;

                if (!newIsPaid) { invoice.IsPaid = false; invoice.PaymentMethod = PaymentMethod.None; }
                else { invoice.IsPaid = true; if (Enum.TryParse(tag, out PaymentMethod method)) invoice.PaymentMethod = method; }

                _context.SaveChanges();
                LoadOrders();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridOrders.SelectedItem is Invoice selectedInvoice)
            {
                if (MessageBox.Show("آیا از حذف این فاکتور مطمئن هستید؟", "حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var invoice = _context.Invoices.Find(selectedInvoice.Id);
                    if (invoice != null)
                    {
                        var items = _context.InvoiceItems.Where(x => x.InvoiceId == invoice.Id).ToList();
                        _context.InvoiceItems.RemoveRange(items);
                        if (!invoice.IsPaid)
                        {
                            var customer = _context.Customers.Find(invoice.CustomerId);
                            if (customer != null) customer.Balance -= invoice.FinalAmount;
                        }
                        _context.Invoices.Remove(invoice);
                        _context.SaveChanges();
                        LoadOrders();
                    }
                }
            }
        }

        private void BtnPrintMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void BtnPrintAction_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridOrders.SelectedItem is Invoice selectedInvoice)
            {
                var fullInvoice = GetFullInvoice(selectedInvoice.Id);
                if (fullInvoice != null) { var printer = new InvoicePrinter(fullInvoice); printer.Print(); }
            }
        }

        private void BtnSaveImageAction_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridOrders.SelectedItem is Invoice selectedInvoice)
            {
                var fullInvoice = GetFullInvoice(selectedInvoice.Id);
                if (fullInvoice != null) { var printer = new InvoicePrinter(fullInvoice); printer.SaveAsImage(); }
            }
        }

        private Invoice GetFullInvoice(int id)
        {
            return _context.Invoices.Include(i => i.Customer).Include(i => i.Items).FirstOrDefault(i => i.Id == id);
        }
    }
}