using StrinovaReplayManager.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StrinovaReplayManager.Services;

public sealed class ReplayExportService : IReplayExportService
{
    private readonly DemosPathService _paths;
    private readonly Func<IntPtr> _getWindowHandle;

    public ReplayExportService(DemosPathService paths, Func<IntPtr> getWindowHandle)
    {
        _paths = paths;
        _getWindowHandle = getWindowHandle;
    }

    public async Task<ExportResult> ExportAsync(ReplayEntry source, CancellationToken cancellationToken = default)
    {
        var sourcePath = _paths.GetOriginalsPath(source.FileName);
        if (!File.Exists(sourcePath))
        {
            sourcePath = source.FilePath;
        }

        if (!File.Exists(sourcePath))
        {
            return ExportResult.Failed;
        }

        var picker = new FileSavePicker
        {
            SuggestedFileName = source.FileName,
        };
        picker.FileTypeChoices.Add("Strinova Replay", [".replay"]);

        var hwnd = _getWindowHandle();
        if (hwnd != IntPtr.Zero)
        {
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return ExportResult.Cancelled;
        }

        cancellationToken.ThrowIfCancellationRequested();
        File.Copy(sourcePath, file.Path, overwrite: true);
        return ExportResult.Success;
    }
}

