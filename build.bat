@echo off
echo Building Easy3D...
echo.

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo .NET SDK not found!
    echo Install from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo Restoring packages...
dotnet restore src\Easy3D.sln

echo.
echo Building...
dotnet build src\Easy3D.sln -c Release

echo.
echo Publishing...
dotnet publish src\Easy3D -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\win-x64

echo.
echo Done!
pause
