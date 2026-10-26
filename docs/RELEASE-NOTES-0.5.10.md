# FFGrabber v0.5.10

## Encoding profiles
- Add the missing selectable **H.265 / HEVC — lossless** profile sourced from `Encode_H.265.txt`.
- Uses `libx265` with `-x265-params lossless=1`; does not require or inject `-crf 0`.
- Copies source audio instead of automatically re-encoding it with a lossy codec.
- Adds an automated regression test verifying that the profile generates the intended encoder arguments without a CRF or target-bitrate override.

## Scope
This release fixes one confirmed gap between the preserved source-guide text and the selectable GUI profile catalogue. The 79 original source documents remain embedded verbatim; that fact alone should not be interpreted as proof that every example from every guide has already been converted into a dedicated one-click profile. Platform-specific, historical, and explanatory examples still need case-by-case mapping where a runnable workflow is appropriate.

## Requirements and limitations
The imported FFmpeg build must contain `libx265`. Lossless encoding can produce very large files and does not preserve the original compressed video bitstream; it encodes frames losslessly through the selected pixel format/encoder path. Review the generated command and test on a short sample before long jobs.

FFmpeg and MediaInfo remain user-imported and are not bundled.
