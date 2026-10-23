#!/usr/bin/env bash
set -euo pipefail

# Build a redistributable, feature-rich Windows FFmpeg GPL package from the exact
# FFmpeg 9.0.2 upstream source tag referenced by GyanD/codexffmpeg release 9.0.2.
# The GyanD/codexffmpeg repository is a release mirror/support site, not the FFmpeg
# source tree. Its 9.0.2 release references the official upstream FFmpeg n9.0.2 tag.
#
# Usage (Linux or WSL2 with Docker and Git installed):
#   ./scripts/Build-FFmpegFromSource.sh x64 ./out/ffmpeg-x64
#   ./scripts/Build-FFmpegFromSource.sh arm64 ./out/ffmpeg-arm64
#
# The GPL profile includes the builder's available compatible external libraries.
# Nonfree components are deliberately excluded: FFmpeg documents that --enable-nonfree
# produces a binary that is not redistributable.
FFMPEG_TAG="n9.0.2"
FFMPEG_COMMIT="946fcce07b6dcd0331c8cc609192aeff5e1924f8"
GYAN_MIRROR_URL="https://github.com/GyanD/codexffmpeg/archive/refs/tags/9.0.2.zip"
FFMPEG_SOURCE_URL="https://github.com/FFmpeg/FFmpeg.git"
BUILDER_REPOSITORY="https://github.com/BtbN/FFmpeg-Builds.git"
BUILDER_COMMIT="937868360a21d711ec94a33f5591d345ad9b7754"

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <x64|arm64> <output-directory>" >&2
    exit 2
fi

ARCH="$1"
OUTPUT_DIR="$(realpath -m "$2")"
case "$ARCH" in
    x64) TARGET="win64"; EXPECTED_ARCH_REGEX='x86-64|x86_64' ;;
    arm64) TARGET="winarm64"; EXPECTED_ARCH_REGEX='Aarch64|aarch64|ARM aarch64' ;;
    *) echo "Unsupported architecture '$ARCH' (use x64 or arm64)." >&2; exit 2 ;;
esac

for tool in git docker curl unzip file sha256sum; do
    command -v "$tool" >/dev/null 2>&1 || { echo "Required tool not found: $tool" >&2; exit 127; }
done
docker info >/dev/null 2>&1 || { echo "Docker daemon is not available." >&2; exit 1; }

WORK_DIR="$(mktemp -d "${TMPDIR:-/tmp}/ffgrabber-ffmpeg-source.XXXXXX")"
trap 'rm -rf "$WORK_DIR"' EXIT
mkdir -p "$OUTPUT_DIR"
find "$OUTPUT_DIR" -mindepth 1 -maxdepth 1 -exec rm -rf -- {} +

echo "Checking the GyanD/codexffmpeg 9.0.2 source archive reference..."
curl --fail --location --retry 3 --retry-all-errors --silent --show-error \
    "$GYAN_MIRROR_URL" -o "$WORK_DIR/gyan-codexffmpeg-9.0.2.zip"
unzip -Z1 "$WORK_DIR/gyan-codexffmpeg-9.0.2.zip" > "$WORK_DIR/gyan-archive-contents.txt"
grep -Eq '(^|/)README\.md$' "$WORK_DIR/gyan-archive-contents.txt"
if grep -Eq '(^|/)configure$|(^|/)libavcodec/|(^|/)libavformat/' "$WORK_DIR/gyan-archive-contents.txt"; then
    echo "The Gyan archive unexpectedly contains FFmpeg source files; review its contents." >&2
    exit 1
fi
echo "Confirmed: the Gyan tag archive is the mirror/support repository; compile the upstream source it references."

echo "Verifying FFmpeg source tag $FFMPEG_TAG resolves to $FFMPEG_COMMIT..."
git clone --filter=blob:none --depth 1 --branch "$FFMPEG_TAG" "$FFMPEG_SOURCE_URL" "$WORK_DIR/ffmpeg-source-check"
actual_commit="$(git -C "$WORK_DIR/ffmpeg-source-check" rev-parse HEAD)"
if [[ "$actual_commit" != "$FFMPEG_COMMIT" ]]; then
    echo "FFmpeg source mismatch: expected $FFMPEG_COMMIT, got $actual_commit" >&2
    exit 1
fi

