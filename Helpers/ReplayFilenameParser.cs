using System.Globalization;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Helpers;

public static class ReplayFilenameParser
{
    public static bool TryParse(string filePath, out ReplayEntry? entry)
    {
        entry = null;
        var fileName = Path.GetFileName(filePath);
        if (!fileName.EndsWith(".replay", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var stem = ReplayOriginalsNaming.StripDuplicateSuffix(Path.GetFileNameWithoutExtension(fileName));
        var parts = stem.Split('_');
        if (parts.Length < 3)
        {
            return false;
        }

        var userId = parts[0];
        var randomSeq = parts[^1];
        string matchId;
        long? unixTime = null;
        int gameVersionEndExclusive;

        var penultimate = parts[^2];
        if (penultimate.Length == 10 && penultimate.All(char.IsDigit))
        {
            if (!long.TryParse(penultimate, NumberStyles.None, CultureInfo.InvariantCulture, out var unix))
            {
                return false;
            }

            if (parts.Length < 4)
            {
                return false;
            }

            unixTime = unix;
            matchId = parts[^3];
            gameVersionEndExclusive = parts.Length - 3;
        }
        else
        {
            matchId = penultimate;
            gameVersionEndExclusive = parts.Length - 2;
        }

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(matchId) || string.IsNullOrEmpty(randomSeq))
        {
            return false;
        }

        string gameVersion;
        if (gameVersionEndExclusive <= 1)
        {
            gameVersion = string.Empty;
        }
        else
        {
            gameVersion = string.Join('_', parts.Skip(1).Take(gameVersionEndExclusive - 1));
        }

        string? dateTimeDisplay = null;
        if (unixTime is long u)
        {
            var local = DateTimeOffset.FromUnixTimeSeconds(u).LocalDateTime;
            dateTimeDisplay = local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
        }

        entry = new ReplayEntry
        {
            FileName = fileName,
            FilePath = filePath,
            UserId = userId,
            GameVersion = gameVersion,
            MatchId = matchId,
            RandomSeq = randomSeq,
            UnixTime = unixTime,
            DateTimeDisplay = dateTimeDisplay,
            IsParsed = true,
        };
        return true;
    }
}

