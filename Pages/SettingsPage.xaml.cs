using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AutoPortal.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var settings = AppSettingsService.Instance.Settings;
            ThemeComboBox.SelectedIndex = settings.Theme;
            AutoStartCheckBox.IsOn = settings.EnableAutoLogin;
            MinimizeToTrayCheckBox.IsOn = settings.StartMinimized;
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
        }

        private void MinimizeToTrayCheckBox_Toggled(object sender, RoutedEventArgs e)
        {
            var settings = AppSettingsService.Instance.Settings;
            settings.StartMinimized = MinimizeToTrayCheckBox.IsOn;
            AppSettingsService.Instance.SaveSettings();
        }

        private async void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var cacheDir = System.IO.Path.Combine(appData, "AutoPortal", "Cache");

            try
            {
                if (System.IO.Directory.Exists(cacheDir))
                {
                    System.IO.Directory.Delete(cacheDir, true);
                }
            }
            catch
            {
            }

            var dialog = new ContentDialog
            {
                Title = "清理完成",
                Content = "缓存已清理完成。",
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
                Content = "这将删除所有配置和数据，确定要继续吗？",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = System.IO.Path.Combine(appData, "AutoPortal");

            try
            {
                if (System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.Delete(dir, true);
                }
                System.IO.Directory.CreateDirectory(dir);
            }
            catch
            {
            }

            LoadSettings();

            var successDialog = new ContentDialog
            {
                Title = "重置完成",
                Content = "应用已重置，部分设置将在重启后生效。",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            };
            await successDialog.ShowAsync();
        }
    }
}
