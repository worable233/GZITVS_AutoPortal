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
using Windows.UI;

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
                // 在初始化之前就记录日志
                var preLog = new StringBuilder();
                preLog.AppendLine("=== App Constructor Starting ===");
                preLog.AppendLine($"Base Directory: {AppContext.BaseDirectory}");
                preLog.AppendLine($"Application Path: {AppDomain.CurrentDomain.BaseDirectory}");
                System.IO.File.AppendAllText(StartupLogPath, preLog.ToString(), Encoding.UTF8);

                NativeDllExtractor.Initialize();
                
                // 预加载 LiveChartsCore 和 SkiaSharp，确保 Native DLL 被加载
                try
                {
                    var liveChartsAssembly = System.Reflection.Assembly.Load("LiveChartsCore.SkiaSharpView.WinUI");
                    var skiaSharpAssembly = System.Reflection.Assembly.Load("SkiaSharp");
                    System.IO.File.AppendAllText(StartupLogPath, $"=== LiveChartsCore Loaded: {liveChartsAssembly.FullName} ===\n", Encoding.UTF8);
                    System.IO.File.AppendAllText(StartupLogPath, $"=== SkiaSharp Loaded: {skiaSharpAssembly.FullName} ===\n", Encoding.UTF8);
                }
                catch (Exception preloadEx)
                {
                    System.IO.File.AppendAllText(StartupLogPath, $"=== LiveChartsCore/SkiaSharp Preload Failed: {preloadEx.Message} ===\n", Encoding.UTF8);
                }
                
                InitializeComponent();
                
                // 记录初始化成功
                System.IO.File.AppendAllText(StartupLogPath, "=== InitializeComponent Success ===\n", Encoding.UTF8);
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
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoPortal", "startup.log");
            try
            {
                System.IO.File.AppendAllText(logPath, "=== OnLaunched: Creating MainWindow ===\n", System.Text.Encoding.UTF8);
                MainWindow = new MainWindow();
                System.IO.File.AppendAllText(logPath, "=== OnLaunched: MainWindow created ===\n", System.Text.Encoding.UTF8);
                ApplySavedTheme();

                // 激活窗口
                System.IO.File.AppendAllText(logPath, "=== OnLaunched: Calling Activate ===\n", System.Text.Encoding.UTF8);
                MainWindow.Activate();
                System.IO.File.AppendAllText(logPath, "=== OnLaunched: Activate called ===\n", System.Text.Encoding.UTF8);
                
                // 设置应用图标（必须在 Activate 之后）
                SetAppIcon();
                System.IO.File.AppendAllText(logPath, "=== OnLaunched: SetAppIcon called ===\n", System.Text.Encoding.UTF8);

                // 监听窗口关闭事件
                MainWindow.Closed += MainWindow_Closed;
                
                // 监听导航事件，在导航后更新页面背景
                if (MainWindow is MainWindow mainWindow && mainWindow.Content is FrameworkElement rootElement)
                {
                    var contentFrame = FindChild<Frame>(rootElement, "ContentFrame");
                    if (contentFrame != null)
                    {
                        contentFrame.Navigated += (s, e) => 
                        {
                            // 导航到任何页面时都更新背景
                            SetPageBackgrounds(AppSettingsService.Instance.Settings.EnableMicaEffect);
                        };
                    }
                }
                
                System.IO.File.AppendAllText(logPath, "=== OnLaunched: Completed ===\n", System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogFatal("OnLaunched failed", ex);
                
                // 记录更详细的错误信息
                var detailedError = new StringBuilder();
                detailedError.AppendLine($"Exception Type: {ex.GetType().FullName}");
                detailedError.AppendLine($"Message: {ex.Message}");
                detailedError.AppendLine($"StackTrace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    detailedError.AppendLine($"\nInner Exception Type: {ex.InnerException.GetType().FullName}");
                    detailedError.AppendLine($"Inner Message: {ex.InnerException.Message}");
                    detailedError.AppendLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
                }
                
                LogFatal("Detailed Error", new Exception(detailedError.ToString()));
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
            
            // 应用云母效果设置
            ApplyMicaEffect();
        }
        
        private void ApplyMicaEffect()
        {
            try
            {
                if (MainWindow is not MainWindow window) return;
                
                var settings = AppSettingsService.Instance.Settings;
                
                if (settings.EnableMicaEffect)
                {
                    // 启用云母效果，根据透明度选择 Mica 类型
                    var micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop()
                    {
                        Kind = settings.MicaOpacity >= 0.5 
                            ? Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base 
                            : Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                    };
                    window.SystemBackdrop = micaBackdrop;
                    
                    // 设置页面背景为透明，让云母效果显示出来
                    SetPageBackgrounds(true);
                }
                else
                {
                    // 禁用云母效果，使用默认背景
                    window.SystemBackdrop = null;
                    
                    // 恢复页面背景
                    SetPageBackgrounds(false);
                }
            }
            catch
            {
                // 如果云母效果不支持，回退到默认背景
            }
        }
        
        private void SetPageBackgrounds(bool isMicaEnabled)
        {
            try
            {
                if (MainWindow is MainWindow window && window.Content is FrameworkElement rootElement)
                {
                    // 查找 ContentFrame
                    var contentFrame = FindChild<Frame>(rootElement, "ContentFrame");
                    if (contentFrame != null && contentFrame.Content is Page currentPage)
                    {
                        // 设置当前页面背景
                        currentPage.Background = isMicaEnabled 
                            ? new SolidColorBrush(Microsoft.UI.Colors.Transparent)
                            : null;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
        }
        
        private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && (child as FrameworkElement)?.Name == childName)
                {
                    return (T)child;
                }

                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }

            return null;
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
                        "app.ico");
                    
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
