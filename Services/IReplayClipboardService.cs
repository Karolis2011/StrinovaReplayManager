using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public interface IReplayClipboardService
{
    Task<ClipboardResult> CopyReplayAsync(ReplayEntry source, CancellationToken cancellationToken = default);
}

public enum ClipboardResult
{
    Success,
    Failed,
}

