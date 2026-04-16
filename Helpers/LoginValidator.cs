using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using AutoPortal.Models;

namespace AutoPortal.Helpers
{
    public class LoginValidator : IDisposable
    {
        private const string DllName = "Login.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int ValidateLogin(
            [MarshalAs(UnmanagedType.LPWStr)] string studentId,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            ref IntPtr errorMsg
        );

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadConfig(ref IntPtr errorMsg);

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int SaveConfig(
            [MarshalAs(UnmanagedType.LPWStr)] string jsonString,
            ref IntPtr errorMsg
        );

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern int DeleteConfig();

        [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private static extern void FreeString(IntPtr ptr);

        public bool Validate(string studentId, string password, out string errorMessage)
        {
            errorMessage = string.Empty;
            IntPtr errorMsgPtr = IntPtr.Zero;

            try
            {
                int result = ValidateLogin(studentId, password, ref errorMsgPtr);
                
                if (errorMsgPtr != IntPtr.Zero)
                {
                    errorMessage = Marshal.PtrToStringUni(errorMsgPtr);
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
            IntPtr errorMsgPtr = IntPtr.Zero;

            try
            {
                IntPtr jsonPtr = LoadConfig(ref errorMsgPtr);

                if (errorMsgPtr != IntPtr.Zero)
                {
                    errorMessage = Marshal.PtrToStringUni(errorMsgPtr);
                    FreeString(errorMsgPtr);
                }

                if (jsonPtr != IntPtr.Zero)
                {
                    string json = Marshal.PtrToStringUni(jsonPtr);
                    FreeString(jsonPtr);

                    if (string.IsNullOrEmpty(json) || json == "{}")
                    {
                        return new LoginConfig();
                    }

                    return JsonSerializer.Deserialize<LoginConfig>(json);
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
            IntPtr errorMsgPtr = IntPtr.Zero;

            try
            {
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                int result = SaveConfig(json, ref errorMsgPtr);

                if (errorMsgPtr != IntPtr.Zero)
                {
                    errorMessage = Marshal.PtrToStringUni(errorMsgPtr);
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
