# Capture and hardware-acceleration controls

This document explains how the main window maps the capture/hardware reference notes into FFmpeg command options.

## Windows desktop capture

- **GDI desktop capture** applies the input sequence -f gdigrab -framerate 30 -i desktop. Region capture uses -offset_x, -offset_y, -video_size and optionally -show_region 1.
- **DirectShow capture** uses -f dshow and an expression such as video=Integrated Camera or video=Integrated Camera:audio=Microphone. Click **List DirectShow devices** and replace the examples with exact device names.
- **Raw pixel / MJPEG selection** is device-specific. Use the device's reported modes; do not assume every camera supports a given frame rate, size or input format.
- **DDAgrab and gfxcapture** are represented by the source-filter field. Examples include ddagrab=0 or gfxcapture=monitor_idx=1:width=1920:height=1080:resize_mode=scale_aspect:output_fmt=10bit. These graphs are inserted into filter_complex without inventing a primary input file. Filter availability varies by FFmpeg build.

## Hardware acceleration

- Decoder API maps to -hwaccel; device selection maps to -hwaccel_device; decoded hardware frames map to -hwaccel_output_format.
- Initialize a named hardware device with -init_hw_device; route hardware-aware filters with -filter_hw_device.
- Extra hardware frames use -extra_hw_frames. Backend, memory context, codec and filter graph must agree; a hardware decode flag alone does not guarantee zero-copy processing.
- Benchmark adds -benchmark; timestamp preservation adds -copyts; automatic output scaling can be disabled with -noautoscale.

## Audio and platform-specific controls

Capture channels and sample rate map to -channels and -sample_rate as shown in the ALSA guide; these apply only when the selected input device supports them. The camera index field maps to -video_device_index for AVFoundation-style workflows and is not a DirectShow device selector. The -rtbufsize and -audio_buffer_size controls are device settings; large buffers consume memory, while very small buffers may cause instability or dropped samples.

## Diagnostics and non-runtime references

The Windows button invokes ffmpeg -hide_banner -list_devices true -f dshow -i dummy. Build-configure flags such as --enable-ffnvcodec are reference-only. Capture_Lightning.txt explicitly says FFmpeg has no -delay option; external-buffering syntax in that file is retained as source material and is not treated as FFmpeg command input.

The six currently available capture/hardware source documents are preserved verbatim under docs/source-guides/dropbox. The previous release's 14 encoder guide profiles/index remain in the app, but the original text files could not be retrieved from the current Dropbox reference set, so no raw-file backup is claimed for them.
