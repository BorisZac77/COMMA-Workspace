#!/bin/bash

set -e

APP_NAME="COMMA Workspace 4.0.app"
PUBLISH_DIR="COMMA.App/bin/Release/net10.0/osx-arm64/publish"
TEMP_APP_DIR="/tmp/COMMA_Workspace_Build"
APP_PATH="$TEMP_APP_DIR/$APP_NAME"
DESKTOP="$HOME/Desktop"

echo "=== CLEAN ==="

rm -rf "$TEMP_APP_DIR"

rm -rf "COMMA.App/bin"
rm -rf "COMMA.App/obj"
rm -rf "COMMA.Core/bin"
rm -rf "COMMA.Core/obj"
rm -rf "COMMA.DrawingsGenerator/bin"
rm -rf "COMMA.DrawingsGenerator/obj"

echo "=== PUBLISH ==="

dotnet publish COMMA.App \
    -c Release \
    -r osx-arm64 \
    --self-contained true

echo "=== CREATE APP ==="

mkdir -p "$APP_PATH/Contents/MacOS"
mkdir -p "$APP_PATH/Contents/Resources"

cp "COMMA.App/Assets/COMMAWorkspace.icns" \
    "$APP_PATH/Contents/Resources/"

cp -R "$PUBLISH_DIR"/. \
    "$APP_PATH/Contents/MacOS/"

cat > "$APP_PATH/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>

<key>CFBundleName</key>
<string>COMMA Workspace 4.0</string>

<key>CFBundleIconFile</key>
<string>COMMAWorkspace</string>

<key>CFBundleDisplayName</key>
<string>COMMA Workspace 4.0</string>

<key>CFBundleIdentifier</key>
<string>com.comma.workspace</string>

<key>CFBundleExecutable</key>
<string>COMMA.App</string>

<key>CFBundlePackageType</key>
<string>APPL</string>

<key>CFBundleVersion</key>
<string>4.0.0</string>

<key>CFBundleShortVersionString</key>
<string>4.0.0</string>

</dict>
</plist>
EOF

chmod +x "$APP_PATH/Contents/MacOS/COMMA.App"

echo "=== COPY TO DESKTOP ==="

rm -rf "$DESKTOP/$APP_NAME"

cp -R "$APP_PATH" "$DESKTOP/"

echo "=== CLEAN BUILD FILES ==="

rm -rf "COMMA.App/bin"
rm -rf "COMMA.App/obj"
rm -rf "COMMA.Core/bin"
rm -rf "COMMA.Core/obj"
rm -rf "COMMA.DrawingsGenerator/bin"
rm -rf "COMMA.DrawingsGenerator/obj"

rm -rf "$TEMP_APP_DIR"

echo "=== GOTOWE ==="
echo "$DESKTOP/$APP_NAME"

open "$DESKTOP/$APP_NAME"
