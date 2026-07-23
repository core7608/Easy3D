#!/bin/bash
# سكريبت تشغيل Easy3D

echo "🎨 جاري تشغيل Easy3D..."
echo ""

# التحقق من Python
if ! command -v python3 &> /dev/null; then
    echo "❌ Python غير مثبت!"
    echo "يرجى تثبيت Python 3.6 أو أحدث"
    exit 1
fi

# تشغيل البرنامج
python3 easy3d.py
