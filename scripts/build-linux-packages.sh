#!/usr/bin/env bash
set -euo pipefail

: "$VERSION"
: "$RID"
: "$SHORT_ARCH"
: "$DEB_ARCH"
: "$RPM_ARCH"
: "$APPIMAGE_ARCH"
: "$APPIMAGE_SHA256"

ROOT_DIR="$(cd -- "$(dirname -- "$0")/.." && pwd)"
PUBLISH_DIR="$ROOT_DIR/artifacts/publish/$RID"
OUT_DIR="$ROOT_DIR/artifacts/linux"
WORK_DIR="$ROOT_DIR/artifacts/linux-work/$RID"
PKG_ROOT="$WORK_DIR/package-root"
PORTABLE_ROOT="$WORK_DIR/FFGrabber-linux-$SHORT_ARCH"
APPIMAGE_DIR="$WORK_DIR/FFGrabber.AppDir"

if [[ ! -x "$PUBLISH_DIR/FFGrabber" ]]; then
  echo "Published Linux apphost is missing or not executable: $PUBLISH_DIR/FFGrabber" >&2
  exit 1
fi
if [[ "$RID" != "linux-x64" && "$RID" != "linux-arm64" ]]; then
  echo "Unsupported runtime identifier: $RID" >&2
  exit 1
fi
if find "$PUBLISH_DIR" -type f \( -name ffmpeg -o -name ffprobe -o -name ffplay -o -name ffmpeg.exe -o -name ffprobe.exe -o -name ffplay.exe \) -print -quit | grep -q .; then
  echo "Packaging violation: FFGrabber must not ship FFmpeg binaries." >&2
  exit 1
fi

mkdir -p "$OUT_DIR" "$WORK_DIR"
rm -rf "$PKG_ROOT" "$PORTABLE_ROOT" "$APPIMAGE_DIR"
mkdir -p "$PKG_ROOT/opt/ffgrabber" "$PKG_ROOT/usr/bin" "$PKG_ROOT/usr/share/applications" "$PKG_ROOT/usr/share/icons/hicolor/scalable/apps"
cp -a "$PUBLISH_DIR/." "$PKG_ROOT/opt/ffgrabber/"
chmod 0755 "$PKG_ROOT/opt/ffgrabber/FFGrabber"
install -m 0755 "$ROOT_DIR/linux/ffgrabber" "$PKG_ROOT/usr/bin/ffgrabber"
install -m 0644 "$ROOT_DIR/linux/ffgrabber.desktop" "$PKG_ROOT/usr/share/applications/ffgrabber.desktop"
install -m 0644 "$ROOT_DIR/linux/ffgrabber.svg" "$PKG_ROOT/usr/share/icons/hicolor/scalable/apps/ffgrabber.svg"

PACKAGE_FILES="opt/ffgrabber usr/bin/ffgrabber usr/share/applications/ffgrabber.desktop usr/share/icons/hicolor/scalable/apps/ffgrabber.svg"

fpm -s dir -t deb -n ffgrabber -v "$VERSION" -a "$DEB_ARCH" -C "$PKG_ROOT" -p "$OUT_DIR/FFGrabber-$VERSION-linux-$SHORT_ARCH.deb" \
  --license MIT --maintainer "FFGrabber contributors" --vendor "FFGrabber" --url "https://github.com/PCUser2021/FFGrabber" \
  --description "Cross-platform graphical frontend for a user-imported FFmpeg. FFmpeg is not bundled." \
  --depends libc6 --depends libgcc-s1 --depends libstdc++6 --depends libx11-6 --depends libice6 --depends libsm6 --depends libfontconfig1 --depends libfreetype6 \
  opt/ffgrabber usr/bin/ffgrabber usr/share/applications/ffgrabber.desktop usr/share/icons/hicolor/scalable/apps/ffgrabber.svg

fpm -s dir -t rpm -n ffgrabber -v "$VERSION" -a "$RPM_ARCH" -C "$PKG_ROOT" -p "$OUT_DIR/FFGrabber-$VERSION-linux-$SHORT_ARCH.rpm" \
  --license MIT --maintainer "FFGrabber contributors" --vendor "FFGrabber" --url "https://github.com/PCUser2021/FFGrabber" \
  --description "Cross-platform graphical frontend for a user-imported FFmpeg. FFmpeg is not bundled." \
  --depends glibc --depends libgcc --depends libstdc++ --depends libX11 --depends libICE --depends libSM --depends fontconfig --depends freetype \
  opt/ffgrabber usr/bin/ffgrabber usr/share/applications/ffgrabber.desktop usr/share/icons/hicolor/scalable/apps/ffgrabber.svg

