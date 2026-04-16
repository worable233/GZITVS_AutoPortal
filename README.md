# AutoPortal - 使用说明

![AutoPortal Logo](Assets/Logo.png)

## 项目概述
AutoPortal 是一个基于 WinUI 3 的 C++ 实现的校园网 Portal 自动登录工具。

## 功能特性

### 🎯 核心功能
- ✅ **首次使用引导** - 欢迎页面和配置步骤引导
- ✅ **自动登录** - 连接校园网时自动登录
- ✅ **状态检测** - 实时检测网络和登录状态
- ✅ **配置管理** - 可视化配置账号信息
- ✅ **设置界面** - 个性化应用设置

### 🎨 界面特性
- ✅ **Mica 背景** - 现代化的毛玻璃效果
- ✅ **侧边栏导航** - 可收缩的导航菜单
- ✅ **流畅动画** - 丝滑的页面过渡效果
- ✅ **进度条** - 炫酷的进度指示器
- ✅ **响应式设计** - 适配不同窗口大小

## 项目结构

```
AutoPortal/
├── Helpers/
│   └── LoginValidator.cs          # DLL 封装类
├── Services/
│   └── NavigationService.cs       # 导航服务
├── Pages/
│   ├── WelcomePage.xaml           # 欢迎页面
│   ├── HomePage.xaml              # 主页
│   ├── LoginPage.xaml             # 登录页面
│   ├── ConfigPage.xaml            # 配置页面
│   └── SettingsPage.xaml          # 设置页面
├── MainWindow.xaml                # 主窗口
├── App.xaml                       # 应用程序资源
└── AutoPortal.csproj              # 项目文件
```

## 使用流程

### 1. 首次使用
1. 启动应用程序
2. 进入欢迎页面，点击"开始配置"
3. 输入学号和密码
4. 点击"下一步"保存配置
5. 进入主界面

### 2. 自动登录
- 连接校园网后，应用会自动检测并登录
- 主页显示当前登录状态
- 可手动点击"立即登录"按钮

### 3. 配置管理
- 点击左侧菜单"配置管理"
- 修改学号、密码、Portal 地址
- 点击"保存配置"

### 4. 设置
- 点击左侧菜单"设置"
- 配置主题、开机自启动等选项

## 页面说明

### 欢迎页面 (WelcomePage)
- 首次使用引导
- 三步配置流程
- 进度条显示当前步骤

### 主页 (HomePage)
- 显示网络连接状态
- 显示登录状态
- 快捷操作按钮
- 使用说明

### 登录页面 (LoginPage)
- 手动登录界面
- 记住密码选项
- 错误提示

### 配置页面 (ConfigPage)
- 账号配置
- Portal 地址配置
- 自动登录开关
- 测试登录功能

### 设置页面 (SettingsPage)
- 主题切换
- 开机自启动
- 最小化到托盘
- 清理缓存
- 关于信息

## 技术栈

- **框架**: WinUI 3 (Windows App SDK)
- **语言**: C# 10 & XAML 23 & C++ 17
- **运行时**: .NET 8.0
- **UI 设计**: Fluent Design System
- **背景效果**: Mica

## 依赖项

### NuGet 包
- Microsoft.WindowsAppSDK (1.8.260317003)
- Microsoft.Windows.SDK.BuildTools (10.0.28000.1721)

### 本地依赖
- Login.dll (C++ DLL)
- libcurl.dll (HTTP 库)

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
### 输出文件
```
bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\
├── AutoPortal.exe    - 单文件可执行程序
├── libcurl.dll      
├── Login.dll        
└── zlib1.dll        
```

## 注意事项

1. **DLL 文件**: 确保 `Login.dll` 和 `libcurl.dll` 在应用程序目录
2. **配置文件**: `config.json` 会自动创建在应用程序目录
3. **网络权限**: 应用需要网络访问权限
4. **管理员权限**: 部分功能可能需要管理员权限

## 故障排除

### 无法登录
- 检查网络连接
- 确认学号和密码正确
- 检查 Portal 地址是否正确

### DLL 加载失败
- 确保 Login.dll 在应用程序目录
- 检查 DLL 架构（x64/x86）是否匹配

### 配置保存失败
- 检查应用程序目录权限
- 确保有足够的磁盘空间

## 开发说明

### 添加新页面
1. 在 `Pages` 文件夹创建 XAML 和代码文件
2. 在 `NavigationService.cs` 添加页面类型
3. 在 `MainWindow.xaml` 添加菜单项

### 修改主题
- 编辑 `App.xaml` 中的资源字典
- 或在设置页面切换主题

### 自定义动画
- 使用 WinUI 3 的动画 API
- 参考 `WelcomePage.xaml.cs` 的动画示例


## 许可证

MIT License

## 联系方式

如有问题或建议，请提交 Issue。
