#!/usr/bin/env sh
set -eu
DATA_HOME="$(printenv XDG_DATA_HOME || true)"
if [ -z "$DATA_HOME" ]; then DATA_HOME="$HOME/.local/share"; fi
rm -rf "$DATA_HOME/ffgrabber"
rm -f "$HOME/.local/bin/ffgrabber"
rm -f "$DATA_HOME/applications/ffgrabber.desktop"
rm -f "$DATA_HOME/icons/hicolor/scalable/apps/ffgrabber.svg"
printf 'Per-user FFGrabber files removed. Imported FFmpeg files and session logs were not removed.\n'
