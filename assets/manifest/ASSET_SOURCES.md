# Asset sources and provenance

The original vector files in this directory are JoyLauncher artwork. The Big Picture hero may also load one optional third-party image at runtime; it is not bundled into the installer.

| Asset | Purpose | Source | License / attribution |
|---|---|---|---|
| `assets/brand/logo/joylauncher-mark.svg` | JL brand mark | Original project vector | Original JoyLauncher artwork; no third-party asset |
| `assets/backgrounds/hero/hero-block.svg` | Original geometric hero artwork / fallback reference | Original project vector | Original JoyLauncher artwork; no third-party asset |
| Big Picture remote hero image | Optional cinematic background, tinted per selected app | [Unsplash photo by Mansy Graphics](https://unsplash.com/photos/dark-blue-abstract-background-with-soft-shapes-PTmF033naYc) | Free to use under the [Unsplash License](https://unsplash.com/license); loaded on demand, not bundled |

## Rules

- Do not bundle third-party app icons, game logos, screenshots, or promotional art without permission.
- Prefer icons exposed by the installed Android application at runtime.
- Keep SVG source files alongside any exported PNG/ICO versions.
- Record the source and license before adding any externally sourced asset.
- Remote artwork must be optional, loaded lazily, and backed by an offline-safe local presentation.
