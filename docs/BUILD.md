# AutoPortal 构建指南

## 环境要求

- .NET 8.0 SDK
- Visual Studio 2022（可选）
- Windows 10 SDK

## 构建项目

```bash
dotnet build
```

## 运行项目

```bash
dotnet run
```

## 发布应用

```bash
dotnet publish -c Release -r win-x64
```

发布输出目录：`bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/`

## 创建安装包

需要安装 [Inno Setup 7](https://jrsoftware.org/isdl.php)

```bash
"C:\Program Files\Inno Setup 7\ISCC.exe" AutoPortal.iss
```

安装包输出位置：`.AutoPortal_Setup_X.X.X.exe/AutoPortal_vX.X.X_Setup.exe`

## 项目结构

```
AutoPortal/
├── App.xaml                  # 应用程序入口
├── MainWindow.xaml           # 主窗口
├── Pages/                    # 页面
│   ├── HomePage.xaml        # 主页
│   ├── LoginPage.xaml       # 登录页
│   ├── SettingsPage.xaml    # 设置页
│   ├── WelcomePage.xaml     # 欢迎页
│   └── NavigationPage.xaml  # 导航页
├── Services/                 # 服务层
│   ├── NavigationService.cs # 导航服务
│   ├── LoggerService.cs     # 日志服务
│   └── AppSettingsService.cs# 设置服务
├── Helpers/                  # 辅助类
│   ├── LoginValidator.cs    # 登录验证
│   └── NativeDllExtractor.cs# DLL 提取
├── Login/                    # C++ DLL 项目
│   ├── Login.cpp            # 登录实现
│   └── Login.h              # 头文件
├── Assets/                   # 资源文件
└── AutoPortal.iss           # Inno Setup 脚本
```

## 技术栈

- **框架**: WinUI 3 (Windows App SDK 1.8.2)
- **运行时**: .NET 8.0
- **语言**: C# 10 & C++ 17
- **图表**: LiveChartsCore
- **安装工具**: Inno Setup 7
