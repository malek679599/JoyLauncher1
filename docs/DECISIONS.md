# JoyLauncher — Decision Log

## 2026-11-14 — Initial decisions

### Windows WPF rather than a new Android runtime
JoyLauncher is a Windows launcher shell that controls an existing BlueStacks 5 installation. It is not an emulator.

### C# / WPF / .NET 10 LTS
WPF provides a native Windows desktop UI and .NET 10 is the chosen LTS target. Confirm SDK availability on the target machine before implementation.

### BlueStacks integration through a discovery service
No single executable path, ADB port, or package ID will be assumed. Runtime detection and user-configurable fallbacks are required.

### Minecraft is featured, not exclusive
The app catalogue must remain general-purpose and support all discoverable installed Android apps.

### Free-first production
No paid APIs, subscriptions, or paid art assets. Free service quotas may have limits and can change.

### Honest validation
Code generation and repository commits do not prove the app builds or works. Windows and BlueStacks tests must be run on the target machine.
