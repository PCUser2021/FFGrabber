FFGrabber Linux portable package

1. Extract the archive.
2. Run "sh ./install.sh" from the extracted directory. This installs FFGrabber for the current user only; sudo/root is not needed.
3. Launch FFGrabber from your desktop application menu, or run ~/.local/bin/ffgrabber.

To remove this per-user installation, run "sh ./uninstall.sh" from the extracted directory. This does not delete your imported FFmpeg toolset or logs.

FFmpeg is not bundled. Import the matching Linux ffmpeg, ffprobe, and ffplay binaries from inside FFGrabber. The GUI uses X11 by default and may run under Wayland through XWayland compatibility.
