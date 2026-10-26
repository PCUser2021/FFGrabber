# Security policy

## Supported versions
Only the latest release receives security fixes.

## Reporting a vulnerability
Please report vulnerabilities privately through GitHub's **Security → Report a vulnerability**
form on this repository rather than a public issue. Include the FFGrabber version, Windows
version, and steps to reproduce. You can expect an acknowledgement within a few days.

## Security design notes
- FFGrabber bundles no FFmpeg binaries; it runs only the tool set the user explicitly imports.
- Commands are executed with `ProcessStartInfo.ArgumentList` (no shell, no `cmd.exe`).
- MediaInfo is auto-discovered only in Program Files; PATH and the application folder are not searched.
- The update checker only reads GitHub release metadata and only opens `https://github.com` release pages.
- Session logs and support bundles mask URL passwords, tokens, stream keys and authorization
  headers on a best-effort basis. Review a support ZIP before sharing it.
