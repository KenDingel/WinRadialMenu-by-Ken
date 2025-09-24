@echo off
echo RadialMenu AutoHotkey Setup Utility
====================================
echo.

REM Check if AutoHotkey is already in PATH
where AutoHotkey.exe >nul 2>&1
if %errorlevel%==0 (
    echo ✓ AutoHotkey found in PATH
    for /f "tokens=*" %%i in ('where AutoHotkey.exe') do echo   Location: %%i
    echo.
    echo Detecting AutoHotkey version...
    AutoHotkey.exe --version 2>nul | findstr /C:"AutoHotkey" >nul
    if %errorlevel%==0 (
        echo ✓ AutoHotkey version detected - RadialMenu will auto-select the correct script
    )
    echo.
    echo AutoHotkey is already configured and ready to use!
    goto :end
)

echo AutoHotkey not found in PATH. Searching common installation locations...
echo.

REM Check common AutoHotkey installation paths
set "AHK_FOUND=0"

if exist "C:\Program Files\AutoHotkey\AutoHotkey.exe" (
    set "AHK_PATH=C:\Program Files\AutoHotkey"
    set "AHK_EXE=C:\Program Files\AutoHotkey\AutoHotkey.exe"
    set "AHK_FOUND=1"
    echo ✓ Found: %AHK_EXE%
)

if exist "C:\Program Files (x86)\AutoHotkey\AutoHotkey.exe" (
    set "AHK_PATH=C:\Program Files (x86)\AutoHotkey"
    set "AHK_EXE=C:\Program Files (x86)\AutoHotkey\AutoHotkey.exe"
    set "AHK_FOUND=1"
    echo ✓ Found: %AHK_EXE%
)

if exist "C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe" (
    set "AHK_PATH=C:\Program Files\AutoHotkey\v2"
    set "AHK_EXE=C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe"
    set "AHK_FOUND=1"
    echo ✓ Found: %AHK_EXE%
)

if %AHK_FOUND%==0 (
    echo ✗ AutoHotkey not found in common locations
    echo.
    echo Please install AutoHotkey from: https://www.autohotkey.com/
    echo.
    echo Alternative: If AutoHotkey is installed elsewhere, you can:
    echo 1. Add the AutoHotkey installation directory to your PATH, OR
    echo 2. Edit radialmenu-settings-latest.json and set "AutoHotkeyPath" to the full path
    goto :end
)

echo.
echo Would you like to add AutoHotkey to your system PATH?
echo This will make it available system-wide for RadialMenu and other applications.
echo.
set /p choice="Add to PATH? (y/N): "

if /i "%choice%"=="y" (
    echo.
    echo Adding %AHK_PATH% to system PATH...
    
    REM Add to PATH using PowerShell (requires admin rights)
    powershell -Command "if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] 'Administrator')) { Write-Host 'This operation requires administrator privileges. Please run as administrator.'; exit 1 } else { $env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine'); if ($env:Path -notlike '*%AHK_PATH%*') { [Environment]::SetEnvironmentVariable('Path', $env:Path + ';%AHK_PATH%', 'Machine'); Write-Host 'AutoHotkey added to system PATH successfully!' } else { Write-Host 'AutoHotkey is already in the system PATH.' } }"
    
    if %errorlevel%==0 (
        echo.
        echo ✓ Successfully added to PATH!
        echo You may need to restart applications for the change to take effect.
    ) else (
        echo.
        echo ✗ Failed to add to PATH. Please run this script as administrator.
        echo.
        echo Manual PATH setup:
        echo 1. Open System Properties ^> Advanced ^> Environment Variables
        echo 2. Edit the System PATH variable
        echo 3. Add: %AHK_PATH%
        echo 4. Click OK and restart applications
    )
) else (
    echo.
    echo To manually configure RadialMenu:
    echo Edit radialmenu-settings-latest.json and set:
    echo "AutoHotkeyPath": "%AHK_EXE%"
)

:end
echo.
pause