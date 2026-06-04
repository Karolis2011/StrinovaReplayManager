namespace StrinovaReplayManager.Models;

public sealed record DeletedReplaySnapshot
{
    public required string FileName { get; init; }

    public required DateTimeOffset DeletedAtUtc { get; init; }

    public required string UserId { get; init; }

    public required string GameVersion { get; init; }

    public required string MatchId { get; init; }

    public required string RandomSeq { get; init; }

    public long? UnixTime { get; init; }

    public string? DateTimeDisplay { get; init; }

    public bool IsParsed { get; init; }

    public static DeletedReplaySnapshot FromEntry(ReplayEntry entry, DateTimeOffset deletedAtUtc)
        => new()
        {
            FileName = entry.FileName,
            DeletedAtUtc = deletedAtUtc,
            UserId = entry.UserId,
            GameVersion = entry.GameVersion,
            MatchId = entry.MatchId,
            RandomSeq = entry.RandomSeq,
            UnixTime = entry.UnixTime,
            DateTimeDisplay = entry.DateTimeDisplay,
            IsParsed = entry.IsParsed,
        };

    public ReplayEntry ToReplayEntry()
    {
        var path = FileName;
        return new ReplayEntry
        {
            FileName = FileName,
            FilePath = path,
            UserId = UserId,
            GameVersion = GameVersion,
            MatchId = MatchId,
            RandomSeq = RandomSeq,
            UnixTime = UnixTime,
            DateTimeDisplay = DateTimeDisplay,
            IsParsed = IsParsed,
            IsImportOnly = false,
        };
    }

    public ReplayEntry ToReplayEntry(string filePath)
        => ToReplayEntry() with { FilePath = filePath };
}
