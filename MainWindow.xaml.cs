using AutoPortal.Helpers;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace AutoPortal
{
    public sealed partial class MainWindow : Window
    {
        private readonly LoginValidator _loginValidator = new();

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBar);

            NavigationService.Instance.Initialize(ContentFrame, NavigationView);

            var config = _loginValidator.Load(out string error);

            if (config == null || string.IsNullOrEmpty(config.Username))
            {
                NavigationService.Instance.NavigateTo(PageType.Welcome);
            }
            else
            {
                NavigationService.Instance.NavigateTo(PageType.Home);
            }
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();

                var pageType = tag switch
                {
                    "Home" => PageType.Home,
                    "Login" => PageType.Login,
                    "Navigation" => PageType.Navigation,
                    "Settings" => PageType.Settings,
                    _ => PageType.Home
                };

                NavigationService.Instance.NavigateTo(pageType);
            }
        }
    }
}
