@echo off
chcp 65001 >nul
color 0A
title AutoPortal v1.1.0 安装程序

:MENU
cls
echo.
echo ╔══════════════════════════════════════════════════════════════╗
echo ║                                                              ║
echo ║                    AutoPortal v1.1.0                         ║
echo ║                   安装向导                                   ║
echo ║                                                              ║
echo ╚══════════════════════════════════════════════════════════════╝
echo.
echo 请选择安装选项：
echo.
echo   [1] 快速安装（推荐）
echo   [2] 自定义安装
echo   [3] 卸载程序
echo   [4] 查看说明
echo   [0] 退出
echo.
set /p choice="请输入选项 (0-4): "

if "%choice%"=="1" goto QUICK_INSTALL
if "%choice%"=="2" goto CUSTOM_INSTALL
if "%choice%"=="3" goto UNINSTALL
if "%choice%"=="4" goto VIEW_HELP
if "%choice%"=="0" goto EXIT

echo 无效选项，请重新输入！
timeout /t 2 >nul
goto MENU

:QUICK_INSTALL
cls
echo.
echo ═══════════════════════════════════════════════════════════
echo   快速安装
echo ═══════════════════════════════════════════════════════════
echo.
echo 正在安装 AutoPortal...
echo.
powershell -ExecutionPolicy Bypass -File "Install.ps1"
if %ERRORLEVEL% EQU 0 (
    echo.
    echo 安装成功！按任意键返回主菜单...
    pause >nul
) else (
    echo.
    echo 安装失败！请检查错误信息。
    pause
)
goto MENU

:CUSTOM_INSTALL
cls
echo.
echo ═══════════════════════════════════════════════════════════
echo   自定义安装
echo ═══════════════════════════════════════════════════════════
echo.
echo 请使用 Inno Setup 创建专业安装包：
echo.
echo 1. 下载并安装 Inno Setup:
echo   https://jrsoftware.org/isdl.php
echo.
echo 2. 运行以下命令编译安装包:
echo   iscc.exe AutoPortal.iss
echo.
echo 3. 运行生成的 Setup 文件进行安装
echo.
pause
goto MENU

:UNINSTALL
cls
echo.
echo ═══════════════════════════════════════════════════════════
echo   卸载程序
echo ═══════════════════════════════════════════════════════════
echo.
powershell -ExecutionPolicy Bypass -File "uninstall.ps1"
echo.
pause
goto MENU

:VIEW_HELP
cls
type "INSTALL.txt"
echo.
pause
goto MENU

:EXIT
cls
echo.
echo 感谢使用 AutoPortal！
echo.
timeout /t 2 >nul
exit
