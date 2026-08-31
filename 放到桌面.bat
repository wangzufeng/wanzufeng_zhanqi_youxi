@echo off
chcp 65001 >nul
set "SRC=%~dp0"
set "DEST=%USERPROFILE%\Desktop\战棋复刻"
echo 正在复制到桌面：
echo   %DEST%
if not exist "%USERPROFILE%\Desktop" (
  echo 找不到桌面目录。请手动把本文件夹拖到桌面。
  pause
  exit /b 1
)
mkdir "%DEST%" 2>nul
xcopy /E /I /Y "%SRC%*" "%DEST%\"
if errorlevel 1 (
  echo 复制失败。请手动把本文件夹拖到桌面。
  pause
  exit /b 1
)
echo.
echo 已放到桌面。
explorer "%DEST%"
pause
