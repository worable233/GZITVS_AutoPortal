# AutoPortal 项目结构文档

## 项目概述
- **项目名称**: AutoPortal
- **项目类型**: WinUI 3 桌面应用程序
- **目标框架**: .NET 8.0 (Windows 10.0.19041.0)
- **主要功能**: 校园网 Portal 自动登录工具
- **适用学校**: 广州市信息技术职业学校

---

## 目录结构

```
AutoPortal/
├── Assets/                       # 应用资源文件
│   ├── ICON.png                  # 原始图标
│   ├── app.ico                   # 应用图标（ICO 格式）
│   ├── Logo.png                  # 应用标志（60x60）
│   ├── Square44x44Logo.scale-200.png
│   ├── Square150x150Logo.scale-200.png
│   ├── Wide310x150Logo.scale-200.png
│   ├── SplashScreen.scale-200.png
│   ├── StoreLogo.png
│   ├── LockScreenLogo.scale-200.png
│   └── Square44x44Logo.targetsize-24_altform-unplated.png
│
├── Pages/                        # 页面层
│   ├── HomePage.xaml            # 主页
│   ├── HomePage.xaml.cs
│   ├── LoginPage.xaml           # 登录页
│   ├── LoginPage.xaml.cs
│   ├── SettingsPage.xaml        # 设置页
│   ├── SettingsPage.xaml.cs
│   ├── WelcomePage.xaml         # 欢迎页
│   ├── WelcomePage.xaml.cs
│   ├── NavigationPage.xaml      # 导航页
│   └── NavigationPage.xaml.cs
│
├── Services/                     # 服务层
│   ├── NavigationService.cs     # 导航服务
│   ├── LoggerService.cs         # 日志服务
│   └── AppSettingsService.cs    # 应用设置服务
│
├── Helpers/                      # 辅助类
│   ├── LoginValidator.cs        # 登录验证器
│   ├── NativeDllExtractor.cs    # 原生 DLL 提取器
│   └── AppJsonContext.cs        # JSON 序列化上下文
│
├── Login/                        # C++ DLL 项目（登录验证核心）
│   ├── Login.cpp                 # C++ 实现文件
│   ├── Login.h                   # C++ 头文件（导出函数声明）
│   ├── Login.slnx                # C++ 解决方案文件
│   ├── Login.vcxproj             # C++ 项目文件
│   ├── pch.h                     # 预编译头文件
│   ├── pch.cpp                   # 预编译源文件
│   ├── portal_login.cpp          # Portal 登录实现
│   └── portal_login.h            # Portal 登录头文件
│
├── Properties/                   # 项目属性
│   ├── PublishProfiles/          # 发布配置
│   │   ├── win-arm64.pubxml
│   │   ├── win-x64.pubxml
│   │   └── win-x86.pubxml
│   └── launchSettings.json       # 启动设置
│
├── Resources/                    # 资源文件
│   └── Styles.xaml              # XAML 样式定义
│
├── tools/                        # 构建工具
│   └── build-login-dll.ps1      # 构建 C++ DLL 脚本
│
├── App.xaml                      # 应用程序资源定义
├── App.xaml.cs                   # 应用程序入口点
├── MainWindow.xaml               # 主窗口 XAML
├── MainWindow.xaml.cs            # 主窗口代码后台
├── AutoPortal.csproj             # C# 项目文件
├── AutoPortal.slnx               # 主解决方案文件
├── AutoPortal.iss                # Inno Setup 安装脚本
├── Package.appxmanifest          # MSIX 包清单
├── app.manifest                  # 应用程序清单
├── global.json                   # .NET SDK 版本配置
├── nuget.config                  # NuGet 包源配置
├── Trim.xml                      # 裁剪配置文件
├── .gitignore                    # Git 忽略文件
├── LICENSE                       # 许可证文件
├── README.md                     # 项目说明文档
├── AGENTS.md                     # 项目结构文档（本文件）
└── ChineseSimplifiedCustom.isl   # Inno Setup 中文语言文件
```

---

## 核心文件说明

### 1. 应用程序入口
- **App.xaml**: 定义应用程序资源和主题
- **App.xaml.cs**: 应用程序启动逻辑，创建主窗口

### 2. 主窗口
- **MainWindow.xaml**: 
  - 定义窗口布局
  - 包含自定义标题栏
  - 使用 Frame 导航到各页面
  
- **MainWindow.xaml.cs**: 
  - 窗口初始化
  - P/Invoke 调用 C++ DLL
  - 配置保存/加载功能
  - 自动登录功能

### 3. 数据模型
- **LoginConfig.cs**: 
  - `LoginConfig` 类：存储登录配置
    - Username: 学号
    - Password: 密码
    - RememberPassword: 记住密码标志
    - AutoLogin: 自动登录标志

