using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Controls;

using StrinovaReplayManager.Helpers;

using StrinovaReplayManager.Models;



namespace StrinovaReplayManager.ViewModels;



public partial class RemapViewModel : ObservableObject

{

    private readonly AppServices _services;



    public RemapViewModel(AppServices services)

    {

        _services = services;

    }



    public ObservableCollection<ReplayEntry> Targets { get; } = [];



    [ObservableProperty]

    private string _infoMessage = string.Empty;



    [ObservableProperty]

    private bool _isInfoOpen;



    [ObservableProperty]

    private InfoBarSeverity _infoSeverity = InfoBarSeverity.Informational;



    [ObservableProperty]

    private ReplayEntry? _selectedTarget;



    private MappedReplay? _slot;

    private string _contextMessage = string.Empty;



    public void Initialize(RemapContext context)

    {

        _slot = context.Slot;

        _contextMessage = ResourceStrings.Format(

            "Message_RemapContext",

            ReplayDisplayHelper.GetSummaryLabel(context.Slot.SlotEntry, context.Slot.SlotFileName),

            ReplayDisplayHelper.GetSummaryLabel(context.Slot.Entry, Path.GetFileName(context.Slot.TargetPath)));

        ShowContextInfo();

        _ = LoadTargetsAsync();

    }



    [RelayCommand]

    private async Task ApplyAsync()

    {

        if (_slot is null || SelectedTarget is null)

        {

            ShowError(ResourceStrings.Get("Message_RemapSelectTarget"));

            return;

        }

        if (_services.GameProcess.IsGameRunning()
            && !await _services.Dialogs.ConfirmProceedAsync(
                ResourceStrings.Get("Message_GameRunningRemapConfirmTitle"),
                ResourceStrings.Get("Message_GameRunningRemapConfirm")).ConfigureAwait(true))
        {
            return;
        }

        try

        {

            _services.Remap.Apply(_slot, SelectedTarget.FilePath);

            if (_services.RootFrame?.CanGoBack == true)

            {

                _services.RootFrame.GoBack();

            }



            await _services.MainViewModel.RefreshAfterRemapAsync().ConfigureAwait(true);

            _services.MainViewModel.ShowRemapSuccess(_slot, SelectedTarget);

        }

        catch (Exception ex)

        {

            ShowError(ResourceStrings.Format("Message_RemapFailed", ex.Message));

        }

    }



    [RelayCommand]

    private void Cancel()

    {

        if (_services.RootFrame?.CanGoBack == true)

        {

            _services.RootFrame.GoBack();

        }

    }



    private async Task LoadTargetsAsync()

    {

        var targets = await _services.Catalog.ListOriginalsAsync().ConfigureAwait(true);

        Targets.Clear();

        foreach (var target in targets)

        {

            Targets.Add(target);

        }

    }



    private void ShowContextInfo()
    {
        if (_services.GameProcess.IsGameRunning())
        {
            InfoMessage = $"{_contextMessage}\n\n{ResourceStrings.Get("Message_GameRunningRemapWarning")}";
            InfoSeverity = InfoBarSeverity.Warning;
        }
        else
        {
            InfoMessage = _contextMessage;
            InfoSeverity = InfoBarSeverity.Informational;
        }

        IsInfoOpen = true;
    }



    private void ShowError(string message)

    {

        InfoMessage = message;

        InfoSeverity = InfoBarSeverity.Error;

        IsInfoOpen = true;

    }

}

