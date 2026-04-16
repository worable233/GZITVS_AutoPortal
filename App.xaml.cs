using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using AutoPortal.Services;
using AutoPortal.Helpers;

namespace AutoPortal
{
    public partial class App : Application
    {
        public static Window? MainWindow { get; private set; }

        public App()
        {
            NativeDllExtractor.Initialize();
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            ApplySavedTheme();
            MainWindow.Activate();
        }

        private void ApplySavedTheme()
        {
            if (MainWindow?.Content is FrameworkElement content)
            {
                var settings = AppSettingsService.Instance.Settings;
                content.RequestedTheme = settings.Theme switch
                {
                    1 => ElementTheme.Light,
                    2 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }
    }
}