echo "Fetching pinned BtbN build tooling at $BUILDER_COMMIT..."
git clone --filter=blob:none "$BUILDER_REPOSITORY" "$WORK_DIR/ffmpeg-builder"
git -C "$WORK_DIR/ffmpeg-builder" checkout --detach "$BUILDER_COMMIT"

# The builder normally targets its own CI repository's GHCR package namespace.
# Use the matching public upstream images as build environments.
sed -i 's#REPO="${GITHUB_REPOSITORY:-btbn/ffmpeg-builds}"#REPO="${FFBUILDS_IMAGE_REPO:-btbn/ffmpeg-builds}"#' \
    "$WORK_DIR/ffmpeg-builder/util/vars.sh"
grep -Fq 'REPO="${FFBUILDS_IMAGE_REPO:-btbn/ffmpeg-builds}"' "$WORK_DIR/ffmpeg-builder/util/vars.sh"

mkdir -p "$WORK_DIR/ffmpeg-builder/artifacts"
(
    cd "$WORK_DIR/ffmpeg-builder"
    export GIT_BRANCH_OVERRIDE="$FFMPEG_TAG"
    export FFMPEG_REPO_OVERRIDE="$FFMPEG_SOURCE_URL"
    export FFBUILDS_IMAGE_REPO="btbn/ffmpeg-builds"
    echo "Building target=$TARGET, variant=gpl, source=$FFMPEG_TAG ($FFMPEG_COMMIT)"
    ./build.sh "$TARGET" gpl 9.0
)

package_zip="$(find "$WORK_DIR/ffmpeg-builder/artifacts" -maxdepth 1 -type f -name '*.zip' -print -quit)"
if [[ -z "$package_zip" ]]; then
    echo "BtbN build completed without producing a Windows package ZIP." >&2
    find "$WORK_DIR/ffmpeg-builder/artifacts" -maxdepth 2 -type f -print >&2 || true
    exit 1
fi

mkdir -p "$WORK_DIR/package"
unzip -q "$package_zip" -d "$WORK_DIR/package"
ffmpeg_path="$(find "$WORK_DIR/package" -type f -iname 'ffmpeg.exe' -print -quit)"
if [[ -z "$ffmpeg_path" ]]; then
    echo "Build archive does not contain ffmpeg.exe: $package_zip" >&2
    exit 1
fi
package_root="$(dirname "$(dirname "$ffmpeg_path")")"
for tool in ffmpeg.exe ffprobe.exe ffplay.exe; do
    [[ -f "$package_root/bin/$tool" ]] || { echo "Build is missing $tool" >&2; exit 1; }
done

# Keep packaged tools, presets, documentation and license notices.
cp -a "$package_root"/. "$OUTPUT_DIR"/
for tool in ffmpeg.exe ffprobe.exe ffplay.exe; do
    file_output="$(file "$OUTPUT_DIR/bin/$tool")"
    echo "$tool: $file_output"
    if ! grep -Eiq "$EXPECTED_ARCH_REGEX" <<<"$file_output"; then
        echo "$tool has the wrong PE architecture for $ARCH: $file_output" >&2
        exit 1
    fi
done

cat > "$OUTPUT_DIR/ffmpeg-build.json" <<JSON
{
  "source_repository": "FFmpeg/FFmpeg",
  "source_tag": "$FFMPEG_TAG",
  "source_commit": "$FFMPEG_COMMIT",
  "source_tag_verified": true,
  "source_tag_url": "https://github.com/FFmpeg/FFmpeg/tree/$FFMPEG_TAG",
  "gyan_codexffmpeg_reference": "$GYAN_MIRROR_URL",
  "stable_version": "9.0.2",
  "architecture": "$ARCH",
  "build_method": "source",
  "builder_repository": "BtbN/FFmpeg-Builds",
  "builder_commit": "$BUILDER_COMMIT",
  "variant": "gpl",
  "compile_profile": "static GPL/version3 dependency profile; nonfree components excluded",
  "redistribution_note": "This build intentionally omits --enable-nonfree. Actual encoder/filter/protocol availability is reported by the installed binary.",
  "built_utc": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
JSON

(
    cd "$OUTPUT_DIR"
    find . -type f ! -name 'SHA256SUMS.txt' -print0 |
        sort -z |
        xargs -0 sha256sum > SHA256SUMS.txt
)

echo "Source-built FFmpeg package staged at: $OUTPUT_DIR"
echo "Provenance: $OUTPUT_DIR/ffmpeg-build.json"
echo "Checksums:  $OUTPUT_DIR/SHA256SUMS.txt"
