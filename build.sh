#!/bin/bash
set -e

echo "=========================================="
echo "   Easy3D Build Script"
echo "=========================================="
echo ""

# Check .NET
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found!"
    echo "Install from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "dotnet version: $(dotnet --version)"
echo ""

# Detect platform
OS="$(uname -s)"
ARCH="$(uname -m)"

case "$OS" in
    Linux*)   RID="linux-x64" ;;
    Darwin*)
        if [ "$ARCH" = "arm64" ]; then
            RID="osx-arm64"
        else
            RID="osx-x64"
        fi
        ;;
    MINGW*|MSYS*|CYGWIN*)  RID="win-x64" ;;
    *)  echo "Unknown OS: $OS"; exit 1 ;;
esac

echo "Platform: $RID"
echo ""

# Restore
echo "[1/3] Restoring..."
dotnet restore src/Easy3D.sln

# Build
echo ""
echo "[2/3] Building..."
dotnet build src/Easy3D.sln -c Release

# Publish
echo ""
echo "[3/3] Publishing..."
dotnet publish src/Easy3D/Easy3D.csproj -c Release -r $RID --self-contained true -p:PublishSingleFile=true -o publish/$RID

echo ""
echo "=========================================="
echo "   Build Complete!"
echo "=========================================="
echo "Output: publish/$RID/"
echo ""
echo "Run: ./publish/$RID/Easy3D"
