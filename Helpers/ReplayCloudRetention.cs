using System.Globalization;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Helpers;

public static class ReplayCloudRetention
{
    /// <summary>
    /// Conservative window for skipping CDN HEAD probes and pruning deleted-replay history.
    /// HEAD remains authoritative within this window; see docs/replay-cdn-retention.md.
    /// </summary>
    public static readonly TimeSpan MaxServerAge = TimeSpan.FromDays(30);

    public static bool HasValidMatchUnixTime(ReplayEntry entry)
        => entry.UnixTime is long unix && IsTenDigitUnix(unix);

    public static bool HasValidMatchUnixTime(DeletedReplaySnapshot snapshot)
        => snapshot.UnixTime is long unix && IsTenDigitUnix(unix);

    public static bool ShouldSkipProbe(ReplayEntry entry)
    {
        if (!HasValidMatchUnixTime(entry))
        {
            return false;
        }

        return GetMatchAge(entry.UnixTime!.Value) > MaxServerAge;
    }

    public static bool ShouldSkipProbe(DeletedReplaySnapshot snapshot)
    {
        if (!HasValidMatchUnixTime(snapshot))
        {
            return false;
        }

        return GetMatchAge(snapshot.UnixTime!.Value) > MaxServerAge;
    }

    public static TimeSpan? GetMatchAge(long unixTimeSeconds)
    {
        if (!IsTenDigitUnix(unixTimeSeconds))
        {
            return null;
        }

        var match = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
        var age = DateTimeOffset.UtcNow - match;
        return age < TimeSpan.Zero ? TimeSpan.Zero : age;
    }

    private static bool IsTenDigitUnix(long unix)
    {
        var s = unix.ToString(CultureInfo.InvariantCulture);
        return s.Length == 10;
    }
}
