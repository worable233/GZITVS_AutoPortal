using AutoPortal.Helpers;
using AutoPortal.Models;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using Microsoft.Win32;
using Windows.UI;

namespace AutoPortal.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private readonly LoginValidator _loginValidator = new();
        private LoginConfig? _config;

        public SettingsPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            LoadConfig();
            LoadVersionInfo();
        }

        private void LoadConfig()
        {
            _config = _loginValidator.Load(out _);
            if (_config == null) return;

            UsernameTextBox.Text = _config.Username;
            PasswordBox.Password = _config.Password;
            PortalUrlTextBox.Text = string.IsNullOrWhiteSpace(_config.PortalUrl)
                ? "http://10.189.108.11/"
                : _config.PortalUrl;
            AutoLoginCheckBox.IsOn = _config.AutoLogin;
        }

        private void LoadSettings()
        {
            var settings = AppSettingsService.Instance.Settings;
            ThemeComboBox.SelectedIndex = settings.Theme;
            AutoStartCheckBox.IsOn = settings.EnableAutoLogin;
            MicaEffectCheckBox.IsOn = settings.EnableMicaEffect;
            MicaOpacitySlider.Value = settings.MicaOpacity;
            UpdateMicaOpacityText(settings.MicaOpacity);
            
            var interval = settings.ChartUpdateInterval;
            ChartUpdateIntervalComboBox.SelectedIndex = interval switch
            {
                1 => 0,
                2 => 1,
                3 => 2,
                5 => 3,
                _ => 2
            };
        }

        private void LoadVersionInfo()
        {
            // 版本号已在 XAML 中通过 x:Bind 绑定
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = ThemeComboBox.SelectedIndex;
            var settings = AppSettingsService.Instance.Settings;
            settings.Theme = selectedIndex;
            AppSettingsService.Instance.SaveSettings();
            ApplyTheme(selectedIndex);
        }

        private static void ApplyTheme(int themeIndex)
        {
            try
            {
                if (App.MainWindow?.Content is not FrameworkElement content) return;

                content.RequestedTheme = themeIndex switch
                {
                    1 => ElementTheme.Light,
                    2 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
            catch
            {
            }
        }

        private void AutoStartCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            var settings = AppSettingsService.Instance.Settings;
            settings.EnableAutoLogin = AutoStartCheckBox.IsOn;
            AppSettingsService.Instance.SaveSettings();
            
            UpdateStartupRegistry(AutoStartCheckBox.IsOn);
        }

        private void MicaEffectCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            var settings = AppSettingsService.Instance.Settings;
            settings.EnableMicaEffect = MicaEffectCheckBox.IsOn;
            AppSettingsService.Instance.SaveSettings();
            
            ApplyMicaEffect(MicaEffectCheckBox.IsOn);
            UpdateCurrentPageBackground(MicaEffectCheckBox.IsOn);
        }

        private void MicaOpacitySlider_ManipulationCompleted(object sender, Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
        {
            if (MicaOpacityTextBlock == null) return;
            
            var settings = AppSettingsService.Instance.Settings;
            settings.MicaOpacity = MicaOpacitySlider.Value;
            AppSettingsService.Instance.SaveSettings();
            
            UpdateMicaOpacityText(MicaOpacitySlider.Value);
            
            // 只在滑块释放时才应用效果
            if (MicaEffectCheckBox.IsOn)
            {
                ApplyMicaEffectWithOpacity(MicaOpacitySlider.Value);
            }
        }
        
        private void UpdateMicaOpacityText(double opacity)
        {
            MicaOpacityTextBlock.Text = $"{(int)(opacity * 100)}%";
        }
        
        private static void ApplyMicaEffectWithOpacity(double opacity)
        {
            try
            {
                if (App.MainWindow is not MainWindow window) return;
                
                // 创建新的 MicaBackdrop
                var micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop()
                {
                    Kind = opacity >= 0.5 
                        ? Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base 
                        : Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                };
                window.SystemBackdrop = micaBackdrop;
                
                // 注意：MicaBackdrop 本身不支持透明度调节
                // 这里我们通过选择不同的 Mica 类型来模拟透明度效果
                // opacity >= 0.5 使用 Base (较不透明)
                // opacity < 0.5 使用 BaseAlt (较透明)
            }
            catch
            {
                // 忽略错误
            }
        }

        private static void ApplyMicaEffect(bool enable)
        {
            try
            {
                if (App.MainWindow is not MainWindow window) return;

                if (enable)
                {
                    // 启用云母效果
                    var micaBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop()
                    {
                        Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base
                    };
                    window.SystemBackdrop = micaBackdrop;
                }
                else
                {
                    // 禁用云母效果，使用默认背景
                    window.SystemBackdrop = null;
                }
            }
            catch
            {
                // 如果云母效果不支持，回退到默认背景
            }
        }
        
        private void UpdateCurrentPageBackground(bool enable)
        {
            try
            {
                if (App.MainWindow?.Content is FrameworkElement rootElement)
                {
                    var contentFrame = FindChild<Frame>(rootElement, "ContentFrame");
                    if (contentFrame != null && contentFrame.Content is Page currentPage)
                    {
                        currentPage.Background = enable
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

        private static void UpdateStartupRegistry(bool enable)
        {
            try
            {
                var appPath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(appPath)) return;

                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run",
                    writable: true);
                
                if (key == null) return;

                if (enable)
                {
                    key.SetValue("AutoPortal", $"\"{appPath}\"");
                }
                else
                {
                    key.DeleteValue("AutoPortal", throwOnMissingValue: false);
                }
            }
            catch
            {
            }
        }

        private void ChartUpdateIntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = ChartUpdateIntervalComboBox.SelectedIndex;
            var interval = selectedIndex switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                3 => 5,
                _ => 3
            };
            
            var settings = AppSettingsService.Instance.Settings;
            settings.ChartUpdateInterval = interval;
            AppSettingsService.Instance.SaveSettings();
        }

        private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "确认清理缓存",
                Content = "这将清理所有临时文件和缓存数据，但不会删除账号配置。确定要继续吗？",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            int clearedCount = 0;
            var message = "";

            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                var cacheDirs = new[]
                {
                    Path.Combine(appData, "AutoPortal", "Cache"),
                    Path.Combine(appData, "AutoPortal", "Temp"),
                    Path.Combine(localAppData, "AutoPortal", "Cache"),
                    Path.Combine(localAppData, "Packages", "87d9a1db-52e8-4421-90dd-88716f64d8a9", "TempState"),
                    Path.Combine(Path.GetTempPath(), "AutoPortal")
                };

                foreach (var cacheDir in cacheDirs)
                {
                    if (Directory.Exists(cacheDir))
                    {
                        Directory.Delete(cacheDir, true);
                        clearedCount++;
                    }
                }

                var logFile = Path.Combine(appData, "AutoPortal", "app.log");
                if (File.Exists(logFile))
                {
                    File.Delete(logFile);
                    clearedCount++;
                }

                message = $"已清理 {clearedCount} 个缓存项。";
            }
            catch (Exception ex)
            {
                message = $"清理过程中出现错误：{ex.Message}";
            }

            var dialog = new ContentDialog
            {
                Title = "清理完成",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "确认重置",
                Content = "这将删除所有配置和数据，包括：\n\n• 账号配置（学号、密码）\n• 应用设置（主题、自启动等）\n• 所有缓存文件\n\n确定要继续吗？此操作不可恢复！",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var secondConfirm = new ContentDialog
            {
                Title = "再次确认",
                Content = "确定要重置应用吗？所有数据将被永久删除。",
                PrimaryButtonText = "确定重置",
                CloseButtonText = "取消",
                XamlRoot = XamlRoot
            };

            var secondResult = await secondConfirm.ShowAsync();
            if (secondResult != ContentDialogResult.Primary) return;

            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appDir = Path.Combine(appData, "AutoPortal");
                var localAppDir = Path.Combine(localAppData, "AutoPortal");

                _loginValidator.Delete();

                var dirsToDelete = new[] { appDir, localAppDir };
                foreach (var dir in dirsToDelete)
                {
                    if (Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }

                AppSettingsService.Instance.Reset();

                var restartDialog = new ContentDialog
                {
                    Title = "重置完成",
                    Content = "应用已重置到初始状态。是否需要立即重启应用？",
                    PrimaryButtonText = "重启应用",
                    CloseButtonText = "稍后重启",
                    XamlRoot = XamlRoot
                };

                var restartResult = await restartDialog.ShowAsync();
                if (restartResult == ContentDialogResult.Primary)
                {
                    await RestartApplicationAsync();
                }
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "重置失败",
                    Content = $"重置过程中出现错误：{ex.Message}\n\n请手动关闭应用后删除以下目录：\n%APPDATA%\\AutoPortal\n%LOCALAPPDATA%\\AutoPortal",
                    CloseButtonText = "确定",
                    XamlRoot = XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async System.Threading.Tasks.Task RestartApplicationAsync()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var restartScript = Path.Combine(Path.GetTempPath(), "restart_autportal.bat");
                var appPath = Environment.ProcessPath;

                if (!string.IsNullOrEmpty(appPath))
                {
                    var scriptContent = $@"@echo off
echo 正在重启应用...
timeout /t 3 /nobreak >nul
taskkill /F /IM AutoPortal.exe 2>nul
timeout /t 2 /nobreak >nul
start """" ""{appPath}""
timeout /t 1 /nobreak >nul
del ""{restartScript}""
";
                    File.WriteAllText(restartScript, scriptContent);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{restartScript}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });

                    App.MainWindow?.Close();
                }
            }
            catch
            {
                var dialog = new ContentDialog
                {
                    Title = "提示",
                    Content = "自动重启失败，请手动重新启动应用。",
                    CloseButtonText = "确定",
                    XamlRoot = XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;
            var portalUrl = PortalUrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                ShowConfigError("请输入学号");
                return;
            }

            if (string.IsNullOrEmpty(portalUrl))
            {
                ShowConfigError("请输入 Portal 地址");
                return;
            }

            HideConfigMessages();

            _config = new LoginConfig
            {
                Username = username,
                Password = password,
                PortalUrl = portalUrl,
                AutoLogin = AutoLoginCheckBox.IsOn
            };

            if (_loginValidator.Save(_config, out string error))
            {
                ShowConfigSuccess("配置已保存");
            }
            else
            {
                ShowConfigError($"保存失败：{error}");
            }
        }

        private void HideConfigMessages()
        {
            ConfigSuccessMessage.IsOpen = false;
            ConfigSuccessMessage.Visibility = Visibility.Collapsed;
            ConfigErrorMessage.IsOpen = false;
            ConfigErrorMessage.Visibility = Visibility.Collapsed;
        }

        private void ShowConfigSuccess(string message)
        {
            HideConfigMessages();
            ConfigSuccessMessage.Message = message;
            ConfigSuccessMessage.IsOpen = true;
            ConfigSuccessMessage.Visibility = Visibility.Visible;
        }

        private void ShowConfigError(string message)
        {
            HideConfigMessages();
            ConfigErrorMessage.Message = message;
            ConfigErrorMessage.IsOpen = true;
            ConfigErrorMessage.Visibility = Visibility.Visible;
        }
    }
}
