using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FactorApp.UI.Data;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;
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

        // ==========================================================
        // متد نمایش پیام (روی دیالوگ بیرونی باز می‌شود)
        // ==========================================================
        private async void ShowMessage(string message, MessageType type = MessageType.Error)
        {
            // نکته: اینجا دیگر ServiceDialog.IsOpen = false نمی‌کنیم
            // تا اگر فرم باز است، پیام روی آن بیاید و کاربر فرم را از دست ندهد.
            
            var view = new MessageDialog(message, type);
            // استفاده از Identifier دیالوگ بیرونی
            await DialogHost.Show(view, "PageRootDialog");
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ServInputPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.TextChanged -= ServInputPrice_TextChanged;
                string rawText = textBox.Text.Replace(",", ""); 
                if (!string.IsNullOrEmpty(rawText) && decimal.TryParse(rawText, out decimal number))
                {
                    textBox.Text = number.ToString("N0");
                    textBox.CaretIndex = textBox.Text.Length;
                }
                else if (string.IsNullOrEmpty(rawText))
                {
                    textBox.Text = "";
                }
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

            // باز کردن دیالوگ داخلی (فرم)
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
                ServInputPrice.Text = service.UnitPrice.ToString("N0");
                ServInputMethod.SelectedIndex = service.Method == CalculationMethod.AreaBased ? 1 : 0;

                // باز کردن دیالوگ داخلی (فرم)
                ServiceDialog.IsOpen = true; 
            }
        }

        private void BtnSaveService_Click(object sender, RoutedEventArgs e)
        {
            // اعتبارسنجی
            if (string.IsNullOrWhiteSpace(ServInputName.Text) || string.IsNullOrWhiteSpace(ServInputPrice.Text))
            {
                // پیام روی فرم باز می‌شود (چون PageRootDialog لایه بالاتر است)
                ShowMessage("لطفاً نام و قیمت خدمت را وارد کنید.", MessageType.Warning);
                return;
            }

            if (!decimal.TryParse(ServInputPrice.Text.Replace(",", ""), out decimal price))
            {
                ShowMessage("قیمت وارد شده صحیح نمی‌باشد.", MessageType.Error);
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

            // بستن فرم فقط در صورت موفقیت
            ServiceDialog.IsOpen = false;
            LoadData();
            
            // نمایش پیام موفقیت (اختیاری)
            // ShowMessage("اطلاعات با موفقیت ذخیره شد.", MessageType.Success);
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Service service)
            {
                var dialog = new ConfirmDialog(
                    $"آیا از حذف سرویس '{service.Name}' مطمئن هستید؟", 
                    "تایید حذف", 
                    ConfirmType.Delete
                );

                // باز کردن دیالوگ سوال روی لایه بیرونی
                var result = await DialogHost.Show(dialog, "PageRootDialog");

                if (result is bool confirm && confirm)
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