#!/bin/bash
set -e

echo "🔨 Building Easy3D..."
echo ""

# Check .NET
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found!"
    echo "Install from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "📦 .NET version:"
dotnet --version
echo ""

# Restore
echo "📥 Restoring packages..."
dotnet restore src/Easy3D.sln

# Build
echo ""
echo "🔨 Building..."
dotnet build src/Easy3D.sln -c Release

echo ""
echo "✅ Build complete!"
echo ""
echo "Run: dotnet run --project src/Easy3D"
echo ""

# Publish for current platform
echo "📦 Publishing..."
case "$(uname -s)" in
    Linux*)   RID="linux-x64" ;;
    Darwin*)  RID="osx-x64" ;;
    MINGW*)   RID="win-x64" ;;
    *)        RID="linux-x64" ;;
esac

dotnet publish src/Easy3D -c Release -r $RID --self-contained true -p:PublishSingleFile=true -o publish/$RID
echo "✅ Published to publish/$RID/"
