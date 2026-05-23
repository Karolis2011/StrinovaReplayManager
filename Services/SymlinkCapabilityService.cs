using StrinovaReplayManager.Helpers;

namespace StrinovaReplayManager.Services;

public sealed class SymlinkCapabilityService
{
    private readonly DemosPathService _paths;
    private readonly IReplayFileSystem _fileSystem;
    private readonly SettingsService _settings;

    public SymlinkCapabilityService(DemosPathService paths, IReplayFileSystem fileSystem, SettingsService settings)
    {
        _paths = paths;
        _fileSystem = fileSystem;
        _settings = settings;
    }

    public bool IsElevated => Win32Interop.IsProcessElevated();

    public Task<bool> ProbeAsync(CancellationToken cancellationToken = default)
        => Task.Run(() => ProbeCore(), cancellationToken);

    public bool ProbeCore()
    {
        try
        {
            _fileSystem.EnsureDirectory(_paths.DemosDirectory);
            var probeName = $"._srm_probe_{Guid.NewGuid():N}.tmp";
            var linkPath = _paths.GetDemosLinkPath(probeName);
            var targetPath = _paths.GetOriginalsPath(probeName);
            _fileSystem.EnsureDirectory(_paths.OriginalsDirectory);
            File.WriteAllText(targetPath, "probe");
            try
            {
                _fileSystem.CreateSymlink(linkPath, targetPath);
                _fileSystem.DeleteFile(linkPath);
                return true;
            }
            finally
            {
                _fileSystem.DeleteFile(targetPath);
            }
        }
        catch
        {
            return false;
        }
    }

    public void OpenDeveloperSettings()
        => _ = Win32Interop.ShellExecute(IntPtr.Zero, "open", "ms-settings:developers", null, null, 1);

    public bool TryRelaunchElevated()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            return false;
        }

        return Win32Interop.TryRelaunchElevated(exe, string.Empty);
    }

    public async Task<bool> MaybeAutoElevateOnStartupAsync(CancellationToken cancellationToken = default)
    {
        if (await ProbeAsync(cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        if (!_settings.LoadAlwaysElevate() || IsElevated)
        {
            return false;
        }

        return TryRelaunchElevated();
    }
}

