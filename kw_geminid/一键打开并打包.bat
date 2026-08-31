@echo off
chcp 65001 >nul
title 三国志曹操传 (kw) - 一键打包工具

echo ========================================================
echo     《三国志曹操传》Unity 移动端 App 一键打包与启动
echo ========================================================
echo.
echo [1] 使用 Unity Hub 打开 kw_geminid 工程
echo [2] 启动 PC 网页即玩版 (本地免安装 60FPS 极速版)
echo [3] 自动将原版素材导入至 Unity StreamingAssets 目录
echo.
set /p opt="请输入选项 (1/2/3): "

if "%opt%"=="1" (
    echo.
    echo 正在启动 Unity Hub 打开 kw_geminid...
    start "" "C:\Program Files\Unity\Hub\Unity Hub.exe"
    echo 打开后请在 Unity 菜单栏点击「战棋复刻 -> 4. 自动化打包/Android (APK)」即可直接生成安装包！
    pause
    exit /b
)

if "%opt%"=="2" (
    echo.
    echo 正在启动本地游戏服务器...
    start http://localhost:5173/
    npm run dev
    exit /b
)

if "%opt%"=="3" (
    echo.
    echo 正在解密提取原版全部素材...
    node scripts/extract_kw.mjs
    echo 素材已成功提取并同步至 public/assets/ 与 StreamingAssets 目录！
    pause
    exit /b
)

echo 无效选项。
pause
