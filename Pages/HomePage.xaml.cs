using AutoPortal.Helpers;
using AutoPortal.Models;
using AutoPortal.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Microsoft.Win32;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace AutoPortal.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly LoginValidator _loginValidator = new();
        private LoginConfig? _config;
        private ThreadPoolTimer? _netTimer;
        private long _lastUploadBytes;
        private long _lastDownloadBytes;
        private DateTime _lastNetSampleTime;
        private bool _isRefreshing;
        private bool _isTimerRunning;
        private readonly object _timerLock = new();

        // 优化：使用固定大小的数组代替 ObservableCollection 减少内存分配
        private readonly double[] _uploadBuffer = new double[MaxPoints];
        private readonly double[] _downloadBuffer = new double[MaxPoints];
        private int _bufferIndex = 0;
        private int _pointCount = 0;
        
        private const int MaxPoints = 20;
        private int _timerCounter = 0;

        // 图表字段
        private ISeries[]? _trafficSeries;
        private Axis[]? _xAxes;
        private Axis[]? _yAxes;

        // 图表属性（在构造函数中已初始化）
        public ISeries[] TrafficSeries => _trafficSeries ?? Array.Empty<ISeries>();
        public Axis[] XAxes => _xAxes ?? Array.Empty<Axis>();
        public Axis[] YAxes => _yAxes ?? Array.Empty<Axis>();

        public HomePage()
        {
            InitializeComponent();
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
            
            // 在构造函数中初始化图表，确保 XAML 绑定时有数据
            InitTrafficChart();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateDashboardLayout(ActualWidth);
            StartNetworkMonitor();
            await RefreshDashboardAsync(includeAutoFlow: true);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            StopNetworkMonitor();
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateDashboardLayout(e.NewSize.Width);
        }

        private void UpdateDashboardLayout(double width)
        {
            bool wide = width >= 980;

            if (wide)
            {
                Grid.SetRow(TrafficCard, 0);
                Grid.SetColumn(TrafficCard, 0);
                Grid.SetRow(SpeedCard, 0);
                Grid.SetColumn(SpeedCard, 1);
                Grid.SetRow(IpCard, 1);
                Grid.SetColumn(IpCard, 0);
                Grid.SetRow(SystemCard, 1);
                Grid.SetColumn(SystemCard, 1);
            }
            else
            {
                Grid.SetRow(TrafficCard, 0);
                Grid.SetColumn(TrafficCard, 0);
                Grid.SetRow(SpeedCard, 1);
                Grid.SetColumn(SpeedCard, 0);
                Grid.SetRow(IpCard, 2);
                Grid.SetColumn(IpCard, 0);
                Grid.SetRow(SystemCard, 3);
                Grid.SetColumn(SystemCard, 0);
            }
        }

        private void InitTrafficChart()
        {
            var uploadValues = new ObservableCollection<double>();
            var downloadValues = new ObservableCollection<double>();
            
            _trafficSeries = new ISeries[]
            {
                new LineSeries<double> 
                { 
                    Values = uploadValues,
                    Name = "上传 (KB/s)", 
                    Fill = null,
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(new SKColor(0x00, 0x78, 0xD4)) { StrokeThickness = 2 }
                },
                new LineSeries<double> 
                { 
                    Values = downloadValues,
                    Name = "下载 (KB/s)", 
                    Fill = null,
                    GeometrySize = 0,
                    Stroke = new SolidColorPaint(new SKColor(0x10, 0x7C, 0x10)) { StrokeThickness = 2 }
                }
            };

            _xAxes = new Axis[] { new Axis { IsVisible = false } };
            _yAxes = new Axis[] { new Axis { MinLimit = 0, IsVisible = false } };
            
            DataContext = this;
        }

        private long _uploadTotalBytes;
        private long _downloadTotalBytes;

        private void StartNetworkMonitor()
        {
            _lastNetSampleTime = DateTime.Now;
            _lastUploadBytes = GetNetworkBytes(upload: true);
            _lastDownloadBytes = GetNetworkBytes(upload: false);
            _uploadTotalBytes = 0;
            _downloadTotalBytes = 0;

            _netTimer?.Cancel();

            _netTimer = ThreadPoolTimer.CreatePeriodicTimer(
                (timer) => OnNetworkTimerTick(),
                TimeSpan.FromSeconds(1));
        }

        private void StopNetworkMonitor()
        {
            _netTimer?.Cancel();
            _netTimer = null;
        }

        private void OnNetworkTimerTick()
        {
            lock (_timerLock)
            {
                if (_isTimerRunning) return;
                _isTimerRunning = true;
            }

            try
            {
                var now = DateTime.Now;
                var upload = GetNetworkBytes(upload: true);
                var download = GetNetworkBytes(upload: false);
                var interval = Math.Max((now - _lastNetSampleTime).TotalSeconds, 0.001);
                var upDelta = upload - _lastUploadBytes;
                var downDelta = download - _lastDownloadBytes;
                var upSpeed = upDelta / interval;
                var downSpeed = downDelta / interval;
                
                if (upDelta > 0) _uploadTotalBytes += upDelta;
                if (downDelta > 0) _downloadTotalBytes += downDelta;
                
                _lastUploadBytes = upload;
                _lastDownloadBytes = download;
                _lastNetSampleTime = now;

                if (upSpeed < 0) upSpeed = 0;
                if (downSpeed < 0) downSpeed = 0;

                var updateInterval = AppSettingsService.Instance.Settings.ChartUpdateInterval;
                _timerCounter++;
                if (_timerCounter < updateInterval)
                {
                    return;
                }
                _timerCounter = 0;

                var upSpeedText = $"{upSpeed / 1024:F2} KB/s";
                var downSpeedText = $"{downSpeed / 1024:F2} KB/s";
                var uploadTotalText = $"{_uploadTotalBytes / 1024.0 / 1024.0:F2} MB";
                var downloadTotalText = $"{_downloadTotalBytes / 1024.0 / 1024.0:F2} MB";

                var dispatcher = App.MainWindow?.DispatcherQueue;
                if (dispatcher == null) return;

                dispatcher.TryEnqueue(() =>
                {
                    if (_trafficSeries != null && _trafficSeries.Length >= 2)
                    {
                        var uploadSeries = (LineSeries<double>)_trafficSeries[0];
                        var downloadSeries = (LineSeries<double>)_trafficSeries[1];
                        
                        if (uploadSeries.Values != null && downloadSeries.Values != null)
                        {
                            var upValue = Math.Round(upSpeed / 1024, 2);
                            var downValue = Math.Round(downSpeed / 1024, 2);
                            
                            _uploadBuffer[_bufferIndex] = upValue;
                            _downloadBuffer[_bufferIndex] = downValue;
                            _bufferIndex = (_bufferIndex + 1) % MaxPoints;
                            if (_pointCount < MaxPoints) _pointCount++;
                            
                            uploadSeries.Values = GetUploadBufferArray();
                            downloadSeries.Values = GetDownloadBufferArray();
                        }
                    }

                    UploadSpeedText.Text = upSpeedText;
                    DownloadSpeedText.Text = downSpeedText;
                    UploadTotalText.Text = uploadTotalText;
                    DownloadTotalText.Text = downloadTotalText;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network monitor error: {ex.Message}");
            }
            finally
            {
                lock (_timerLock)
                {
                    _isTimerRunning = false;
                }
            }
        }

        private double[] GetUploadBufferArray()
        {
            var result = new double[_pointCount];
            for (int i = 0; i < _pointCount; i++)
            {
                var idx = (_bufferIndex + i) % MaxPoints;
                result[i] = _uploadBuffer[idx];
            }
            return result;
        }

        private double[] GetDownloadBufferArray()
        {
            var result = new double[_pointCount];
            for (int i = 0; i < _pointCount; i++)
            {
                var idx = (_bufferIndex + i) % MaxPoints;
                result[i] = _downloadBuffer[idx];
            }
            return result;
        }

        private static long GetNetworkBytes(bool upload)
        {
            try
            {
                long total = 0;
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // 只统计正在运行且非回环的网络接口
                    if (ni.OperationalStatus != OperationalStatus.Up || 
                        ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    {
                        continue;
                    }

                    var stats = ni.GetIPv4Statistics();
                    total += upload ? stats.BytesSent : stats.BytesReceived;
                }
                return total;
            }
            catch
            {
                // 如果无法获取网络统计信息，返回 0
                return 0;
            }
        }

        private static int GetActiveTcpConnections()
        {
            return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Length;
        }

        private async void PingApple_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("apple.com", ApplePingText);
        private async void PingGithub_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("github.com", GithubPingText);
        private async void PingBaidu_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("baidu.com", BaiduPingText);
        private async void PingGoogle_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("google.com", GooglePingText);
        private async void PingBing_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("bing.com", BingPingText);
        private async void PingQQ_Click(object sender, RoutedEventArgs e) => await PingAndShowAsync("qq.com", QQPingText);

        private async void PingAll_Click(object sender, RoutedEventArgs e)
        {
            await Task.WhenAll(
                PingAndShowAsync("apple.com", ApplePingText),
                PingAndShowAsync("github.com", GithubPingText),
                PingAndShowAsync("baidu.com", BaiduPingText),
                PingAndShowAsync("google.com", GooglePingText),
                PingAndShowAsync("bing.com", BingPingText),
                PingAndShowAsync("qq.com", QQPingText)
            );
        }

        private async Task PingAndShowAsync(string host, TextBlock textBlock)
        {
            try
            {
                textBlock.Text = "测试中...";
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 3000);
                
                if (reply.Status == IPStatus.Success)
                {
                    textBlock.Text = $"{Math.Max(1, reply.RoundtripTime)} ms";
                }
                else
                {
                    textBlock.Text = "超时";
                }
            }
            catch (PingException)
            {
                textBlock.Text = "失败";
            }
            catch (Exception)
            {
                textBlock.Text = "错误";
            }
        }

        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(8) };

        private async Task LoadIpInfoAsync()
        {
            try
            {
                var json = await _httpClient.GetStringAsync("http://ip-api.com/json/?lang=zh-CN");
                var root = JsonDocument.Parse(json).RootElement;

                var status = root.GetProperty("status").GetString();
                if (status == "success")
                {
                    CountryText.Text = root.GetProperty("country").GetString() ?? "-";
                    IPText.Text = root.GetProperty("query").GetString() ?? "-";
                    ISPText.Text = root.GetProperty("isp").GetString() ?? "-";
                    OrgText.Text = root.GetProperty("org").GetString() ?? "-";
                    LocationText.Text = $"{root.GetProperty("city").GetString() ?? "-"}, {root.GetProperty("regionName").GetString() ?? "-"}";
                    ASNText.Text = $"AS{root.GetProperty("as").GetString() ?? "-"}";
                    TimezoneText.Text = root.GetProperty("timezone").GetString() ?? "-";
                }
                else
                {
                    SetIpInfoError();
                }
            }
            catch
            {
                SetIpInfoError();
            }
        }

        private void SetIpInfoError()
        {
            CountryText.Text = "-";
            IPText.Text = "-";
            ISPText.Text = "-";
            OrgText.Text = "-";
            LocationText.Text = "-";
            ASNText.Text = "-";
            TimezoneText.Text = "-";
        }

        private void LoadSystemInfo()
        {
            OSInfoText.Text = $"{Environment.OSVersion}";
            StartupText.Text = $" {(IsStartupEnabled() ? "是" : "否")}";
            RunModeText.Text = $" {(IsAdministrator() ? "管理员" : "普通")}";
            AppVersionText.Text = $" {GetAppVersion()}";
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
                if (key == null) return false;
                return key.GetValue("AutoPortal") != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static string GetAppVersion()
        {
            return AppVersionService.Version;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            await ManualLoginAsync();
        }

        private void ConfigButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Instance.NavigateTo(PageType.Settings);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshDashboardAsync(includeAutoFlow: true);
        }

        private async Task RefreshDashboardAsync(bool includeAutoFlow)
        {
            if (_isRefreshing) return;

            _isRefreshing = true;
            RefreshButton.IsEnabled = false;
            try
            {
                await LoadIpInfoAsync();
                LoadSystemInfo();
                if (includeAutoFlow)
                {
                    await CheckNetworkAndLoginAsync();
                }
            }
            finally
            {
                RefreshButton.IsEnabled = true;
                _isRefreshing = false;
            }
        }

        private async Task CheckNetworkAndLoginAsync()
        {
            ShowStatus("\uE895", "正在检测网络...", "正在检查校园网连接状态");

            bool isCampusNetwork = await CheckCampusNetworkAsync();
            if (!isCampusNetwork)
            {
                ShowStatus("\uE774", "未连接到校园网", "请连接校园网后重试。");
                return;
            }

            _config = _loginValidator.Load(out string loadError);
            if (!string.IsNullOrWhiteSpace(loadError))
            {
                ShowStatus("\uE783", "读取配置失败", loadError);
                return;
            }

            if (_config == null || string.IsNullOrWhiteSpace(_config.Username))
            {
                ShowStatus("\uE7BA", "未配置账号", "请先在“配置管理”中设置学号和密码。");
                return;
            }

            if (_config.AutoLogin)
            {
                await PerformAutoLoginAsync();
            }
            else
            {
                ShowStatus("\uE73E", "网络正常", $"当前账号：{_config.Username}（自动登录已关闭）");
            }
        }

        private static async Task<bool> CheckCampusNetworkAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
                using var response = await client.GetAsync("http://10.189.108.11/");
                return response.IsSuccessStatusCode || (int)response.StatusCode == 302;
            }
            catch
            {
                return false;
            }
        }

        private async Task PerformAutoLoginAsync()
        {
            if (_config == null) return;
            if (string.IsNullOrWhiteSpace(_config.Password))
            {
                ShowStatus("\uE783", "自动登录失败", "未保存密码，请前往“配置管理”补充密码。");
                return;
            }

            ShowStatus("\uE895", "正在自动登录...", $"使用账号 {_config.Username} 登录中...");

            string error = string.Empty;
            bool success = await Task.Run(() => _loginValidator.Validate(_config.Username, _config.Password, out error));

            if (success)
            {
                await Task.Delay(350);
                ShowStatus("\uE73E", "自动登录成功", $"当前账号：{_config.Username}");
            }
            else
            {
                ShowStatus("\uE783", "自动登录失败", string.IsNullOrWhiteSpace(error) ? "请检查账号配置后重试。" : error);
            }
        }

        private async Task ManualLoginAsync()
        {
            _config = _loginValidator.Load(out string loadError);
            if (!string.IsNullOrWhiteSpace(loadError))
            {
                ShowStatus("\uE783", "读取配置失败", loadError);
                return;
            }

            if (_config == null || string.IsNullOrWhiteSpace(_config.Username))
            {
                ShowStatus("\uE7BA", "未配置账号", "已跳转到设置页，请先配置账号。");
                NavigationService.Instance.NavigateTo(PageType.Settings);
                return;
            }

            if (string.IsNullOrWhiteSpace(_config.Password))
            {
                ShowStatus("\uE783", "缺少密码", "已跳转到设置页，请先保存密码。");
                NavigationService.Instance.NavigateTo(PageType.Settings);
                return;
            }

            ShowStatus("\uE895", "正在登录...", $"使用账号 {_config.Username} 登录中...");

            string error = string.Empty;
            bool success = await Task.Run(() => _loginValidator.Validate(_config.Username, _config.Password, out error));
            ShowStatus(
                success ? "\uE73E" : "\uE783",
                success ? "登录成功" : "登录失败",
                success ? $"当前账号：{_config.Username}" : (string.IsNullOrWhiteSpace(error) ? "请稍后重试。" : error));
        }

        private void ShowStatus(string glyph, string title, string description)
        {
            StatusCard.Visibility = Visibility.Visible;
            StatusIcon.Glyph = glyph;
            StatusTitle.Text = title;
            StatusDescription.Text = description;
        }
    }
}
