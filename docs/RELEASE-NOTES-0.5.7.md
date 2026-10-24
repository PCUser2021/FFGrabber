# FFGrabber v0.5.7

**Release date:** October 24, 2026

## Source-guide library

- Integrated the 78 original .txt documents found in the connected Dropbox source set, keeping their filenames and source wording intact.
- Embedded all source documents at build time; the searchable Source Guide Library displays the complete document body without adding editorial text to it.
- Preserved the separate reconstructed one-click profile text files and explicitly documented that they are summaries rather than replacements for original guides.
- Added a regression test that compares each tracked source .txt file to its embedded resource character for character.
- Moved the obsolete ffserver.txt archive warning into separate GUI metadata so the warning no longer alters the historical source text.

## Setup downloads

- **Windows x64 (Intel/AMD):** FFGrabber-Setup-x64-v0.5.7.exe
- **Windows ARM64:** FFGrabber-Setup-arm64-v0.5.7.exe
- **SHA-256 checksums:** the per-installer .sha256 files and combined SHA256SUMS.txt are attached to this release.

## Verification

The release workflow runs the automated tests, builds both architecture-specific installers, checks the executable PE architecture, rejects bundled FFmpeg executables, and generates SHA-256 checksums. FFGrabber remains an import-only frontend: FFmpeg is not shipped with the app and must be imported separately by the user.

Historical guides may contain obsolete examples or options unavailable in a current FFmpeg build. The imported executable's runtime help and discovered capabilities remain authoritative.
