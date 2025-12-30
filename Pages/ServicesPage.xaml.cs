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
using FactorApp.UI.UserControls;

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

        // --- اعتبارسنجی: فقط عدد وارد شود ---
        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // --- جدا کردن 3 رقم 3 رقم هنگام تایپ ---
        private void ServInputPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                // حذف رویداد برای جلوگیری از لوپ بی‌نهایت هنگام تغییر متن
                textBox.TextChanged -= ServInputPrice_TextChanged;

                string rawText = textBox.Text.Replace(",", ""); // حذف کاماهای قبلی
                if (!string.IsNullOrEmpty(rawText) && decimal.TryParse(rawText, out decimal number))
                {
                    // فرمت‌دهی با جداکننده هزارگان
                    textBox.Text = number.ToString("N0");
                    
                    // قرار دادن نشانگر تایپ در انتهای متن
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (string.IsNullOrEmpty(rawText))
                {
                    textBox.Text = "";
                }

                // اتصال مجدد رویداد
                textBox.TextChanged += ServInputPrice_TextChanged;
            }
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            _editingService = null;
            TxtDialogTitle.Text = "افزودن خدمت جدید";
            ServInputName.Clear();
            ServInputCategory.Text = "";
            ServInputPrice.Clear();
            ServInputMethod.SelectedIndex = 0;

            ServiceDialog.IsOpen = true;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Service service)
            {
                _editingService = service;
                TxtDialogTitle.Text = "ویرایش خدمت";

                ServInputName.Text = service.Name;
                ServInputCategory.Text = service.Category;
                
                // مقداردهی قیمت با فرمت 3 رقم 3 رقم
                ServInputPrice.Text = service.UnitPrice.ToString("N0");
                
                ServInputMethod.SelectedIndex = service.Method == CalculationMethod.AreaBased ? 1 : 0;

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

            // حذف کاما قبل از تبدیل به عدد برای ذخیره در دیتابیس
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

            ServiceDialog.IsOpen = false;
            LoadData();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Service service)
            {
                var dialog = new ConfirmDialog($"آیا از حذف سرویس '{service.Name}' مطمئن هستید؟");
                // اگر می‌خواهید از ConfirmDialog سفارشی که قبلا ساختیم استفاده کنید، باید اینجا هندل شود
                // فعلا همان مسیج باکس استاندارد را می‌گذارم چون سریع‌تر است
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