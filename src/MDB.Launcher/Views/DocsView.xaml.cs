using System.Reflection;
using System.Windows.Controls;
using MDB.Launcher.ViewModels;

namespace MDB.Launcher.Views;

public partial class DocsView : UserControl
{
    private const string DocsHost = "zaclin-git.github.io";

    // JS to remove all GitHub links and strip target="_blank" so links stay in-frame
    private const string CleanupScript =
        "var links=document.querySelectorAll('a[href*=\"github.com\"]');" +
        "for(var i=0;i<links.length;i++){" +
        "var t=links[i].textContent||'';" +
        "if(t.indexOf('GitHub')!==-1||t.indexOf('View on')!==-1){links[i].style.display='none';}}" +
        "var all=document.querySelectorAll('a[target]');" +
        "for(var j=0;j<all.length;j++){all[j].removeAttribute('target');}";

    public DocsView()
    {
        InitializeComponent();
        Loaded += DocsView_Loaded;

        // Suppress script error popups from the WebBrowser control
        SuppressScriptErrors(DocsBrowser);
    }

    private void DocsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DocsViewModel vm)
        {
            try
            {
                DocsBrowser.Navigate(new Uri(vm.DocsUrl));
            }
            catch
            {
                // Browser control unavailable
            }
        }
    }

    private void DocsBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
    {
        // Block any navigation away from the docs site
        if (e.Uri != null && !e.Uri.Host.Equals(DocsHost, StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
        }
    }

    private void DocsBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        if (DataContext is DocsViewModel vm)
        {
            vm.OnPageLoaded();
        }

        // Inject JS to remove GitHub buttons and strip target="_blank"
        try
        {
            DocsBrowser.InvokeScript("execScript", [CleanupScript, "JavaScript"]);
        }
        catch
        {
            // Script injection may fail — not critical
        }
    }

    /// <summary>
    /// Suppress script error dialogs in the WPF WebBrowser by setting
    /// the underlying ActiveX SilentMode property via reflection.
    /// </summary>
    private static void SuppressScriptErrors(System.Windows.Controls.WebBrowser browser)
    {
        try
        {
            var fi = typeof(System.Windows.Controls.WebBrowser).GetField("_axIWebBrowser2",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (fi == null) return;

            void OnNavigated(object? s, System.Windows.Navigation.NavigationEventArgs args)
            {
                var ax = fi.GetValue(browser);
                if (ax != null)
                {
                    ax.GetType().InvokeMember("Silent",
                        BindingFlags.SetProperty, null, ax, [true]);
                    browser.Navigated -= OnNavigated;
                }
            }

            browser.Navigated += OnNavigated;
        }
        catch
        {
            // Reflection may fail — non-critical
        }
    }
}
