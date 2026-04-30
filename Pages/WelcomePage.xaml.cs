using AutoPortal.Helpers;
using AutoPortal.Models;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AutoPortal.Pages
{
    public sealed partial class WelcomePage : Page
    {
        private int _currentStep;
        private readonly LoginValidator _loginValidator = new();

        public WelcomePage()
        {
            InitializeComponent();
            Loaded += (_, _) => Page_Loaded();
        }

        private async void Page_Loaded()
        {
            await ShowStepAsync(0);
        }

        private async Task ShowStepAsync(int step)
        {
            _currentStep = step;

            StepContent.Transitions = new TransitionCollection { new EntranceThemeTransition { FromVerticalOffset = 40 } };
            StepContent.Visibility = Visibility.Collapsed;
            await Task.Delay(100);

            switch (step)
            {
                case 0:
                    StepTitle.Text = "欢迎使用 AutoPortal";
                    StepDescription.Text = "自动登录校园网门户系统\n让网络连接更省心";
                    NextButton.Content = "开始配置";
                    InputPanel.Visibility = Visibility.Collapsed;
                    ContentBorder.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    StepTitle.Text = "配置账号信息";
                    StepDescription.Text = "请输入您的学号和密码";
                    NextButton.Content = "下一步";
                    InputPanel.Visibility = Visibility.Visible;
                    ContentBorder.Visibility = Visibility.Visible;
                    break;
                case 2:
                    StepTitle.Text = "配置完成";
                    StepDescription.Text = "配置已保存，点击按钮进入首页";
                    NextButton.Content = "开始使用";
                    InputPanel.Visibility = Visibility.Collapsed;
                    ContentBorder.Visibility = Visibility.Collapsed;
                    break;
            }

            StepContent.Visibility = Visibility.Visible;
            ProgressBar.Value = (_currentStep + 1) / 3.0 * 100;
        }

        private async void NextButton_Click(object? sender, object? e)
        {
            if (_currentStep == 1)
            {
                var username = UsernameTextBox.Text?.Trim() ?? string.Empty;
                var password = PasswordBox.Password ?? string.Empty;

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
                NextButton.IsEnabled = false;
                ProgressRing.IsActive = true;

                var config = new LoginConfig
                {
                    Username = username,
                    Password = password,
                    PortalUrl = "http://10.189.108.11/",
                    AutoLogin = true
                };

                if (_loginValidator.Save(config, out string error))
                {
                    await ShowStepAsync(2);
                }
                else
                {
                    ErrorMessage.Text = $"保存配置失败: {error}";
                    ErrorMessage.Visibility = Visibility.Visible;
                }

                NextButton.IsEnabled = true;
                ProgressRing.IsActive = false;
                return;
            }

            if (_currentStep == 2)
            {
                try
                {
                    var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoPortal", "welcome.log");
                    File.AppendAllText(logPath, $"Step 2: About to navigate to HomePage\n", Encoding.UTF8);
                    
                    // 先导航到 HomePage
                    NavigationService.Instance.NavigateTo(PageType.Home);
                    
                    // 等待一小段时间让 HomePage 完成初始化
                    await Task.Delay(100);
                    
                    File.AppendAllText(logPath, $"Step 2: Navigation completed\n", Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoPortal", "welcome.log");
                    File.AppendAllText(logPath, $"Navigation error: {ex.Message}\n{ex.StackTrace}\n", Encoding.UTF8);
                    
                    // 显示错误对话框而不是直接崩溃
                    var errorDialog = new ContentDialog
                    {
                        Title = "导航失败",
                        Content = $"跳转到首页时出错：{ex.Message}\n\n详细信息：{ex.GetType().Name}",
                        CloseButtonText = "确定",
                        XamlRoot = XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
                return;
            }

            await ShowStepAsync(_currentStep + 1);
        }
    }
}
