namespace StrinovaReplayManager.Services;

public sealed class DemosPathService
{
    private const string DemosRelative = "Strinova\\Saved\\Demos";
    private const string OriginalsFolder = ".original";

    public DemosPathService(string? demosDirectory = null)
    {
        if (demosDirectory is not null)
        {
            DemosDirectory = demosDirectory;
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            DemosDirectory = Path.Combine(localAppData, DemosRelative);
        }

        OriginalsDirectory = Path.Combine(DemosDirectory, OriginalsFolder);
    }

    public string DemosDirectory { get; }

    public string OriginalsDirectory { get; }

    public void EnsureDirectories(IReplayFileSystem fileSystem)
    {
        fileSystem.EnsureDirectory(DemosDirectory);
        fileSystem.EnsureDirectory(OriginalsDirectory);
    }

    public string GetOriginalsPath(string fileName) => Path.Combine(OriginalsDirectory, fileName);

    public string GetDemosLinkPath(string fileName) => Path.Combine(DemosDirectory, fileName);

    public string GetRelativeOriginalsTarget(string fileName) => Path.Combine(OriginalsFolder, fileName);
}

