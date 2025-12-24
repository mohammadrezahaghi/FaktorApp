using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using Microsoft.EntityFrameworkCore;

// رفع تداخل‌ها (برای اطمینان)
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using MenuItem = System.Windows.Controls.MenuItem;

namespace FactorApp.UI.Pages
{
    public partial class OrdersPage : Page
    {
        private AppDbContext _context;

        public OrdersPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadOrders();
        }

        // >>> متد اصلی بارگذاری با تمام فیلترها <<<
        private void LoadOrders()
        {
            if (_context == null || TxtSearch == null || CmbStatusFilter == null || CmbPaymentFilter == null) return;

            // 1. شروع کوئری
            var query = _context.Invoices.Include(i => i.Customer).AsQueryable();

            // 2. اعمال فیلتر جستجو (نام یا شماره فاکتور)
            if (!string.IsNullOrWhiteSpace(TxtSearch.Text))
            {
                string search = TxtSearch.Text.Trim();
                query = query.Where(i => i.Customer.Name.Contains(search) || i.InvoiceNumber.Contains(search));
            }

            // 3. اعمال فیلتر وضعیت چاپ
            if (CmbStatusFilter.SelectedIndex > 0)
            {
                switch (CmbStatusFilter.SelectedIndex)
                {
                    case 1: query = query.Where(i => i.Status == InvoiceStatus.Pending); break;        // جدید
                    case 2: query = query.Where(i => i.Status == InvoiceStatus.Printing); break;       // تغییر کرد
                    case 3: query = query.Where(i => i.Status == InvoiceStatus.ReadyToDeliver); break; // تغییر کرد
                    case 4: query = query.Where(i => i.Status == InvoiceStatus.Delivered); break;      // تغییر کرد
                }
            }

            // 4. اعمال فیلتر وضعیت مالی (جدید)
            if (CmbPaymentFilter.SelectedIndex > 0)
            {
                switch (CmbPaymentFilter.SelectedIndex)
                {
                    case 1: // پرداخت شده‌ها
                        query = query.Where(i => i.IsPaid == true);
                        break;
                    case 2: // پرداخت نشده‌ها (بدهکاران)
                        query = query.Where(i => i.IsPaid == false);
                        break;
                }
            }

            // 5. اجرا و نمایش (مرتب‌سازی بر اساس جدیدترین)
            DataGridOrders.ItemsSource = query.OrderByDescending(i => i.Id).ToList();
        }

        // ایونت مشترک برای تغییر هر کدام از فیلترها
        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            LoadOrders();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // ریست کردن فیلترها (اختیاری)
            // TxtSearch.Text = "";
            // CmbStatusFilter.SelectedIndex = 0;
            // CmbPaymentFilter.SelectedIndex = 0;
            LoadOrders();
        }

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

            var invoice = _context.Invoices
                                  .Include(i => i.Customer)
                                  .FirstOrDefault(i => i.Id == selectedInvoice.Id);

            if (invoice != null && invoice.Customer != null)
            {
                bool oldIsPaid = invoice.IsPaid;
                bool newIsPaid = (tag != "None");

                // بروزرسانی حساب مشتری
                if (!oldIsPaid && newIsPaid)
                {
                    invoice.Customer.Balance -= invoice.FinalAmount;
                }
                else if (oldIsPaid && !newIsPaid)
                {
                    invoice.Customer.Balance += invoice.FinalAmount;
                }

                // تغییر وضعیت فاکتور
                if (!newIsPaid)
                {
                    invoice.IsPaid = false;
                    invoice.PaymentMethod = PaymentMethod.None;
                }
                else
                {
                    invoice.IsPaid = true;
                    if (Enum.TryParse(tag, out PaymentMethod method))
                    {
                        invoice.PaymentMethod = method;
                    }
                }

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

                        // اگر فاکتور پرداخت نشده بود، باید بدهی مشتری را کم کنیم (چون فاکتور حذف شد)
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

        // --- مدیریت منوی چاپ ---
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
                if (fullInvoice != null)
                {
                    var printer = new InvoicePrinter(fullInvoice);
                    printer.Print();
                }
            }
        }

        private void BtnSaveImageAction_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridOrders.SelectedItem is Invoice selectedInvoice)
            {
                var fullInvoice = GetFullInvoice(selectedInvoice.Id);
                if (fullInvoice != null)
                {
                    var printer = new InvoicePrinter(fullInvoice);
                    printer.SaveAsImage();
                }
            }
        }

        private Invoice GetFullInvoice(int id)
        {
            return _context.Invoices
                    .Include(i => i.Customer)
                    .Include(i => i.Items)
                    .FirstOrDefault(i => i.Id == id);
        }
    }
}