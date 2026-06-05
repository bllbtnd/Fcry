#!/usr/bin/env bash
set -euo pipefail

ARCH="${1:-$(uname -m)}"
case "$ARCH" in
  arm64) RID="osx-arm64" ;;
  x86_64) RID="osx-x64" ;;
  *) echo "Unknown arch: $ARCH (use arm64 or x86_64)"; exit 1 ;;
esac

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_PROJ="$ROOT/App/App.csproj"
PUBLISH_DIR="$ROOT/build/publish-$RID"
BUNDLE="$ROOT/build/Fcry.app"

echo ">> Publishing self-contained for $RID"
rm -rf "$PUBLISH_DIR"
dotnet publish "$APP_PROJ" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:DebugType=none \
  -o "$PUBLISH_DIR"

echo ">> Building Fcry.app bundle"
rm -rf "$BUNDLE"
mkdir -p "$BUNDLE/Contents/MacOS"
mkdir -p "$BUNDLE/Contents/Resources"

cp "$ROOT/build/Info.plist" "$BUNDLE/Contents/Info.plist"
cp -R "$PUBLISH_DIR"/* "$BUNDLE/Contents/MacOS/"

if [ -f "$ROOT/build/Fcry.icns" ]; then
  cp "$ROOT/build/Fcry.icns" "$BUNDLE/Contents/Resources/Fcry.icns"
fi

chmod +x "$BUNDLE/Contents/MacOS/Fcry"

echo ">> Removing quarantine and ad-hoc signing"
xattr -cr "$BUNDLE" || true
codesign --force --deep --sign - "$BUNDLE" || true

echo ">> Done: $BUNDLE"
