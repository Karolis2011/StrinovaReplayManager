using System.Globalization;
using Microsoft.Windows.Globalization;

namespace StrinovaReplayManager.Helpers;

/// <summary>
/// Parses <c>--lang</c> / <c>--language</c> and applies MRT display language before UI loads.
/// </summary>
public static class CommandLineLanguage
{
    /// <summary>Languages under <c>Strings/</c>; fallback when manifest languages are unavailable.</summary>
    private static readonly string[] PackagedLanguages = ["en-us", "zh-cn", "ja-jp"];

    private static readonly string[] LangSwitches =
    [
        "--lang",
        "--language",
        "-lang",
        "/lang",
        "/language",
    ];

    private static readonly Dictionary<string, string> ShortAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "en-us",
        ["zh"] = "zh-cn",
        ["ja"] = "ja-jp",
    };

    public static bool TryApplyFromArgs(string[] args)
    {
        if (!TryParseLanguageArg(args, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var tag = ResolveToManifestLanguage(raw);
        if (tag is null)
        {
            return false;
        }

        ApplicationLanguages.PrimaryLanguageOverride = tag;
        ApplyThreadCultures(tag);
        return true;
    }

    private static bool TryParseLanguageArg(string[] args, out string? value)
    {
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            foreach (var sw in LangSwitches)
            {
                if (arg.Equals(sw, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        value = args[i + 1];
                        return true;
                    }

                    value = null;
                    return false;
                }

                var prefix = sw + "=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = arg[prefix.Length..];
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private static string? ResolveToManifestLanguage(string raw)
    {
        var tag = raw.Trim().Replace('_', '-');
        if (ShortAliases.TryGetValue(tag, out var alias))
        {
            tag = alias;
        }

        foreach (var language in GetAvailableLanguages())
        {
            if (string.Equals(language, tag, StringComparison.OrdinalIgnoreCase))
            {
                return language;
            }
        }

        var dash = tag.IndexOf('-');
        if (dash > 0)
        {
            var languageOnly = tag[..dash];
            if (ShortAliases.TryGetValue(languageOnly, out alias))
            {
                tag = alias;
            }
            else
            {
                tag = languageOnly;
            }

            foreach (var language in GetAvailableLanguages())
            {
                if (language.StartsWith(tag + "-", StringComparison.OrdinalIgnoreCase))
                {
                    return language;
                }
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetAvailableLanguages()
    {
        try
        {
            var manifest = ApplicationLanguages.ManifestLanguages;
            if (manifest.Count > 0)
            {
                return manifest;
            }
        }
        catch (Exception)
        {
            // Fall back to known resource folders.
        }

        return PackagedLanguages;
    }

    private static void ApplyThreadCultures(string languageTag)
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(languageTag);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            // MRT override is sufficient for UI strings; formatting keeps system culture.
        }
    }
}
