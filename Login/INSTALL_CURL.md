# 安装 libcurl

本项目需要 libcurl 库。请按照以下步骤安装：

## 方法一：使用 vcpkg（推荐）

1. 安装 vcpkg：
```powershell
git clone https://github.com/Microsoft/vcpkg.git
cd vcpkg
.\bootstrap-vcpkg.bat
```

2. 安装 libcurl：
```powershell
.\vcpkg install curl:x64-windows
.\vcpkg install curl:x86-windows
```

3. 集成到 Visual Studio：
```powershell
.\vcpkg integrate install
```

4. 重新打开 Visual Studio 并构建项目

## 方法二：手动下载

1. 访问 https://curl.se/windows/
2. 下载适合你平台的开发包
3. 解压后将：
   - `include` 目录内容复制到项目的 `include` 目录
   - `lib` 目录内容复制到项目的 `lib` 目录（需要创建）
   - DLL 文件复制到输出目录

## 创建 lib 目录

如果使用手动方式，需要创建 lib 目录：

```powershell
mkdir lib
```

然后将 libcurl.lib 等库文件复制到该目录。