# Portable tarball: per-user installer and uninstaller, with no root privileges required.
mkdir -p "$PORTABLE_ROOT/app"
cp -a "$PUBLISH_DIR/." "$PORTABLE_ROOT/app/"
install -m 0644 "$ROOT_DIR/linux/ffgrabber.svg" "$PORTABLE_ROOT/ffgrabber.svg"
install -m 0644 "$ROOT_DIR/LICENSE" "$PORTABLE_ROOT/LICENSE"
install -m 0644 "$ROOT_DIR/THIRD-PARTY-NOTICES.md" "$PORTABLE_ROOT/THIRD-PARTY-NOTICES.md"
install -m 0644 "$ROOT_DIR/linux/PORTABLE-INSTALL.md" "$PORTABLE_ROOT/README.txt"
install -m 0644 "$ROOT_DIR/linux/install.sh" "$PORTABLE_ROOT/install.sh"
install -m 0644 "$ROOT_DIR/linux/uninstall.sh" "$PORTABLE_ROOT/uninstall.sh"
tar -czf "$OUT_DIR/FFGrabber-$VERSION-linux-$SHORT_ARCH.tar.gz" -C "$WORK_DIR" "FFGrabber-linux-$SHORT_ARCH"

# AppImage package. The official appimagetool binary is downloaded only after SHA-256 is pinned and checked.
mkdir -p "$APPIMAGE_DIR/usr/lib/ffgrabber" "$APPIMAGE_DIR/usr/share/applications" "$APPIMAGE_DIR/usr/share/icons/hicolor/scalable/apps"
cp -a "$PUBLISH_DIR/." "$APPIMAGE_DIR/usr/lib/ffgrabber/"
chmod 0755 "$APPIMAGE_DIR/usr/lib/ffgrabber/FFGrabber"
install -m 0644 "$ROOT_DIR/linux/ffgrabber.svg" "$APPIMAGE_DIR/ffgrabber.svg"
install -m 0644 "$ROOT_DIR/linux/ffgrabber.svg" "$APPIMAGE_DIR/usr/share/icons/hicolor/scalable/apps/ffgrabber.svg"
sed 's/^Exec=ffgrabber$/Exec=AppRun/' "$ROOT_DIR/linux/ffgrabber.desktop" > "$APPIMAGE_DIR/ffgrabber.desktop"
cp "$APPIMAGE_DIR/ffgrabber.desktop" "$APPIMAGE_DIR/usr/share/applications/ffgrabber.desktop"
cat > "$APPIMAGE_DIR/AppRun" <<'APPRUN'
#!/usr/bin/env sh
HERE="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
exec "$HERE/usr/lib/ffgrabber/FFGrabber" "$@"
APPRUN
chmod 0755 "$APPIMAGE_DIR/AppRun"

case "$APPIMAGE_ARCH" in
  x86_64) APPIMAGETOOL_URL="https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage" ;;
  aarch64) APPIMAGETOOL_URL="https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-aarch64.AppImage" ;;
  *) echo "Unsupported AppImage architecture: $APPIMAGE_ARCH" >&2; exit 1 ;;
esac
APPIMAGETOOL="$WORK_DIR/appimagetool-$APPIMAGE_ARCH.AppImage"
curl --fail --location --retry 3 --silent --show-error "$APPIMAGETOOL_URL" --output "$APPIMAGETOOL"
echo "$APPIMAGE_SHA256  $APPIMAGETOOL" | sha256sum --check --status || {
  echo "Official appimagetool SHA-256 verification failed." >&2
  exit 1
}
chmod 0755 "$APPIMAGETOOL"
APPIMAGE_EXTRACT_AND_RUN=1 ARCH="$APPIMAGE_ARCH" VERSION="$VERSION" "$APPIMAGETOOL" "$APPIMAGE_DIR" "$OUT_DIR/FFGrabber-$VERSION-linux-$SHORT_ARCH.AppImage"
chmod 0755 "$OUT_DIR/FFGrabber-$VERSION-linux-$SHORT_ARCH.AppImage"

echo "Created Linux packages in $OUT_DIR:"
ls -lh "$OUT_DIR/FFGrabber-$VERSION-linux-$SHORT_ARCH."*
