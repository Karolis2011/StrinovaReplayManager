using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StrinovaReplayManager.Helpers;
using StrinovaReplayManager.Models;
using StrinovaReplayManager.Views;

namespace StrinovaReplayManager.ViewModels;

public partial class SetupViewModel : ObservableObject
{
    private readonly AppServices _services;

    public SetupViewModel(AppServices services)
    {
        _services = services;
        AlwaysElevate = _services.Settings.LoadAlwaysElevate();
        ShowAlwaysElevateOption = _services.SymlinkCapability.IsElevated;
    }

    [ObservableProperty]
    private string _statusMessage = ResourceStrings.Get("Message_SetupIntro");

    [ObservableProperty]
    private bool _alwaysElevate;

    [ObservableProperty]
    private bool _showAlwaysElevateOption;

    [RelayCommand]
    private void OpenDeveloperSettings()
    {
        _services.SymlinkCapability.OpenDeveloperSettings();
        StatusMessage = ResourceStrings.Get("Message_SetupOpenedDevSettings");
    }

    [RelayCommand]
    private void RestartAsAdmin()
    {
        if (_services.SymlinkCapability.TryRelaunchElevated())
        {
            Environment.Exit(0);
        }
        else
        {
            StatusMessage = ResourceStrings.Get("Message_SetupElevationFailed");
        }
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        StatusMessage = ResourceStrings.Get("Message_SetupProbing");
        var ok = await _services.SymlinkCapability.ProbeAsync().ConfigureAwait(true);
        if (ok)
        {
            _services.Settings.SaveAlwaysElevate(AlwaysElevate);
            NavigateToMain(AppMode.Full);
        }
        else
        {
            StatusMessage = ResourceStrings.Get("Message_SetupProbeFailed");
            ShowAlwaysElevateOption = _services.SymlinkCapability.IsElevated;
        }
    }

    [RelayCommand]
    private void ContinueViewOnly()
    {
        NavigateToMain(AppMode.ViewOnly);
    }

    partial void OnAlwaysElevateChanged(bool value)
    {
        _services.Settings.SaveAlwaysElevate(value);
    }

    private void NavigateToMain(AppMode mode)
    {
        _services.CurrentMode = mode;
        _services.Settings.SaveAppMode(mode);
        _services.RootFrame?.Navigate(typeof(MainPage), mode);
    }
}

