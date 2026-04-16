using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoPortal.Services
{
    /// <summary>
    /// 应用程序设置管理服务
    /// </summary>
    public class AppSettingsService
    {
        private static AppSettingsService? _instance;
        private readonly string _settingsFilePath;
        private AppSettings _settings;

        public static AppSettingsService Instance => _instance ??= new AppSettingsService();

        public AppSettings Settings => _settings;

        public AppSettingsService()
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AutoPortal"
            );

            if (!Directory.Exists(appDataDir))
                Directory.CreateDirectory(appDataDir);

            _settingsFilePath = Path.Combine(appDataDir, "settings.json");
            _settings = LoadSettings();
        }

        public void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                LoggerService.Instance.Error("保存设置失败", ex, "AppSettings");
            }
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.Error("加载设置失败", ex, "AppSettings");
            }

            return new AppSettings();
        }
    }

    /// <summary>
    /// 应用程序设置类
    /// </summary>
    public class AppSettings
    {
        [JsonPropertyName("theme")]
        public int Theme { get; set; } = 0; // 0: System, 1: Light, 2: Dark

        [JsonPropertyName("enableAutoLogin")]
        public bool EnableAutoLogin { get; set; } = true;

        [JsonPropertyName("autoLoginOnNetworkConnect")]
        public bool AutoLoginOnNetworkConnect { get; set; } = true;

        [JsonPropertyName("enableNotifications")]
        public bool EnableNotifications { get; set; } = true;

        [JsonPropertyName("checkNetworkTimeout")]
        public int CheckNetworkTimeout { get; set; } = 3; // seconds

        [JsonPropertyName("startMinimized")]
        public bool StartMinimized { get; set; } = false;

        [JsonPropertyName("windowWidth")]
        public double WindowWidth { get; set; } = 1000;

        [JsonPropertyName("windowHeight")]
        public double WindowHeight { get; set; } = 700;
    }
}
