# FFGrabber v0.5.9 — current source snapshot

## Release notes

This release republishes the current audited `main` source snapshot using consistent v0.5.9 version metadata for Windows x64 and ARM64 installers.

## Source-guide audit

- All 79 original Dropbox `.txt` source guides were re-fetched and compared with the tracked source files; all 79 matched character-for-character.
- `cli.rst.txt` is preserved at 128,933 characters.
- The application embeds the tracked source guides and the automated regression test checks that embedded text matches the tracked source text exactly.

## Runtime changes

No new runtime behavior is claimed for this packaging/documentation follow-up compared with v0.5.8. FFGrabber remains import-only: FFmpeg/FFprobe/FFplay and MediaInfo are not bundled.

## Downloads and verification

The release workflow builds Windows x64 and ARM64 installers, generates individual SHA-256 files, and publishes a combined `SHA256SUMS.txt` manifest only after successful build/test and architecture checks.
