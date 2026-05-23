using Microsoft.UI.Xaml;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Helpers;

public static class ReplayDisplayHelper
{
    public const string MissingValue = "\u2014";

    public static string GetMatchId(ReplayEntry? entry)
        => string.IsNullOrEmpty(entry?.MatchId) ? MissingValue : entry.MatchId;

    public static string GetDateTimeDisplay(ReplayEntry? entry)
        => string.IsNullOrEmpty(entry?.DateTimeDisplay) ? MissingValue : entry.DateTimeDisplay;

    public static string GetGameVersion(ReplayEntry? entry)
        => string.IsNullOrEmpty(entry?.GameVersion) ? MissingValue : entry.GameVersion;

    public static string GetFileName(ReplayEntry? entry)
        => string.IsNullOrEmpty(entry?.FileName) ? MissingValue : entry.FileName;

    public static string GetUserId(ReplayEntry? entry)
        => string.IsNullOrEmpty(entry?.UserId) ? MissingValue : entry.UserId;

    public static string FormatImportOnly(bool isImportOnly)
        => isImportOnly ? "Yes" : string.Empty;

    public static Visibility ParsedColumnsVisibility(bool isParsed)
        => isParsed ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility UnparsedFilenameVisibility(bool isParsed)
        => isParsed ? Visibility.Collapsed : Visibility.Visible;

    public static string GetSummaryLabel(ReplayEntry? entry, string fallbackFileName)
    {
        if (entry is { IsParsed: true })
        {
            return $"{GetMatchId(entry)} · {GetDateTimeDisplay(entry)}";
        }

        if (!string.IsNullOrEmpty(entry?.FileName))
        {
            return entry.FileName;
        }

        return string.IsNullOrEmpty(fallbackFileName) ? MissingValue : fallbackFileName;
    }

}

