@echo off
echo Building RadialMenu...
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

echo Restoring packages...
dotnet restore RadialMenu.csproj

echo.
echo Building Release version...
dotnet build -c Release RadialMenu.csproj

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
echo The executable is located at:
echo bin\Release\net8.0-windows\RadialMenu.exe
echo.
echo To create a single portable executable, run: build-portable.bat
echo.
pause
