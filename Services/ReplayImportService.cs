using StrinovaReplayManager.Helpers;
using StrinovaReplayManager.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StrinovaReplayManager.Services;

public sealed class ReplayImportService : IReplayImportService
{
    private readonly DemosPathService _paths;
    private readonly IReplayFileSystem _fileSystem;
    private readonly Func<IntPtr> _getWindowHandle;

    public ReplayImportService(DemosPathService paths, IReplayFileSystem fileSystem, Func<IntPtr> getWindowHandle)
    {
        _paths = paths;
        _fileSystem = fileSystem;
        _getWindowHandle = getWindowHandle;
    }

    public async Task<ImportResult> ImportAsync(CancellationToken cancellationToken = default)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".replay");

        var hwnd = _getWindowHandle();
        if (hwnd != IntPtr.Zero)
        {
            InitializeWithWindow.Initialize(picker, hwnd);
        }

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return new ImportResult { Cancelled = true };
        }

        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(_paths.OriginalsDirectory);
        var fileName = file.Name;
        var destPath = _paths.GetOriginalsPath(fileName);

        if (File.Exists(destPath))
        {
            fileName = ReplayOriginalsNaming.GetUniqueFileName(
                fileName,
                candidate => File.Exists(_paths.GetOriginalsPath(candidate)));
            destPath = _paths.GetOriginalsPath(fileName);
        }

        File.Copy(file.Path, destPath, overwrite: false);
        _fileSystem.MarkImported(destPath);

        if (!ReplayFilenameParser.TryParse(destPath, out var entry) || entry is null)
        {
            return new ImportResult
            {
                Entry = ReplayEntry.FromUnparsedPath(destPath, isImportOnly: true),
            };
        }

        entry = entry with { IsImportOnly = true };
        return new ImportResult { Entry = entry };
    }
}

