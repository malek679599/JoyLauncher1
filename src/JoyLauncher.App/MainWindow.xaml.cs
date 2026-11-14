using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace JoyLauncher.App;

public partial class MainWindow : Window
{
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
        RuntimeStatusText.Text = "Detection pending";
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
        AddIntro(root, "Your apps", "These preview entries demonstrate the layout. They are not yet verified as installed.");

        var filters = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 18)
        };
        var grid = new WrapPanel();
        void Show(string category)
        {
            grid.Children.Clear();
            var items = category == "All apps"
                ? SampleApps
                : SampleApps.Where(app => app.Category == category).ToArray();

            foreach (var app in items)
                grid.Children.Add(CreateAppCard(app));
        }

        foreach (var label in new[] { "All apps", "Games", "Entertainment", "Productivity", "System" })
        {
            var filter = CreateFilterButton(label);
            filter.Click += (_, _) => Show(label);
            filters.Children.Add(filter);
        }

        root.Children.Add(filters);
        root.Children.Add(grid);
        Show("All apps");
        root.Children.Add(CreateNotice(
            "Live app discovery is not connected",
            "The final library will be populated from BlueStacks. JoyLauncher will not invent package IDs or mark sample tiles as installed."));
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
        void UpdateResults()
        {
            results.Children.Clear();
            var query = search.Text.Trim();
            var matches = SampleApps.Where(app =>
                app.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                app.Category.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var app in matches)
                results.Children.Add(CreateAppCard(app));

            if (results.Children.Count == 0)
                root.Children.Add(CreateNotice("No matching sample apps", "Try another name or category."));
        }

        search.TextChanged += (_, _) =>
        {
            results.Children.Clear();
            var query = search.Text.Trim();
            foreach (var app in SampleApps.Where(app =>
                         app.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         app.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
                results.Children.Add(CreateAppCard(app));
        };

        root.Children.Add(search);
        root.Children.Add(results);
        UpdateResults();
        root.Children.Add(CreateNotice("Search scope", "Search currently covers preview entries only. Live results require BlueStacks package discovery."));
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
        AddIntro(root, "Runtime and preferences", "Local settings will be saved on this Windows device.");

        root.Children.Add(CreateInfoCard(
            "BlueStacks detection",
            "Pending implementation",
            "Discover the installed player executable and configured Android Debug Bridge endpoint. Do not assume a fixed installation path or port."));

        root.Children.Add(CreateInfoCard(
            "Installed app discovery",
            "Not connected",
            "Read the Android package list through the discovered ADB connection. Display package names and friendly labels only when verified."));

        root.Children.Add(CreateInfoCard(
            "Diagnostics",
            "Read-only checks first",
            "Report missing executables, unavailable ADB connections, command timeouts, and permission errors with actionable details."));

        root.Children.Add(CreateInfoCard(
            "Preferences",
            "Planned",
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
            Foreground = (Brush)Application.Current.FindResource("SecondaryTextBrush"),
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
            Text = app.Category + " · preview",
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

    private sealed record AppEntry(string Name, string Category, string Description);
}
