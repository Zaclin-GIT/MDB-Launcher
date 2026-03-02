using CommunityToolkit.Mvvm.ComponentModel;

namespace MDB.Launcher.ViewModels;

/// <summary>
/// ViewModel for the in-app documentation view.
/// </summary>
public partial class DocsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _docsUrl = "https://zaclin-git.github.io/MDB/?embedded=1";

    [ObservableProperty]
    private bool _isLoading = true;

    public void OnNavigated()
    {
        IsLoading = true;
    }

    public void OnPageLoaded()
    {
        IsLoading = false;
    }
}
