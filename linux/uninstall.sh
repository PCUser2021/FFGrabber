#!/usr/bin/env sh
set -eu
if [ -n "$XDG_DATA_HOME" ]; then DATA_HOME="$XDG_DATA_HOME"; else DATA_HOME="$HOME/.local/share"; fi
rm -rf "$DATA_HOME/ffgrabber"
rm -f "$HOME/.local/bin/ffgrabber"
rm -f "$DATA_HOME/applications/ffgrabber.desktop"
rm -f "$DATA_HOME/icons/hicolor/scalable/apps/ffgrabber.svg"
printf 'Per-user FFGrabber files removed. Imported FFmpeg files and session logs were not removed.\n'
