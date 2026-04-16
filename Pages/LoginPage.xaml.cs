using AutoPortal.Helpers;
using AutoPortal.Models;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace AutoPortal.Pages
{
    public sealed partial class LoginPage : Page
    {
        private readonly LoginValidator _loginValidator = new();
        private LoginConfig? _config;

        public LoginPage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _config = _loginValidator.Load(out string error);

            if (_config != null && !string.IsNullOrEmpty(_config.Username))
            {
                UsernameTextBox.Text = _config.Username;

                if (!string.IsNullOrEmpty(_config.Password))
                {
                    PasswordBox.Password = _config.Password;
                }
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username))
            {
                ErrorMessage.Text = "请输入学号";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ErrorMessage.Text = "请输入密码";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }

            ErrorMessage.Visibility = Visibility.Collapsed;
            SetLoadingState(true);

            try
            {
                if (_loginValidator.Validate(username, password, out string error))
                {
                    await ShowSuccessAsync();
                    NavigationService.Instance.NavigateTo(PageType.Home);
                }
                else
                {
                    ErrorMessage.Text = $"登录失败: {error}";
                    ErrorMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"登录出错: {ex.Message}";
                ErrorMessage.Visibility = Visibility.Visible;
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(PageType.Config);
        }

        private void SetLoadingState(bool isLoading)
        {
            LoginButton.IsEnabled = !isLoading;
            ProgressRing.IsActive = isLoading;
            ProgressRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            UsernameTextBox.IsEnabled = !isLoading;
            PasswordBox.IsEnabled = !isLoading;
        }

        private async Task ShowSuccessAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "登录成功",
                Content = $"欢迎，{UsernameTextBox.Text}！",
                CloseButtonText = "确定",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
