# FFGrabber v0.5.8 — development candidate

## Reliability and performance
- Add a live FFmpeg progress display with elapsed/total media time and stage reporting (including two-pass jobs).
- Display FFmpeg process CPU share, system CPU, working-set/private memory, thread count, and system RAM usage while a job is running.
- Add a software encoder/filter thread budget selector with reduced, balanced, and maximum modes.
- Make cancellation terminate the FFmpeg process tree and remove new output-related temporary files and partial final output created by the canceled job, while protecting files that existed before the job.
- Bound ffplay's initial display to a 1280×720 envelope by forcing only the limiting dimension; let ffplay calculate the other dimension using the media's default display aspect ratio.

## MediaInfo and update checks
- Add user-imported MediaInfo CLI support for JSON technical reports and copy-to-clipboard.
- Check FFGrabber and an imported MediaInfo CLI against official GitHub Releases metadata on startup, no more than once every 24 hours; updates open official release pages and are not silently installed.

## Windows integration and appearance
- Add current-user File Explorer context-menu verbs for convert, remux/stream copy, video-only extraction, audio extraction, play, and MediaInfo inspection.
- Use the Windows 11 blue four-pane mark as the consistent app/control branding and installer icon; soften the light theme's contrast.
- Correct the product tagline to “Fluent Media Workspace”.

## Source-guide library
- Embed `cli.rst.txt` verbatim from the connected Dropbox root (128,933 characters); the Source Guide Library embeds it through the existing `.txt` resource wildcard.
- Extend the tracked guide manifest and retain the other source documents unchanged.

## Packaging
- Windows x64 and ARM64 remain the only supported installer targets. No Linux build or Linux installer is included.
- FFmpeg and MediaInfo remain external user-provided tools and are not bundled in the application package.
- This release candidate must pass CI build/tests and installer validation before publication.
