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

        public static void Initialize()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var assemblyName = assembly.GetName().Name ?? "AutoPortal";

                    _extractPath = Path.Combine(
                        Path.GetTempPath(),
                        "AutoPortal",
                        "NativeDlls",
                        $"{assemblyName}_{assembly.GetName().Version}"
                    );
                    Directory.CreateDirectory(_extractPath);

                    ExtractDll(assembly, $"{assemblyName}.NativeDlls.Login.dll", "Login.dll");
                    ExtractDll(assembly, $"{assemblyName}.NativeDlls.libcurl.dll", "libcurl.dll");
                    ExtractDll(assembly, $"{assemblyName}.NativeDlls.zlib1.dll", "zlib1.dll");

                    SetDllDirectory(_extractPath);

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to extract native DLLs: {ex.Message}");
                }
            }
        }

        private static void ExtractDll(Assembly assembly, string resourceName, string fileName)
        {
            var targetPath = Path.Combine(_extractPath!, fileName);

            if (File.Exists(targetPath))
            {
                var existingBytes = File.ReadAllBytes(targetPath);
                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream != null)
                {
                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    var newBytes = memoryStream.ToArray();

                    if (existingBytes.Length == newBytes.Length)
                    {
                        bool same = true;
                        for (int i = 0; i < existingBytes.Length; i++)
                        {
                            if (existingBytes[i] != newBytes[i])
                            {
                                same = false;
                                break;
                            }
                        }
                        if (same) return;
                    }
                }
            }

            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream != null)
            {
                using var fileStream = File.Create(targetPath);
                resourceStream.CopyTo(fileStream);
            }
            else
            {
                var exePath = Path.GetDirectoryName(assembly.Location);
                var sourcePath = Path.Combine(exePath!, fileName);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, targetPath, true);
                }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string? lpPathName);
    }
}
