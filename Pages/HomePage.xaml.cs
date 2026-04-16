using AutoPortal.Helpers;
using AutoPortal.Models;
using AutoPortal.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Windows.System;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using System.Collections.ObjectModel;

namespace AutoPortal.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly LoginValidator _loginValidator = new();
        private LoginConfig? _config;
        private Timer? _netTimer;
        private long _lastUploadBytes = 0;
        private long _lastDownloadBytes = 0;
        private DateTime _lastNetSampleTime;
        private ObservableCollection<double> _uploadSeries = new();
        private ObservableCollection<double> _downloadSeries = new();
        private const int MaxPoints = 60;
        public ISeries[] TrafficSeries { get; set; } = new ISeries[0];
        public Axis[] XAxes { get; set; } = new Axis[0];
        public Axis[] YAxes { get; set; } = new Axis[0];

        public HomePage()
        {
            this.InitializeComponent();
            Loaded += Page_Loaded;
            InitTrafficChart();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckNetworkAndLoginAsync();
            StartNetworkMonitor();
            await LoadIpInfoAsync();
            LoadSystemInfo();
        }

        #region 卡片1：流量统计
        private void InitTrafficChart()
        {
            TrafficSeries = new ISeries[]
            {
                new LineSeries<double> { Values = _uploadSeries, Name = "上传", Fill = null },
                new LineSeries<double> { Values = _downloadSeries, Name = "下载", Fill = null }
            };
            XAxes = new Axis[] { new Axis { IsVisible = false } };
            YAxes = new Axis[] { new Axis { MinLimit = 0 } };
            this.DataContext = this;
        }

        private void StartNetworkMonitor()
        {
            _lastNetSampleTime = DateTime.Now;
            _lastUploadBytes = GetNetworkBytes(true);
            _lastDownloadBytes = GetNetworkBytes(false);
            _netTimer = new Timer(1000);
            _netTimer.Elapsed += NetTimer_Elapsed;
            _netTimer.Start();
        }

        private void NetTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            var upload = GetNetworkBytes(true);
            var download = GetNetworkBytes(false);
            var interval = (now - _lastNetSampleTime).TotalSeconds;
            var upSpeed = (upload - _lastUploadBytes) / interval;
            var downSpeed = (download - _lastDownloadBytes) / interval;
            _lastUploadBytes = upload;
            _lastDownloadBytes = download;
            _lastNetSampleTime = now;
            App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                if (_uploadSeries.Count >= MaxPoints) _uploadSeries.RemoveAt(0);
                if (_downloadSeries.Count >= MaxPoints) _downloadSeries.RemoveAt(0);
                _uploadSeries.Add(Math.Round(upSpeed / 1024, 2));
                _downloadSeries.Add(Math.Round(downSpeed / 1024, 2));
                UploadSpeedText.Text = $"{upSpeed / 1024:F2} KB/s";
                DownloadSpeedText.Text = $"{downSpeed / 1024:F2} KB/s";
                UploadTotalText.Text = $"{upload / 1024.0 / 1024.0:F2} MB";
                DownloadTotalText.Text = $"{download / 1024.0 / 1024.0:F2} MB";
                ActiveConnectionsText.Text = GetActiveTcpConnections().ToString();
                MemoryUsageText.Text = $"{Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0:F2} MB";
            });
        }

        private long GetNetworkBytes(bool upload)
        {
            long total = 0;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var stats = ni.GetIPv4Statistics();
                    total += upload ? stats.BytesSent : stats.BytesReceived;
                }
            }
            return total;
        }

        private int GetActiveTcpConnections()
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Length;
        }
        #endregion

        #region 卡片2：网站测速
        private async void PingApple_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("apple.com", ApplePingText);
        private async void PingGithub_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("github.com", GithubPingText);
        private async void PingBaidu_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("baidu.com", BaiduPingText);
        private async void PingGoogle_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("google.com", GooglePingText);
        private async void PingBing_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("bing.com", BingPingText);
        private async void PingQQ_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("qq.com", QQPingText);

        private async Task PingAndShowAsync(string host, TextBlock textBlock)
        {
            textBlock.Text = "测试中...";
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 2000);
                textBlock.Text = reply.Status == IPStatus.Success ? $"{reply.RoundtripTime} ms" : "超时";
            }
            catch
            {
                textBlock.Text = "失败";
            }
        }
        #endregion

        #region 卡片3：IP信息
        private async Task LoadIpInfoAsync()
        {
            try
            {
                using var client = new HttpClient();
                var json = await client.GetStringAsync("https://ipapi.co/json/");
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                CountryText.Text = $"国家: {root.GetProperty("country_name").GetString()}";
                IPText.Text = $"IP地址: {root.GetProperty("ip").GetString()}";
                ISPText.Text = $"服务商: {root.GetProperty("org").GetString()}";
                OrgText.Text = $"组织: {root.GetProperty("org").GetString()}";
                LocationText.Text = $"位置: {root.GetProperty("city").GetString()}, {root.GetProperty("region").GetString()}";
                ASNText.Text = $"自治域: {root.GetProperty("asn").GetString()}";
                TimezoneText.Text = $"时区: {root.GetProperty("timezone").GetString()}";
            }
            catch
            {
                CountryText.Text = "国家: -";
                IPText.Text = "IP地址: -";
                ISPText.Text = "服务商: -";
                OrgText.Text = "组织: -";
                LocationText.Text = "位置: -";
                ASNText.Text = "自治域: -";
                TimezoneText.Text = "时区: -";
            }
        }
        #endregion

        #region 卡片4：系统信息
        private void LoadSystemInfo()
        {
            OSInfoText.Text = $"操作系统: {Environment.OSVersion}";
            StartupText.Text = $"开机自启动: {(IsStartupEnabled() ? "是" : "否")}";
            RunModeText.Text = $"运行模式: {(IsAdministrator() ? "管理员" : "普通")}";

            AppVersionText.Text = $"程序版本: {GetAppVersion()}";
        }

        private bool IsStartupEnabled()
        {
            // 仅示例，实际可根据注册表或计划任务判断
            return false;
        }

        private bool IsAdministrator()
        {
            return false;
        }

        private string GetAppVersion()
        {
            var asm = typeof(App).Assembly;
            var ver = asm.GetName().Version;
            return ver != null ? ver.ToString() : "-";
        }
        #endregion

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
    }
}
