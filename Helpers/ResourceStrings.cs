using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace StrinovaReplayManager.Helpers;

public static class ResourceStrings
{
    private static readonly ResourceLoader Loader = new();

    public static string Get(string key) => Loader.GetString(key);

    public static string Format(string key, params object[] args)
        => string.Format(CultureInfo.CurrentCulture, Get(key), args);
}

