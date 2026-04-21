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
        private AppWindow? _appWindow;

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
                _appWindow = AppWindow.GetFromWindowId(windowId);
                
                if (_appWindow != null)
                {
                    // 设置窗口图标 - 使用完整路径
                    var iconPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Assets",
                        "Logo.png");
                    
                    // 尝试设置图标
                    if (System.IO.File.Exists(iconPath))
                    {
                        _appWindow.SetIcon(iconPath);
                    }
                    else
                    {
                        // 如果找不到，尝试从资源中设置
                        var hwndPtr = hwnd;
                        SendMessage(hwndPtr, 0x0080, 0, 0); // WM_SETICON
                    }
                    
                    // 仅在窗口初始小于 400x270 时设置初始大小
                    var size = _appWindow.Size;
                    if (size.Width < 400 || size.Height < 270)
                    {
                        _appWindow.Resize(new SizeInt32 { Width = 400, Height = 270 });
                    }

                    // 监听窗口最小化事件
                    _appWindow.Changed += AppWindow_Changed;
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

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var placement = new WINDOWPLACEMENT();
            placement.length = System.Runtime.InteropServices.Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            
            if (placement.showCmd == 2)
            {
                var settings = AppSettingsService.Instance.Settings;
                if (settings.StartMinimized)
                {
                    TrayService.Instance.MinimizeToTray();
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
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
