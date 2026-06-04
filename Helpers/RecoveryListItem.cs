using System.Globalization;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Helpers;

public sealed class RecoveryListItem
{
    public required DeletedReplaySnapshot Snapshot { get; init; }

    public required string PrimaryLine { get; init; }

    public required string SecondaryLine { get; init; }

    public static RecoveryListItem FromSnapshot(DeletedReplaySnapshot snap)
    {
        var primary = snap.IsParsed && !string.IsNullOrEmpty(snap.DateTimeDisplay)
            ? $"{snap.DateTimeDisplay} · {snap.MatchId} · {snap.GameVersion}"
            : snap.FileName;

        var secondary = ResourceStrings.Format(
            "Message_RecoverReplayDeletedAt",
            snap.DeletedAtUtc.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture));

        return new RecoveryListItem
        {
            Snapshot = snap,
            PrimaryLine = primary,
            SecondaryLine = secondary,
        };
    }
}
