using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using AutoPortal.Services;
using AutoPortal.Helpers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoPortal
{
    public partial class App : Application
    {
        public static Window? MainWindow { get; private set; }
        private static readonly string StartupLogPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoPortal",
            "startup.log");

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                NativeDllExtractor.Initialize();
                InitializeComponent();
            }
            catch (Exception ex)
            {
                LogFatal("App ctor failed", ex);
                ShowFatalDialog("应用初始化失败", ex);
                throw;
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                MainWindow = new MainWindow();
                ApplySavedTheme();
                
                // 设置应用图标
                SetAppIcon();

                // 激活窗口
                MainWindow.Activate();

                // 监听窗口关闭事件
                MainWindow.Closed += MainWindow_Closed;
            }
            catch (Exception ex)
            {
                LogFatal("OnLaunched failed", ex);
                ShowFatalDialog("应用启动失败", ex);
                throw;
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
        }

        private void ApplySavedTheme()
        {
            if (MainWindow?.Content is FrameworkElement content)
            {
                var settings = AppSettingsService.Instance.Settings;
                content.RequestedTheme = settings.Theme switch
                {
                    1 => ElementTheme.Light,
                    2 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogFatal("Unhandled exception", ex);
                ShowFatalDialog("发生未处理异常", ex);
            }
            else
            {
                var unknown = new Exception($"Unknown exception object: {e.ExceptionObject}");
                LogFatal("Unhandled exception", unknown);
                ShowFatalDialog("发生未处理异常", unknown);
            }
        }

        private static void LogFatal(string phase, Exception ex)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(StartupLogPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                var sb = new StringBuilder();
                sb.AppendLine("========================================");
                sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                sb.AppendLine(phase);
                sb.AppendLine(ex.ToString());

                System.IO.File.AppendAllText(StartupLogPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        private static void ShowFatalDialog(string title, Exception ex)
        {
            try
            {
                var message = $"{title}\n{ex.Message}\n\n日志: {StartupLogPath}";
                MessageBoxW(IntPtr.Zero, message, "AutoPortal", 0x00000010);
            }
            catch
            {
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);
        
        private void SetAppIcon()
        {
            try
            {
                if (MainWindow == null) return;
                
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                
                if (appWindow != null)
                {
                    var iconPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Assets",
                        "Logo.png");
                    
                    if (System.IO.File.Exists(iconPath))
                    {
                        appWindow.SetIcon(iconPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set app icon: {ex.Message}");
            }
        }
    }
}
