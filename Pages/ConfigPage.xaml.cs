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
            _config = _loginValidator.Load(out string error);

            if (_config != null)
            {
                UsernameTextBox.Text = _config.Username;
                PasswordBox.Password = _config.Password;
                PortalUrlTextBox.Text = _config.PortalUrl;
                AutoLoginCheckBox.IsChecked = _config.AutoLogin;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;
            var portalUrl = PortalUrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(username))
            {
                ErrorMessage.Message = "请输入学号";
                ErrorMessage.IsOpen = true;
                return;
            }

            if (string.IsNullOrEmpty(portalUrl))
            {
                ErrorMessage.Message = "请输入 Portal 地址";
                ErrorMessage.IsOpen = true;
                return;
            }

            SuccessMessage.IsOpen = false;
            ErrorMessage.IsOpen = false;

            _config = new LoginConfig
            {
                Username = username,
                Password = password,
                PortalUrl = portalUrl,
                AutoLogin = AutoLoginCheckBox.IsChecked ?? false
            };

            if (_loginValidator.Save(_config, out string error))
            {
                SuccessMessage.Message = "配置已保存";
                SuccessMessage.IsOpen = true;
            }
            else
            {
                ErrorMessage.Message = $"保存失败: {error}";
                ErrorMessage.IsOpen = true;
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

                SuccessMessage.Message = "配置已删除";
                SuccessMessage.IsOpen = true;
                ErrorMessage.IsOpen = false;
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(PageType.Login);
        }
    }
}
