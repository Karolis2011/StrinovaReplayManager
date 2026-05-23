namespace StrinovaReplayManager.Services;

/// <summary>
/// One-time migration from registry <c>ImportOnlyFiles</c> to per-file NTFS alternate data streams.
/// </summary>
public static class ImportedReplayMigration
{
    public static void MigrateRegistryMarksToStreams(
        SettingsService settings,
        DemosPathService paths,
        IReplayFileSystem fileSystem)
    {
        var legacyNames = settings.LoadLegacyImportOnlyFileNames();
        if (legacyNames.Count == 0)
        {
            return;
        }

        foreach (var fileName in legacyNames)
        {
            var originalsPath = paths.GetOriginalsPath(fileName);
            if (fileSystem.FileExists(originalsPath))
            {
                fileSystem.MarkImported(originalsPath);
            }
        }

        settings.ClearLegacyImportOnlyFileNames();
    }
}

