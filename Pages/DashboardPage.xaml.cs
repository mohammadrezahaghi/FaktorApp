using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FactorApp.UI.Data;
using Microsoft.EntityFrameworkCore;

namespace FactorApp.UI.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            // لود دیتا در رویداد Page_Loaded انجام می‌شود
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadStats();
        }

        private void LoadStats()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var today = DateTime.Today;

                    // 1. محاسبه فروش امروز
                    var todayInvoices = context.Invoices
                                               .Where(i => i.Date >= today)
                                               .ToList();

                    decimal todaySales = todayInvoices.Sum(i => i.FinalAmount);
                    int todayCount = todayInvoices.Count;
                    
                    // 2. تعداد کل مشتریان
                    int totalCustomers = context.Customers.Count();

                    // 3. نمایش در کارت‌ها
                    TxtTodaySales.Text = todaySales.ToString("N0") + " ریال";
                    TxtTodayCount.Text = todayCount.ToString() + " عدد";
                    TxtTotalCustomers.Text = totalCustomers.ToString() + " نفر";

                    // 4. دریافت 10 فاکتور آخر
                    var recentInvoices = context.Invoices
                                                .Include(i => i.Customer) // حتما مشتری را لود کن
                                                .OrderByDescending(i => i.Id)
                                                .Take(10)
                                                .ToList();

                    DataGridRecent.ItemsSource = recentInvoices;
                }
            }
            catch (Exception ex)
            {
                // اگر دیتابیس هنوز ساخته نشده باشد یا خطایی رخ دهد
                // MessageBox.Show("خطا در بارگذاری داشبورد: " + ex.Message);
            }
        }
    }
}