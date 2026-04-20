using AutoPortal.Helpers;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Windows.Graphics;
using System;
using System.Runtime.InteropServices;

namespace AutoPortal
{
    public sealed partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        private readonly LoginValidator _loginValidator = new();

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBar);

            // 设置窗口图标
            try
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                
                if (appWindow != null)
                {
                    // 设置窗口图标 - 使用完整路径
                    var iconPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Assets",
                        "Logo.png");
                    
                    // 尝试设置图标
                    if (System.IO.File.Exists(iconPath))
                    {
                        appWindow.SetIcon(iconPath);
                    }
                    else
                    {
                        // 如果找不到，尝试从资源中设置
                        var hwndPtr = hwnd;
                        SendMessage(hwndPtr, 0x0080, 0, 0); // WM_SETICON
                    }
                    
                    // 仅在窗口初始小于 400x270 时设置初始大小
                    var size = appWindow.Size;
                    if (size.Width < 400 || size.Height < 270)
                    {
                        appWindow.Resize(new SizeInt32 { Width = 400, Height = 270 });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Set icon failed: {ex.Message}");
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
                    "Settings" => PageType.Settings,
                    _ => PageType.Home
                };

                NavigationService.Instance.NavigateTo(pageType);
            }
        }
    }
}
