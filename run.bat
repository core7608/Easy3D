@echo off
REM سكريبت تشغيل Easy3D لويندوز

echo 🎨 جاري تشغيل Easy3D...
echo.

REM التحقق من Python
python --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Python غير مثبت!
    echo يرجى تثبيت Python 3.6 أو أحدث
    echo https://www.python.org/downloads/
    pause
    exit /b 1
)

REM تشغيل البرنامج
python easy3d.py
pause
