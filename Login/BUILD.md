# Login 项目构建说明

## 项目概述
这是一个 C++ 项目，用于提供 Portal 自动登录功能。它使用 libcurl 库进行 HTTP 请求，使用 nlohmann/json 库进行配置文件解析。

项目可以编译为：
- **DLL**: 供其他程序调用（Login.dll）
- **独立 EXE**: 直接运行（main.cpp）

## 依赖项

### 1. libcurl
项目需要 libcurl 库进行 HTTP 请求。

#### 安装 libcurl（Windows）

**方法一：使用 vcpkg（推荐）**
```powershell
# 安装 vcpkg（如果还没有）
git clone https://github.com/Microsoft/vcpkg.git
cd vcpkg
.\bootstrap-vcpkg.bat

# 安装 libcurl
.\vcpkg install curl:x64-windows
.\vcpkg install curl:x86-windows

# 集成到 Visual Studio
.\vcpkg integrate install
```

**方法二：手动下载**
1. 访问 https://curl.se/windows/
2. 下载适合你平台的开发包
3. 解压后将：
   - `include` 目录添加到项目的包含路径
   - `lib` 目录添加到项目的库路径
   - DLL 文件复制到输出目录

### 2. nlohmann/json
项目使用 nlohmann/json 库进行 JSON 解析。

已包含在项目的 `include/nlohmann` 目录中，无需额外安装。

### 3. Windows SDK
项目使用 Windows SDK 提供的系统功能，确保已安装：
- Windows 10 SDK 或更高版本

## 构建步骤

### 使用 Visual Studio
1. 打开 `Login.slnx` 解决方案
2. 选择构建配置（Debug/Release）和平台（x64/x86）
3. 右键项目 -> 构建

### 使用 MSBuild（命令行）
```powershell
# Debug x64
msbuild Login.vcxproj /p:Configuration=Debug /p:Platform=x64

# Release x64
msbuild Login.vcxproj /p:Configuration=Release /p:Platform=x64
```

## 输出文件

构建成功后，会生成以下文件：
- `Login.dll` - 动态链接库
- `Login.lib` - 导入库（用于链接）

## 导出函数

DLL 导出以下函数供 C# 调用：

### 1. ValidateLogin
```cpp
int ValidateLogin(const wchar_t* studentId, const wchar_t* password);
```
验证登录凭据。
- 参数：
  - `studentId`: 学号
  - `password`: 密码
- 返回值：
  - `1`: 登录成功
  - `0`: 登录失败

### 2. LoadConfig
```cpp
wchar_t* LoadConfig();
```
加载配置文件。
- 返回值：JSON 格式的配置字符串（需要调用 FreeString 释放）

### 3. SaveConfig
```cpp
int SaveConfig(const wchar_t* jsonString);
```
保存配置到文件。
- 参数：
  - `jsonString`: JSON 格式的配置字符串
- 返回值：
  - `1`: 保存成功
  - `0`: 保存失败

### 4. DeleteConfig
```cpp
int DeleteConfig();
```
删除配置文件。
- 返回值：
  - `1`: 删除成功
  - `0`: 删除失败

### 5. FreeString
```cpp
void FreeString(wchar_t* ptr);
```
释放由 LoadConfig 分配的内存。
- 参数：
  - `ptr`: 要释放的内存指针

## 配置文件

配置文件存储在当前程序所在目录下：
```
<程序所在目录>\config.json
```

例如，如果你的程序在 `C:\MyApp\AutoPortal.exe`，配置文件路径为：
```
C:\MyApp\config.json
```

配置文件格式：
```json
{
    "username": "你的学号",
    "password": "你的密码",
    "portal_url": "http://10.189.108.11/"
}
```

### 配置文件说明
- `username`: 学号或用户名
- `password`: 登录密码
- `portal_url`: Portal 网关地址（可选，默认为 http://10.189.108.11/）

### 使用说明
1. 将 `config.json.example` 复制为 `config.json`
2. 修改其中的 `username`、`password` 和 `portal_url` 为你的实际信息
3. 将配置文件放在与可执行文件相同的目录下

## C# 客户端使用

### 导入 DLL 函数
```csharp
using System;
using System.Runtime.InteropServices;

public class LoginValidator
{
    private const string DllName = "Login.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern int ValidateLogin(
        [MarshalAs(UnmanagedType.LPWStr)] string studentId,
        [MarshalAs(UnmanagedType.LPWStr)] string password,
        ref IntPtr errorMsg
    );

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern void FreeString(IntPtr ptr);
}
```

### 调用示例
```csharp
public bool Login(string studentId, string password, out string errorMessage)
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
```

### 在 XAML 中显示错误
```csharp
// 在按钮点击事件中
private async void OnLoginClick(object sender, RoutedEventArgs e)
{
    string studentId = StudentIdTextBox.Text;
    string password = PasswordBox.Password;
    
    // 显示加载状态
    SetLoadingState(true);
    
    // 调用登录
    bool success = await Task.Run(() => 
        loginValidator.ValidateLogin(studentId, password, out string errorMsg)
    );
    
    SetLoadingState(false);
    
    if (success)
    {
        // 登录成功
        ErrorMessageText.Visibility = Visibility.Collapsed;
        // 导航到主页...
    }
    else
    {
        // 显示错误信息
        ErrorMessageText.Text = errorMsg;
        ErrorMessageText.Visibility = Visibility.Visible;
    }
}
```

完整示例请参考 `CSharpExample.cs` 文件。

## 注意事项

1. **libcurl DLL**: 运行时需要确保 libcurl DLL 在系统路径中或与应用程序在同一目录
2. **字符编码**: 所有字符串使用宽字符（wchar_t）
3. **内存管理**: LoadConfig 返回的字符串必须通过 FreeString 释放
4. **线程安全**: 当前实现不是线程安全的，请勿多线程调用

## 故障排除

### 链接错误：找不到 libcurl.lib
- 确保 libcurl 已正确安装
- 检查项目属性 -> 链接器 -> 常规 -> 附加库目录
- 检查项目属性 -> 链接器 -> 输入 -> 附加依赖项

### 运行时错误：找不到 libcurl.dll
- 将 libcurl.dll 复制到应用程序目录
- 或将 libcurl bin 目录添加到系统 PATH

### 编译错误：找不到 curl.h
- 检查项目属性 -> C/C++ -> 常规 -> 附加包含目录
- 确保 curl 头文件路径正确
- 项目已包含 `include` 目录，其中包含 curl 和 nlohmann/json 头文件

### 编译错误：找不到 nlohmann/json.hpp
- 确保项目包含目录中有 `include` 文件夹
- 检查 `include/nlohmann/json.hpp` 文件是否存在

## 开发环境

- Visual Studio 2022 或更高版本
- C++20 标准
- Windows 10 SDK
