using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MDB.Launcher.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Bubble mouse-wheel events from the ListBox up to the parent ScrollViewer
    /// so the page scrolls even when the cursor is over the profile cards.
    /// </summary>
    private void ListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };
        RootScroll.RaiseEvent(args);
    }
}
