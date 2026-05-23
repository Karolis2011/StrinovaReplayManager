using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StrinovaReplayManager.ViewModels;

namespace StrinovaReplayManager.Views;

public sealed partial class SetupPage : Page
{
    public SetupViewModel ViewModel { get; }

    public SetupPage()
    {
        ViewModel = App.Services.SetupViewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.StatusMessage = Helpers.ResourceStrings.Get("Message_SetupIntro");
    }
}

