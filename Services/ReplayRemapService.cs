using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public sealed class ReplayRemapService
{
    private readonly DemosPathService _paths;
    private readonly IReplayFileSystem _fileSystem;

    public ReplayRemapService(DemosPathService paths, IReplayFileSystem fileSystem)
    {
        _paths = paths;
        _fileSystem = fileSystem;
    }

    public void Apply(MappedReplay slot, string originalFilePath)
    {
        if (!_fileSystem.FileExists(originalFilePath))
        {
            throw new FileNotFoundException("Original replay file not found.", originalFilePath);
        }

        var targetFileName = Path.GetFileName(originalFilePath);
        if (!targetFileName.EndsWith(".replay", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Target must be a .replay file.");
        }

        var linkPath = _paths.GetDemosLinkPath(slot.SlotFileName);
        if (_fileSystem.FileExists(linkPath))
        {
            _fileSystem.DeleteFile(linkPath);
        }

        _fileSystem.CreateSymlink(linkPath, originalFilePath);
    }
}

