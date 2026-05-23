using Microsoft.UI.Xaml;

namespace StrinovaReplayManager.Helpers;

public static class UiHelpers
{
    public static Visibility BoolToVisibility(bool value)
        => value ? Visibility.Visible : Visibility.Collapsed;
}

