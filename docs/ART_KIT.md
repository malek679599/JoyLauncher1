# JoyLauncher — Art Kit Specification

## Brand direction

JoyLauncher should feel like a modern console interface: confident, calm, futuristic, and practical. Avoid copying Steam branding, logos, UI assets, or proprietary artwork. The reference is interaction clarity and console-style navigation, not a direct reproduction.

## Created source assets

- `assets/brand/logo/joylauncher-mark.svg` — original JL mark.
- `assets/backgrounds/hero/hero-block.svg` — original geometric block hero art.
- `assets/manifest/ASSET_SOURCES.md` — provenance and usage rules.
- WPF Home hero — editable geometric vector shapes.
- Big Picture hero — optional on-demand Unsplash abstract background, dynamically tinted by the selected app; original WPF geometry remains as the offline fallback.
- Figma Home screen — editable hero block illustration, not a flattened screenshot.

## Remaining brand assets

- Primary JoyLauncher wordmark and complete logo lockups.
- Windows ICO app icon in standard sizes.
- Small navigation glyph set.
- Neutral placeholder covers for apps without icons.
- Empty-state illustration for an unpopulated library.

## Logo concept

An original geometric mark combining a subtle play/launch motif with the letters J and L. It must remain legible at 24 px and work in monochrome. Do not use Minecraft's official logo or other third-party marks as the JoyLauncher brand.

## Art direction

- Deep navy and blue atmospheric gradients.
- Restrained luminous edge highlights.
- Abstract geometric forms, soft depth, and generous negative space.
- No visual noise behind text.
- Avoid overusing neon, glass blur, lens flares, or particle effects.
- Hero artwork should use a wide 16:9 composition with a calm text-safe area on the left.

## App artwork rules

- Prefer app icons supplied by the installed Android app or user-provided artwork.
- Do not scrape or redistribute copyrighted covers as bundled assets.
- Keep fallback tiles consistent and recognizable using app name and category.
- Preserve original aspect ratio; crop only in presentation containers.
- Store asset source, license, and attribution details in an asset manifest.

## Asset directory conventions

```
assets/
  brand/
    logo/
    app-icon/
  backgrounds/
    hero/
    surfaces/
  icons/
    navigation/
    status/
  placeholders/
  manifest/
    ASSET_SOURCES.md
```

## Export targets

- App icon: vector source plus Windows ICO at multiple standard sizes.
- Wordmark: SVG plus transparent PNG.
- Hero: 1920×1080 PNG or WebP.
- UI glyphs: SVG, monochrome, consistent stroke weight.

## Quality checklist

- [x] Original JoyLauncher identity; no Steam logo or copied wordmark.
- [x] Readable over both dark and bright portions of artwork through a dark left-to-right tint.
- [ ] Works in monochrome and at small sizes.
- [x] No unlicensed third-party art bundled in the current asset set; the optional remote Unsplash image is not bundled.
- [x] Source files and asset provenance retained.
- [ ] Image sizes optimized to avoid excessive memory/disk usage.
