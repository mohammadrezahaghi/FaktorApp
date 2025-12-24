using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FactorApp.UI.Data;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
namespace FactorApp.UI.Pages
{
    public partial class ServicesPage : Page
    {
        private Service? _editingService = null;

        public ServicesPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData(string search = "")
        {
            using (var context = new AppDbContext())
            {
                var query = context.Services.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => s.Name.Contains(search) || s.Category.Contains(search));
                }

                DataGridServices.ItemsSource = query.OrderByDescending(s => s.Id).ToList();
            }
        }

        // دکمه افزودن
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _editingService = null;
            TxtDialogTitle.Text = "افزودن خدمت جدید";
            ServInputName.Clear();
            ServInputCategory.Text = "";
            ServInputPrice.Clear();
            ServInputMethod.SelectedIndex = 0;

            // باز کردن مستقیم
            ServiceDialog.IsOpen = true;
        }

        // دکمه ویرایش
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Service service)
            {
                _editingService = service;
                TxtDialogTitle.Text = "ویرایش خدمت";

                ServInputName.Text = service.Name;
                ServInputCategory.Text = service.Category;
                ServInputPrice.Text = service.UnitPrice.ToString("N0").Replace(",", "");
                ServInputMethod.SelectedIndex = service.Method == CalculationMethod.AreaBased ? 1 : 0;

                // باز کردن مستقیم
                ServiceDialog.IsOpen = true;
            }
        }

        private void BtnSaveService_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ServInputName.Text) || string.IsNullOrWhiteSpace(ServInputPrice.Text))
            {
                MessageBox.Show("نام و قیمت الزامی است.");
                return;
            }

            if (!decimal.TryParse(ServInputPrice.Text.Replace(",", ""), out decimal price))
            {
                MessageBox.Show("قیمت وارد شده صحیح نیست.");
                return;
            }

            var method = ServInputMethod.SelectedIndex == 1 ? CalculationMethod.AreaBased : CalculationMethod.FixedQuantity;

            using (var context = new AppDbContext())
            {
                if (_editingService == null)
                {
                    var newService = new Service
                    {
                        Name = ServInputName.Text,
                        Category = ServInputCategory.Text,
                        UnitPrice = price,
                        Method = method
                    };
                    context.Services.Add(newService);
                }
                else
                {
                    var serviceToUpdate = context.Services.Find(_editingService.Id);
                    if (serviceToUpdate != null)
                    {
                        serviceToUpdate.Name = ServInputName.Text;
                        serviceToUpdate.Category = ServInputCategory.Text;
                        serviceToUpdate.UnitPrice = price;
                        serviceToUpdate.Method = method;
                    }
                }
                context.SaveChanges();
            }

            // بستن مستقیم
            ServiceDialog.IsOpen = false;
            LoadData();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Service service)
            {
                var result = MessageBox.Show($"آیا از حذف سرویس '{service.Name}' مطمئن هستید؟", "تایید حذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    using (var context = new AppDbContext())
                    {
                        var itemToDelete = context.Services.Find(service.Id);
                        if (itemToDelete != null)
                        {
                            context.Services.Remove(itemToDelete);
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