#!/usr/bin/env bash
set -euo pipefail

ARCH="${1:-x64}"
case "$ARCH" in
  x64) RID="linux-x64" ;;
  arm64) RID="linux-arm64" ;;
  *) echo "Unknown arch: $ARCH (use x64 or arm64)"; exit 1 ;;
esac

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_PROJ="$ROOT/App/App.csproj"
ICON="$ROOT/App/Assets/icon.png"
STAGE="$ROOT/build/Fcry-$RID"
TARBALL="$ROOT/build/Fcry-$RID.tar.gz"

echo ">> Publishing self-contained for $RID"
rm -rf "$STAGE"
dotnet publish "$APP_PROJ" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:DebugType=none \
  -o "$STAGE/bin"

echo ">> Adding icon and desktop entry"
cp "$ICON" "$STAGE/fcry.png"
chmod +x "$STAGE/bin/Fcry"

cat > "$STAGE/fcry.desktop" <<'DESKTOP'
[Desktop Entry]
Type=Application
Name=Fcry
Comment=File encryption
Exec=__EXEC__
Icon=fcry
Terminal=false
Categories=Utility;Security;
DESKTOP

cat > "$STAGE/install.sh" <<'INSTALL'
#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
PREFIX="$HOME/.local"
APPDIR="$PREFIX/share/fcry"

mkdir -p "$APPDIR" "$PREFIX/share/applications" "$PREFIX/share/icons/hicolor/512x512/apps"
cp -R "$HERE/bin/." "$APPDIR/"
chmod +x "$APPDIR/Fcry"
cp "$HERE/fcry.png" "$PREFIX/share/icons/hicolor/512x512/apps/fcry.png"

sed "s|__EXEC__|$APPDIR/Fcry|" "$HERE/fcry.desktop" > "$PREFIX/share/applications/fcry.desktop"
chmod +x "$PREFIX/share/applications/fcry.desktop"

command -v update-desktop-database >/dev/null 2>&1 && update-desktop-database "$PREFIX/share/applications" || true
command -v gtk-update-icon-cache >/dev/null 2>&1 && gtk-update-icon-cache -f "$PREFIX/share/icons/hicolor" || true

echo "Installed. Find 'Fcry' in your app menu, or run: $APPDIR/Fcry"
INSTALL
chmod +x "$STAGE/install.sh"

cat > "$STAGE/README.txt" <<'README'
Fcry for Linux

Run directly:
    ./bin/Fcry

Or install to your application menu with icon:
    ./install.sh
README

echo ">> Creating tarball"
rm -f "$TARBALL"
( cd "$ROOT/build" && tar -czf "$(basename "$TARBALL")" "$(basename "$STAGE")" )

echo ">> Done"
echo "   Folder:  $STAGE"
echo "   Tarball: $TARBALL"
echo "   On Linux: extract, then run ./install.sh (menu+icon) or ./bin/Fcry directly"
