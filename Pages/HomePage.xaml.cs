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
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace AutoPortal.Pages
{
    public sealed partial class HomePage : Page
    {
        private readonly LoginValidator _loginValidator = new();
        private LoginConfig? _config;
        private Timer? _netTimer;
        private long _lastUploadBytes;
        private long _lastDownloadBytes;
        private DateTime _lastNetSampleTime;
        private bool _isRefreshing;

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
            // 设置 SkiaSharp 字体目录以支持中文
            try
            {
                // 加载系统中文字体文件
                var fontPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "Fonts",
                    "msyh.ttc"); // 微软雅黑

                if (System.IO.File.Exists(fontPath))
                {
                    // 预加载字体以确保全局可用
                    var typeface = SKTypeface.FromFile(fontPath);
                    var paint = new SKPaint { Typeface = typeface };
                    paint.Dispose();
                }
            }
            catch { }
            
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
            DashboardCol1.Width = wide ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

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
            // 加载系统中文字体
            var fontPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "Fonts",
                "msyh.ttc"); // 微软雅黑

            SKTypeface? customFont = null;
            if (System.IO.File.Exists(fontPath))
            {
                try
                {
                    customFont = SKTypeface.FromFile(fontPath);
                }
                catch { }
            }

            // 如果找不到微软雅黑，尝试找其他字体
            if (customFont == null)
            {
                fontPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "Fonts",
                    "simhei.ttf"); // 黑体
                if (System.IO.File.Exists(fontPath))
                {
                    try
                    {
                        customFont = SKTypeface.FromFile(fontPath);
                    }
                    catch { }
                }
            }

            // 优化：初始化图表系列，使用 ObservableCollection
            var uploadValues = new ObservableCollection<double>();
            var downloadValues = new ObservableCollection<double>();
            
            _trafficSeries = new ISeries[]
            {
                new LineSeries<double> 
                { 
                    Values = uploadValues,
                    Name = "上传 (KB/s)", 
                    Fill = null,
                    GeometrySize = 0
                },
                new LineSeries<double> 
                { 
                    Values = downloadValues,
                    Name = "下载 (KB/s)", 
                    Fill = null,
                    GeometrySize = 0
                }
            };

            _xAxes = new Axis[] { new Axis { IsVisible = false } };
            
            // 配置 Y 轴标签字体
            var yAxis = new Axis { MinLimit = 0 };
            if (customFont != null)
            {
                // 使用自定义字体渲染 Y 轴标签
                var labelPaint = new SolidColorPaint { Color = new SKColor(0, 0, 0, 255) };
                yAxis.LabelsPaint = labelPaint;
            }
            _yAxes = new Axis[] { yAxis };
            
            DataContext = this;
        }

        private void StartNetworkMonitor()
        {
            _lastNetSampleTime = DateTime.Now;
            _lastUploadBytes = GetNetworkBytes(upload: true);
            _lastDownloadBytes = GetNetworkBytes(upload: false);

            _netTimer?.Stop();
            _netTimer?.Dispose();

            _netTimer = new Timer(1000);
            _netTimer.Elapsed += NetTimer_Elapsed;
            _netTimer.Start();
        }

        private void StopNetworkMonitor()
        {
            if (_netTimer == null) return;
            _netTimer.Stop();
            _netTimer.Elapsed -= NetTimer_Elapsed;
            _netTimer.Dispose();
            _netTimer = null;
        }

        private void NetTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                var upload = GetNetworkBytes(upload: true);
                var download = GetNetworkBytes(upload: false);
                var interval = Math.Max((now - _lastNetSampleTime).TotalSeconds, 0.001);
                var upSpeed = (upload - _lastUploadBytes) / interval;
                var downSpeed = (download - _lastDownloadBytes) / interval;
                _lastUploadBytes = upload;
                _lastDownloadBytes = download;
                _lastNetSampleTime = now;

                // 确保速度不为负数
                if (upSpeed < 0) upSpeed = 0;
                if (downSpeed < 0) downSpeed = 0;

                // 每 3 秒更新一次图表数据，使显示更清晰
                _timerCounter++;
                if (_timerCounter < 3)
                {
                    return;
                }
                _timerCounter = 0;

                // 缓存进程对象，避免每次创建
                var currentProcess = Process.GetCurrentProcess();
                var memoryMB = currentProcess.WorkingSet64 / 1024.0 / 1024.0;
                var connections = GetActiveTcpConnections();

                // 预计算所有文本
                var upSpeedText = $"{upSpeed / 1024:F2} KB/s";
                var downSpeedText = $"{downSpeed / 1024:F2} KB/s";
                var uploadTotalText = $"{upload / 1024.0 / 1024.0:F2} MB";
                var downloadTotalText = $"{download / 1024.0 / 1024.0:F2} MB";
                var connectionsText = connections.ToString();
                var memoryText = $"{memoryMB:F2} MB";

                App.MainWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    // 更新图表数据
                    if (_trafficSeries != null && _trafficSeries.Length >= 2)
                    {
                        var uploadSeries = (LineSeries<double>)_trafficSeries[0];
                        var downloadSeries = (LineSeries<double>)_trafficSeries[1];
                        
                        if (uploadSeries.Values != null && downloadSeries.Values != null)
                        {
                            // 使用循环缓冲区
                            var upValue = Math.Round(upSpeed / 1024, 2);
                            var downValue = Math.Round(downSpeed / 1024, 2);
                            
                            _uploadBuffer[_bufferIndex] = upValue;
                            _downloadBuffer[_bufferIndex] = downValue;
                            _bufferIndex = (_bufferIndex + 1) % MaxPoints;
                            if (_pointCount < MaxPoints) _pointCount++;
                            
                            // 直接设置 Values 属性，避免频繁 Clear/Add
                            uploadSeries.Values = GetUploadBufferArray();
                            downloadSeries.Values = GetDownloadBufferArray();
                        }
                    }

                    UploadSpeedText.Text = upSpeedText;
                    DownloadSpeedText.Text = downSpeedText;
                    UploadTotalText.Text = uploadTotalText;
                    DownloadTotalText.Text = downloadTotalText;
                    ActiveConnectionsText.Text = connectionsText;
                    MemoryUsageText.Text = memoryText;
                });
            }
            catch (Exception ex)
            {
                // 静默失败，不影响其他功能
                System.Diagnostics.Debug.WriteLine($"Network monitor error: {ex.Message}");
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

        private static async Task PingAndShowAsync(string host, TextBlock textBlock)
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

        private async Task LoadIpInfoAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                var json = await client.GetStringAsync("https://ipapi.co/json/");
                var root = JsonDocument.Parse(json).RootElement;

                CountryText.Text = $"国家: {root.GetProperty("country_name").GetString()}";
                IPText.Text = $"IP 地址: {root.GetProperty("ip").GetString()}";
                ISPText.Text = $"服务商: {root.GetProperty("org").GetString()}";
                OrgText.Text = $"组织: {root.GetProperty("org").GetString()}";
                LocationText.Text = $"位置: {root.GetProperty("city").GetString()}, {root.GetProperty("region").GetString()}";
                ASNText.Text = $"自治域: {root.GetProperty("asn").GetString()}";
                TimezoneText.Text = $"时区: {root.GetProperty("timezone").GetString()}";
            }
            catch
            {
                CountryText.Text = "国家: -";
                IPText.Text = "IP 地址: -";
                ISPText.Text = "服务商: -";
                OrgText.Text = "组织: -";
                LocationText.Text = "位置: -";
                ASNText.Text = "自治域: -";
                TimezoneText.Text = "时区: -";
            }
        }

        private void LoadSystemInfo()
        {
            OSInfoText.Text = $"操作系统: {Environment.OSVersion}";
            StartupText.Text = $"开机自启动: {(IsStartupEnabled() ? "是" : "否")}";
            RunModeText.Text = $"运行模式: {(IsAdministrator() ? "管理员" : "普通")}";
            AppVersionText.Text = $"程序版本: {GetAppVersion()}";
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
