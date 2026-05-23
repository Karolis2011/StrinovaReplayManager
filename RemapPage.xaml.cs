using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using StrinovaReplayManager.Models;
using StrinovaReplayManager.ViewModels;

namespace StrinovaReplayManager.Views;

public sealed partial class RemapPage : Page
{
    public RemapViewModel ViewModel { get; } = App.Services.RemapViewModel;

    public RemapPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        App.Services.Dialogs.AttachHost(this);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        App.Services.Dialogs.DetachHost(this);
    }

    private void TargetList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ResolveListItem<ReplayEntry>(sender, e) is not { } target)
        {
            return;
        }

        ViewModel.SelectedTarget = target;
        ViewModel.ApplyCommand.Execute(null);
    }

    private static T? ResolveListItem<T>(object sender, DoubleTappedRoutedEventArgs e)
        where T : class
    {
        if (sender is ListView listView)
        {
            if (listView.SelectedItem is T selected)
            {
                return selected;
            }

            if (e.OriginalSource is DependencyObject source)
            {
                var element = source;
                while (element is not null)
                {
                    if (element is ListViewItem { Content: T item })
                    {
                        return item;
                    }

                    element = VisualTreeHelper.GetParent(element);
                }
            }
        }

        return null;
    }
}

