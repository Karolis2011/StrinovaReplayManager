using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StrinovaReplayManager.Helpers;

namespace StrinovaReplayManager.Services;

public sealed class ReplayDialogService : IReplayDialogService
{
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
}