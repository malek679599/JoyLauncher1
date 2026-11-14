using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace JoyLauncher.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RuntimeStatusText.Text = "Not connected yet";
    }

    private void Navigation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string page)
            return;

        PageTitle.Text = page;
        PageSubtitle.Text = page switch
        {
            "Home" => "Everything you play and watch, in one place.",
            "Library" => "All your installed Android apps.",
            "Games" => "Your games, ready when you are.",
            "Entertainment" => "Video and entertainment apps.",
            "Search" => "Find an app in your library.",
            "Store" => "App discovery will be added in a later milestone.",
            "Settings" => "Configure JoyLauncher and its runtime.",
            _ => "Your Android hub."
        };

        foreach (var item in FindVisualChildren<Button>(this))
        {
            if (item.Tag is string tag && tag is "Home" or "Library" or "Games" or "Entertainment" or "Search" or "Store")
                item.Background = tag == page ? (System.Windows.Media.Brush)FindResource("AccentSoftBrush") : (System.Windows.Media.Brush)FindResource("SurfaceBrush");
        }

        if (page != "Home")
        {
            HomeContent.Visibility = Visibility.Collapsed;
            if (page == "Library")
                PageSubtitle.Text = "Live app discovery is not connected yet. This screen will show apps installed in BlueStacks.";
            else if (page == "Settings")
                PageSubtitle.Text = "Runtime discovery, appearance, and local preferences will be configured here.";
        }
        else
        {
            HomeContent.Visibility = Visibility.Visible;
        }
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
        if (e.Key == Key.Escape && WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            e.Handled = true;
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
                yield return typed;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }
}
