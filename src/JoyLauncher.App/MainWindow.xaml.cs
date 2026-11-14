using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using JoyLauncher.App.Services;

namespace JoyLauncher.App;

public partial class MainWindow : Window
{
    private readonly BlueStacksDiscoveryService _blueStacksDiscovery = new();
    private readonly AdbClient _adbClient = new();
    private BlueStacksDiscoveryResult? _discoveryResult;
    private IReadOnlyList<string> _installedPackages = Array.Empty<string>();
    private string? _connectedDeviceSerial;
    private string? _adbError;

    private static readonly IReadOnlyList<AppEntry> SampleApps =
    [
        new("Minecraft", "Games", "Featured sandbox game"),
        new("YouTube", "Entertainment", "Video and creator content"),
        new("YouTube Kids", "Entertainment", "Family-focused video app"),
        new("Play Store", "Store", "Android app marketplace"),
        new("Chrome", "Productivity", "Web browser"),
        new("Files", "System", "File manager"),
        new("Camera", "System", "Camera app"),
        new("Settings", "System", "Android settings")
    ];

    public MainWindow()
    {
        InitializeComponent();
        RuntimeStatusText.Text = "Waiting for detection";
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshRuntimeStatusAsync();
    }

    private async Task RefreshRuntimeStatusAsync()
    {
        RuntimeStatusText.Text = "Scanning BlueStacks…";
        _installedPackages = Array.Empty<string>();
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

        if (DynamicPageContent.Visibility == Visibility.Visible && PageTitle.Text == "Settings")
            DynamicPageContent.Content = BuildSettingsPage();
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
            _adbError = scan.Error ?? "ADB connection failed for an unknown reason.";
            RuntimeStatusText.Text = "ADB connection failed";
        }
        else
        {
            _connectedDeviceSerial = scan.DeviceSerial;
            _installedPackages = scan.PackageNames;
            RuntimeStatusText.Text = $"ADB connected · {_installedPackages.Count} packages";
        }

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
        "Games" => BuildCategoryPage("Games", "Sample game tiles. Real entries will come from BlueStacks package discovery.", "Games"),
        "Entertainment" => BuildCategoryPage("Entertainment", "Your video and entertainment apps, once discovered from the Android runtime.", "Entertainment"),
        "Search" => BuildSearchPage(),
        "Store" => BuildStorePage(),
        "Settings" => BuildSettingsPage(),
        _ => BuildLibraryPage()
    };

    private FrameworkElement BuildLibraryPage()
    {
        var root = CreatePageStack();
        var liveApps = _installedPackages
            .Select(packageName => new AppEntry(packageName, "Installed Android package", "Discovered from BlueStacks ADB", IsVerified: true))
            .ToArray();
        var hasLiveApps = liveApps.Length > 0;
        var catalogue = hasLiveApps ? liveApps : SampleApps;

        AddIntro(root, "Your apps", hasLiveApps
            ? $"Live package list from {_connectedDeviceSerial}. Friendly app names and icons are a later metadata step."
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
            var items = hasLiveApps
                ? catalogue
                : category == "All apps"
                    ? catalogue
                    : catalogue.Where(app => app.Category == category).ToArray();

            foreach (var app in items)
                grid.Children.Add(CreateAppCard(app));
        }

        var filterLabels = hasLiveApps
            ? new[] { "All apps" }
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
                ? $"{liveApps.Length} package names were returned by BlueStacks ADB. App display names and icons have not yet been resolved."
                : "Connect to a discovered BlueStacks ADB endpoint from Settings to replace these sample cards with real package data."));
        return root;
    }

    private FrameworkElement BuildCategoryPage(string title, string description, string category)
    {
        var root = CreatePageStack();
        AddIntro(root, title, description);
        var grid = new WrapPanel { Margin = new Thickness(0, 14, 0, 0) };
        foreach (var app in SampleApps.Where(app => app.Category == category))
            grid.Children.Add(CreateAppCard(app));

        if (grid.Children.Count == 0)
            root.Children.Add(CreateNotice("No sample entries", "This category has no preview entries yet."));
        else
            root.Children.Add(grid);

        root.Children.Add(CreateNotice(
            "Preview data only",
            "Cards become launchable only after BlueStacks detection and Android package discovery are implemented."));
        return root;
    }

    private FrameworkElement BuildSearchPage()
    {
        var root = CreatePageStack();
        AddIntro(root, "Search your apps", "Search the sample catalogue by app name or category.");

        var searchCatalogue = _installedPackages.Count > 0
            ? _installedPackages.Select(packageName => new AppEntry(packageName, "Installed Android package", "Discovered from BlueStacks ADB", IsVerified: true)).ToArray()
            : SampleApps;
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
            Text = "No matching sample apps. Try another name or category.",
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
                         app.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
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
            "Search currently covers preview entries only. Live results require BlueStacks package discovery."));
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

        var adbState = result is null
            ? "NOT SCANNED"
            : result.HasConfiguredAdbEndpoint
                ? "CONFIGURATION FOUND · NOT CONNECTED"
                : "NO CONFIGURED ENDPOINT FOUND";
        var adbDetails = result?.AdbEndpoints.Count > 0
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
                    Background = (Brush)FindResource("AccentSoftBrush"),
                    Click = ConnectAndScan_Click
                };
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
            Foreground = Brushes.White,
            Click = RefreshDetection_Click
        };
        root.Children.Add(refresh);

        root.Children.Add(CreateInfoCard(
            "Next integration step",
            "NOT CONNECTED",
            "After detection is verified, the next milestone will test the discovered endpoint with a time-limited ADB command, then list installed packages. No app is reported as installed or launchable before that succeeds."));

        root.Children.Add(CreateInfoCard(
            "Preferences",
            "PLANNED",
            "Fullscreen startup, last-opened page, keyboard navigation, controller navigation, and local appearance preferences."));
        return root;
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

    private Border CreateAppCard(AppEntry app)
    {
        var content = new StackPanel();
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

    private void LaunchFeatured_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "BlueStacks app discovery and launch integration are not implemented yet. The button is intentionally non-launching in this prototype.",
            "Runtime integration pending",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DynamicPageContent.Visibility == Visibility.Visible)
        {
            Navigation_Click(new Button { Tag = "Home" }, new RoutedEventArgs());
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            e.Handled = true;
        }
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

    private sealed record AppEntry(string Name, string Category, string Description, bool IsVerified = false);
}
