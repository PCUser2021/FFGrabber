# Preserved source guides

The app embeds every `.txt` file in this directory at build time and the in-app **Source Guide Library** displays and searches each complete embedded document.

## Complete source snapshots available in this repository

- Capture and hardware references: `DirectShow.txt`, `HWAccelIntro.txt`, `Capture_ALSA.txt`, `Capture_Desktop.txt`, `Capture_Webcam.txt` and `Capture_Lightning.txt`.
- Streaming references recovered from the connected Dropbox: `EncodingForStreamingSites.txt` and `StreamingGuide.txt`. These are full text snapshots, not the shorter reconstructed profile payloads.
- `ffserver.txt` is preserved as a historical archive only. **ffserver is obsolete and was removed from modern FFmpeg releases.** Its config is searchable in the library but it is not offered as a one-click workflow and should not be treated as current operating guidance.

These complete sources are research inputs for the searchable settings index and profiles. Historical examples may use Linux/macOS devices, removed options, old FFmpeg syntax, or external utilities. The UI marks `ffserver.txt` as archival. For current support, the imported executable's own `-h full` catalogue and discovered capability lists remain authoritative.

## Reconstructed encoder profile payloads

The 14 lower-case files (for example, `h264_master_guide.txt` and `streaming_live_delivery.txt`) preserve the supplied recommended-flags line, application stage and description. They are **reconstructed summaries, not verbatim copies of the missing original historical encoder-guide documents**. Each has a matching one-click profile in the workspace.

The full streaming guides also link to three additional workflows: file-to-RTMP (uses `-re` to pace file input), live-input-to-RTMP (does not use `-re`), and RTMP plus a local recording via the tee muxer. These profiles expose their settings in the ordinary editable controls; replace example inputs/outputs, inspect the generated command and verify the imported FFmpeg build's capabilities before queueing a job.

The full encoder source documents referenced in `EncodingGuideOptionIndex.cs` that are not listed among the complete snapshots above have not been recovered into this repository. Their curated option index and the 14 supplied profile summaries remain available, but that is not equivalent to recovering those original full texts.
