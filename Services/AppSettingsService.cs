using AutoPortal.Helpers;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoPortal.Services
{
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
                "AutoPortal");

            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            _settingsFilePath = Path.Combine(appDataDir, "settings.json");
            _settings = LoadSettings();
        }

        public void SaveSettings()
        {
            try
            {
                var json = JsonSerializer.Serialize(_settings, AppJsonContext.Default.AppSettings);
                File.WriteAllText(_settingsFilePath, json);
                
                // 写入调试日志
                var logPath = Path.Combine(Path.GetDirectoryName(_settingsFilePath)!, "settings_debug.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 保存设置 - Theme={_settings.Theme}\n";
                File.AppendAllText(logPath, logEntry);
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
                    var settings = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppSettings);
                    if (settings != null)
                    {
                        // 写入调试日志
                        var logPath = Path.Combine(Path.GetDirectoryName(_settingsFilePath)!, "settings_debug.log");
                        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 加载设置 - Theme={settings.Theme}\n";
                        File.AppendAllText(logPath, logEntry);
                        
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.Error("加载设置失败", ex, "AppSettings");
            }

            return new AppSettings();
        }

        public void Reset()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    File.Delete(_settingsFilePath);
                }
                
                // 重新创建目录（如果不存在）
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                _settings = new AppSettings();
            }
            catch (Exception ex)
            {
                LoggerService.Instance.Error("重置设置失败", ex, "AppSettings");
            }
        }
    }

    public class AppSettings
    {
        [JsonPropertyName("theme")]
        public int Theme { get; set; }

        [JsonPropertyName("enableAutoLogin")]
        public bool EnableAutoLogin { get; set; } = true;

        [JsonPropertyName("autoLoginOnNetworkConnect")]
        public bool AutoLoginOnNetworkConnect { get; set; } = true;

        [JsonPropertyName("enableNotifications")]
        public bool EnableNotifications { get; set; } = true;

        [JsonPropertyName("checkNetworkTimeout")]
        public int CheckNetworkTimeout { get; set; } = 3;

        [JsonPropertyName("windowWidth")]
        public double WindowWidth { get; set; } = 1000;

        [JsonPropertyName("windowHeight")]
        public double WindowHeight { get; set; } = 700;

        [JsonPropertyName("chartUpdateInterval")]
        public int ChartUpdateInterval { get; set; } = 3;

        [JsonPropertyName("enableMicaEffect")]
        public bool EnableMicaEffect { get; set; } = true;

        [JsonPropertyName("micaOpacity")]
        public double MicaOpacity { get; set; } = 1.0;
    }
}
