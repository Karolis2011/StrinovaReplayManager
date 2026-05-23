using StrinovaReplayManager.Helpers;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public sealed class ReplayIngestService
{
    private readonly DemosPathService _paths;
    private readonly IReplayFileSystem _fileSystem;

    public ReplayIngestService(DemosPathService paths, IReplayFileSystem fileSystem)
    {
        _paths = paths;
        _fileSystem = fileSystem;
    }

    public IngestResult Run(AppMode mode)
    {
        if (mode == AppMode.ViewOnly)
        {
            return new IngestResult(0, 0, Skipped: true);
        }

        _paths.EnsureDirectories(_fileSystem);
        var moved = 0;
        var linked = 0;

        foreach (var entryPath in _fileSystem.EnumerateDemosEntries(_paths.DemosDirectory))
        {
            if (!entryPath.EndsWith(".replay", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileName = Path.GetFileName(entryPath);
            var originalsPath = _paths.GetOriginalsPath(fileName);

            if (_fileSystem.IsSymlink(entryPath))
            {
                continue;
            }

            if (_fileSystem.FileExists(entryPath) && _fileSystem.FileExists(originalsPath))
            {
                var conflictPath = GetUniqueOriginalsPath(fileName);

                try
                {
                    _fileSystem.MoveFile(entryPath, conflictPath);
                    moved++;
                    TryCreateSymlink(fileName, ref linked);
                }
                catch
                {
                    if (_fileSystem.FileExists(conflictPath) && !_fileSystem.FileExists(entryPath))
                    {
                        try
                        {
                            _fileSystem.MoveFile(conflictPath, entryPath);
                        }
                        catch
                        {
                            // Best effort rollback.
                        }
                    }
                }

                continue;
            }

            if (_fileSystem.FileExists(entryPath) && !_fileSystem.FileExists(originalsPath))
            {
                try
                {
                    _fileSystem.MoveFile(entryPath, originalsPath);
                    moved++;
                    TryCreateSymlink(fileName, ref linked);
                }
                catch
                {
                    if (_fileSystem.FileExists(originalsPath) && !_fileSystem.FileExists(entryPath))
                    {
                        try
                        {
                            _fileSystem.MoveFile(originalsPath, entryPath);
                        }
                        catch
                        {
                            // Best effort rollback.
                        }
                    }
                }

                continue;
            }
        }

        return new IngestResult(moved, linked, Skipped: false);
    }

    private string GetUniqueOriginalsPath(string fileName)
    {
        var conflictFileName = ReplayOriginalsNaming.GetUniqueFileName(
            fileName,
            candidate => _fileSystem.FileExists(_paths.GetOriginalsPath(candidate)));
        return _paths.GetOriginalsPath(conflictFileName);
    }

    private void TryCreateSymlink(string fileName, ref int linked)
    {
        var linkPath = _paths.GetDemosLinkPath(fileName);
        var originalsPath = _paths.GetOriginalsPath(fileName);
        if (!_fileSystem.FileExists(originalsPath))
        {
            return;
        }

        if (_fileSystem.IsMarkedImported(originalsPath))
        {
            return;
        }

        if (_fileSystem.FileExists(linkPath) && !_fileSystem.IsSymlink(linkPath))
        {
            return;
        }

        try
        {
            _fileSystem.CreateSymlink(linkPath, originalsPath);
            linked++;
        }
        catch
        {
            // Caller may surface via status.
        }
    }
}

public readonly record struct IngestResult(int MovedCount, int LinkedCount, bool Skipped);

