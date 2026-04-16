using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AutoPortal.Helpers
{
    public static class NativeDllExtractor
    {
        private static string? _extractPath;
        private static bool _initialized = false;
        private static readonly object _lock = new();

        public static bool IsDllAvailable(string dllName)
        {
            try
            {
                Initialize();
                return !string.IsNullOrEmpty(_extractPath) && File.Exists(Path.Combine(_extractPath, dllName));
            }
            catch
            {
                return false;
            }
        }

        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var exePath = Path.GetDirectoryName(assembly.Location);

                    if (string.IsNullOrEmpty(exePath))
                    {
                        _initialized = true;
                        return;
                    }

                    _extractPath = exePath;

                    SetDllDirectory(_extractPath);

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set DLL directory: {ex.Message}");
                    _initialized = true;
                }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string? lpPathName);
    }
}
