using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Runtime.InteropServices;
using FactorApp.UI.Data;
using FactorApp.UI.Helpers;
using FactorApp.UI.Models;
using MaterialDesignThemes.Wpf;
using Hardcodet.Wpf.TaskbarNotification; // کتابخانه جدید
using System.Windows.Input; // برای ICommand

// تداخل‌ها
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace FactorApp.UI
{
    public partial class App : System.Windows.Application
    {
        private TaskbarIcon? _taskbarIcon; // استفاده از آیکون مدرن
        private static Mutex? _mutex = null;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        protected override void OnStartup(StartupEventArgs e)
        {
            const string appName = "FactorApp_Unique_ID";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                BringExistingInstanceToFront();
                Shutdown();
                return;
            }

            base.OnStartup(e);

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            ApplySavedTheme();

            // راه اندازی Tray Icon مدرن
            SetupModernTrayIcon();

            var loadingWindow = new LoadingWindow();
            loadingWindow.OperationCompleted += OnLoadingFinished;
            loadingWindow.Show();
        }

        private void SetupModernTrayIcon()
        {
            // خواندن ریسورس تعریف شده در App.xaml
            _taskbarIcon = (TaskbarIcon)FindResource("MyNotifyIcon");

            if (_taskbarIcon != null)
            {
                // بایند کردن رویداد دابل کلیک
                _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowMainWindow();

                // ایجاد DataContext برای منوی راست کلیک (جهت اتصال Command ها در XAML)
                _taskbarIcon.DataContext = new
                {
                    OpenCommand = new RelayCommand(o => ShowMainWindow()),
                    ExitCommand = new RelayCommand(o => ExitApplication()),
                    ToggleThemeCommand = new RelayCommand(o => ToggleAppTheme()) // New Command
                };
            }
        }
        private void ToggleAppTheme()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var info = context.StoreInfos.FirstOrDefault();
                    if (info == null)
                    {
                        info = new StoreInfo { IsDarkMode = false }; // Default
                        context.StoreInfos.Add(info);
                    }

                    // Toggle
                    info.IsDarkMode = !info.IsDarkMode;
                    context.SaveChanges();
                }

                // Apply
                ApplySavedTheme();
            }
            catch (Exception ex)
            {
                // Log or show notification if needed
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
        public void ShowNotification(string title, string message)
        {
            if (_taskbarIcon == null) return;

            // نمایش نوتیفیکیشن مدرن
            _taskbarIcon.ShowBalloonTip(title, message, BalloonIcon.Info);
        }

        private void BringExistingInstanceToFront()
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var processes = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);

            foreach (var process in processes)
            {
                if (process.Id != currentProcess.Id)
                {
                    IntPtr handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        ShowWindow(handle, 9);
                        SetForegroundWindow(handle);
                    }
                    break;
                }
            }
        }

        private void OnLoadingFinished(LoadingResult result, User? user)
        {
            if (result == LoadingResult.ShowMain && user != null)
            {
                ShowMainWindow(user);
                ShowNotification("خوش آمدید", $"ورود با موفقیت انجام شد.\nکاربر: {user.FullName}");
            }
            else if (result == LoadingResult.ShowLogin)
            {
                var loginWindow = new LoginWindow();
                bool? dialogResult = loginWindow.ShowDialog();

                if (loginWindow.IsLoggedIn && loginWindow.User != null)
                {
                    ShowMainWindow(loginWindow.User);
                }
                else
                {
                    ExitApplication();
                }
            }
            else
            {
                ExitApplication();
            }
        }

        private void ShowMainWindow(User user)
        {
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();
        }

        public void ShowMainWindow()
        {
            if (MainWindow != null)
            {
                MainWindow.Show();
                if (MainWindow.WindowState == WindowState.Minimized)
                    MainWindow.WindowState = WindowState.Normal;
                MainWindow.Activate();
            }
        }

        public void ApplySavedTheme()
        {
            try
            {
                bool isDark = false;
                try
                {
                    using (var context = new AppDbContext())
                    {
                        if (context.Database.CanConnect())
                        {
                            var info = context.StoreInfos.FirstOrDefault();
                            if (info != null) isDark = info.IsDarkMode;
                        }
                    }
                }
                catch { }

                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                BaseTheme baseTheme = isDark ? BaseTheme.Dark : BaseTheme.Light;
                theme.SetBaseTheme(baseTheme);

                if (isDark)
                {
                    theme.SetPrimaryColor((MediaColor)MediaColorConverter.ConvertFromString("#FFD700"));
                    theme.SetSecondaryColor((MediaColor)MediaColorConverter.ConvertFromString("#A9A9A9"));
                    theme.Background = (MediaColor)MediaColorConverter.ConvertFromString("#171717");

                    Application.Current.Resources["SidebarBackgroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#1E1E1E"));
                    Application.Current.Resources["SidebarForegroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#E0E0E0"));
                    Application.Current.Resources["CardBackgroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#212121"));
                    Application.Current.Resources["CardForegroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#FFFFFF"));
                }
                else
                {
                    theme.SetPrimaryColor((MediaColor)MediaColorConverter.ConvertFromString("#3F51B5"));
                    theme.SetSecondaryColor((MediaColor)MediaColorConverter.ConvertFromString("#FF4081"));
                    theme.Background = (MediaColor)MediaColorConverter.ConvertFromString("#F9F9F9");

                    Application.Current.Resources["SidebarBackgroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#FFFFFF"));
                    Application.Current.Resources["SidebarForegroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#212121"));
                    Application.Current.Resources["CardBackgroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#FAFAFA"));
                    Application.Current.Resources["CardForegroundBrush"] = new SolidColorBrush((MediaColor)MediaColorConverter.ConvertFromString("#000000"));
                }

                paletteHelper.SetTheme(theme);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Theme Error: " + ex.Message);
            }
        }

        public void ExitApplication()
        {
            if (MainWindow is MainWindow myWindow)
            {
                myWindow.CanClose = true;
                myWindow.Close();
            }

            if (_taskbarIcon != null)
            {
                _taskbarIcon.Dispose();
                _taskbarIcon = null;
            }

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_taskbarIcon != null) _taskbarIcon.Dispose();
            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
                _mutex = null;
            }
            base.OnExit(e);
        }

    }

    // کلاس کمکی برای مدیریت کلیک‌ها (اگر ندارید اضافه کنید)
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object>? _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter!);
        public void Execute(object? parameter) => _execute(parameter!);
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}