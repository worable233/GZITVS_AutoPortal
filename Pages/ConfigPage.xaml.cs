using AutoPortal.Helpers;
using AutoPortal.Models;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AutoPortal.Pages
{
    public sealed partial class ConfigPage : Page
    {
        private readonly LoginValidator _loginValidator = new();
        private LoginConfig? _config;

        public ConfigPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
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
            AutoLoginCheckBox.IsChecked = _config.AutoLogin;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;
            var portalUrl = PortalUrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                ShowError("请输入学号");
                return;
            }

            if (string.IsNullOrEmpty(portalUrl))
            {
                ShowError("请输入 Portal 地址");
                return;
            }

            HideMessages();

            _config = new LoginConfig
            {
                Username = username,
                Password = password,
                PortalUrl = portalUrl,
                AutoLogin = AutoLoginCheckBox.IsChecked ?? false
            };

            if (_loginValidator.Save(_config, out string error))
            {
                ShowSuccess("配置已保存");
            }
            else
            {
                ShowError($"保存失败: {error}");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loginValidator.Delete())
            {
                UsernameTextBox.Text = string.Empty;
                PasswordBox.Password = string.Empty;
                PortalUrlTextBox.Text = "http://10.189.108.11/";
                AutoLoginCheckBox.IsChecked = false;
                ShowSuccess("配置已删除");
            }
            else
            {
                ShowError("删除失败，请稍后重试");
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(PageType.Login);
        }

        private void HideMessages()
        {
            SuccessMessage.IsOpen = false;
            SuccessMessage.Visibility = Visibility.Collapsed;
            ErrorMessage.IsOpen = false;
            ErrorMessage.Visibility = Visibility.Collapsed;
        }

        private void ShowSuccess(string message)
        {
            ErrorMessage.IsOpen = false;
            ErrorMessage.Visibility = Visibility.Collapsed;
            SuccessMessage.Message = message;
            SuccessMessage.Visibility = Visibility.Visible;
            SuccessMessage.IsOpen = true;
        }

        private void ShowError(string message)
        {
            SuccessMessage.IsOpen = false;
            SuccessMessage.Visibility = Visibility.Collapsed;
            ErrorMessage.Message = message;
            ErrorMessage.Visibility = Visibility.Visible;
            ErrorMessage.IsOpen = true;
        }
    }
}
