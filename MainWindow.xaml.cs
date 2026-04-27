using AutoPortal.Helpers;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AutoPortal
{
    public sealed partial class MainWindow : Window
    {
        private readonly LoginValidator _loginValidator = new();

        public MainWindow()
        {
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoPortal", "startup.log");
            try
            {
                System.IO.File.AppendAllText(logPath, "=== MainWindow: Before InitializeComponent ===\n", System.Text.Encoding.UTF8);
                InitializeComponent();
                System.IO.File.AppendAllText(logPath, "=== MainWindow: After InitializeComponent ===\n", System.Text.Encoding.UTF8);
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(TitleBar);

                NavigationService.Instance.Initialize(ContentFrame, NavigationView);

                var config = _loginValidator.Load(out string error);

                if (config == null || string.IsNullOrEmpty(config.Username))
                {
                    NavigationService.Instance.NavigateTo(PageType.Welcome);
                }
                else
                {
                    NavigationService.Instance.NavigateTo(PageType.Home);
                }
                
                System.IO.File.AppendAllText(logPath, "=== MainWindow: Constructor completed ===\n", System.Text.Encoding.UTF8);
                
                // 设置窗口尺寸，确保窗口能显示
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    logPath,
                    $"=== MainWindow Constructor Failed: {ex.GetType().FullName} ===\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}\n",
                    System.Text.Encoding.UTF8);
                throw;
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();

                var pageType = tag switch
                {
                    "Home" => PageType.Home,
                    "Login" => PageType.Login,
                    "Navigation" => PageType.Navigation,
                    "Settings" => PageType.Settings,
                    _ => PageType.Home
                };

                NavigationService.Instance.NavigateTo(pageType);
            }
        }
    }
}
