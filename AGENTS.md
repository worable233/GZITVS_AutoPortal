# AutoPortal 项目结构文档

## 项目概述
- **项目名称**: AutoPortal
- **项目类型**: WinUI 3 桌面应用程序
- **目标框架**: .NET 8.0 (Windows 10.0.19041.0)
- **主要功能**: 自动登录门户系统，支持记住密码和自动登录

---

## 目录结构

```
AutoPortal/
├── .vs/                          # Visual Studio 配置文件
│   ├── AutoPortal/               # VS Copilot 索引
│   ├── AutoPortal.slnx/          # 解决方案配置
│   └── ProjectEvaluation/        # 项目评估缓存
│
├── Assets/                       # 应用资源文件
│   ├── LockScreenLogo.scale-200.png
│   ├── SplashScreen.scale-200.png
│   ├── Square150x150Logo.scale-200.png
│   ├── Square44x44Logo.scale-200.png
│   ├── Square44x44Logo.targetsize-24_altform-unplated.png
│   ├── StoreLogo.png
│   └── Wide310x150Logo.scale-200.png
│
├── Login/                        # C++ DLL 项目（登录验证核心）
│   ├── .vs/                      # C++ 项目 VS 配置
│   ├── Login.cpp                 # C++ 实现文件
│   ├── Login.h                   # C++ 头文件（导出函数声明）
│   ├── Login.slnx                # C++ 解决方案文件
│   ├── Login.vcxproj             # C++ 项目文件
│   ├── Login.vcxproj.filters     # 项目筛选器
│   └── Login.vcxproj.user        # 用户配置
│
├── Model/                        # 数据模型层
│   └── Models.cs                 # 包含 LoginConfig 数据模型
│
├── Page/                         # 页面层
│   ├── StartPage.xaml            # 启动页面 XAML
│   └── StartPage.xaml.cs         # 启动页面代码后台
│
├── Properties/                   # 项目属性
│   ├── PublishProfiles/          # 发布配置
│   │   ├── win-arm64.pubxml
│   │   ├── win-x64.pubxml
│   │   └── win-x86.pubxml
│   └── launchSettings.json       # 启动设置
│
├── bin/                          # 编译输出目录
│   └── x64/Debug/                # Debug 版本输出
│
├── obj/                          # 中间编译文件
│
├── App.xaml                      # 应用程序资源定义
├── App.xaml.cs                   # 应用程序入口点
├── MainWindow.xaml               # 主窗口 XAML
├── MainWindow.xaml.cs            # 主窗口代码后台
├── AutoPortal.csproj             # C# 项目文件
├── AutoPortal.csproj.user        # 项目用户配置
├── AutoPortal.slnx               # 主解决方案文件
├── Package.appxmanifest          # MSIX 包清单
└── app.manifest                  # 应用程序清单
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
  - 使用 Frame 导航到 StartPage
  
- **MainWindow.xaml.cs**: 
  - 包含登录逻辑
  - P/Invoke 调用 C++ DLL
  - 配置保存/加载功能
  - 自动登录功能

### 3. 数据模型
- **Models.cs**: 
  - `LoginConfig` 类：存储登录配置
    - StudentId: 学号
    - Password: 密码
    - RememberPassword: 记住密码标志
    - AutoLogin: 自动登录标志

### 4. 页面
- **StartPage.xaml**: 登录界面 UI
  - 学号输入框
  - 密码输入框
  - 登录按钮
  - 错误提示
  
- **StartPage.xaml.cs**: 登录页面逻辑（目前部分实现）

### 5. C++ DLL (LoginValidator.dll)
- **Login.h**: 导出函数声明
  - `ValidateLogin`: 验证登录凭据
  - `LoadConfig`: 加载配置
  - `SaveConfig`: 保存配置
  - `DeleteConfig`: 删除配置
  - `FreeString`: 释放字符串内存

- **Login.cpp**: C++ 实现（需要完整实现）

---

## 技术栈

### 前端框架
- **WinUI 3** (Windows App SDK 1.8.260317003)
- **Mica 背景效果**

### 后端技术
- **.NET 8.0**
- **C# 10** (主应用)
- **C++** (LoginValidator.dll)

### 依赖包
- Microsoft.Windows.SDK.BuildTools (10.0.28000.1721)
- Microsoft.WindowsAppSDK (1.8.260317003)
- System.Text.Json (JSON 序列化)

### 构建工具
- MSBuild
- MSIX 打包
- 支持 x86, x64, ARM64 平台

---

## 关键功能模块

### 1. 登录验证
- 通过 P/Invoke 调用 C++ DLL 进行验证
- 异步处理避免阻塞 UI

### 2. 配置管理
- JSON 格式存储配置
- 支持记住密码
- 支持自动登录
- 配置文件加密存储（在 C++ DLL 中实现）

### 3. UI 特性
- 自定义标题栏
- Mica 背景效果
- 加载状态指示器
- 错误提示显示
- 首次使用提示

---

## 项目配置

### 目标平台
- 最低版本: Windows 10.0.17763.0
- 目标版本: Windows 10.0.19041.0

### 构建配置
- Debug: 不启用 ReadyToRun 和 Trimming
- Release: 启用 ReadyToRun 和 Trimming

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
- MVVM 模式（部分实现）
- 代码后台处理逻辑
- P/Invoke 调用原生代码

### 待完成功能
- [ ] 完善 C++ DLL 实现
- [ ] 实现登录后导航到主页
- [ ] 添加更多页面
- [ ] 完善错误处理
- [ ] 添加单元测试

---

## 构建和运行

### 构建项目
```bash
dotnet build AutoPortal.csproj
```

### 运行项目
```bash
dotnet run
```

### 发布项目
```bash
dotnet publish -c Release -r win-x64
```

---

## 文件依赖关系

```
App.xaml.cs
    └── MainWindow.xaml.cs
        ├── Models.cs (LoginConfig)
        ├── LoginValidator.dll (C++ DLL)
        └── StartPage.xaml.cs
```

---

## 重要路径

- **配置文件存储**: 由 C++ DLL 管理（具体路径在 Login.cpp 中定义）
- **编译输出**: `bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/`
- **中间文件**: `obj/x64/Debug/net8.0-windows10.0.19041.0/win-x64/`

---

## 版本信息

- **项目版本**: 1.0.2.0
- **发布者**: worable
- **包名**: 87d9a1db-52e8-4421-90dd-88716f64d8a9
