# JoyLauncher 0.1.0-preview.1

This is an early Windows x64 preview release of JoyLauncher.

## Included

- Self-contained Windows x64 WPF launcher built with .NET 10.
- Home, Library, Games, Entertainment, Search, Store discovery, and Settings pages.
- BlueStacks 5 discovery, ADB package discovery, and Android app launch requests.
- Local favorites, pinned apps, recent apps, five app-library sort modes, category filters, and library search.
- Publisher favicon lookup for supported apps, with text initials as the offline fallback.
- Keyboard navigation and XInput controller navigation.
- Portable ZIP and per-user Windows installer.

## Requirements and known limitations

- Windows 10/11 x64.
- BlueStacks 5 is installed separately. JoyLauncher does not include Android, BlueStacks, or game/app files.
- ADB must be enabled manually in BlueStacks settings before app discovery.
- Embedded BlueStacks hosting is experimental and must be validated against the installed BlueStacks build. Standalone BlueStacks remains the fallback.
- JoyLauncher binaries are not code-signed. Windows may show an unrecognized-publisher or reputation warning. Do not bypass Windows security warnings; use the portable archive or wait for a signed release if publisher verification is required.
- Friends/social activity and the Store are not connected features in this preview.
- Publisher website favicons need an internet connection; initials remain available when no icon can be loaded.

## Installation

Use `JoyLauncher-Setup-0.1.0-preview.1-win-x64.exe` for a per-user installation without administrator rights, or extract the portable ZIP and run `JoyLauncher.exe`.

The installer creates a Start Menu shortcut. A desktop shortcut is optional and off by default.

## Data and privacy

Favorites, pins, recents, and sort preference are stored locally under `%LOCALAPPDATA%\JoyLauncher\settings.json`. Crash reports are stored locally under `%LOCALAPPDATA%\JoyLauncher\Logs`. Neither is uploaded by JoyLauncher.
