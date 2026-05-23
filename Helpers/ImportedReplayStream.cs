namespace StrinovaReplayManager.Helpers;

/// <summary>
/// NTFS alternate data stream used to mark replays imported via the app (not yet remapped to a slot).
/// </summary>
public static class ImportedReplayStream
{
    public const string Name = "strinova.imported";

    public static string GetPath(string filePath) => $"{filePath}:{Name}";
}

