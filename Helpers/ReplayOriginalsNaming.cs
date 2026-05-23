using System.Text.RegularExpressions;

namespace StrinovaReplayManager.Helpers;

public static class ReplayOriginalsNaming
{
    /// <summary>
    /// Appended by this app for duplicate originals. Stripped before replay filename parsing
    /// so a trailing _N segment is not mistaken for randomSeq.
    /// </summary>
    public const string DuplicateSuffixPrefix = "__dup";

    private static readonly Regex DuplicateSuffixPattern = new(
        $"{Regex.Escape(DuplicateSuffixPrefix)}\\d+$",
        RegexOptions.CultureInvariant);

    public static string GetUniqueFileName(string fileName, Func<string, bool> fileExists)
    {
        if (!fileExists(fileName))
        {
            return fileName;
        }

        var stem = StripDuplicateSuffix(Path.GetFileNameWithoutExtension(fileName));
        var extension = Path.GetExtension(fileName);

        for (var index = 1; ; index++)
        {
            var candidate = $"{stem}{DuplicateSuffixPrefix}{index}{extension}";
            if (!fileExists(candidate))
            {
                return candidate;
            }
        }
    }

    public static string StripDuplicateSuffix(string stem) => DuplicateSuffixPattern.Replace(stem, string.Empty);
}
