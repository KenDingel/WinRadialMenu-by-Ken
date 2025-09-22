@echo off
echo Building Portable RadialMenu Executable...
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK is not installed or not in PATH
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo Creating single-file portable executable...
echo This may take a minute...
echo.

REM If RadialMenu is running, attempt to close it so publish can overwrite the exe
echo Checking for running RadialMenu process...
tasklist /FI "IMAGENAME eq RadialMenu.exe" | find /I "RadialMenu.exe" >nul 2>&1
if %errorlevel%==0 (
    echo Found running RadialMenu. Attempting graceful close...
    taskkill /IM "RadialMenu.exe" /T >nul 2>&1
    timeout /T 2 /NOBREAK >nul 2>&1
    tasklist /FI "IMAGENAME eq RadialMenu.exe" | find /I "RadialMenu.exe" >nul 2>&1
    if %errorlevel%==0 (
        echo Process still running; forcing termination...
        taskkill /IM "RadialMenu.exe" /T /F >nul 2>&1
        timeout /T 1 /NOBREAK >nul 2>&1
    ) else (
        echo RadialMenu closed.
    )
) else (
    echo No running RadialMenu process found.
)


dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false

if errorlevel 1 (
    echo.
    echo BUILD FAILED!
    echo Please check the error messages above.
    pause
    exit /b 1
)

echo.
echo Build successful!
echo.
echo The portable executable is located at:
echo bin\Release\net8.0-windows\win-x64\publish\RadialMenu.exe
echo.
echo This is a self-contained executable that includes the .NET runtime.
echo You can copy this single file anywhere and it will work!
echo.
start "" "bin\Release\net8.0-windows\win-x64\publish\RadialMenu.exe"
echo.