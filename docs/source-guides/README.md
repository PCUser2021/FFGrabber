# Preserved source guides

FFGrabber embeds every `.txt` file in `docs/source-guides/dropbox/` through the wildcard in `src/FFGrabber/FFGrabber.csproj`. The **Source Guide Library** lists and searches the embedded documents and displays their complete source text. It does not prepend editorial text to a source body; a historical note such as the `ffserver.txt` warning is shown separately in the source metadata label.

## Verbatim Dropbox source snapshot manifest

The following **79 original `.txt` files** are preserved in the connected Dropbox root when this integration was performed. Their original filenames are retained under `docs/source-guides/dropbox/`, with no editorial rewrites of their text.

- `AfadeCurves.txt`
- `audio types.txt`
- `AudioChannelManipulation.txt`
- `AudioVolume.txt`
- `Blend.txt`
- `BouncingOverlay.txt`
- `Bump59.txt`
- `Capture_ALSA.txt`
- `Capture_Desktop.txt`
- `Capture_Lightning.txt`
- `Capture_Webcam.txt`
- `ChangingFrameRate.txt`
- `Chroma Subsampling.txt`
- `Cleanup.txt`
- `colorspace.txt`
- `cli.rst.txt` (x265 encoder command-line reference)
- `Concatenate.txt`
- `Create a mosaic out of several input videos using xstack.txt`
- `Create a mosaic out of several input videos.txt`
- `Create a thumbnail image every X seconds of the video.txt`
- `Creating multiple outputs.txt`
- `CroppingVideo.txt`
- `Debug_MacroblocksAndMotionVectors.txt`
- `DenoiseExamples.txt`
- `DirectShow.txt`
- `Eclipse.txt`
- `Encode_AV1.txt`
- `Encode_FFV1.txt`
- `Encode_H.264.txt`
- `Encode_H.265.txt`
- `Encode_HighQualityAudio.txt`
- `Encode_MPEG-4.txt`
- `Encode_VFX.txt`
- `Encode_VP8.txt`
- `Encode_VP9.txt`
- `Encode_YouTube.txt`
- `EncodingForStreamingSites.txt`
- `Errors.txt`
- `ExtractSubtitles.txt`
- `FancyFilteringExamples.txt`
- `FATE.txt`
- `FFmpeg and the SoX Resampler.txt`
- `FFprobeTips.txt`
- `ffserver.txt`
- `FilteringGuide.txt`
- `framemd5 Intro and HowTo.txt`
- `Histogram.txt`
- `How to speed up _ slow down a video.txt`
- `How to take multiple screenshots to an image (tile, mosaic).txt`
- `HowToBurnSubtitlesIntoVideo.txt`
- `HowToCheckIfFaststartIsEnabledForPlayback.txt`
- `HowToConvertSubtitleToASS.txt`
- `HowToConvertYouTubeSBVtoSRT.txt`
- `HowToExtractAndRemoveClosedCaptions.txt`
- `HowToFixTvPlaybackIssues.txt`
- `HowToRemoveAdvertising.txt`
- `HWAccelIntro.txt`
- `Including FFmpeg headers in a C++ application.txt`
- `Limiting the output bitrate.txt`
- `MaintainingFFmpeg.txt`
- `ManagingOtherLanguagesThanEnglish.txt`
- `Map.txt`
- `Null.txt`
- `PHP.txt`
- `Postprocessing.txt`
- `RemapFilter.txt`
- `Scaling.txt`
- `Seeking.txt`
- `Slideshow.txt`
- `Stereoscopic.txt`
- `StreamingGuide.txt`
- `TheoraVorbisEncodingGuide.txt`
- `UnderstandingItsoffset.txt`
- `Using libav_.txt`
- `Vectorscope.txt`
- `ViterbiAlgorithm.txt`
- `Waveform.txt`
- `WaveformMonitor.txt`
- `Xfade.txt`

## Exact-source audit — 2026-10-25

- The Dropbox root inventory was fully enumerated and contained **79 `.txt` files**; the repository manifest contains the same 79 filenames, with no missing or additional source files.
- Each source file was fetched again and compared against `docs/source-guides/dropbox/<original filename>` in this repository. **All 79 of 79 files matched character-for-character**, including `cli.rst.txt` (128,933 characters). No source `.txt` file required modification.
- `src/FFGrabber/FFGrabber.csproj` embeds every file in `docs/source-guides/dropbox/*.txt`. The Source Guide Library exposes the embedded documents for search and displays their full source text. The automated regression test `EmbeddedSourceGuideText_MatchesEveryTrackedTxtFileExactly` verifies that embedded text matches each tracked `.txt` file exactly.

## Reconstructed profile payloads

The separate lower-case companion profile `.txt` files already present in this folder remain clearly identified as reconstructed summaries. They provide editable one-click settings but are not substitutes for the full source-guide texts listed above.

## Historical and platform-specific examples

`ffserver.txt` is preserved as a historical archive only. **ffserver is obsolete and was removed from modern FFmpeg releases.** The GUI identifies this status outside the source text itself and does not offer the file as a runnable one-click workflow.

Source material may include historical syntax, platform-specific devices, external utilities, or options unavailable in a user's imported FFmpeg build. Treat examples as reference material and use the imported executable's current `-h full` output and discovered capability lists to confirm support.