### 4. 页面
- **WelcomePage.xaml**: 欢迎页面 UI
  - 应用介绍
  - 开始配置按钮
  
- **LoginPage.xaml**: 登录页面 UI
  - 学号输入框
  - 密码输入框
  - 登录按钮
  - 错误提示
  
- **HomePage.xaml**: 主页 UI
  - 登录状态显示
  - 网络状态指示
  - 快速操作按钮
  
- **SettingsPage.xaml**: 设置页面 UI
  - 账号配置
  - 主题切换
  - 开机自启设置
  
- **NavigationPage.xaml**: 导航页面 UI
  - 侧边导航菜单
  - 页面切换

### 5. 服务层
- **NavigationService.cs**: 
  - 管理页面导航
  - 维护导航历史
  - 提供页面类型枚举
  
- **LoggerService.cs**: 
  - 应用日志记录
  - 错误追踪
  
- **AppSettingsService.cs**: 
  - 应用设置管理
  - 配置持久化

### 6. C++ DLL (LoginValidator.dll)
- **Login.h**: 导出函数声明
  - `ValidateLogin`: 验证登录凭据
  - `LoadConfig`: 加载配置
  - `SaveConfig`: 保存配置
  - `DeleteConfig`: 删除配置
  - `FreeString`: 释放字符串内存

- **Login.cpp**: C++ 实现
  - Portal 登录逻辑
  - 配置管理
  - 网络请求（使用 libcurl）

---

## 技术栈

### 前端框架
- **WinUI 3** (Windows App SDK 1.8.2)
- **Mica 背景效果**
- **Fluent Design**

### 后端技术
- **.NET 8.0**
- **C# 10** (主应用)
- **C++ 17** (LoginValidator.dll)

### 依赖包
- Microsoft.Windows.SDK.BuildTools
- Microsoft.WindowsAppSDK
- LiveChartsCore.SkiaSharpView.WinUI
- System.Text.Json

### 构建工具
- MSBuild
- Inno Setup 7（安装包制作）
- PowerShell（自动化脚本）

---

## 项目配置

### 目标平台
- **最低版本**: Windows 10.0.17763.0
- **目标版本**: Windows 10.0.19041.0
- **架构**: x64

### 构建配置
- **Debug**: 不启用 ReadyToRun 和 Trimming
- **Release**: 启用 ReadyToRun 和 Trimming

### 应用能力
- runFullTrust: 完全信任权限
- systemAIModels: 系统 AI 模型访问

---

## 开发注意事项

### 代码规范
- 使用 Nullable 引用类型
- 异步方法命名以 Async 结尾
- UI 操作需要在 UI 线程执行

### 架构模式
- 服务层模式
- 代码后台处理逻辑
- P/Invoke 调用原生代码

### 文件依赖关系

```
App.xaml.cs
    └── MainWindow.xaml.cs
        ├── Pages/
        │   ├── WelcomePage.xaml.cs
        │   ├── LoginPage.xaml.cs
        │   ├── HomePage.xaml.cs
        │   └── SettingsPage.xaml.cs
        ├── Services/
        │   ├── NavigationService.cs
        │   ├── LoggerService.cs
        │   └── AppSettingsService.cs
        ├── Helpers/
        │   ├── LoginValidator.cs
        │   └── NativeDllExtractor.cs
        └── LoginValidator.dll (C++ DLL)
```

---

## 重要路径

- **配置文件存储**: `%LOCALAPPDATA%\AutoPortal\config.json`
- **日志文件**: `%LOCALAPPDATA%\AutoPortal\startup.log`
- **编译输出**: `bin/Release/net8.0-windows10.0.19041.0/win-x64/`
- **中间文件**: `obj/x64/Debug/net8.0-windows10.0.19041.0/`
- **安装包输出**: `.AutoPortal_Setup_X.X.X.exe/`

---

## 构建和运行

### 构建项目
```bash
dotnet build
```

### 运行项目
```bash
dotnet run
```

### 发布项目
```bash
dotnet publish -c Release -r win-x64
```

### 创建安装包
```bash
"C:\Program Files\Inno Setup 7\ISCC.exe" AutoPortal.iss
```

---

## 版本信息

- **当前版本**: 1.1.0
- **发布者**: worable
- **许可证**: CC BY-NC-SA 4.0
- **包名**: 87d9a1db-52e8-4421-90dd-88716f64d8a9

---

## 更新历史

### v1.1.0 (2026-04-24)
- ✨ 优化安装程序，添加中文语言支持
- 🎨 更新应用图标
- 🐛 修复窗口显示问题
- 📝 完善文档
- 🗑️ 清理无用文件

### v1.0.0
- 🎉 首次发布
- ✅ 实现基本登录功能
- ✅ 支持记住密码
- ✅ 支持自动登录
