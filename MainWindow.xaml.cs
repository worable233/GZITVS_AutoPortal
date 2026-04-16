using AutoPortal.Helpers;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;

namespace AutoPortal
{
    public sealed partial class MainWindow : Window
    {
        private readonly LoginValidator _loginValidator = new();

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBar);

            // 仅在窗口初始小于 400x270 时设置初始大小，WinUI 3 目前不支持直接设置最小窗口尺寸
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                if (appWindow != null)
                {
                    var size = appWindow.Size;
                    if (size.Width < 400 || size.Height < 270)
                    {
                        appWindow.Resize(new SizeInt32 { Width = 400, Height = 270 });
                    }
                }
            }
            catch
            {
                // ignore if AppWindow APIs are not available at runtime
            }

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
                    "Config" => PageType.Config,
                    "Settings" => PageType.Settings,
                    _ => PageType.Home
                };

                NavigationService.Instance.NavigateTo(pageType);
            }
        }
    }
}
