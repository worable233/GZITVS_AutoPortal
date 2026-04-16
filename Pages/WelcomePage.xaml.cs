using AutoPortal.Helpers;
using AutoPortal.Models;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Threading.Tasks;

namespace AutoPortal.Pages
{
    public sealed partial class WelcomePage : Page
    {
        private int _currentStep = 0;
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

            StepContent.Transitions = new TransitionCollection { new EntranceThemeTransition { FromVerticalOffset = 50 } };
            StepContent.Visibility = Visibility.Collapsed;
            await Task.Delay(100);

            switch (step)
            {
                case 0:
                    StepTitle.Text = "欢迎使用 AutoPortal";
                    StepDescription.Text = "自动登录校园网门户系统\n让您的网络连接更加便捷";
                    NextButton.Content = "开始配置";
                    InputPanel.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    StepTitle.Text = "配置账号信息";
                    StepDescription.Text = "请输入您的学号和密码";
                    NextButton.Content = "下一步";
                    InputPanel.Visibility = Visibility.Visible;
                    break;
                case 2:
                    StepTitle.Text = "配置完成";
                    StepDescription.Text = "配置已保存，即将进入主界面";
                    NextButton.Content = "开始使用";
                    InputPanel.Visibility = Visibility.Collapsed;
                    break;
            }

            StepContent.Visibility = Visibility.Visible;
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            ProgressBar.Value = (_currentStep + 1) / 3.0 * 100;
        }

        private async void NextButton_Click(object? _, object? __)
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
            }
            else if (_currentStep == 2)
            {
                NavigationService.Instance.NavigateTo(PageType.Home);
            }
            else
            {
                await ShowStepAsync(_currentStep + 1);
            }
        }
    }
}
