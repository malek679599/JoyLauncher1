# JoyLauncher Windows release process

## Current preview candidate

The application version is defined in `src/JoyLauncher.App/JoyLauncher.App.csproj`. The current preview version is `0.1.0-preview.1`.

Every push to `main` builds a versioned package bundle and uploads it as the `JoyLauncher-Release-Windows-x64` Actions artifact. The artifact includes:

- `JoyLauncher-Setup-<version>-win-x64.exe`: per-user installer, no administrator rights required.
- `JoyLauncher-Portable-<version>-win-x64.zip`: self-contained portable app.
- `BUILD-INFO.txt`: version, source commit, workflow run, platform, and signing state.
- `RELEASE_NOTES.md`: features, requirements, known limitations, and security notes.
- `SHA256SUMS.txt`: hashes for each release output file.

Workflow verification includes Release build, automated tests, an 8-second startup smoke test, pinned installer-compiler hash and signature verification, installer compile, silent install and uninstall, and release-output checksum verification.

## Download a preview package

1. Open [Windows build workflow runs](https://github.com/malek679599/JoyLauncher1/actions/workflows/windows-build.yml).
2. Open the latest run with a successful conclusion.
3. In the Artifacts section, download `JoyLauncher-Release-Windows-x64`.
4. Verify the downloaded files against `SHA256SUMS.txt` before running an installer or portable executable.

The hosted Actions artifact expires after 30 days. A successful `main` build does not create a public GitHub Release.

## Publish a tagged GitHub Release

Only create a version tag after the preview candidate has passed the target-machine release gate in `docs/TEST_PLAN.md`. Version tags must match the `v<version>` format and the app's project version.

Example for a future approved preview tag:

```powershell
git tag v0.1.0-preview.1
git push origin v0.1.0-preview.1
```

The workflow extracts the version from the tag, rebuilds and re-runs all checks, then creates a GitHub Release containing the installer, portable ZIP, build info, release notes, and checksum manifest. Versions containing a hyphen are published as prereleases. Stable tags should be used only after all target-machine gates pass.

## Signing and safety

JoyLauncher is currently unsigned. Do not disable SmartScreen, antivirus, or other Windows protections to install it. A signed release requires a separately configured code-signing certificate and a protected signing workflow; neither is configured by this project.

BlueStacks 5 is a separate prerequisite and is not bundled, downloaded, configured, or modified by the installer. User preferences and crash reports remain on the local machine.
