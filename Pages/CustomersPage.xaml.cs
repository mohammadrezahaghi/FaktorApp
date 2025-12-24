using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FactorApp.UI.Data;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
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

        // باز کردن دیالوگ برای افزودن
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _editingCustomer = null;
            TxtDialogTitle.Text = "افزودن مشتری جدید";
            CustInputName.Clear();
            CustInputPhone.Clear();
            CustInputAddress.Clear();
            CustInputBalance.Text = "0";

            // تغییر مهم: باز کردن مستقیم دیالوگ
            CustomerDialog.IsOpen = true;
        }

        // باز کردن دیالوگ برای ویرایش
        // دکمه ویرایش
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

                // تغییر مهم: باز کردن مستقیم دیالوگ
                CustomerDialog.IsOpen = true;
            }
        }

        // ذخیره (Add یا Update)
        // دکمه ذخیره
        private void BtnSaveCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CustInputName.Text))
            {
                MessageBox.Show("نام مشتری الزامی است.");
                return;
            }

            decimal.TryParse(CustInputBalance.Text.Replace(",", ""), out decimal balance);

            using (var context = new AppDbContext())
            {
                if (_editingCustomer == null)
                {
                    var newCustomer = new Customer
                    {
                        Name = CustInputName.Text,
                        PhoneNumber = CustInputPhone.Text,
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
                        customerToUpdate.PhoneNumber = CustInputPhone.Text;
                        customerToUpdate.Address = CustInputAddress.Text;
                        customerToUpdate.Balance = balance;
                    }
                }
                context.SaveChanges();
            }

            // تغییر مهم: بستن مستقیم دیالوگ
            CustomerDialog.IsOpen = false;
            LoadData();
        }

        // حذف مشتری
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Customer customer)
            {
                var result = MessageBox.Show($"با حذف مشتری '{customer.Name}'، تمام فاکتورهای او نیز حذف می‌شوند.\nآیا مطمئن هستید؟", "اخطار حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
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