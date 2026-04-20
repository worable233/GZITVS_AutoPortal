using System;

namespace AutoPortal.Services
{
    public class AppVersionService
    {
        private static string? _version;

        public static string Version
        {
            get
            {
                if (_version == null)
                {
                    var ver = typeof(App).Assembly.GetName().Version;
                    _version = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.0.0";
                }
                return _version;
            }
        }

        public static string VersionWithoutPrefix
        {
            get
            {
                var ver = typeof(App).Assembly.GetName().Version;
                return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";
            }
        }
    }
}
