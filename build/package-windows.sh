#!/usr/bin/env bash
set -euo pipefail

ARCH="${1:-x64}"
case "$ARCH" in
  x64) RID="win-x64" ;;
  arm64) RID="win-arm64" ;;
  x86) RID="win-x86" ;;
  *) echo "Unknown arch: $ARCH (use x64, arm64 or x86)"; exit 1 ;;
esac

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_PROJ="$ROOT/App/App.csproj"
ICON="$ROOT/build/Fcry.ico"
OUT_DIR="$ROOT/build/Fcry-$RID"
ZIP="$ROOT/build/Fcry-$RID.zip"

echo ">> Publishing self-contained for $RID"
rm -rf "$OUT_DIR"
dotnet publish "$APP_PROJ" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:DebugType=none \
  -p:ApplicationIcon="$ICON" \
  -o "$OUT_DIR"

echo ">> Zipping"
rm -f "$ZIP"
( cd "$ROOT/build" && zip -qr "$(basename "$ZIP")" "$(basename "$OUT_DIR")" )

echo ">> Done"
echo "   Folder: $OUT_DIR"
echo "   Zip:    $ZIP"
echo "   Run on Windows: double-click Fcry.exe inside the folder"
