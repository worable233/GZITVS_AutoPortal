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
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
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
            
            // 加载保存的卡片顺序
            LoadCardOrder();
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

        // ========== 卡片管理功能 ==========
        
        private Border? _selectedCard;

        private void Card_ContextRequested(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            if (sender is Border card)
            {
                _selectedCard = card;
                
                // 获取鼠标位置
                var position = e.GetPosition(card);
                
                // 显示选中效果
                ShowSelectedEffect(card);
                
                // 在鼠标位置显示菜单
                ShowCardMenu(card, position);
            }
        }

        private void ShowSelectedEffect(Border card)
        {
            // 设置选中效果 - 使用主题强调色
            card.BorderBrush = App.Current.Resources["AccentTextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush;
            card.BorderThickness = new Microsoft.UI.Xaml.Thickness(3);
            
            // 添加轻微透明度变化
            card.Opacity = 0.95;
        }

        private void ShowCardMenu(FrameworkElement placementTarget, Point position)
        {
            var menuFlyout = new MenuFlyout();
            
            // 添加菜单项
            var addMenuItem = new MenuFlyoutItem 
            { 
                Text = "在此卡片后添加新卡片",
                Icon = new SymbolIcon(Symbol.Add)
            };
            addMenuItem.Click += AddCard_Click;
            menuFlyout.Items.Add(addMenuItem);
            
            // 添加分隔线
            menuFlyout.Items.Add(new MenuFlyoutSeparator());
            
            // 上移/下移菜单项
            var moveUpItem = new MenuFlyoutItem 
            { 
                Text = "上移",
                Icon = new FontIcon { Glyph = "\uE70E" }
            };
            moveUpItem.Click += MoveCardUp_Click;
            menuFlyout.Items.Add(moveUpItem);
            
            var moveDownItem = new MenuFlyoutItem 
            { 
                Text = "下移",
                Icon = new FontIcon { Glyph = "\uE70D" }
            };
            moveDownItem.Click += MoveCardDown_Click;
            menuFlyout.Items.Add(moveDownItem);
            
            // 添加分隔线
            menuFlyout.Items.Add(new MenuFlyoutSeparator());
            
            // 删除菜单项（除了状态卡片和快捷操作卡片、使用说明卡片）
            if (_selectedCard != StatusCard && _selectedCard != HelpCard)
            {
                var deleteMenuItem = new MenuFlyoutItem 
                { 
                    Text = "删除此卡片",
                    Icon = new SymbolIcon(Symbol.Delete)
                };
                deleteMenuItem.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.Red);
                deleteMenuItem.Click += DeleteCard_Click;
                menuFlyout.Items.Add(deleteMenuItem);
            }
            
            // 重置菜单项
            var resetMenuItem = new MenuFlyoutItem 
            { 
                Text = "重置所有卡片位置",
                Icon = new SymbolIcon(Symbol.Refresh)
            };
            resetMenuItem.Click += ResetCards_Click;
            menuFlyout.Items.Add(resetMenuItem);
            
            // 在鼠标位置显示菜单
            menuFlyout.ShowAt(placementTarget, position);
            
            // 菜单关闭后恢复边框
            menuFlyout.Closed += (s, args) =>
            {
                if (_selectedCard != null)
                {
                    _selectedCard.BorderBrush = App.Current.Resources["CardStrokeColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush;
                    _selectedCard.BorderThickness = new Microsoft.UI.Xaml.Thickness(1);
                    _selectedCard.Opacity = 1.0;
                }
            };
        }

        private async void AddCard_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCard == null) return;
            
            var dialog = new ContentDialog
            {
                Title = "添加新卡片",
                Content = "请选择要添加的卡片类型：",
                PrimaryButtonText = "添加",
                CloseButtonText = "取消",
                XamlRoot = XamlRoot
            };
            
            var stackPanel = new StackPanel { Spacing = 12 };
            
            var trafficOption = new RadioButton { Content = "流量统计卡片", IsChecked = true, Tag = "TrafficCard" };
            stackPanel.Children.Add(trafficOption);
            
            var speedOption = new RadioButton { Content = "连通测试卡片", Tag = "SpeedCard" };
            stackPanel.Children.Add(speedOption);
            
            var ipOption = new RadioButton { Content = "IP 信息卡片", Tag = "IpCard" };
            stackPanel.Children.Add(ipOption);
            
            var systemOption = new RadioButton { Content = "系统信息卡片", Tag = "SystemCard" };
            stackPanel.Children.Add(systemOption);
            
            dialog.Content = stackPanel;
            
            var result = await dialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                var selected = stackPanel.Children.Cast<RadioButton>().FirstOrDefault(r => r.IsChecked == true);
                if (selected != null && selected.Tag != null)
                {
                    DuplicateCard(selected.Tag.ToString()!);
                }
            }
        }

        private void DuplicateCard(string cardType)
        {
            if (_selectedCard == null) return;
            
            var parent = _selectedCard.Parent as StackPanel;
            if (parent == null) return;
            
            var currentIndex = parent.Children.IndexOf(_selectedCard);
            if (currentIndex < 0) return;
            
            // 根据类型创建新卡片
            var newCard = CreateCardByType(cardType);
            if (newCard != null)
            {
                parent.Children.Insert(currentIndex + 1, newCard);
            }
        }

        private Border CreateCardByType(string cardType)
        {
            var newCard = new Border
            {
                Padding = new Microsoft.UI.Xaml.Thickness(18),
                CornerRadius = new Microsoft.UI.Xaml.CornerRadius(12),
                Background = App.Current.Resources["CardBackgroundFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
                BorderBrush = App.Current.Resources["CardStrokeColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
                BorderThickness = new Microsoft.UI.Xaml.Thickness(1)
            };
            
            newCard.RightTapped += Card_ContextRequested;

            var content = cardType switch
            {
                "TrafficCard" => (UIElement)CreateTrafficCardContent(),
                "SpeedCard" => (UIElement)CreateSpeedCardContent(),
                "IpCard" => (UIElement)CreateIpCardContent(),
                "SystemCard" => (UIElement)CreateSystemCardContent(),
                _ => (UIElement)CreateDefaultContent("未知卡片类型")
            };

            newCard.Child = content;
            return newCard;
        }

        private StackPanel CreateTrafficCardContent()
        {
            return new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                        Spacing = 10,
                        Children =
                        {
                            new FontIcon { Glyph = "\uE7F7", FontSize = 20, Foreground = App.Current.Resources["AccentTextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush },
                            new TextBlock { Text = "流量统计", Style = (Microsoft.UI.Xaml.Style)App.Current.Resources["BodyStrongTextBlockStyle"], FontSize = 16 }
                        }
                    },
                    new Border
                    {
                        Background = App.Current.Resources["CardBackgroundFillColorSecondaryBrush"] as Microsoft.UI.Xaml.Media.Brush,
                        CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
                        Padding = new Microsoft.UI.Xaml.Thickness(12),
                        Child = new TextBlock { Text = "新流量统计卡片", Foreground = App.Current.Resources["TextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush }
                    }
                }
            };
        }

        private StackPanel CreateSpeedCardContent()
        {
            return new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                        Spacing = 10,
                        Children =
                        {
                            new FontIcon { Glyph = "\uE909", FontSize = 20, Foreground = App.Current.Resources["AccentTextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush },
                            new TextBlock { Text = "连通测试", Style = (Microsoft.UI.Xaml.Style)App.Current.Resources["BodyStrongTextBlockStyle"], FontSize = 16 }
                        }
                    },
                    new TextBlock { Text = "新连通测试卡片", Foreground = App.Current.Resources["TextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush }
                }
            };
        }

        private StackPanel CreateIpCardContent()
        {
            return new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                        Spacing = 10,
                        Children =
                        {
                            new FontIcon { Glyph = "\uE909", FontSize = 20, Foreground = App.Current.Resources["AccentTextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush },
                            new TextBlock { Text = "IP 信息", Style = (Microsoft.UI.Xaml.Style)App.Current.Resources["BodyStrongTextBlockStyle"], FontSize = 16 }
                        }
                    },
                    new TextBlock { Text = "新 IP 信息卡片", Foreground = App.Current.Resources["TextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush }
                }
            };
        }

        private StackPanel CreateSystemCardContent()
        {
            return new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                        Spacing = 10,
                        Children =
                        {
                            new FontIcon { Glyph = "\uE7F8", FontSize = 20, Foreground = App.Current.Resources["AccentTextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush },
                            new TextBlock { Text = "系统信息", Style = (Microsoft.UI.Xaml.Style)App.Current.Resources["BodyStrongTextBlockStyle"], FontSize = 16 }
                        }
                    },
                    new TextBlock { Text = "新系统信息卡片", Foreground = App.Current.Resources["TextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush }
                }
            };
        }

        private TextBlock CreateDefaultContent(string text)
        {
            return new TextBlock 
            { 
                Text = text,
                Foreground = App.Current.Resources["TextFillColorPrimaryBrush"] as Microsoft.UI.Xaml.Media.Brush
            };
        }

        private void DeleteCard_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCard == null) return;
            if (_selectedCard == StatusCard) return;
            
            var parent = _selectedCard.Parent as StackPanel;
            if (parent != null)
            {
                parent.Children.Remove(_selectedCard);
                SaveCardOrder(); // 保存卡片顺序
            }
            _selectedCard = null;
        }

        private void MoveCardUp_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCard == null) return;
            
            var parent = _selectedCard.Parent as StackPanel;
            if (parent == null) return;
            
            var index = parent.Children.IndexOf(_selectedCard);
            if (index > 0)
            {
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index - 1, _selectedCard);
                SaveCardOrder(); // 保存卡片顺序
            }
        }

        private void MoveCardDown_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCard == null) return;
            
            var parent = _selectedCard.Parent as StackPanel;
            if (parent == null) return;
            
            var index = parent.Children.IndexOf(_selectedCard);
            if (index >= 0 && index < parent.Children.Count - 1)
            {
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index + 1, _selectedCard);
                SaveCardOrder(); // 保存卡片顺序
            }
        }

        private void ResetCards_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "确认重置",
                Content = "确定要重置所有卡片位置吗？",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = XamlRoot
            };
            
            _ = dialog.ShowAsync();
        }

        // ========== 卡片顺序保存和加载 ==========
        
        private void SaveCardOrder()
        {
            try
            {
                var parent = StatusCard?.Parent as StackPanel;
                if (parent == null) return;
                
                var cardOrder = new List<string>();
                foreach (var child in parent.Children)
                {
                    if (child is Border border && border.Name != null)
                    {
                        cardOrder.Add(border.Name);
                    }
                }
                
                // 保存到配置文件
                var config = new { CardOrder = cardOrder };
                var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                
                var configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AutoPortal",
                    "cardOrder.json");
                
                System.IO.File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存卡片顺序失败：{ex.Message}");
            }
        }

        private void LoadCardOrder()
        {
            try
            {
                var configPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AutoPortal",
                    "cardOrder.json");
                
                if (!System.IO.File.Exists(configPath)) return;
                
                var json = System.IO.File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<CardOrderConfig>(json);
                
                if (config?.CardOrder == null) return;
                
                var parent = StatusCard?.Parent as StackPanel;
                if (parent == null) return;
                
                // 根据保存的顺序重新排列卡片
                foreach (var cardName in config.CardOrder)
                {
                    var card = FindCardByName(parent, cardName);
                    if (card != null)
                    {
                        parent.Children.Remove(card);
                        parent.Children.Add(card);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载卡片顺序失败：{ex.Message}");
            }
        }

        private Border? FindCardByName(StackPanel parent, string name)
        {
            foreach (var child in parent.Children)
            {
                if (child is Border border && border.Name == name)
                {
                    return border;
                }
            }
            return null;
        }

        private class CardOrderConfig
        {
            public List<string>? CardOrder { get; set; }
        }
    }
}
