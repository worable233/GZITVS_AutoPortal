using H.NotifyIcon;
using Microsoft.UI.Xaml;
using System;
using System.IO;

namespace AutoPortal.Services
{
    public class TrayService : IDisposable
    {
        private static TrayService? _instance;
        private TaskbarIcon? _trayIcon;
        private Window? _window;
        private bool _isMinimizeToTrayEnabled;

        public static TrayService Instance => _instance ??= new TrayService();

        public void Initialize(Window window)
        {
            _window = window;
            _isMinimizeToTrayEnabled = AppSettingsService.Instance.Settings.StartMinimized;

            _trayIcon = new TaskbarIcon();
            _trayIcon.ToolTipText = "AutoPortal - 校园网自动登录助手";

            SetIconFromAssets();

            _trayIcon.LeftClickCommand = new RelayCommand(_ => ShowWindow());
        }

        private void SetIconFromAssets()
        {
            try
            {
                var iconPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets",
                    "Logo.png");

                if (File.Exists(iconPath))
                {
                    _trayIcon?.SetValue(TaskbarIcon.IconSourceProperty, iconPath);
                }
            }
            catch
            {
            }
        }

        public void UpdateSettings()
        {
            _isMinimizeToTrayEnabled = AppSettingsService.Instance.Settings.StartMinimized;
        }

        public void MinimizeToTray()
        {
            if (!_isMinimizeToTrayEnabled || _window == null) return;

            _window.Hide();
        }

        public void ShowWindow()
        {
            if (_window == null) return;

            _window.Show();
        }

        public void ShowNotification(string title, string message)
        {
            _trayIcon?.ShowNotification(title, message);
        }

        public void ExitApplication()
        {
            _trayIcon?.Dispose();
            App.MainWindow?.Close();
        }

        public void Dispose()
        {
            _trayIcon?.Dispose();
        }
    }

    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }
}
