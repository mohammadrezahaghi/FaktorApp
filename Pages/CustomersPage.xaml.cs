using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using FactorApp.UI.Data;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;
using Button = System.Windows.Controls.Button;
using FactorApp.UI.UserControls; // برای دسترسی به MessageDialog و ConfirmDialog

namespace FactorApp.UI.Pages
{
    public partial class CustomersPage : Page
    {
        private Customer? _editingCustomer = null;

        public CustomersPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData(string search = "")
        {
            using (var context = new AppDbContext())
            {
                var query = context.Customers.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => c.Name.Contains(search) || c.PhoneNumber.Contains(search));
                }

                DataGridCustomers.ItemsSource = query.OrderByDescending(c => c.Id).ToList();
            }
        }

        // =========================================================
        // متد نمایش پیام سفارشی (روی لایه بیرونی)
        // =========================================================
        private async void ShowMessage(string message, MessageType type = MessageType.Error)
        {
            var view = new MessageDialog(message, type);
            // نمایش روی دیالوگ اصلی صفحه (PageRootDialog)
            await DialogHost.Show(view, "PageRootDialog");
        }

        // =========================================================
        // متد کمکی: استانداردسازی شماره تلفن
        // =========================================================
        private string NormalizePhoneNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            string digitsOnly = new string(input.Where(char.IsDigit).ToArray());

            if (digitsOnly.StartsWith("98"))
            {
                if (digitsOnly.Length > 2) digitsOnly = "0" + digitsOnly.Substring(2);
            }
            else if (digitsOnly.StartsWith("9") && digitsOnly.Length == 10)
            {
                digitsOnly = "0" + digitsOnly;
            }
            else if (digitsOnly.StartsWith("0098"))
            {
                if (digitsOnly.Length > 4) digitsOnly = "0" + digitsOnly.Substring(4);
            }

            return digitsOnly;
        }

        private void CustInputPhone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void CustInputPhone_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            e.CancelCommand();

            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string rawText = (string)e.DataObject.GetData(typeof(string));
                string cleanText = NormalizePhoneNumber(rawText);
                CustInputPhone.Text = cleanText;
                CustInputPhone.CaretIndex = CustInputPhone.Text.Length;
            }
        }

        private void BtnPastePhone_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                string clipboardText = System.Windows.Clipboard.GetText();
                CustInputPhone.Text = NormalizePhoneNumber(clipboardText);
                CustInputPhone.Focus();
                CustInputPhone.CaretIndex = CustInputPhone.Text.Length;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _editingCustomer = null;
            TxtDialogTitle.Text = "افزودن مشتری جدید";

            CustInputName.Clear();
            CustInputPhone.Clear();
            CustInputAddress.Clear();
            CustInputBalance.Text = "0";

            // باز کردن دیالوگ فرم (داخلی)
            CustomerDialog.IsOpen = true;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Customer customer)
            {
                _editingCustomer = customer;
                TxtDialogTitle.Text = "ویرایش مشتری";

                CustInputName.Text = customer.Name;
                CustInputPhone.Text = customer.PhoneNumber;
                CustInputAddress.Text = customer.Address;
                CustInputBalance.Text = customer.Balance.ToString("N0").Replace(",", "");

                // باز کردن دیالوگ فرم (داخلی)
                CustomerDialog.IsOpen = true;
            }
        }

        private void BtnSaveCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CustInputName.Text))
            {
                // استفاده از دیالوگ سفارشی به جای MessageBox
                ShowMessage("نام مشتری الزامی است.", MessageType.Warning);
                return;
            }

            string cleanPhone = NormalizePhoneNumber(CustInputPhone.Text);
            decimal.TryParse(CustInputBalance.Text.Replace(",", ""), out decimal balance);

            using (var context = new AppDbContext())
            {
                if (_editingCustomer == null)
                {
                    var newCustomer = new Customer
                    {
                        Name = CustInputName.Text,
                        PhoneNumber = cleanPhone,
                        Address = CustInputAddress.Text,
                        Balance = balance
                    };
                    context.Customers.Add(newCustomer);
                }
                else
                {
                    var customerToUpdate = context.Customers.Find(_editingCustomer.Id);
                    if (customerToUpdate != null)
                    {
                        customerToUpdate.Name = CustInputName.Text;
                        customerToUpdate.PhoneNumber = cleanPhone;
                        customerToUpdate.Address = CustInputAddress.Text;
                        customerToUpdate.Balance = balance;
                    }
                }
                context.SaveChanges();
            }

            // بستن فرم
            CustomerDialog.IsOpen = false;
            LoadData();
            
            // ShowMessage("اطلاعات با موفقیت ثبت شد.", MessageType.Success);
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Customer customer)
            {
                // دیالوگ حذف سفارشی
                var dialog = new ConfirmDialog(
                    $"با حذف مشتری '{customer.Name}'، تمام فاکتورهای او نیز حذف می‌شوند.\nآیا مطمئن هستید؟", 
                    "اخطار حذف", 
                    ConfirmType.Delete
                );

                // باز کردن روی لایه بیرونی
                var result = await DialogHost.Show(dialog, "PageRootDialog");

                if (result is bool confirm && confirm)
                {
                    using (var context = new AppDbContext())
                    {
                        var itemToDelete = context.Customers.Find(customer.Id);
                        if (itemToDelete != null)
                        {
                            context.Customers.Remove(itemToDelete);
                            context.SaveChanges();
                        }
                    }
                    LoadData();
                }
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadData(TxtSearch.Text);
        }
    }
}