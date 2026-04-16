using AutoPortal.Helpers;
using AutoPortal.Models;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutoPortal.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly LoginValidator _loginValidator = new();
        private LoginConfig? _config;

        public HomePage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckNetworkAndLoginAsync();
        }

        private async Task CheckNetworkAndLoginAsync()
        {
            StatusCard.Visibility = Visibility.Visible;
            StatusIcon.Glyph = "\uE895";
            StatusTitle.Text = "正在检测网络...";
            StatusDescription.Text = "正在检查校园网连接状态";

            bool isCampusNetwork = await CheckCampusNetworkAsync();

            if (isCampusNetwork)
            {
                _config = _loginValidator.Load(out string loadError);

                if (_config != null && !string.IsNullOrEmpty(_config.Username))
                {
                    if (_config.AutoLogin)
                    {
                        await PerformAutoLoginAsync();
                    }
                    else
                    {
                        ShowLoggedInStatus();
                    }
                }
                else
                {
                    ShowNeedConfigStatus();
                }
            }
            else
            {
                ShowOfflineStatus();
            }
        }

        private async Task<bool> CheckCampusNetworkAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                var response = await client.GetAsync("http://10.189.108.11/");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task PerformAutoLoginAsync()
        {
            StatusIcon.Glyph = "\uE895";
            StatusTitle.Text = "正在自动登录...";
            StatusDescription.Text = $"使用账号 {_config?.Username} 登录中";

            string error = string.Empty;
            if (_config != null && _loginValidator.Validate(_config.Username, _config.Password, out error))
            {
                await Task.Delay(500);
                ShowLoggedInStatus();
            }
            else
            {
                StatusIcon.Glyph = "\uE783";
                StatusTitle.Text = "自动登录失败";
                StatusDescription.Text = error;
            }
        }

        private void ShowLoggedInStatus()
        {
            StatusIcon.Glyph = "\uE73E";
            StatusTitle.Text = "已登录";
            StatusDescription.Text = $"当前账号: {_config?.Username}";
        }

        private void ShowNeedConfigStatus()
        {
            StatusIcon.Glyph = "\uE7BA";
            StatusTitle.Text = "未配置账号";
            StatusDescription.Text = "请先配置您的账号信息";
        }

        private void ShowOfflineStatus()
        {
            StatusIcon.Glyph = "\uE774";
            StatusTitle.Text = "未连接到校园网";
            StatusDescription.Text = "请连接到校园网后重试";
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(PageType.Login);
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(PageType.Config);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            _ = CheckNetworkAndLoginAsync();
        }
    }
}
