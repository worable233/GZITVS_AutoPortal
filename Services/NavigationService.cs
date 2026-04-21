using Microsoft.UI.Xaml.Controls;

namespace AutoPortal.Services
{
    public enum PageType
    {
        Welcome,
        Home,
        Login,
        Navigation,
        Settings
    }

    public class NavigationService
    {
        private static NavigationService? _instance;
        private Frame? _frame;
        private NavigationView? _navigationView;

        public static NavigationService Instance => _instance ??= new NavigationService();

        public void Initialize(Frame frame, NavigationView? navigationView = null)
        {
            _frame = frame;
            _navigationView = navigationView;
        }

        public void NavigateTo(PageType pageType, object? parameter = null)
        {
            if (_frame == null) return;

            var page = pageType switch
            {
                PageType.Welcome => typeof(Pages.WelcomePage),
                PageType.Home => typeof(Pages.HomePage),
                PageType.Login => typeof(Pages.LoginPage),
                PageType.Navigation => typeof(Pages.NavigationPage),
                PageType.Settings => typeof(Pages.SettingsPage),
                _ => typeof(Pages.HomePage)
            };

            _frame.Navigate(page, parameter);

            UpdateNavigationViewSelection(pageType);
        }

        private void UpdateNavigationViewSelection(PageType pageType)
        {
            if (_navigationView == null) return;

            if (pageType == PageType.Welcome)
            {
                _navigationView.IsPaneVisible = false;
                return;
            }

            _navigationView.IsPaneVisible = true;

            string? tag = pageType switch
            {
                PageType.Home => "Home",
                PageType.Login => "Login",
                PageType.Navigation => "Navigation",
                PageType.Settings => "Settings",
                _ => null
            };

            if (tag == null) return;

            foreach (var item in _navigationView.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == tag)
                {
                    _navigationView.SelectedItem = navItem;
                    return;
                }
            }

            foreach (var item in _navigationView.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == tag)
                {
                    _navigationView.SelectedItem = navItem;
                    return;
                }
            }
        }

        public void GoBack()
        {
            if (_frame != null && _frame.CanGoBack)
            {
                _frame.GoBack();
            }
        }
    }
}
