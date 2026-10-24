# FFGrabber 0.5.3

## GUI appearance and theme fixes

- Removed Acrylic/Mica/transparent window backdrops from the main window, Options window, and FFmpeg capability browser. The UI now uses opaque, predictable surfaces instead of allowing desktop pixels to affect panel and control colors.
- Added **System**, **Light**, and **Dark** application themes, with live switching and a saved preference under `%LOCALAPPDATA%\\FFGrabber\\ui-theme.txt`.
- Replaced dark-only and alpha-blended panel/control colors with separate theme palettes for backgrounds, cards, status panels, text, borders, controls, focus states, and primary-button labels.
- Applied the shared theme to secondary windows and the runtime capability browser.

## FFmpeg settings discovery

- Retained the imported-binary-driven `-h full` option catalogue and detailed component help.
- Added runtime `-sources device` and `-sinks device` queries for device backends supported by the imported FFmpeg build.
- Added direct access to the official FFmpeg CLI/component references, ffprobe/ffplay documentation, and the FFmpeg Trac wiki from Options and the capability browser.

## Packaging and safety

- Windows x64 and ARM64 installers are built and checked by GitHub Actions.
- CI verifies executable architecture and fails if FFmpeg binaries or a bundled FFmpeg tools folder are present. FFGrabber remains a pure GUI: the user must import their own `ffmpeg.exe`, `ffprobe.exe`, and `ffplay.exe`.
- SHA-256 checksums are supplied beside both installers and in the combined `SHA256SUMS.txt` manifest.

## Option coverage note

FFmpeg component options vary with the exact build and can include hundreds of private encoder, decoder, demuxer, muxer, and filter options. The capability browser queries the selected local executable rather than presenting a stale, hard-coded catalogue; advanced input, mapping, encoder, filter, muxer, and output fields allow the corresponding flags to be supplied at the appropriate command-line stage.

The automated documentation client was blocked by the Trac wiki's Anubis access challenge during this update. This release therefore links to the wiki and complements it with FFmpeg's official command-line and complete component documentation; it does not claim a verified exhaustive crawl of every Trac wiki subpage.
