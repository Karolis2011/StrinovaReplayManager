namespace StrinovaReplayManager.Models;

public sealed class MappedReplay
{
    public required string SlotFileName { get; init; }

    public ReplayEntry? SlotEntry { get; init; }

    public required string SymlinkPath { get; init; }

    public required string TargetPath { get; init; }

    public required ReplayEntry Entry { get; init; }

    public bool IsSlotParsed => SlotEntry is { IsParsed: true };

    public bool IsTargetParsed => Entry.IsParsed;
}

