using StrinovaReplayManager.Models;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace StrinovaReplayManager.Services;

public sealed class ReplayClipboardService : IReplayClipboardService
{
    private readonly DemosPathService _paths;

    public ReplayClipboardService(DemosPathService paths)
    {
        _paths = paths;
    }

    public async Task<ClipboardResult> CopyReplayAsync(ReplayEntry source, CancellationToken cancellationToken = default)
    {
        var sourcePath = _paths.GetOriginalsPath(source.FileName);
        if (!File.Exists(sourcePath))
        {
            sourcePath = source.FilePath;
        }

        if (!File.Exists(sourcePath))
        {
            return ClipboardResult.Failed;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(sourcePath).AsTask().ConfigureAwait(false);
            var package = new DataPackage();
            package.SetStorageItems([storageFile]);
            Clipboard.SetContent(package);
            return ClipboardResult.Success;
        }
        catch
        {
            return ClipboardResult.Failed;
        }
    }
}

