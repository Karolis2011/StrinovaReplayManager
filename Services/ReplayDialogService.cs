using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using StrinovaReplayManager.Helpers;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public sealed class ReplayDialogService : IReplayDialogService
{
    private static readonly Lazy<DataTemplate> RecoveryItemTemplate = new(CreateRecoveryItemTemplate);

    private FrameworkElement? _host;

    public void AttachHost(FrameworkElement host)
    {
        _host = host;
    }

    public void DetachHost(FrameworkElement host)
    {
        if (ReferenceEquals(_host, host))
        {
            _host = null;
        }
    }

    public async Task<bool> ConfirmDeleteAsync(string title, string content)
    {
        if (_host?.XamlRoot is null)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = _host.XamlRoot,
            Title = title,
            Content = content,
            PrimaryButtonText = ResourceStrings.Get("DialogDeleteButtonText"),
            CloseButtonText = ResourceStrings.Get("DialogCancelButtonText"),
            DefaultButton = ContentDialogButton.Primary,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public async Task<bool> ConfirmProceedAsync(string title, string content)
    {
        if (_host?.XamlRoot is null)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = _host.XamlRoot,
            Title = title,
            Content = content,
            PrimaryButtonText = ResourceStrings.Get("DialogProceedButtonText"),
            CloseButtonText = ResourceStrings.Get("DialogCancelButtonText"),
            DefaultButton = ContentDialogButton.Close,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public async Task<bool> ConfirmReplaceAsync(string title, string content)
    {
        if (_host?.XamlRoot is null)
        {
            return false;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = _host.XamlRoot,
            Title = title,
            Content = content,
            PrimaryButtonText = ResourceStrings.Get("DialogReplaceButtonText"),
            CloseButtonText = ResourceStrings.Get("DialogCancelButtonText"),
            DefaultButton = ContentDialogButton.Close,
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    public async Task<DeletedReplaySnapshot?> PickRecoveryAsync(IReadOnlyList<DeletedReplaySnapshot> items)
    {
        if (_host?.XamlRoot is null || items.Count == 0)
        {
            return null;
        }

        DeletedReplaySnapshot? selected = null;

        var subtitle = new TextBlock
        {
            Text = ResourceStrings.Get("Message_RecoverReplayDialogSubtitle"),
            TextWrapping = TextWrapping.WrapWholeWords,
            Margin = new Thickness(0, 0, 0, 12),
        };

        var rows = items.Select(RecoveryListItem.FromSnapshot).ToList();

        var list = new ListView
        {
            ItemsSource = rows,
            ItemTemplate = RecoveryItemTemplate.Value,
            SelectionMode = ListViewSelectionMode.Single,
            MaxHeight = 320,
        };

        list.SelectionChanged += (_, _) =>
        {
            selected = (list.SelectedItem as RecoveryListItem)?.Snapshot;
        };

        var panel = new StackPanel { Spacing = 0 };
        panel.Children.Add(subtitle);
        panel.Children.Add(list);

        var dialog = new ContentDialog
        {
            XamlRoot = _host.XamlRoot,
            Title = ResourceStrings.Get("Message_RecoverReplayDialogTitle"),
            Content = panel,
            PrimaryButtonText = ResourceStrings.Get("DialogRecoverButtonText"),
            CloseButtonText = ResourceStrings.Get("DialogCancelButtonText"),
            DefaultButton = ContentDialogButton.Primary,
            IsPrimaryButtonEnabled = false,
        };

        list.SelectionChanged += (_, _) =>
        {
            dialog.IsPrimaryButtonEnabled = list.SelectedItem is RecoveryListItem;
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? selected : null;
    }

    private static DataTemplate CreateRecoveryItemTemplate()
    {
        const string xaml = """
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <StackPanel Padding="8,6">
                    <TextBlock Text="{Binding PrimaryLine}" TextWrapping="WrapWholeWords" />
                    <TextBlock Text="{Binding SecondaryLine}" Opacity="0.8" FontSize="12" TextWrapping="WrapWholeWords" />
                </StackPanel>
            </DataTemplate>
            """;

        return (DataTemplate)XamlReader.Load(xaml);
    }
}