# libcurl 库文件目录

请将以下文件复制到此目录：

- libcurl.lib
- libcurl-d.lib (Debug 版本)
- 其他依赖库文件

## 获取库文件

### 方法一：使用 vcpkg
如果使用 vcpkg 安装 curl，库文件会自动集成，无需手动复制。

### 方法二：手动下载
从 https://curl.se/windows/ 下载开发包，解压后将 lib 目录中的文件复制到这里。

## 注意事项

确保库文件架构与项目配置匹配：
- x64 项目使用 x64 库文件
- x86 项目使用 x86 库文件
