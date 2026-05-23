using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public interface IReplayImportService
{
    Task<ImportResult> ImportAsync(CancellationToken cancellationToken = default);
}

public sealed class ImportResult
{
    public bool Cancelled { get; init; }

    public ReplayEntry? Entry { get; init; }

    public string? ErrorMessage { get; init; }
}

