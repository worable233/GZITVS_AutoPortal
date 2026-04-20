# AutoPortal

![AutoPortal Logo](Assets/Logo.png)

## 简介
AutoPortal 是一个基于 WinUI 3 的校园网 Portal 自动登录工具，支持记住密码、自动登录等功能。

## 功能特性
- ✅ **自动登录** - 连接校园网时自动检测并登录
- ✅ **记住密码** - 安全保存账号密码
- ✅ **状态检测** - 实时检测网络和登录状态
- ✅ **主题切换** - 支持深色/浅色主题
- ✅ **Mica 背景** - 现代化毛玻璃效果

## 系统要求
- **操作系统**: Windows 10 1809 或更高版本
- **架构**: x64

## 使用说明

### 首次使用
1. 启动应用
2. 进入欢迎页面，点击"开始配置"
3. 输入学号和密码，点击"保存配置"

### 配置账号
- 点击左侧菜单"设置"
- 在"账号配置"区域修改学号、密码、Portal 地址
- 点击"保存配置"

### 登录
- 连接校园网后自动登录
- 或点击"立即登录"按钮手动登录

## 构建和发布

### 构建
```bash
dotnet build
```

### 运行
```bash
dotnet run
```

### 发布
```bash
dotnet publish -c Release -r win-x64
```

## 故障排除

### 无法登录
- 检查网络连接
- 确认学号和密码正确
- 检查 Portal 地址

### XAML 解析失败
- 确保 AutoPortal.pri 文件存在
- 确保 Assets 文件夹和图标文件存在
- 重新解压 ZIP 文件

### DLL 加载失败
- 确保 Login.dll、libcurl.dll、zlib1.dll 在应用目录

## 技术栈
- **框架**: WinUI 3 (Windows App SDK)
- **运行时**: .NET 8.0
- **语言**: C# 10 & C++ 17
- **图表**: LiveChartsCore

## 许可证
MIT License
