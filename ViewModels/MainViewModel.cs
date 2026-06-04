using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Dispatching;

using StrinovaReplayManager.Helpers;

using StrinovaReplayManager.Models;

using StrinovaReplayManager.Services;

using StrinovaReplayManager.Views;



namespace StrinovaReplayManager.ViewModels;



public partial class MainViewModel : ObservableObject

{

    private readonly AppServices _services;

    private readonly IReplayDialogService _dialogs;

    private readonly DispatcherQueue _dispatcher;

    private bool _watcherHooked;

    private bool _suppressSelectionSync;

    private CancellationTokenSource? _cloudProbeCts;



    public MainViewModel(AppServices services)

    {
        _dialogs = services.Dialogs;

        _services = services;

        _dispatcher = DispatcherQueue.GetForCurrentThread();

    }



    public ObservableCollection<MappedReplay> MappedReplays { get; } = [];



    public ObservableCollection<ReplayEntry> OriginalEntries { get; } = [];



    [ObservableProperty]

    private MappedReplay? _selectedMapped;



    [ObservableProperty]

    private ReplayEntry? _selectedOriginal;



    private string _modeHintMessage = string.Empty;

    private bool _statusIsTransient;



    [ObservableProperty]

    private string _statusInfoMessage = string.Empty;



    [ObservableProperty]

    private bool _isStatusInfoOpen;



    [ObservableProperty]

    private bool _isFullMode;



    [ObservableProperty]

    private bool _canRemap;



    [ObservableProperty]

    private bool _canSaveAs;



    [ObservableProperty]

    private bool _canCopyReplay;

    [ObservableProperty]
    private bool _canDownloadFromServer;

    [ObservableProperty]
    private bool _isCheckingServer;

    [ObservableProperty]
    private bool _canRecoverReplay;

    [ObservableProperty]
    private string _downloadFromServerTooltip = string.Empty;

    public void Initialize(AppMode mode)

    {

        IsFullMode = mode == AppMode.Full;

        _services.CurrentMode = mode;

        UpdateStatusForMode();

        UpdateRecoverReplayCommand();

    }



    public async Task OnLoadedAsync()

    {

        if (IsFullMode)

        {

            if (!_watcherHooked)

            {

                _services.Watcher.Changed += OnWatcherChanged;

                _services.Watcher.Start();

                _watcherHooked = true;

            }



            await RefreshAsync(runIngest: true).ConfigureAwait(true);

        }

        else

        {

            await RefreshAsync(runIngest: false).ConfigureAwait(true);

        }

    }



    public void OnUnloaded()

    {

        if (_watcherHooked)

        {

            _services.Watcher.Changed -= OnWatcherChanged;

            _services.Watcher.Stop();

            _watcherHooked = false;

        }

        _cloudProbeCts?.Cancel();
        _cloudProbeCts?.Dispose();
        _cloudProbeCts = null;

    }



    [RelayCommand]

    private async Task RefreshListAsync()

    {

        await RefreshAsync(runIngest: IsFullMode).ConfigureAwait(true);

    }



    public Task RefreshAfterRemapAsync() => RefreshAsync(runIngest: false);

