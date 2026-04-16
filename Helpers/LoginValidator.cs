using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using AutoPortal.Models;

namespace AutoPortal.Helpers
{
    public class LoginValidator : IDisposable
    {
        private const string DllName = "Login.dll";
        private static bool _dllAvailable = true;

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int ValidateLogin(
            [MarshalAs(UnmanagedType.LPWStr)] string studentId,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            ref IntPtr errorMsg
        );

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr LoadConfig(ref IntPtr errorMsg);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int SaveConfig(
            [MarshalAs(UnmanagedType.LPWStr)] string jsonString,
            ref IntPtr errorMsg
        );

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int DeleteConfig();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern void FreeString(IntPtr ptr);

        static LoginValidator()
        {
            try
            {
                _dllAvailable = NativeDllExtractor.IsDllAvailable(DllName);
            }
            catch
            {
                _dllAvailable = false;
            }
        }

        public bool Validate(string studentId, string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_dllAvailable)
            {
                errorMessage = "Login.dll 不存在，请确保 Login.dll 与 AutoPortal.exe 在同一目录";
                return false;
            }

            IntPtr errorMsgPtr = IntPtr.Zero;

            try
            {
                int result = ValidateLogin(studentId, password, ref errorMsgPtr);
                
                if (errorMsgPtr != IntPtr.Zero)
                {
                    errorMessage = Marshal.PtrToStringUni(errorMsgPtr) ?? string.Empty;
                    FreeString(errorMsgPtr);
                }

                return result == 1;
            }
            catch (Exception ex)
            {
                errorMessage = $"调用 DLL 失败: {ex.Message}";
                return false;
            }
        }

        public LoginConfig? Load(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_dllAvailable)
            {
                return new LoginConfig();
            }

            IntPtr errorMsgPtr = IntPtr.Zero;

            try
            {
                IntPtr jsonPtr = LoadConfig(ref errorMsgPtr);

                if (errorMsgPtr != IntPtr.Zero)
                {
                    errorMessage = Marshal.PtrToStringUni(errorMsgPtr) ?? string.Empty;
                    FreeString(errorMsgPtr);
                }

                if (jsonPtr != IntPtr.Zero)
                {
                    string? json = Marshal.PtrToStringUni(jsonPtr);
                    FreeString(jsonPtr);

                    if (string.IsNullOrEmpty(json) || json == "{}")
                    {
                        return new LoginConfig();
                    }

                    return JsonSerializer.Deserialize(json, AppJsonContext.Default.LoginConfig);
                }

                return new LoginConfig();
            }
            catch (Exception ex)
            {
                errorMessage = $"加载配置失败: {ex.Message}";
                return new LoginConfig();
            }
        }

        public bool Save(LoginConfig config, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_dllAvailable)
            {
                errorMessage = "Login.dll 不存在，无法保存配置";
                return false;
            }

            IntPtr errorMsgPtr = IntPtr.Zero;

            try
            {
                string json = JsonSerializer.Serialize(config, AppJsonContext.Default.LoginConfig);

                int result = SaveConfig(json, ref errorMsgPtr);

                if (errorMsgPtr != IntPtr.Zero)
                {
                    errorMessage = Marshal.PtrToStringUni(errorMsgPtr) ?? string.Empty;
                    FreeString(errorMsgPtr);
                }

                return result == 1;
            }
            catch (Exception ex)
            {
                errorMessage = $"保存配置失败: {ex.Message}";
                return false;
            }
        }

        public bool Delete()
        {
            if (!_dllAvailable)
            {
                return false;
            }

            try
            {
                return DeleteConfig() == 1;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
        }
    }
}
