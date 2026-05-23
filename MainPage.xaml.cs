using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using StrinovaReplayManager.Models;
using StrinovaReplayManager.ViewModels;

namespace StrinovaReplayManager.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Services.MainViewModel;
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        var mode = e.Parameter is AppMode m ? m : App.Services.CurrentMode;
        ViewModel.Initialize(mode);
        App.Services.Dialogs.AttachHost(this);
        await ViewModel.OnLoadedAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        App.Services.Dialogs.DetachHost(this);
        ViewModel.OnUnloaded();
    }

    private void MappedGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel.SelectedMapped is null)
        {
            return;
        }

        if (ViewModel.CanRemap)
        {
            ViewModel.RemapSelectedCommand.Execute(null);
        }
    }

    private void StatusInfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        ViewModel.OnStatusInfoBarClosed();
    }

    private void DeleteSlotButton_Click(object sender, RoutedEventArgs e)
    {
        var slot = (sender as Button)?.Tag as MappedReplay
            ?? (sender as FrameworkElement)?.DataContext as MappedReplay;
        if (slot is not null)
        {
            ViewModel.DeleteSlotCommand.Execute(slot);
        }
    }

    private void DeleteOriginalButton_Click(object sender, RoutedEventArgs e)
    {
        var entry = (sender as Button)?.Tag as ReplayEntry
            ?? (sender as FrameworkElement)?.DataContext as ReplayEntry;
        if (entry is not null)
        {
            ViewModel.DeleteOriginalCommand.Execute(entry);
        }
    }
}

