using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Windows.Threading;
using JoyLauncher.App.Services;

namespace JoyLauncher.App;

public partial class MainWindow : Window
{
    private static readonly string CrashLogWriterDirectory = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "JoyLauncher",
        "Logs");

    private readonly BlueStacksDiscoveryService _blueStacksDiscovery = new();
    private readonly AdbClient _adbClient = new();
    private readonly WebsiteLogoService _websiteLogoService = new();
    private readonly LocalPreferencesStore _preferencesStore = new();
    private readonly XInputGamepadService _gamepadService = new();
    private LocalPreferences _preferences;
    private BlueStacksDiscoveryResult? _discoveryResult;
    private IReadOnlyList<string> _installedPackages = Array.Empty<string>();
    private IReadOnlyList<string> _launchablePackages = Array.Empty<string>();
    private bool _launchabilityDetectionAvailable;
    private string? _launchabilityDetectionError;
    private string? _connectedDeviceSerial;
    private string? _adbError;
    private string? _windowEmbeddingDiagnostic;
    private string? _windowDiscoveryDiagnostic;
    private bool _isFullscreen;
    private bool _isBigPictureMode;
    private bool _wasBigPictureBeforePlay;
    private bool _bigPictureOwnedFullscreen;
    private DispatcherTimer? _clockTimer;
    private string _bigPictureActiveTab = "WHAT'S NEW";
    private WindowStyle _previousWindowStyle;
    private ResizeMode _previousResizeMode;

    private static readonly IReadOnlyList<AppEntry> SampleApps =
    [
        new("Minecraft", "Games", "Featured sandbox game", PackageName: "com.mojang.minecraftpe"),
        new("YouTube", "Entertainment", "Video and creator content", PackageName: "com.google.android.youtube"),
        new("YouTube Kids", "Entertainment", "Family-focused video app", PackageName: "com.google.android.apps.youtube.kids"),
        new("Play Store", "Store", "Android app marketplace", PackageName: "com.android.vending"),
        new("Chrome", "Productivity", "Web browser", PackageName: "com.android.chrome"),
        new("Files", "System", "File manager"),
        new("Camera", "System", "Camera app"),
        new("Settings", "System", "Android settings")
    ];

    private static readonly IReadOnlyDictionary<string, (string Name, string Category, string Description)> KnownPackages =
        new Dictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["com.mojang.minecraftpe"] = ("Minecraft", "Games", "Minecraft for Android"),
            ["com.google.android.youtube"] = ("YouTube", "Entertainment", "Video and creator content"),
            ["com.google.android.apps.youtube.kids"] = ("YouTube Kids", "Entertainment", "Family-focused video app"),
            ["com.google.android.apps.youtube.music"] = ("YouTube Music", "Entertainment", "Music and audio"),
            ["com.google.android.videos"] = ("Google TV", "Entertainment", "Movies and television"),
            ["com.android.chrome"] = ("Chrome", "Productivity", "Web browser"),
            ["com.google.android.gm"] = ("Gmail", "Productivity", "Email"),
            ["com.google.android.apps.maps"] = ("Google Maps", "Productivity", "Maps and navigation"),
            ["com.google.android.apps.photos"] = ("Google Photos", "Productivity", "Photo library"),
            ["com.android.vending"] = ("Google Play Store", "Store", "Android app marketplace"),
            ["com.android.settings"] = ("Android Settings", "System", "Android system settings"),
            ["com.google.android.googlequicksearchbox"] = ("Google", "Productivity", "Search and assistant")
        };

    public MainWindow()
    {
        InitializeComponent();
        _preferences = _preferencesStore.Load();
        LocationChanged += (_, _) => BlueStacksWindowHost.RefreshDockedPosition();
        SizeChanged += (_, _) => BlueStacksWindowHost.RefreshDockedPosition();
        StateChanged += (_, _) => BlueStacksWindowHost.RefreshDockedPosition();
        Activated += (_, _) => BlueStacksWindowHost.RefreshDockedPosition();
        RuntimeStatusText.Text = "Waiting for detection";
        BigPictureClock.Text = DateTime.Now.ToString("h:mm tt");
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _clockTimer.Tick += (_, _) => BigPictureClock.Text = DateTime.Now.ToString("h:mm tt");
        _clockTimer.Start();
        UpdateHomeAppTiles();
        _gamepadService.ButtonPressed += Gamepad_ButtonPressed;
        _gamepadService.ConnectionChanged += Gamepad_ConnectionChanged;
        _gamepadService.Start();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        BlueStacksWindowHost.Detach();
        _clockTimer?.Stop();
        _gamepadService.Dispose();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshRuntimeStatusAsync();
    }

    private async Task RefreshRuntimeStatusAsync()
    {
        RuntimeStatusText.Text = "Scanning BlueStacks…";
        _installedPackages = Array.Empty<string>();
        _launchablePackages = Array.Empty<string>();
        _launchabilityDetectionAvailable = false;
        _launchabilityDetectionError = null;
        _connectedDeviceSerial = null;
        _adbError = null;
        try
        {
            _discoveryResult = await Task.Run(_blueStacksDiscovery.Discover);
            RuntimeStatusText.Text = _discoveryResult switch
            {
                { PlayerFound: true, HasConfiguredAdbEndpoint: true } => "Installed · ADB config found",
                { PlayerFound: true } => "Installed · enable ADB",
                { PlayerRunning: true } => "Player running · path unavailable",
                _ => "BlueStacks not detected"
            };
        }
        catch (Exception)
        {
            RuntimeStatusText.Text = "Detection unavailable";
        }

        UpdateFeaturedMinecraftState();
        UpdateHomeAppTiles();
        if (DynamicPageContent.Visibility == Visibility.Visible)
            DynamicPageContent.Content = BuildPage(PageTitle.Text);
    }

    private async void RefreshDetection_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRuntimeStatusAsync();
    }

    private async void ConnectAndScan_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: BlueStacksAdbEndpoint endpoint })
            return;

        var adbPath = _discoveryResult?.AdbPath;
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            _adbError = "The discovered ADB executable is missing. Refresh local detection first.";
            DynamicPageContent.Content = BuildSettingsPage();
            return;
        }

        RuntimeStatusText.Text = $"Connecting to {endpoint.InstanceName}…";
        _adbError = null;
        var scan = await _adbClient.ConnectAndListPackagesAsync(adbPath, endpoint);

        if (!scan.Success)
        {
            _connectedDeviceSerial = null;
            _installedPackages = Array.Empty<string>();
            _launchablePackages = Array.Empty<string>();
            _launchabilityDetectionAvailable = false;
            _launchabilityDetectionError = null;
            _adbError = scan.Error ?? "ADB connection failed for an unknown reason.";
            RuntimeStatusText.Text = "ADB connection failed";
        }
        else
        {
            _connectedDeviceSerial = scan.DeviceSerial;
            _installedPackages = scan.PackageNames;
            _launchablePackages = scan.LaunchablePackageNames;
            _launchabilityDetectionAvailable = scan.LaunchabilityDetectionAvailable;
            _launchabilityDetectionError = scan.LaunchabilityDetectionError;
            RuntimeStatusText.Text = scan.LaunchabilityDetectionAvailable
                ? $"ADB connected · {_launchablePackages.Count} launchable apps"
                : $"ADB connected · launch scan unavailable · {_installedPackages.Count} packages";
        }

        UpdateFeaturedMinecraftState();
        UpdateHomeAppTiles();
        DynamicPageContent.Content = PageTitle.Text switch
        {
            "Library" => BuildLibraryPage(),
            "Settings" => BuildSettingsPage(),
            "Search" => BuildSearchPage(),
            _ => DynamicPageContent.Content
        };
    }

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string page)
            return;

        if (PlayPage.Visibility == Visibility.Visible)
            ReturnFromPlayView(navigateHome: false);

        PageTitle.Text = page;
        PageSubtitle.Text = page switch
        {
            "Home" => "Everything you play and watch, in one place.",
            "Library" => "Browse the Android apps in your collection.",
            "Games" => "Your Android games, grouped in one place.",
            "Entertainment" => "Video and entertainment apps.",
            "Search" => "Find an app by name or category.",
            "Store" => "Discoverability is planned; no external store is connected yet.",
            "Settings" => "Runtime discovery, diagnostics, and local preferences.",
            _ => "Your Android hub."
        };

        foreach (var item in FindVisualChildren<Button>(this))
        {
            if (item.Tag is not string tag ||
                tag is not ("Home" or "Library" or "Games" or "Entertainment" or "Search" or "Store" or "Settings"))
                continue;

            item.Background = tag == page
                ? (Brush)FindResource("AccentSoftBrush")
                : (Brush)FindResource("SurfaceBrush");
        }

        var isHome = page == "Home";
        HomeContent.Visibility = isHome ? Visibility.Visible : Visibility.Collapsed;
        DynamicPageContent.Visibility = isHome ? Visibility.Collapsed : Visibility.Visible;
        DynamicPageContent.Content = isHome ? null : BuildPage(page);
    }

    private FrameworkElement BuildPage(string page) => page switch
    {
        "Library" => BuildLibraryPage(),
        "Games" => BuildCategoryPage("Games", "Your installed Android games.", "Games"),
        "Entertainment" => BuildCategoryPage("Entertainment", "Your installed video and entertainment apps.", "Entertainment"),
        "Search" => BuildSearchPage(),
        "Store" => BuildStorePage(),
        "Settings" => BuildSettingsPage(),
        _ => BuildLibraryPage()
    };

    private FrameworkElement BuildLibraryPage()
    {
        var root = CreatePageStack();
        var liveApps = GetLiveApps();
        var hasLiveApps = _connectedDeviceSerial is not null;
        var catalogue = hasLiveApps ? liveApps : SampleApps;

        AddIntro(root, "Your apps", hasLiveApps
            ? _launchabilityDetectionAvailable
                ? $"Launchable apps from {_connectedDeviceSerial}. Packages without a MAIN/LAUNCHER activity are filtered out."
                : $"Installed packages from {_connectedDeviceSerial}. Launcher-activity discovery is unavailable, so this is a compatibility fallback."
            : "These preview entries demonstrate the layout. They are not yet verified as installed.");

        var filters = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 18)
        };
        var grid = new WrapPanel();

        void Show(string category)
        {
            grid.Children.Clear();
            IEnumerable<AppEntry> items = catalogue;
            if (category == "Favorites")
                items = catalogue.Where(IsFavorite);
            else if (category == "Pinned")
                items = catalogue.Where(IsPinned);
            else if (category == "Recently launched")
                items = catalogue.Where(app => RecentIndex(app.PackageName) != int.MaxValue)
                    .OrderBy(app => RecentIndex(app.PackageName));
            else if (category != "All apps")
                items = catalogue.Where(app => app.Category == category);

            foreach (var app in items)
                grid.Children.Add(CreateAppCard(app));
        }

        var filterLabels = hasLiveApps
            ? new[] { "All apps", "Favorites", "Pinned", "Recently launched" }
            : new[] { "All apps", "Games", "Entertainment", "Productivity", "System" };
        foreach (var label in filterLabels)
        {
            var filter = CreateFilterButton(label);
            filter.Click += (_, _) => Show(label);
            filters.Children.Add(filter);
        }

        root.Children.Add(filters);
        root.Children.Add(grid);
        Show("All apps");
        root.Children.Add(CreateNotice(
            hasLiveApps ? "Verified package data" : "Preview data only",
            hasLiveApps
                ? _launchabilityDetectionAvailable
                    ? $"{liveApps.Count} apps have a verified Android MAIN/LAUNCHER activity."
                    : $"{liveApps.Count} package entries are shown because launcher-activity discovery is unavailable."
                : "Connect to a discovered BlueStacks ADB endpoint from Settings to replace these sample cards with real package data."));
        return root;
    }

    private FrameworkElement BuildCategoryPage(string title, string description, string category)
    {
        var root = CreatePageStack();
        var hasLiveApps = _connectedDeviceSerial is not null;
        var apps = hasLiveApps
            ? GetLiveApps().Where(app => app.Category == category).ToArray()
            : SampleApps.Where(app => app.Category == category).ToArray();
        AddIntro(root, title, hasLiveApps
            ? $"{apps.Length} matching launchable apps from the connected BlueStacks instance."
            : description);
        var grid = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        foreach (var app in apps)
            grid.Children.Add(CreateAppCard(app));

        if (grid.Children.Count == 0)
            root.Children.Add(CreateNotice(
                hasLiveApps ? "No matching installed apps" : "No sample entries",
                hasLiveApps
                    ? "No installed packages have been identified in this category yet."
                    : "This category has no preview entries yet."));
        else
            root.Children.Add(grid);

        root.Children.Add(CreateNotice(
            hasLiveApps ? "Live package data" : "Preview data only",
            hasLiveApps
                ? "Cards represent package identifiers returned by the connected Android runtime."
                : "Connect BlueStacks ADB to replace preview cards with your installed apps."));
        return root;
    }

    private FrameworkElement BuildSearchPage()
    {
        var root = CreatePageStack();
        var searchCatalogue = _connectedDeviceSerial is not null ? GetLiveApps() : SampleApps;
        AddIntro(root, "Search your apps", _connectedDeviceSerial is not null
            ? "Search friendly app names, categories, or package identifiers."
            : "Search the sample catalogue by app name or category.");
        var search = new TextBox
        {
            Height = 46,
            FontSize = 15,
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 12, 0, 18),
            Background = (Brush)FindResource("SurfaceBrush"),
            Foreground = (Brush)FindResource("PrimaryTextBrush"),
            BorderThickness = new Thickness(0),
            CaretBrush = (Brush)FindResource("AccentBrush"),
            ToolTip = "Search app names or categories"
        };
        var results = new WrapPanel();
        var noResults = new TextBlock
        {
            Text = "No matching apps. Try another name or category.",
            FontSize = 12,
            Foreground = (Brush)FindResource("SecondaryTextBrush"),
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 8, 0, 0)
        };

        void UpdateResults()
        {
            results.Children.Clear();
            var query = search.Text.Trim();
            foreach (var app in searchCatalogue.Where(app =>
                         app.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         app.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         (app.PackageName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)))
                results.Children.Add(CreateAppCard(app));

            noResults.Visibility = results.Children.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        search.TextChanged += (_, _) => UpdateResults();
        root.Children.Add(search);
        root.Children.Add(results);
        root.Children.Add(noResults);
        UpdateResults();
        root.Children.Add(CreateNotice(
            "Search scope",
            _connectedDeviceSerial is not null
                ? "Results are based on the verified package list returned by the connected BlueStacks instance."
                : "Search currently covers preview entries only. Connect BlueStacks ADB to search installed packages."));
        return root;
    }

    private FrameworkElement BuildStorePage()
    {
        var root = CreatePageStack();
        AddIntro(root, "App discovery", "A future discovery area for finding Android apps.");
        root.Children.Add(CreateNotice(
            "No store connection is configured",
            "JoyLauncher does not download APKs, install apps, or pretend to have a working store. A future version can link to trusted official sources while keeping installed apps separate."));
        root.Children.Add(CreateNotice(
            "Planned safeguards",
            "Show the source clearly, open official publisher pages, and ask before any install action."));
        return root;
    }

    private FrameworkElement BuildSettingsPage()
    {
        var root = CreatePageStack();
        AddIntro(root, "Runtime and preferences", "Local discovery is read-only. JoyLauncher does not change BlueStacks settings.");

        var result = _discoveryResult;
        var playerState = result is null
            ? "NOT SCANNED"
            : result.PlayerRunning
                ? "RUNNING"
                : result.PlayerFound
                    ? "INSTALLED"
                    : "NOT DETECTED";
        var playerDetails = result?.PlayerPath
            ?? "No verified HD-Player.exe path found. Check that BlueStacks 5 is installed for this Windows account.";

        root.Children.Add(CreateInfoCard(
            "BlueStacks player",
            playerState,
            playerDetails));

        var adbState = _connectedDeviceSerial is not null
            ? "CONNECTED"
            : result is null
                ? "NOT SCANNED"
                : result.HasConfiguredAdbEndpoint
                    ? "CONFIGURATION FOUND · NOT CONNECTED"
                    : "NO CONFIGURED ENDPOINT FOUND";
        var adbDetails = _connectedDeviceSerial is not null
            ? $"Verified ADB device: {_connectedDeviceSerial}"
            : result?.AdbEndpoints.Count > 0
                ? string.Join(Environment.NewLine, result.AdbEndpoints.Select(endpoint =>
                    $"{endpoint.InstanceName}: {endpoint.Host}:{endpoint.Port}"))
                : "Enable Android Debug Bridge in BlueStacks Settings → Advanced, then refresh detection. The port is read from local configuration; no fixed port is assumed.";

        root.Children.Add(CreateInfoCard(
            "ADB configuration",
            adbState,
            adbDetails));

        root.Children.Add(CreateInfoCard(
            "ADB executable",
            result?.AdbExecutableFound == true ? "FOUND" : "NOT FOUND",
            result?.AdbPath ?? "The BlueStacks-bundled HD-Adb.exe or adb.exe was not found in the detected install directory."));

        if (_connectedDeviceSerial is not null)
            root.Children.Add(CreateInfoCard("ADB device", "CONNECTED", $"Verified device serial: {_connectedDeviceSerial}"));

        if (!string.IsNullOrWhiteSpace(_adbError))
            root.Children.Add(CreateInfoCard("Last ADB error", "ACTION REQUIRED", _adbError));

        if (result?.AdbExecutableFound == true && result.AdbEndpoints.Count > 0)
        {
            root.Children.Add(new TextBlock
            {
                Text = "AVAILABLE BLUESTACKS INSTANCES",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("SecondaryTextBrush"),
                Margin = new Thickness(0, 8, 0, 8)
            });

            foreach (var endpoint in result.AdbEndpoints)
            {
                var connectButton = new Button
                {
                    Content = $"Connect to {endpoint.InstanceName}  ·  {endpoint.Host}:{endpoint.Port}",
                    Tag = endpoint,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(14, 10, 14, 10),
                    Background = (Brush)FindResource("AccentSoftBrush")
                };
                connectButton.Click += ConnectAndScan_Click;
                root.Children.Add(connectButton);
            }
        }

        var refresh = new Button
        {
            Content = "↻   Refresh local detection",
            Width = 220,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 4, 0, 18),
            Background = (Brush)FindResource("AccentBrush"),
            Foreground = Brushes.White
        };
        refresh.Click += RefreshDetection_Click;
        root.Children.Add(refresh);

        root.Children.Add(CreateInfoCard(
            "Installed package scan",
            _connectedDeviceSerial is not null ? $"{_installedPackages.Count} PACKAGES FOUND" : "NOT SCANNED",
            _connectedDeviceSerial is not null
                ? "All installed package identifiers returned by Android Package Manager."
                : "Connect to a detected endpoint to request the Android package list."));

        root.Children.Add(CreateInfoCard(
            "Launchable app scan",
            _connectedDeviceSerial is null
                ? "NOT SCANNED"
                : _launchabilityDetectionAvailable
                    ? $"{_launchablePackages.Count} LAUNCHABLE APPS VERIFIED"
                    : "COMPATIBILITY FALLBACK · NOT VERIFIED",
            _connectedDeviceSerial is null
                ? "The launcher checks Android MAIN/LAUNCHER activities after connecting."
                : _launchabilityDetectionAvailable
                    ? "Only packages with a normal launcher activity appear as app cards."
                    : _launchabilityDetectionError ?? "Android launcher-activity discovery is unavailable. Installed packages are shown as a fallback."));

        root.Children.Add(CreateInfoCard(
            "BlueStacks child-window hosting",
            BlueStacksWindowHost.IsDocked ? "EMBEDDED" : PlayPage.Visibility == Visibility.Visible ? "NOT EMBEDDED" : "NOT ACTIVE",
            "Mode: guarded cross-process child-window hosting. JoyLauncher compares Windows DPI-awareness contexts before calling SetParent, embeds only when the contexts match, and restores the original BlueStacks window when leaving Play." +
            Environment.NewLine +
            string.Join(Environment.NewLine, new[]
            {
                _windowEmbeddingDiagnostic ?? "No player-window embedding attempt has been recorded in this session.",
                _windowDiscoveryDiagnostic ?? string.Empty
            }.Where(value => !string.IsNullOrWhiteSpace(value)))));
        
        root.Children.Add(CreateInfoCard(
            "Next integration step",
            "PACKAGE METADATA",
            "Resolve friendly app names and icons, classify apps, and add a verified Minecraft package match. The current launch buttons use package IDs returned by ADB."));

        var copyDiagnostics = new Button
        {
            Content = "Copy diagnostics",
            Width = 180,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 18),
            Padding = new Thickness(14, 10, 14, 10),
            Background = (Brush)FindResource("AccentSoftBrush"),
            Foreground = (Brush)FindResource("PrimaryTextBrush"),
            ToolTip = "Copy non-sensitive runtime details and the latest ADB error"
        };
        copyDiagnostics.Click += CopyDiagnostics_Click;
        root.Children.Add(copyDiagnostics);

        root.Children.Add(CreateInfoCard(
            "Library preferences",
            "SAVED LOCALLY",
            $"{_preferences.FavoritePackages.Count} favorites · {_preferences.PinnedPackages.Count} pinned · {_preferences.RecentPackages.Count} recent apps. Stored in your Windows user profile."));

        root.Children.Add(CreateInfoCard(
            "Controller navigation",
            _gamepadService.IsConnected ? "XINPUT CONTROLLER CONNECTED" : "WAITING FOR CONTROLLER",
            _gamepadService.StatusText + Environment.NewLine +
            "D-pad / left stick: move · A: select · B: back · Start: fullscreen."));
        return root;
    }

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var result = _discoveryResult;
        var endpoints = result?.AdbEndpoints.Count > 0
            ? string.Join(Environment.NewLine, result.AdbEndpoints.Select(endpoint =>
                $"- {endpoint.InstanceName}: {endpoint.Host}:{endpoint.Port}"))
            : "- None detected";

        var report = string.Join(Environment.NewLine,
        [
            "JoyLauncher diagnostics",
            $"Timestamp (local): {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}",
            $"Windows version: {Environment.OSVersion.VersionString}",
            $"BlueStacks player running: {result?.PlayerRunning.ToString() ?? "Not scanned"}",
            $"BlueStacks player path found: {result?.PlayerFound.ToString() ?? "Not scanned"}",
            $"ADB executable found: {result?.AdbExecutableFound.ToString() ?? "Not scanned"}",
            $"ADB executable path: {result?.AdbPath ?? "Not found"}",
            "Configured ADB endpoints:",
            endpoints,
            $"Connected device: {_connectedDeviceSerial ?? "Not connected"}",
            $"Installed package count: {_installedPackages.Count}",
            $"Launchable app count: {_launchablePackages.Count}",
            $"Launchability detection available: {_launchabilityDetectionAvailable}",
            $"Launchability detection detail: {_launchabilityDetectionError ?? "None"}",
            $"BlueStacks player embedded in Play area: {BlueStacksWindowHost.IsDocked}",
            $"Embedded player window handle: {(BlueStacksWindowHost.DockedWindowHandle == IntPtr.Zero ? "None" : $"0x{BlueStacksWindowHost.DockedWindowHandle.ToInt64():X}")}",
            $"Player-window embedding detail: {_windowEmbeddingDiagnostic ?? "None"}",
            $"Player-window focus detail: {BlueStacksWindowHost.LastFocusDiagnostic ?? "Not attempted"}",
            $"Window discovery detail: {_windowDiscoveryDiagnostic ?? "None"}",
            "BlueStacks embedded child-window inventory:",
            BlueStacksWindowHost.GetChildWindowInventory(),
            $"Big Picture mode active: {_isBigPictureMode}",
            $"Play page visible: {PlayPage.Visibility == Visibility.Visible}",
            $"Latest runtime error: {_adbError ?? "None"}",
            $"Local crash-log folder: {CrashLogWriterDirectory}"
        ]);

        try
        {
            Clipboard.SetText(report);
            RuntimeStatusText.Text = "Diagnostics copied";
        }
        catch (Exception exception) when (
            exception is System.Runtime.InteropServices.ExternalException or
            System.InvalidOperationException or
            System.Threading.ThreadStateException)
        {
            RuntimeStatusText.Text = "Could not copy diagnostics";
            _adbError = $"Clipboard copy failed: {exception.Message}";
        }
    }

    private StackPanel CreatePageStack() => new()
    {
        Orientation = Orientation.Vertical,
        Margin = new Thickness(0, 0, 12, 24)
    };

    private static void AddIntro(Panel parent, string title, string subtitle)
    {
        parent.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 6)
        });
        parent.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["SecondaryTextBrush"],
            Margin = new Thickness(0, 0, 0, 14)
        });
    }

    private void UpdateHomeAppTiles()
    {
        HomeAppGrid.Children.Clear();
        var hasLiveApps = _connectedDeviceSerial is not null;
        var apps = hasLiveApps ? GetLiveApps().Take(6).ToArray() : SampleApps.Take(3).ToArray();

        HomeAppsLabel.Text = hasLiveApps
            ? $"{_launchablePackages.Count} LAUNCHABLE APPS"
            : "SAMPLE APPS";

        foreach (var app in apps.OrderByDescending(IsPinned).ThenBy(app => RecentIndex(app.PackageName)))
            HomeAppGrid.Children.Add(CreateAppCard(app));

        HomeAppNotice.Text = hasLiveApps
            ? apps.Length == 0
                ? _launchabilityDetectionAvailable
                    ? "Android reported no apps with a normal launcher activity."
                    : "ADB is connected, but no packages were returned. Refresh the connection or check the Android runtime."
                : $"Showing {apps.Length} of {_launchablePackages.Count} launchable apps returned by BlueStacks."
            : "These are sample tiles, not verified installed apps. Connect BlueStacks ADB to load your actual library.";
        RefreshBigPictureContent();
    }

    private IReadOnlyList<AppEntry> GetLiveApps() =>
        _launchablePackages.Select(packageName =>
        {
            if (KnownPackages.TryGetValue(packageName, out var known))
                return new AppEntry(known.Name, known.Category, known.Description, IsVerified: true, PackageName: packageName);

            var shortName = packageName.Split('.').LastOrDefault();
            return new AppEntry(
                string.IsNullOrWhiteSpace(shortName) ? packageName : shortName,
                "Other",
                packageName,
                IsVerified: true,
                PackageName: packageName);
        }).ToArray();

    private void UpdateFeaturedMinecraftState()
    {
        var minecraftPackage = _launchablePackages.FirstOrDefault(packageName =>
            string.Equals(packageName, "com.mojang.minecraftpe", StringComparison.OrdinalIgnoreCase));

        LaunchFeaturedButton.IsEnabled = minecraftPackage is not null;
        LaunchFeaturedButton.Tag = minecraftPackage;
        LaunchFeaturedButton.Content = minecraftPackage is null
            ? "Minecraft not detected"
            : "▶   Launch Minecraft";
        LaunchFeaturedButton.ToolTip = minecraftPackage is null
            ? "Connect ADB and install Minecraft in this BlueStacks instance to enable this action."
            : $"Launch verified package {minecraftPackage}";
        LaunchFeaturedButton.Opacity = minecraftPackage is null ? 0.62 : 1.0;
    }

    private Border CreateAppCard(AppEntry app)
    {
        var content = new StackPanel();
        var fallback = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(app.Name) ? "?" : app.Name[..1].ToUpperInvariant(),
            FontSize = 19,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("AccentBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var logoImage = new Image
        {
            Width = 44,
            Height = 44,
            Stretch = Stretch.Uniform,
            Visibility = Visibility.Collapsed
        };
        var logoSurface = new Grid
        {
            Width = 54,
            Height = 54,
            Children = { fallback, logoImage }
        };
        content.Children.Add(new Border
        {
            Width = 60,
            Height = 60,
            CornerRadius = new CornerRadius(12),
            Background = (Brush)FindResource("ElevatedBrush"),
            Padding = new Thickness(3),
            Margin = new Thickness(0, 0, 0, 12),
            Child = logoSurface
        });
        if (!string.IsNullOrWhiteSpace(app.PackageName))
            _ = LoadWebsiteLogoAsync(app.PackageName, logoImage, fallback);
        content.Children.Add(new TextBlock
        {
            Text = app.Name,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White
        });
        content.Children.Add(new TextBlock
        {
            Text = app.Category + (app.IsVerified ? " · verified" : " · preview"),
            FontSize = 11,
            Foreground = (Brush)FindResource("SecondaryTextBrush"),
            Margin = new Thickness(0, 7, 0, 0)
        });
        content.Children.Add(new TextBlock
        {
            Text = app.Description,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("SecondaryTextBrush"),
            Margin = new Thickness(0, 10, 0, 0)
        });

        if (app.IsVerified && !string.IsNullOrWhiteSpace(app.PackageName))
        {
            var actionRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 0)
            };
            var favoriteButton = new Button
            {
                Content = IsFavorite(app) ? "★ Saved" : "☆ Favorite",
                Tag = app.PackageName,
                Padding = new Thickness(9, 7, 9, 7),
                Margin = new Thickness(0, 0, 6, 0),
                Background = (Brush)FindResource("ElevatedBrush"),
                ToolTip = "Save this app to Favorites"
            };
            favoriteButton.Click += ToggleFavorite_Click;
            var pinButton = new Button
            {
                Content = IsPinned(app) ? "Unpin" : "Pin",
                Tag = app.PackageName,
                Padding = new Thickness(9, 7, 9, 7),
                Background = (Brush)FindResource("ElevatedBrush"),
                ToolTip = "Keep this app near the top of Home"
            };
            pinButton.Click += TogglePin_Click;
            actionRow.Children.Add(favoriteButton);
            actionRow.Children.Add(pinButton);
            content.Children.Add(actionRow);

            var launchButton = new Button
            {
                Content = "Launch app",
                Tag = app.PackageName ?? app.Name,
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 8, 0, 0),
                Background = (Brush)FindResource("AccentSoftBrush")
            };
            launchButton.Click += LaunchVerifiedPackage_Click;
            content.Children.Add(launchButton);
        }

        return new Border
        {
            Width = 208,
            MinHeight = 126,
            Margin = new Thickness(0, 0, 14, 14),
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(12),
            Background = (Brush)FindResource("SurfaceBrush"),
            Child = content
        };
    }

    private void RefreshBigPictureContent()
    {
        BigPictureRecentGamesPanel.Children.Clear();

        var hasLiveApps = _connectedDeviceSerial is not null;
        var apps = (hasLiveApps ? GetLiveApps() : SampleApps).ToList();
        var ordered = apps
            .OrderByDescending(IsPinned)
            .ThenByDescending(IsFavorite)
            .ThenBy(app => RecentIndex(app.PackageName))
            .ThenBy(app => app.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (_bigPictureActiveTab == "RECOMMENDED")
        {
            var selected = ordered.Where(app => IsPinned(app) || IsFavorite(app)).ToList();
            if (selected.Count > 0)
                ordered = selected;
        }

        var featured = apps.FirstOrDefault(app =>
                            string.Equals(app.PackageName, "com.mojang.minecraftpe", StringComparison.OrdinalIgnoreCase))
                        ?? ordered.FirstOrDefault();

        if (featured is not null)
            SetBigPictureFeaturedApp(featured);
        else
            SetBigPictureEmptyHero(hasLiveApps);

        BigPictureNetworkDot.Fill = hasLiveApps
            ? new SolidColorBrush(Color.FromRgb(84, 212, 151))
            : new SolidColorBrush(Color.FromRgb(231, 180, 93));
        BigPictureNetworkText.Text = hasLiveApps ? "ADB CONNECTED" : "ADB OFFLINE";

        BigPictureAppsCount.Text = hasLiveApps
            ? $"{apps.Count} LIVE APPS  ·  {_connectedDeviceSerial}"
            : $"{apps.Count} PREVIEW TILES  ·  CONNECT ADB TO LOAD YOUR LIBRARY";

        BigPictureStatusText.Text = hasLiveApps
            ? $"{_launchablePackages.Count} launchable Android apps detected in this BlueStacks instance."
            : "Preview entries only. Connect BlueStacks ADB to load your installed apps.";

        if (_bigPictureActiveTab == "FRIENDS")
        {
            BigPictureRecentGamesPanel.Children.Add(new TextBlock
            {
                Text = "Friends activity is not connected yet. Your local app library remains available in What's New.",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(184, 195, 198)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 28, 0, 0)
            });
            BigPictureTabStatus.Text = "SOCIAL FEED NOT CONNECTED";
            return;
        }

        BigPictureTabStatus.Text = _bigPictureActiveTab == "RECOMMENDED"
            ? "PINNED & FAVORITE APPS"
            : "YOUR LOCAL LIBRARY";

        if (featured is not null)
            BigPictureRecentGamesPanel.Children.Add(CreateBigPictureFeaturedCard(featured));

        foreach (var app in ordered.Where(app => !ReferenceEquals(app, featured)))
            BigPictureRecentGamesPanel.Children.Add(CreateBigPicturePosterCard(app));

        if (BigPictureRecentGamesPanel.Children.Count == 0)
        {
            BigPictureRecentGamesPanel.Children.Add(new TextBlock
            {
                Text = "No launchable apps were found. Connect BlueStacks ADB or install an Android app.",
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(184, 195, 198)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 28, 0, 0)
            });
        }
    }

    private void SetBigPictureFeaturedApp(AppEntry app)
    {
        var palette = GetBigPicturePalette(app);

        BigPictureHeroBackdrop.Background = new LinearGradientBrush(palette.Start, palette.End, 0);
        BigPictureHeroArtworkSurface.Background = new LinearGradientBrush(palette.End, palette.Accent, 35);
        BigPictureHeroEyebrow.Text = $"{app.Category.ToUpperInvariant()}  ·  {(app.IsVerified ? "INSTALLED" : "PREVIEW")}";
        BigPictureHeroTitle.Text = app.Name.ToUpperInvariant();
        BigPictureHeroDescription.Text = app.IsVerified
            ? $"{app.Description}  Ready to launch from your connected BlueStacks instance."
            : $"{app.Description}  Preview artwork only — connect BlueStacks ADB to enable launching.";
        BigPictureHeroFallback.Text = GetAppInitial(app.Name);
        BigPictureHeroLogo.Source = null;
        BigPictureHeroLogo.Visibility = Visibility.Collapsed;
        BigPictureHeroFallback.Visibility = Visibility.Visible;

        BigPictureLaunchButton.Tag = app.PackageName;
        BigPictureLaunchButton.IsEnabled = app.IsVerified && !string.IsNullOrWhiteSpace(app.PackageName);
        BigPictureLaunchButton.Content = BigPictureLaunchButton.IsEnabled
            ? $"▶   PLAY {app.Name.ToUpperInvariant()}"
            : "CONNECT TO PLAY";
        BigPictureLaunchButton.ToolTip = BigPictureLaunchButton.IsEnabled
            ? $"Launch {app.Name} in BlueStacks"
            : "Connect BlueStacks ADB and verify this app before launching.";
        BigPictureLaunchButton.Opacity = BigPictureLaunchButton.IsEnabled ? 1 : 0.58;

        if (app.IsVerified && !string.IsNullOrWhiteSpace(app.PackageName))
            _ = LoadWebsiteLogoAsync(app.PackageName, BigPictureHeroLogo, BigPictureHeroFallback);
    }

    private void SetBigPictureEmptyHero(bool hasLiveApps)
    {
        BigPictureHeroEyebrow.Text = "JOYLAUNCHER  ·  ANDROID LIBRARY";
        BigPictureHeroTitle.Text = "READY WHEN YOU ARE";
        BigPictureHeroDescription.Text = "Connect BlueStacks to discover and launch your installed Android apps.";
        BigPictureHeroFallback.Text = "J";
        BigPictureHeroLogo.Source = null;
        BigPictureHeroLogo.Visibility = Visibility.Collapsed;
        BigPictureHeroFallback.Visibility = Visibility.Visible;
        BigPictureLaunchButton.Tag = null;
        BigPictureLaunchButton.IsEnabled = false;
        BigPictureLaunchButton.Content = "NO APP SELECTED";
        BigPictureStatusText.Text = hasLiveApps
            ? "No launchable apps were returned by Android."
            : "Connect BlueStacks ADB to load your installed apps.";
        BigPictureHeroBackdrop.Background = new LinearGradientBrush(
            Color.FromRgb(25, 52, 60), Color.FromRgb(22, 25, 32), 0);
        BigPictureHeroArtworkSurface.Background = new SolidColorBrush(Color.FromRgb(34, 67, 68));
    }

    private Button CreateBigPictureFeaturedCard(AppEntry app)
    {
        var palette = GetBigPicturePalette(app);
        var logoImage = new Image { Width = 72, Height = 72, Stretch = Stretch.Uniform, Visibility = Visibility.Collapsed };
        var fallback = new TextBlock
        {
            Text = GetAppInitial(app.Name),
            FontSize = 66,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var artwork = new Grid
        {
            Background = new LinearGradientBrush(palette.Start, palette.End, 0),
            ClipToBounds = true
        };
        artwork.Children.Add(new Ellipse
        {
            Width = 205,
            Height = 205,
            Fill = new SolidColorBrush(palette.Accent),
            Opacity = 0.24,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -48, -24, 0)
        });
        artwork.Children.Add(new Ellipse
        {
            Width = 145,
            Height = 145,
            Stroke = new SolidColorBrush(Color.FromArgb(115, 255, 255, 255)),
            StrokeThickness = 1,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -18, 8, 0)
        });
        artwork.Children.Add(fallback);
        artwork.Children.Add(logoImage);

        var fade = new Border
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Height = 118,
            Background = new LinearGradientBrush(
                Color.FromArgb(0, 8, 12, 17), Color.FromArgb(235, 8, 12, 17), 90)
        };
        artwork.Children.Add(fade);

        var titleStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(17, 0, 110, 14)
        };
        titleStack.Children.Add(new TextBlock
        {
            Text = app.Name.ToUpperInvariant(),
            FontSize = 21,
            FontWeight = FontWeights.ExtraBold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        titleStack.Children.Add(new TextBlock
        {
            Text = app.Category.ToUpperInvariant(),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(184, 226, 208)),
            Margin = new Thickness(0, 3, 0, 0)
        });
        artwork.Children.Add(titleStack);

        var badge = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(225, 19, 35, 31)),
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 0, 12, 13),
            Child = new TextBlock
            {
                Text = app.IsVerified ? "✓  READY" : "PREVIEW",
                Foreground = app.IsVerified
                    ? new SolidColorBrush(Color.FromRgb(126, 232, 176))
                    : new SolidColorBrush(Color.FromRgb(216, 219, 222)),
                FontSize = 9,
                FontWeight = FontWeights.Bold
            }
        };
        artwork.Children.Add(badge);

        var button = new Button
        {
            Width = 408,
            Height = 207,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 13, 5),
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.FromRgb(132, 231, 190)),
            BorderThickness = new Thickness(1.5),
            Content = new Border
            {
                CornerRadius = new CornerRadius(10),
                ClipToBounds = true,
                Child = artwork
            },
            Tag = app,
            ToolTip = $"Focus {app.Name}; select to launch when connected"
        };
        button.Click += BigPictureAppCard_Click;
        button.MouseEnter += (_, _) => SetBigPictureFeaturedApp(app);
        button.GotKeyboardFocus += (_, _) => SetBigPictureFeaturedApp(app);

        if (app.IsVerified && !string.IsNullOrWhiteSpace(app.PackageName))
            _ = LoadWebsiteLogoAsync(app.PackageName, logoImage, fallback);

        return button;
    }

    private Button CreateBigPicturePosterCard(AppEntry app)
    {
        var palette = GetBigPicturePalette(app);
        var logoImage = new Image { Width = 55, Height = 55, Stretch = Stretch.Uniform, Visibility = Visibility.Collapsed };
        var fallback = new TextBlock
        {
            Text = GetAppInitial(app.Name),
            FontSize = 53,
            FontWeight = FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var art = new Grid
        {
            Background = new LinearGradientBrush(palette.Start, palette.End, 35),
            ClipToBounds = true
        };
        art.Children.Add(new Ellipse
        {
            Width = 160,
            Height = 160,
            Fill = new SolidColorBrush(palette.Accent),
            Opacity = 0.28,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -36, -46, 0)
        });
        art.Children.Add(fallback);
        art.Children.Add(logoImage);
        art.Children.Add(new Border
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Height = 90,
            Background = new LinearGradientBrush(
                Color.FromArgb(0, 7, 11, 16), Color.FromArgb(245, 7, 11, 16), 90)
        });

        var labels = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(10, 0, 8, 12)
        };
        labels.Children.Add(new TextBlock
        {
            Text = app.Name,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = 34
        });
        labels.Children.Add(new TextBlock
        {
            Text = app.Category.ToUpperInvariant(),
            FontSize = 8,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(182, 202, 197)),
            Margin = new Thickness(0, 4, 0, 0)
        });
        art.Children.Add(labels);

        if (app.IsVerified)
        {
            art.Children.Add(new Border
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(225, 18, 43, 34)),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(7),
                Child = new TextBlock
                {
                    Text = "✓",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(126, 232, 176))
                }
            });
        }

        var button = new Button
        {
            Width = 137,
            Height = 207,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 11, 5),
            Background = Brushes.Transparent,
            Content = new Border
            {
                CornerRadius = new CornerRadius(9),
                ClipToBounds = true,
                Child = art
            },
            Tag = app,
            ToolTip = app.IsVerified ? $"Launch {app.Name}" : $"{app.Name} preview; connect ADB to enable launching"
        };
        button.Click += BigPictureAppCard_Click;
        button.MouseEnter += (_, _) => SetBigPictureFeaturedApp(app);
        button.GotKeyboardFocus += (_, _) => SetBigPictureFeaturedApp(app);

        if (app.IsVerified && !string.IsNullOrWhiteSpace(app.PackageName))
            _ = LoadWebsiteLogoAsync(app.PackageName, logoImage, fallback);

        return button;
    }

    private async void BigPictureAppCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: AppEntry app })
            return;

        SetBigPictureFeaturedApp(app);
        if (app.IsVerified && !string.IsNullOrWhiteSpace(app.PackageName))
            await LaunchPackageAsync(app.PackageName);
        else
            BigPictureStatusText.Text = "Preview only. Connect BlueStacks ADB and install this app to enable launching.";
    }

    private async void BigPictureLaunch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
            await LaunchPackageAsync(packageName);
    }

    private void BigPictureSearch_Click(object sender, RoutedEventArgs e)
    {
        if (_isBigPictureMode)
            ExitBigPictureMode();
        Navigation_Click(new Button { Tag = "Search" }, new RoutedEventArgs());
    }

    private void BigPictureSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_isBigPictureMode)
            ExitBigPictureMode();
        Navigation_Click(new Button { Tag = "Settings" }, new RoutedEventArgs());
    }

    private void BigPictureTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tab })
            return;

        _bigPictureActiveTab = tab;
        foreach (var button in FindVisualChildren<Button>(BigPictureOverlay)
                     .Where(candidate => candidate.Tag is string tag &&
                         tag is "WHAT'S NEW" or "FRIENDS" or "RECOMMENDED"))
        {
            var isSelected = string.Equals(button.Tag as string, tab, StringComparison.Ordinal);
            button.Background = isSelected
                ? new SolidColorBrush(Color.FromRgb(36, 75, 67))
                : Brushes.Transparent;
            button.Foreground = isSelected
                ? new SolidColorBrush(Color.FromRgb(217, 247, 234))
                : new SolidColorBrush(Color.FromRgb(170, 181, 192));
        }

        RefreshBigPictureContent();
    }

    private static string GetAppInitial(string name) =>
        string.IsNullOrWhiteSpace(name) ? "?" : name[..1].ToUpperInvariant();

    private static (Color Start, Color End, Color Accent) GetBigPicturePalette(AppEntry app)
    {
        var key = (app.Name + " " + app.Category).ToLowerInvariant();
        if (key.Contains("minecraft"))
            return (Color.FromRgb(31, 77, 59), Color.FromRgb(13, 31, 37), Color.FromRgb(123, 219, 141));
        if (key.Contains("youtube"))
            return (Color.FromRgb(112, 38, 43), Color.FromRgb(29, 24, 34), Color.FromRgb(255, 92, 87));
        if (key.Contains("chrome") || key.Contains("google"))
            return (Color.FromRgb(35, 67, 108), Color.FromRgb(20, 27, 45), Color.FromRgb(112, 178, 246));
        if (key.Contains("music"))
            return (Color.FromRgb(87, 45, 92), Color.FromRgb(26, 22, 43), Color.FromRgb(202, 129, 225));
        if (key.Contains("game"))
            return (Color.FromRgb(31, 75, 88), Color.FromRgb(18, 27, 43), Color.FromRgb(111, 211, 206));
        return (Color.FromRgb(54, 73, 89), Color.FromRgb(22, 29, 41), Color.FromRgb(150, 188, 205));
    }

    private void EnterBigPictureMode()
    {
        if (_isBigPictureMode || PlayPage.Visibility == Visibility.Visible)
            return;

        _bigPictureOwnedFullscreen = !_isFullscreen;
        if (_bigPictureOwnedFullscreen)
            ToggleFullscreen();

        RefreshBigPictureContent();
        StandardShell.Visibility = Visibility.Collapsed;
        BigPictureOverlay.Visibility = Visibility.Visible;
        _isBigPictureMode = true;

        if (BigPictureLaunchButton.IsEnabled)
            BigPictureLaunchButton.Focus();
        else
            BigPictureExitButton.Focus();
    }

    private void ExitBigPictureMode()
    {
        if (!_isBigPictureMode)
            return;

        _isBigPictureMode = false;
        BigPictureOverlay.Visibility = Visibility.Collapsed;
        StandardShell.Visibility = Visibility.Visible;

        if (_bigPictureOwnedFullscreen && _isFullscreen)
            ToggleFullscreen();

        _bigPictureOwnedFullscreen = false;
        Navigation_Click(new Button { Tag = "Home" }, new RoutedEventArgs());
    }

    private void BigPictureMode_Click(object sender, RoutedEventArgs e) => EnterBigPictureMode();

    private void ExitBigPicture_Click(object sender, RoutedEventArgs e) => ExitBigPictureMode();

    private async Task LoadWebsiteLogoAsync(string packageName, Image image, TextBlock fallback)
    {
        var logo = await _websiteLogoService.GetLogoAsync(packageName);
        if (logo is null)
            return;

        image.Source = logo;
        image.Visibility = Visibility.Visible;
        fallback.Visibility = Visibility.Collapsed;
    }

    private Button CreateFilterButton(string label) => new()
    {
        Content = label,
        Margin = new Thickness(0, 0, 8, 0),
        Padding = new Thickness(13, 8, 13, 8),
        FontSize = 11,
        Background = (Brush)FindResource("SurfaceBrush"),
        Foreground = (Brush)FindResource("PrimaryTextBrush"),
        Tag = null
    };

    private Border CreateNotice(string title, string description) =>
        CreateInfoCard(title, "INFORMATION", description);

    private Border CreateInfoCard(string title, string status, string description)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White
        });
        stack.Children.Add(new TextBlock
        {
            Text = status.ToUpperInvariant(),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("AccentBrush"),
            Margin = new Thickness(0, 7, 0, 5)
        });
        stack.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("SecondaryTextBrush")
        });

        return new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 0, 12),
            Child = stack
        };
    }

    private void ToggleFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string packageName })
            return;
        TogglePackage(packageName, _preferences.FavoritePackages);
        SavePreferences();
        RefreshCurrentPage();
    }

    private void TogglePin_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string packageName })
            return;
        TogglePackage(packageName, _preferences.PinnedPackages);
        SavePreferences();
        UpdateHomeAppTiles();
        RefreshCurrentPage();
    }

    private static void TogglePackage(string packageName, List<string> collection)
    {
        var index = collection.FindIndex(value =>
            string.Equals(value, packageName, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            collection.RemoveAt(index);
        else
            collection.Add(packageName);
    }

    private bool IsFavorite(AppEntry app) => IsFavorite(app.PackageName);
    private bool IsFavorite(string? packageName) =>
        !string.IsNullOrWhiteSpace(packageName) &&
        _preferences.FavoritePackages.Contains(packageName, StringComparer.OrdinalIgnoreCase);

    private bool IsPinned(AppEntry app) => IsPinned(app.PackageName);
    private bool IsPinned(string? packageName) =>
        !string.IsNullOrWhiteSpace(packageName) &&
        _preferences.PinnedPackages.Contains(packageName, StringComparer.OrdinalIgnoreCase);

    private int RecentIndex(string? packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return int.MaxValue;
        var index = _preferences.RecentPackages.FindIndex(value =>
            string.Equals(value, packageName, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? int.MaxValue : index;
    }

    private void SavePreferences()
    {
        try
        {
            _preferencesStore.Save(_preferences);
        }
        catch (Exception exception) when (
            exception is System.IO.IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            RuntimeStatusText.Text = "Could not save library preferences";
            _adbError = $"Local preferences could not be saved: {exception.Message}";
        }
    }

    private void RefreshCurrentPage()
    {
        if (PageTitle.Text == "Home")
            UpdateHomeAppTiles();
        else if (PlayPage.Visibility != Visibility.Visible)
            DynamicPageContent.Content = BuildPage(PageTitle.Text);
    }

    private async void LaunchVerifiedPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
            await LaunchPackageAsync(packageName);
    }

    private async Task LaunchPackageAsync(string packageName)
    {
        if (!_launchablePackages.Contains(packageName, StringComparer.OrdinalIgnoreCase))
        {
            _adbError = "The selected package is no longer in the verified installed-package list. Refresh the library.";
            RuntimeStatusText.Text = "Package not verified";
            return;
        }

        var adbPath = _discoveryResult?.AdbPath;
        if (string.IsNullOrWhiteSpace(adbPath) || string.IsNullOrWhiteSpace(_connectedDeviceSerial))
        {
            _adbError = "Connect to a verified BlueStacks ADB device before launching an app.";
            RuntimeStatusText.Text = "ADB device not connected";
            return;
        }

        RuntimeStatusText.Text = $"Sending launch request for {packageName}…";
        var result = await _adbClient.LaunchPackageAsync(adbPath, _connectedDeviceSerial, packageName);

        if (result.TimedOut)
        {
            _adbError = $"Launch request timed out for {packageName}.";
            RuntimeStatusText.Text = "Launch timed out";
        }
        else if (result.ExitCode == 0 &&
                 !result.StandardOutput.Contains("No activities found", StringComparison.OrdinalIgnoreCase) &&
                 !result.StandardError.Contains("No activities found", StringComparison.OrdinalIgnoreCase))
        {
            _preferences.RecentPackages.RemoveAll(value =>
                string.Equals(value, packageName, StringComparison.OrdinalIgnoreCase));
            _preferences.RecentPackages.Insert(0, packageName);
            if (_preferences.RecentPackages.Count > 30)
                _preferences.RecentPackages.RemoveRange(30, _preferences.RecentPackages.Count - 30);
            SavePreferences();
            UpdateHomeAppTiles();
            _adbError = null;
            RuntimeStatusText.Text = $"ADB launch request accepted · {packageName}";
            await ShowEmbeddedPlayerAsync(packageName);
        }
        else
        {
            _adbError = string.IsNullOrWhiteSpace(result.StandardError)
                ? result.StandardOutput.Trim()
                : result.StandardError.Trim();
            if (string.IsNullOrWhiteSpace(_adbError))
                _adbError = $"BlueStacks did not accept the launch request for {packageName}.";
            RuntimeStatusText.Text = "Launch request failed";
        }
    }

    private async Task ShowEmbeddedPlayerAsync(string packageName)
    {
        var app = GetLiveApps().FirstOrDefault(item =>
            string.Equals(item.PackageName, packageName, StringComparison.OrdinalIgnoreCase));
        var displayName = app?.Name ?? packageName;

        _wasBigPictureBeforePlay = _isBigPictureMode;
        if (_isBigPictureMode)
        {
            _isBigPictureMode = false;
            BigPictureOverlay.Visibility = Visibility.Collapsed;
            StandardShell.Visibility = Visibility.Visible;
        }

        PlayingAppTitle.Text = displayName;
        PlayingStatusText.Text = "Waiting for the BlueStacks player window…";
        PageTitle.Text = "Now playing";
        PageSubtitle.Text = $"Attempting to run {displayName} in BlueStacks inside JoyLauncher’s Play area.";
        MainContentScrollViewer.Visibility = Visibility.Collapsed;
        PlayPage.Visibility = Visibility.Visible;

        await Task.Delay(150);
        await TryEmbedRunningPlayerAsync();
    }

    private async Task TryEmbedRunningPlayerAsync()
    {
        PlayingStatusText.Text = "Locating the BlueStacks player window…";
        _windowEmbeddingDiagnostic = null;
        _windowDiscoveryDiagnostic = null;

        string? attachError = null;
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var playerWindow = BlueStacksWindowLocator.FindMainPlayerWindow(
                _discoveryResult?.PlayerPath,
                out var discoveryDiagnostic);
            _windowDiscoveryDiagnostic = discoveryDiagnostic;

            if (playerWindow != IntPtr.Zero)
            {
                PlayingStatusText.Text = "BlueStacks window found · embedding it inside the Play area…";
                if (BlueStacksWindowHost.TryAttach(playerWindow, out attachError))
                {
                    _windowEmbeddingDiagnostic = BlueStacksWindowHost.LastAttachDiagnostic;
                    PlayingStatusText.Text = "BlueStacks is embedded in the Play area. Click the game area to focus controls.";
                    RuntimeStatusText.Text = "Player window embedded in JoyLauncher";
                    _adbError = null;
                    return;
                }

                // A concrete Win32 attach error is not fixed by repeating the
                // the same native parent operation repeatedly. Keep the error and
                // allow the user to retry after changing the BlueStacks state.
                break;
            }

            await Task.Delay(300);
        }

        _windowEmbeddingDiagnostic = attachError ??
            "No embedding attempt was possible because no matching BlueStacks player window was located.";
        _adbError = _windowEmbeddingDiagnostic;
        var detail = attachError ?? _windowDiscoveryDiagnostic ?? "No additional Windows window details are available.";
        PlayingStatusText.Text = $"Could not embed the BlueStacks player: {detail}";
        RuntimeStatusText.Text = "Game opened · embedding unavailable";
        if (DynamicPageContent.Visibility == Visibility.Visible)
            DynamicPageContent.Content = BuildPage(PageTitle.Text);
    }

    private async void RetryEmbedding_Click(object sender, RoutedEventArgs e)
    {
        await TryEmbedRunningPlayerAsync();
    }

    private void TogglePlayerFullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (BlueStacksWindowHost.TryTogglePlayerFullscreen(out var detail))
        {
            PlayingStatusText.Text = detail;
            RuntimeStatusText.Text = "Sent F11 fullscreen toggle to BlueStacks";
        }
        else
        {
            PlayingStatusText.Text = detail;
            _adbError = detail;
        }
    }

    private void ReturnToLauncher_Click(object sender, RoutedEventArgs e)
    {
        ReturnFromPlayView(navigateHome: true);
    }

    private void ReturnFromPlayView(bool navigateHome)
    {
        BlueStacksWindowHost.Detach();
        PlayPage.Visibility = Visibility.Collapsed;
        MainContentScrollViewer.Visibility = Visibility.Visible;

        if (_wasBigPictureBeforePlay)
        {
            _wasBigPictureBeforePlay = false;
            RefreshBigPictureContent();
            StandardShell.Visibility = Visibility.Collapsed;
            BigPictureOverlay.Visibility = Visibility.Visible;
            _isBigPictureMode = true;
            if (BigPictureLaunchButton.IsEnabled)
                BigPictureLaunchButton.Focus();
            else
                BigPictureExitButton.Focus();
            return;
        }

        if (navigateHome)
            Navigation_Click(new Button { Tag = "Home" }, new RoutedEventArgs());
    }

    private async void LaunchFeatured_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
            await LaunchPackageAsync(packageName);
        else
            RuntimeStatusText.Text = "Minecraft is not installed in this BlueStacks instance.";
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (PlayPage.Visibility == Visibility.Visible ||
            FindVisualAncestor<TextBox>(Keyboard.FocusedElement as DependencyObject) is not null)
            return;

        var direction = e.Key switch
        {
            Key.Up => FocusDirection.Up,
            Key.Down => FocusDirection.Down,
            Key.Left => FocusDirection.Left,
            Key.Right => FocusDirection.Right,
            _ => (FocusDirection?)null
        };

        if (direction is not null && MoveKeyboardFocus(direction.Value))
            e.Handled = true;
    }

    private bool MoveKeyboardFocus(FocusDirection direction)
    {
        if (PlayPage.Visibility == Visibility.Visible ||
            FindVisualAncestor<TextBox>(Keyboard.FocusedElement as DependencyObject) is not null)
            return false;

        var candidates = FindVisualChildren<Button>(this)
            .Where(button => button.IsVisible && button.IsEnabled && button.Focusable)
            .Distinct()
            .ToArray();

        if (candidates.Length == 0)
            return false;

        var current = FindVisualAncestor<Button>(Keyboard.FocusedElement as DependencyObject);
        if (current is null || !candidates.Contains(current))
        {
            var first = candidates[0];
            first.BringIntoView();
            return first.Focus();
        }

        Rect currentBounds;
        try
        {
            currentBounds = current.TransformToAncestor(this)
                .TransformBounds(new Rect(new Point(0, 0), current.RenderSize));
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        var originX = currentBounds.Left + currentBounds.Width / 2;
        var originY = currentBounds.Top + currentBounds.Height / 2;

        var next = candidates
            .Where(candidate => candidate != current)
            .Select(candidate =>
            {
                try
                {
                    var bounds = candidate.TransformToAncestor(this)
                        .TransformBounds(new Rect(new Point(0, 0), candidate.RenderSize));
                    var dx = bounds.Left + bounds.Width / 2 - originX;
                    var dy = bounds.Top + bounds.Height / 2 - originY;
                    var primary = direction switch
                    {
                        FocusDirection.Left => -dx,
                        FocusDirection.Right => dx,
                        FocusDirection.Up => -dy,
                        FocusDirection.Down => dy,
                        _ => -1
                    };
                    var cross = direction is FocusDirection.Left or FocusDirection.Right
                        ? Math.Abs(dy)
                        : Math.Abs(dx);
                    return (Candidate: candidate, Score: primary > 1 ? primary + cross * 1.65 : double.PositiveInfinity);
                }
                catch (InvalidOperationException)
                {
                    return (Candidate: candidate, Score: double.PositiveInfinity);
                }
            })
            .Where(item => double.IsFinite(item.Score))
            .OrderBy(item => item.Score)
            .FirstOrDefault();

        if (next.Candidate is null || double.IsInfinity(next.Score))
            return false;

        next.Candidate.BringIntoView();
        return next.Candidate.Focus();
    }

    private void Gamepad_ButtonPressed(GamepadNavigationButton button)
    {
        if (PlayPage.Visibility == Visibility.Visible)
            return;

        switch (button)
        {
            case GamepadNavigationButton.Up:
                MoveKeyboardFocus(FocusDirection.Up);
                break;
            case GamepadNavigationButton.Down:
                MoveKeyboardFocus(FocusDirection.Down);
                break;
            case GamepadNavigationButton.Left:
                MoveKeyboardFocus(FocusDirection.Left);
                break;
            case GamepadNavigationButton.Right:
                MoveKeyboardFocus(FocusDirection.Right);
                break;
            case GamepadNavigationButton.Accept:
                if (FindVisualAncestor<Button>(Keyboard.FocusedElement as DependencyObject) is { IsEnabled: true } focusedButton)
                    focusedButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                break;
            case GamepadNavigationButton.Back:
                if (_isBigPictureMode)
                    ExitBigPictureMode();
                else if (DynamicPageContent.Visibility == Visibility.Visible)
                    Navigation_Click(new Button { Tag = "Home" }, new RoutedEventArgs());
                break;
            case GamepadNavigationButton.Start:
                if (_isBigPictureMode)
                    ExitBigPictureMode();
                else
                    EnterBigPictureMode();
                break;
        }
    }

    private void Gamepad_ConnectionChanged(bool connected, string status)
    {
        if (PageTitle.Text == "Settings" &&
            DynamicPageContent.Visibility == Visibility.Visible &&
            PlayPage.Visibility != Visibility.Visible)
            DynamicPageContent.Content = BuildSettingsPage();
    }

    private static T? FindVisualAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T match)
                return match;

            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && PlayPage.Visibility == Visibility.Visible)
        {
            ReturnFromPlayView(navigateHome: true);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _isBigPictureMode)
        {
            ExitBigPictureMode();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F10 && PlayPage.Visibility != Visibility.Visible)
        {
            if (_isBigPictureMode)
                ExitBigPictureMode();
            else
                EnterBigPictureMode();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F11)
        {
            if (!_isBigPictureMode)
                ToggleFullscreen();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            var page = e.Key switch
            {
                Key.D1 or Key.NumPad1 => "Home",
                Key.D2 or Key.NumPad2 => "Library",
                Key.D3 or Key.NumPad3 => "Games",
                Key.D4 or Key.NumPad4 => "Entertainment",
                Key.D5 or Key.NumPad5 => "Search",
                Key.D6 or Key.NumPad6 => "Store",
                Key.D7 or Key.NumPad7 => "Settings",
                _ => null
            };

            if (page is not null)
            {
                Navigation_Click(new Button { Tag = page }, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.Escape && DynamicPageContent.Visibility == Visibility.Visible)
        {
            Navigation_Click(new Button { Tag = "Home" }, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _isFullscreen)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _previousWindowStyle = WindowStyle;
            _previousResizeMode = ResizeMode;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullscreen = true;
            return;
        }

        WindowStyle = _previousWindowStyle;
        ResizeMode = _previousResizeMode;
        WindowState = WindowState.Normal;
        _isFullscreen = false;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
                yield return typed;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private enum FocusDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    private sealed record AppEntry(
        string Name,
        string Category,
        string Description,
        bool IsVerified = false,
        string? PackageName = null);
}
