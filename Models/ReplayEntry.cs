namespace StrinovaReplayManager.Models;

public sealed record ReplayEntry
{
    public required string FileName { get; init; }

    public required string FilePath { get; init; }

    public required string UserId { get; init; }

    public required string GameVersion { get; init; }

    public required string MatchId { get; init; }

    public required string RandomSeq { get; init; }

    public long? UnixTime { get; init; }

    public string? DateTimeDisplay { get; init; }

    public bool IsImportOnly { get; init; }

    public bool IsParsed { get; init; } = true;

    public static ReplayEntry FromUnparsedPath(string filePath, bool isImportOnly = false)
        => new()
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            UserId = string.Empty,
            GameVersion = string.Empty,
            MatchId = string.Empty,
            RandomSeq = string.Empty,
            IsImportOnly = isImportOnly,
            IsParsed = false,
        };
}

