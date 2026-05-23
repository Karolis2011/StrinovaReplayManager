using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public interface IReplayExportService
{
    Task<ExportResult> ExportAsync(ReplayEntry source, CancellationToken cancellationToken = default);
}

public enum ExportResult
{
    Cancelled,
    Success,
    Failed,
}

