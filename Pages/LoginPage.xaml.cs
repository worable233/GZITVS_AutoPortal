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
            _config = _loginValidator.Load(out _);

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
                ShowError("请输入学号");
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("请输入密码");
                return;
            }

            ErrorMessage.Visibility = Visibility.Collapsed;
            SetLoadingState(true);

            try
            {
                bool ok = await Task.Run(() => _loginValidator.Validate(username, password, out string error)
                    ? true
                    : throw new InvalidOperationException(error));

                if (ok)
                {
                    await ShowSuccessAsync();
                    NavigationService.Instance.NavigateTo(PageType.Home);
                }
            }
            catch (Exception ex)
            {
                ShowError($"登录失败: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(PageType.Settings);
        }

        private void SetLoadingState(bool isLoading)
        {
            LoginButton.IsEnabled = !isLoading;
            ProgressRing.IsActive = isLoading;
            ProgressRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            UsernameTextBox.IsEnabled = !isLoading;
            PasswordBox.IsEnabled = !isLoading;
        }

        private void ShowError(string message)
        {
            ErrorMessage.Text = message;
            ErrorMessage.Visibility = Visibility.Visible;
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
