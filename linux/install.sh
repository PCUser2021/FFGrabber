#!/usr/bin/env sh
set -eu
ROOT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
DATA_HOME="$(printenv XDG_DATA_HOME || true)"
if [ -z "$DATA_HOME" ]; then DATA_HOME="$HOME/.local/share"; fi
APP_DIR="$DATA_HOME/ffgrabber"
BIN_DIR="$HOME/.local/bin"
APP_MENU_DIR="$DATA_HOME/applications"
ICON_DIR="$DATA_HOME/icons/hicolor/scalable/apps"

if [ ! -x "$ROOT_DIR/app/FFGrabber" ]; then
  echo "FFGrabber apphost not found in $ROOT_DIR/app" >&2
  exit 1
fi

mkdir -p "$DATA_HOME" "$BIN_DIR" "$APP_MENU_DIR" "$ICON_DIR"
STAGING="$APP_DIR.new"
rm -rf "$STAGING"
mkdir -p "$STAGING"
cp -a "$ROOT_DIR/app/." "$STAGING/"
rm -rf "$APP_DIR"
mv "$STAGING" "$APP_DIR"
install -m 0644 "$ROOT_DIR/ffgrabber.svg" "$ICON_DIR/ffgrabber.svg"
printf '#!/usr/bin/env sh\nexec "%s/FFGrabber" "$@"\n' "$APP_DIR" > "$BIN_DIR/ffgrabber"
chmod 0755 "$BIN_DIR/ffgrabber"
{
  printf '[Desktop Entry]\nVersion=1.0\nType=Application\nName=FFGrabber\n'
  printf 'Comment=Graphical frontend for your own FFmpeg tools\n'
  printf 'Exec="%s/FFGrabber"\n' "$APP_DIR"
  printf 'Icon=%s/ffgrabber.svg\n' "$ICON_DIR"
  printf 'Terminal=false\nStartupNotify=true\nCategories=AudioVideo;Video;Audio;Utility;\n'
} > "$APP_MENU_DIR/ffgrabber.desktop"
chmod 0644 "$APP_MENU_DIR/ffgrabber.desktop"
printf 'FFGrabber installed for this user at %s\n' "$APP_DIR"
printf 'Desktop launcher: %s/ffgrabber.desktop\n' "$APP_MENU_DIR"
printf 'Command-line launcher: %s/ffgrabber\n' "$BIN_DIR/ffgrabber"
printf 'No system-wide installation or sudo access was required.\n'
