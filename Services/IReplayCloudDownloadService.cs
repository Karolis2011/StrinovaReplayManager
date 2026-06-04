using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public interface IReplayCloudDownloadService
{
    Task<ReplayCloudAvailability> ProbeAsync(ReplayEntry entry, CancellationToken cancellationToken = default);

    Task<ReplayCloudAvailability> ProbeAsync(DeletedReplaySnapshot snapshot, CancellationToken cancellationToken = default);

    Task<ReplayCloudDownloadResult> DownloadAsync(
        ReplayEntry entry,
        string destinationPath,
        CancellationToken cancellationToken = default);

    void InvalidateCacheForEntry(ReplayEntry entry);

    void InvalidateCacheForSnapshot(DeletedReplaySnapshot snapshot);
}
