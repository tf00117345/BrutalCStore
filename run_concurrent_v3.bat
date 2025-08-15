@echo off
setlocal enabledelayedexpansion
chcp 65001 > nul

:: 切換到腳本所在目錄
cd /d "%~dp0"

echo ========================================
echo   BrutalCStore 並發壓力測試腳本 v3
echo ========================================
echo.

:: 檢查 BrutalCStore.exe 是否存在
if not exist "BrutalCStore.exe" (
    echo 錯誤: 找不到 BrutalCStore.exe 檔案
    echo 當前目錄: %CD%
    pause
    exit /b 1
)

:: 詢問設定
:ask_concurrent
set /p concurrent_count="請輸入並發執行數量 (1-10): "
if "%concurrent_count%"=="" goto ask_concurrent
if %concurrent_count% LSS 1 goto ask_concurrent
if %concurrent_count% GTR 10 goto ask_concurrent

:ask_duration
set /p duration_minutes="請輸入執行時間 (分鐘): "
if "%duration_minutes%"=="" goto ask_duration
if %duration_minutes% LSS 1 goto ask_duration

echo.
echo 設定摘要:
echo - 並發數量: %concurrent_count%
echo - 執行時間: %duration_minutes% 分鐘
echo.

set /p confirm="確認開始執行嗎? (Y/N): "
if /i not "%confirm%"=="Y" exit /b

:: 建立日誌目錄
set logdir=logs_v3
if not exist "%logdir%" mkdir "%logdir%"

:: 計算結束時間
set /a duration_seconds=%duration_minutes%*60
for /f %%i in ('powershell -command "[DateTimeOffset]::Now.ToUnixTimeSeconds()"') do set start_time=%%i
set /a end_time=start_time+duration_seconds

echo 開始執行 %concurrent_count% 個並發實例...
echo.

:: 創建臨時執行腳本
for /l %%i in (1,1,%concurrent_count%) do (
    echo 創建實例 %%i 的執行腳本...
    
    echo @echo off > instance_%%i.bat
    echo setlocal enabledelayedexpansion >> instance_%%i.bat
    echo set instance_id=%%i >> instance_%%i.bat
    echo set end_time=%end_time% >> instance_%%i.bat
    echo set run_count=0 >> instance_%%i.bat
    echo echo 實例 %%instance_id%% 開始執行... >> instance_%%i.bat
    echo :loop >> instance_%%i.bat
    echo for /f %%%%a in ('powershell -command "[DateTimeOffset]::Now.ToUnixTimeSeconds()"'^) do set current_time=%%%%a >> instance_%%i.bat
    echo if ^^!current_time^^! GEQ %%end_time%% goto end >> instance_%%i.bat
    echo set /a run_count=run_count+1 >> instance_%%i.bat
    echo echo [%%time%%] 實例 %%instance_id%% 第^^!run_count^^!次執行 >> instance_%%i.bat
    echo BrutalCStore.exe >> instance_%%i.bat
    echo if errorlevel 1 ( >> instance_%%i.bat
    echo     echo 實例 %%instance_id%% 執行出錯！ >> instance_%%i.bat
    echo ^) >> instance_%%i.bat
    echo timeout /t 1 /nobreak ^> nul >> instance_%%i.bat
    echo goto loop >> instance_%%i.bat
    echo :end >> instance_%%i.bat
    echo echo 實例 %%instance_id%% 結束，共執行^^!run_count^^!次 ^>^> "%logdir%\instance_%%i_summary.log" >> instance_%%i.bat
    echo del instance_%%i.bat >> instance_%%i.bat
)

echo.
echo 啟動所有實例到背景...
for /l %%i in (1,1,%concurrent_count%) do (
    echo 啟動背景實例 %%i...
    start "BrutalCStore 實例 %%i" /min cmd /c instance_%%i.bat
    timeout /t 1 /nobreak > nul
)

echo.
echo 所有實例已啟動！將執行 %duration_minutes% 分鐘
echo 監控執行狀態...

:: 監控
:monitor
for /f %%i in ('powershell -command "[DateTimeOffset]::Now.ToUnixTimeSeconds()"') do set current_time=%%i
set /a elapsed=current_time-start_time

if %elapsed% GEQ %duration_seconds% goto cleanup

set /a remaining=duration_seconds-elapsed
set /a remaining_minutes=remaining/60

:: 檢查執行中的 BrutalCStore 程序數量
for /f %%a in ('tasklist /fi "imagename eq BrutalCStore.exe" /fo csv 2^>nul ^| find /c /v ""') do set running_count=%%a
set /a running_count=running_count-1
if %running_count% LSS 0 set running_count=0

echo [%time%] 已執行 %elapsed% 秒，剩餘約 %remaining_minutes% 分鐘，執行中程序: %running_count%

timeout /t 15 /nobreak > nul
goto monitor

:cleanup
echo.
echo 時間到達，清理所有實例...
taskkill /f /im BrutalCStore.exe > nul 2>&1
taskkill /f /im cmd.exe /fi "WINDOWTITLE eq BrutalCStore 實例*" > nul 2>&1

:: 清理臨時檔案
for /l %%i in (1,1,%concurrent_count%) do (
    if exist instance_%%i.bat del instance_%%i.bat > nul 2>&1
)

echo 清理完成！
pause