    [RelayCommand]
    public async Task DeleteSlotAsync(MappedReplay slot)
    {
        if (!await _dialogs.ConfirmDeleteAsync(
            ResourceStrings.Get("Message_DeleteSlotConfirmTitle"),
            ResourceStrings.Get("Message_DeleteSlotConfirm")).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            _services.FileSystem.DeleteFile(slot.SymlinkPath);
            SelectedMapped = null;
            ShowStatusInfo(ResourceStrings.Format("Message_DeleteSlotSuccess", slot.SlotFileName));
            await RefreshAsync(runIngest: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowStatusInfo(ResourceStrings.Format("Message_DeleteFailed", ex.Message));
        }
    }

    [RelayCommand]
    public async Task DeleteOriginalAsync(ReplayEntry entry)
    {
        if (!await _dialogs.ConfirmDeleteAsync(
            ResourceStrings.Get("Message_DeleteOriginalConfirmTitle"),
            ResourceStrings.Get("Message_DeleteOriginalConfirm")).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            _services.DeletedReplayTracker.RecordDeleted(entry);
            _services.FileSystem.DeleteFile(entry.FilePath);
            SelectedOriginal = null;

            // Clean up broken symlinks that pointed to this original.
            await CleanupBrokenSymlinksAfterOriginalDeletionAsync(entry.FileName).ConfigureAwait(true);

            ShowStatusInfo(ResourceStrings.Format("Message_DeleteOriginalSuccess", entry.FileName));
            UpdateRecoverReplayCommand();
            await RefreshAsync(runIngest: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            ShowStatusInfo(ResourceStrings.Format("Message_DeleteFailed", ex.Message));
        }
    }



    [RelayCommand]

    private void RemapSelected()

    {

        if (SelectedMapped is null || !IsFullMode)

        {

            return;

        }



        _services.RemapViewModel.Initialize(new RemapContext { Slot = SelectedMapped });

        _services.RootFrame?.Navigate(typeof(RemapPage), _services.RemapViewModel);

    }



    [RelayCommand]

    private async Task ImportAsync()

    {

        if (!IsFullMode)

        {

            ShowStatusInfo(ResourceStrings.Get("Message_MainViewOnlyImport"));

            return;

        }



        var result = await _services.Import.ImportAsync().ConfigureAwait(true);

        if (result.Cancelled)

        {

            return;

        }



        if (!string.IsNullOrEmpty(result.ErrorMessage))

        {

            ShowStatusInfo(result.ErrorMessage);

            return;

        }



        ShowStatusInfo(result.Entry?.IsParsed == false
            ? ResourceStrings.Get("Message_ImportUnparsedSuccess")
            : ResourceStrings.Get("Message_ImportSuccessHint"));

        await RefreshAsync(runIngest: false).ConfigureAwait(true);

    }



    [RelayCommand]

    private async Task SaveAsAsync()

    {

        if (SelectedOriginal is not { } entry)

        {

            return;

        }



        var exportResult = await _services.Export.ExportAsync(entry).ConfigureAwait(true);

        ShowStatusInfo(exportResult switch

        {

            Services.ExportResult.Success => ResourceStrings.Format("Message_ExportSuccess", entry.FileName),

            Services.ExportResult.Cancelled => ResourceStrings.Get("Message_ExportCancelled"),

            _ => ResourceStrings.Get("Message_ExportFailed"),

        });

    }



    [RelayCommand]

    private async Task CopyReplayAsync()

    {

        if (SelectedOriginal is not { } entry)

        {

            return;

        }



        var clipboardResult = await _services.Clipboard.CopyReplayAsync(entry).ConfigureAwait(true);

        ShowStatusInfo(clipboardResult switch

        {

            Services.ClipboardResult.Success => ResourceStrings.Format("Message_ClipboardSuccess", entry.FileName),

            _ => ResourceStrings.Get("Message_ClipboardFailed"),

        });

    }



    partial void OnSelectedMappedChanged(MappedReplay? value)

    {

        CanRemap = value is not null && IsFullMode;

        if (value is not null && !_suppressSelectionSync)

        {

            _suppressSelectionSync = true;

            SelectedOriginal = null;

            _suppressSelectionSync = false;

        }



        UpdateShareCommands();

    }



    partial void OnSelectedOriginalChanged(ReplayEntry? value)

    {

        if (value is not null && !_suppressSelectionSync)

        {

            _suppressSelectionSync = true;

            SelectedMapped = null;

            _suppressSelectionSync = false;

        }



        UpdateShareCommands();

        _ = ProbeSelectedOriginalAsync(value);

    }



    partial void OnIsFullModeChanged(bool value)

    {

        CanRemap = SelectedMapped is not null && value;

        UpdateStatusForMode();

        UpdateShareCommands();

        UpdateRecoverReplayCommand();

    }



    private void UpdateShareCommands()

    {

        var canShare = SelectedOriginal is not null;

        CanSaveAs = canShare;

        CanCopyReplay = canShare;

        if (!IsFullMode || SelectedOriginal is null)
        {
            CanDownloadFromServer = false;
            IsCheckingServer = false;
            DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadViewOnly");
            return;
        }

        if (IsCheckingServer)
        {
            CanDownloadFromServer = false;
            DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadChecking");
        }
    }

    private void UpdateRecoverReplayCommand()
        => CanRecoverReplay = IsFullMode && _services.DeletedReplayTracker.HasRecoverableEntries;

    [RelayCommand]
    private async Task DownloadFromServerAsync()
    {
        if (!IsFullMode || SelectedOriginal is not { } entry)
        {
            return;
        }

        var destPath = entry.FilePath;
        if (_services.FileSystem.FileExists(destPath)
            && new FileInfo(destPath).Length > 0
            && !await _dialogs.ConfirmReplaceAsync(
                ResourceStrings.Get("Message_CloudReplaceConfirmTitle"),
                ResourceStrings.Format("Message_CloudReplaceConfirm", entry.FileName)).ConfigureAwait(true))
        {
            return;
        }

        ShowStatusInfo(ResourceStrings.Get("Message_CloudDownloadInProgress"));
        var result = await _services.CloudDownload.DownloadAsync(entry, destPath).ConfigureAwait(true);
        ApplyCloudDownloadResult(result, entry.FileName);
        if (result == ReplayCloudDownloadResult.Success)
        {
            _services.CloudDownload.InvalidateCacheForEntry(entry);
            await ProbeSelectedOriginalAsync(entry).ConfigureAwait(true);
            await RefreshAsync(runIngest: false).ConfigureAwait(true);
        }
        else if (SelectedOriginal?.FileName == entry.FileName)
        {
            await ProbeSelectedOriginalAsync(entry).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task RecoverReplayAsync()
    {
        if (!IsFullMode)
        {
            return;
        }

        var items = _services.DeletedReplayTracker.ListRecoverable();
        if (items.Count == 0)
        {
            UpdateRecoverReplayCommand();
            return;
        }

        var picked = await _dialogs.PickRecoveryAsync(items).ConfigureAwait(true);
        if (picked is null)
        {
            return;
        }

        if (ReplayCloudRetention.ShouldSkipProbe(picked))
        {
            ShowStatusInfo(ResourceStrings.Get("Message_CloudDownloadTooOld"));
            return;
        }

        var entry = picked.ToReplayEntry(_services.Paths.GetOriginalsPath(picked.FileName));
        var availability = await _services.CloudDownload.ProbeAsync(picked).ConfigureAwait(true);
        if (availability != ReplayCloudAvailability.Available)
        {
            ApplyCloudAvailabilityMessage(availability);
            return;
        }

        var destPath = entry.FilePath;
        if (_services.FileSystem.FileExists(destPath)
            && new FileInfo(destPath).Length > 0
            && !await _dialogs.ConfirmReplaceAsync(
                ResourceStrings.Get("Message_CloudReplaceConfirmTitle"),
                ResourceStrings.Format("Message_CloudReplaceConfirm", entry.FileName)).ConfigureAwait(true))
        {
            return;
        }

        ShowStatusInfo(ResourceStrings.Get("Message_CloudDownloadInProgress"));
        var result = await _services.CloudDownload.DownloadAsync(entry, destPath).ConfigureAwait(true);
        ApplyCloudDownloadResult(result, entry.FileName);
        if (result == ReplayCloudDownloadResult.Success)
        {
            _services.DeletedReplayTracker.Remove(picked.FileName);
            UpdateRecoverReplayCommand();
            await RefreshAsync(runIngest: false).ConfigureAwait(true);
            ShowStatusInfo(ResourceStrings.Get("Message_ImportSuccessHint"));
        }
    }

    private async Task ProbeSelectedOriginalAsync(ReplayEntry? entry)
    {
        _cloudProbeCts?.Cancel();
        _cloudProbeCts?.Dispose();
        _cloudProbeCts = new CancellationTokenSource();
        var token = _cloudProbeCts.Token;

        if (!IsFullMode || entry is null)
        {
            CanDownloadFromServer = false;
            IsCheckingServer = false;
            DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadViewOnly");
            return;
        }

        if (!entry.IsParsed)
        {
            CanDownloadFromServer = false;
            IsCheckingServer = false;
            DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadUnparsed");
            return;
        }

        if (ReplayCloudRetention.ShouldSkipProbe(entry))
        {
            CanDownloadFromServer = false;
            IsCheckingServer = false;
            DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadTooOld");
            return;
        }

        IsCheckingServer = true;
        CanDownloadFromServer = false;
        DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadChecking");

        try
        {
            var availability = await _services.CloudDownload.ProbeAsync(entry, token).ConfigureAwait(true);
            if (token.IsCancellationRequested || !ReferenceEquals(SelectedOriginal, entry))
            {
                return;
            }

            ApplyCloudAvailabilityToDownloadButton(availability);
        }
        catch (OperationCanceledException)
        {
            // Selection changed.
        }
        finally
        {
            if (!token.IsCancellationRequested && ReferenceEquals(SelectedOriginal, entry))
            {
                IsCheckingServer = false;
            }
        }
    }

    private void ApplyCloudAvailabilityToDownloadButton(ReplayCloudAvailability availability)
    {
        switch (availability)
        {
            case ReplayCloudAvailability.Available:
                CanDownloadFromServer = IsFullMode;
                DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadAvailable");
                break;
            case ReplayCloudAvailability.NotFound:
                CanDownloadFromServer = false;
                DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadNotOnServer");
                break;
            case ReplayCloudAvailability.TooOld:
                CanDownloadFromServer = false;
                DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadTooOld");
                break;
            case ReplayCloudAvailability.NotApplicable:
                CanDownloadFromServer = false;
                DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadUnparsed");
                break;
            case ReplayCloudAvailability.ProbeFailed:
                CanDownloadFromServer = false;
                DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadProbeFailed");
                break;
            default:
                CanDownloadFromServer = false;
                DownloadFromServerTooltip = ResourceStrings.Get("Message_CloudDownloadUnavailable");
                break;
        }
    }

    private void ApplyCloudAvailabilityMessage(ReplayCloudAvailability availability)
    {
        var key = availability switch
        {
            ReplayCloudAvailability.NotFound => "Message_CloudDownloadNotOnServer",
            ReplayCloudAvailability.TooOld => "Message_CloudDownloadTooOld",
            ReplayCloudAvailability.NotApplicable => "Message_CloudDownloadUnparsed",
            ReplayCloudAvailability.ProbeFailed => "Message_CloudDownloadProbeFailed",
            _ => "Message_CloudDownloadUnavailable",
        };
        ShowStatusInfo(ResourceStrings.Get(key));
    }

    private void ApplyCloudDownloadResult(ReplayCloudDownloadResult result, string fileName)
    {
        var message = result switch
        {
            ReplayCloudDownloadResult.Success => ResourceStrings.Format("Message_CloudDownloadSuccess", fileName),
            ReplayCloudDownloadResult.NotFound => ResourceStrings.Get("Message_CloudDownloadNotOnServer"),
            ReplayCloudDownloadResult.TooOld => ResourceStrings.Get("Message_CloudDownloadTooOld"),
            ReplayCloudDownloadResult.NotApplicable => ResourceStrings.Get("Message_CloudDownloadUnparsed"),
            ReplayCloudDownloadResult.Cancelled => ResourceStrings.Get("Message_CloudDownloadCancelled"),
            _ => ResourceStrings.Get("Message_CloudDownloadFailed"),
        };
        ShowStatusInfo(message);
    }

    private void OnWatcherChanged(object? sender, EventArgs e)

    {

        _ = _dispatcher.TryEnqueue(async () =>

        {

            await _services.Watcher.RunSerializedAsync(async () =>

            {

                await RefreshAsync(runIngest: true).ConfigureAwait(false);

            }).ConfigureAwait(false);

        });

    }



    private async Task RefreshAsync(bool runIngest)

    {

        if (runIngest && IsFullMode)

        {

            var ingest = _services.Ingest.Run(AppMode.Full);

            if (!ingest.Skipped)

            {

                ShowStatusInfo(ResourceStrings.Format("Message_IngestStatus", ingest.MovedCount, ingest.LinkedCount));

            }

        }



        var mapped = await _services.Catalog.ListMappedAsync(_services.CurrentMode).ConfigureAwait(true);

        MappedReplays.Clear();

        foreach (var item in mapped)

        {

            MappedReplays.Add(item);

        }



        var originals = await _services.Catalog.ListOriginalsAsync().ConfigureAwait(true);

        OriginalEntries.Clear();

        foreach (var item in originals)

        {

            OriginalEntries.Add(item);

        }



        if (!runIngest || !IsFullMode)

        {

            UpdateStatusForMode();

        }

        UpdateRecoverReplayCommand();

    }

    private async Task CleanupBrokenSymlinksAfterOriginalDeletionAsync(string deletedOriginalFileName)

    {

        await Task.Run(() =>

        {

            try

            {

                // Scan for symlinks in the demos directory

                var demosDir = _services.Paths.DemosDirectory;

                var originalsDir = _services.Paths.OriginalsDirectory;



                foreach (var demoEntry in _services.FileSystem.EnumerateDemosEntries(demosDir))

                {

                    // Only process .replay files

                    if (!demoEntry.EndsWith(".replay", StringComparison.OrdinalIgnoreCase))

                    {

                        continue;

                    }



                    // Skip if not a symlink

                    if (!_services.FileSystem.IsSymlink(demoEntry))

                    {

                        continue;

                    }



                    // Get the symlink target

                    var target = _services.FileSystem.GetSymlinkTarget(demoEntry);



                    // If target doesn't exist, this symlink is broken

                    if (string.IsNullOrEmpty(target) || !_services.FileSystem.FileExists(target))

                    {

                        var slotFileName = Path.GetFileName(demoEntry);



                        // Check if there's a matching original with the same name

                        var matchingOriginalPath = Path.Combine(originalsDir, slotFileName);

                        if (_services.FileSystem.FileExists(matchingOriginalPath))

                        {

                            // Relink the broken symlink to the matching original

                            try

                            {

                                _services.FileSystem.CreateSymlink(demoEntry, matchingOriginalPath);

                            }

                            catch

                            {

                                // If relink fails, delete the broken symlink

                                _services.FileSystem.DeleteFile(demoEntry);

                            }

                        }

                        else

                        {

                            // No matching original exists, delete the broken symlink

                            _services.FileSystem.DeleteFile(demoEntry);

                        }

                    }

                }

            }

            catch

            {

                // Silently ignore errors during cleanup

            }

        }).ConfigureAwait(true);

    }



    private void UpdateStatusForMode()

    {

        _modeHintMessage = IsFullMode

            ? ResourceStrings.Get("Message_MainFullModeStatus")

            : ResourceStrings.Get("Message_MainViewOnlyStatus");



        if (!_statusIsTransient)

        {

            ShowStatusInfo(_modeHintMessage, isTransient: false);

        }

    }



    public void RestoreModeHint()

    {

        _statusIsTransient = false;

        ShowStatusInfo(_modeHintMessage, isTransient: false);

    }



    public void OnStatusInfoBarClosed()

    {

        if (_statusIsTransient)

        {

            RestoreModeHint();

        }

    }



    public void ShowStatusInfo(string message)

    {

        ShowStatusInfo(message, isTransient: true);

    }



    private void ShowStatusInfo(string message, bool isTransient)

    {

        StatusInfoMessage = message;

        IsStatusInfoOpen = !string.IsNullOrEmpty(message);

        _statusIsTransient = isTransient && message != _modeHintMessage;

    }



    public void ShowRemapSuccess(MappedReplay slot, ReplayEntry target)

    {

        ShowStatusInfo(ResourceStrings.Format(

            "Message_RemapSuccess",

            ReplayDisplayHelper.GetSummaryLabel(slot.SlotEntry, slot.SlotFileName),

            ReplayDisplayHelper.GetSummaryLabel(target, target.FileName)));

    }

}



