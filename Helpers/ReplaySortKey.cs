namespace StrinovaReplayManager.Helpers;

public static class ReplaySortKey
{
    public static DateTimeOffset FromReplay(string filePath, long? unixTime)
    {
        if (unixTime is long unix)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix);
        }

        return File.GetLastWriteTimeUtc(filePath);
    }

    public static int Compare(ReplayEntrySortable a, ReplayEntrySortable b)
        => b.SortTime.CompareTo(a.SortTime);
}

public readonly record struct ReplayEntrySortable(DateTimeOffset SortTime);

