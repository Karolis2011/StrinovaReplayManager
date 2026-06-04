using System.Globalization;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Helpers;

public static class ReplayDownloadUrlBuilder
{
    public const string RecordBaseUrl = "https://replay-download.strinova.com/record/";

    public static bool TryGetServerRecordFileName(ReplayEntry entry, out string? serverFileName)
    {
        serverFileName = null;
        if (!entry.IsParsed
            || string.IsNullOrEmpty(entry.GameVersion)
            || string.IsNullOrEmpty(entry.MatchId)
            || string.IsNullOrEmpty(entry.RandomSeq)
            || entry.UnixTime is not long unix)
        {
            return false;
        }

        serverFileName = BuildServerFileName(entry.GameVersion, entry.MatchId, unix, entry.RandomSeq);
        return IsSafeReplayFileName(serverFileName);
    }

    public static bool TryGetServerRecordFileName(DeletedReplaySnapshot snapshot, out string? serverFileName)
        => TryGetServerRecordFileName(snapshot.ToReplayEntry(), out serverFileName);

    public static bool TryGetRecordUrl(ReplayEntry entry, out Uri? url)
    {
        url = null;
        if (!TryGetServerRecordFileName(entry, out var serverFileName) || serverFileName is null)
        {
            return false;
        }

        return TryBuildRecordUri(serverFileName, out url);
    }

    public static bool TryGetRecordUrl(DeletedReplaySnapshot snapshot, out Uri? url)
        => TryGetRecordUrl(snapshot.ToReplayEntry(), out url);

    public static bool TryGetServerRecordFileNameFromLocalFileName(string localFileName, out string? serverFileName)
    {
        serverFileName = null;
        if (!ReplayFilenameParser.TryParse(localFileName, out var entry) || entry is null)
        {
            return false;
        }

        return TryGetServerRecordFileName(entry, out serverFileName);
    }

    private static string BuildServerFileName(string gameVersion, string matchId, long unixTime, string randomSeq)
        => string.Create(CultureInfo.InvariantCulture, $"{gameVersion}_{matchId}_{unixTime}_{randomSeq}.replay");

    private static bool TryBuildRecordUri(string serverFileName, out Uri? url)
    {
        url = null;
        if (!IsSafeReplayFileName(serverFileName))
        {
            return false;
        }

        url = new Uri(new Uri(RecordBaseUrl, UriKind.Absolute), serverFileName);
        return true;
    }

    private static bool IsSafeReplayFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || fileName.Contains('\\', StringComparison.Ordinal)
            || fileName.Contains('/', StringComparison.Ordinal)
            || fileName.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        return fileName.EndsWith(".replay", StringComparison.OrdinalIgnoreCase);
    }
}
