using Microsoft.UI.Xaml;

namespace StrinovaReplayManager.Services;

public interface IReplayDialogService
{
    void AttachHost(FrameworkElement host);

    void DetachHost(FrameworkElement host);

    Task<bool> ConfirmDeleteAsync(string title, string content);

    Task<bool> ConfirmProceedAsync(string title, string content);
}