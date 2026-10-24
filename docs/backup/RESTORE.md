# FFGrabber source backup and restore points

The complete FFGrabber project is version-controlled in this GitHub repository.

## Snapshot branches

- backup/pre-v0.5.6-2026-10-23 — exact main source state before the v0.5.6 changes, parent commit 0810f352a9f840ec4d16e1cbd0c4151446d1647f.
- backup/ffgrabber-v0.5.6-complete — complete v0.5.6 source snapshot after code, icon and source-guide additions.

The v0.5.6 GitHub Release includes GitHub-generated source archives alongside the x64/ARM64 installers and SHA-256 manifests.

## Source preservation

The six currently available capture/hardware text files fetched from Dropbox are stored unchanged under docs/source-guides/dropbox/. The existing 14 encoding-guide profiles and option index from the previous release remain in the repository. The older text-file objects are not accessible through the current Dropbox reference set, so raw copies of those files are not claimed here.

The generated application icon is source-controlled at src/FFGrabber/Assets/FFGrabber.ico. Main-window source, option index, capture profile definitions, command builder, project metadata, installer definition, build workflow, docs and release notes are all included in Git history.

## Restore

Open the repository on GitHub, choose the desired backup branch from the branch selector, and download its source ZIP. To return to the prior release, use the v0.5.5 release or the pre-change backup branch. To inspect the delta, compare backup/pre-v0.5.6-2026-10-23 with main in GitHub.

FFGrabber remains import-only; no FFmpeg executables are included in source or installer payloads.
