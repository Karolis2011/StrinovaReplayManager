using StrinovaReplayManager.Helpers;

using StrinovaReplayManager.Models;



namespace StrinovaReplayManager.Services;



public sealed class ReplayCatalogService

{

    private readonly DemosPathService _paths;

    private readonly IReplayFileSystem _fileSystem;



    public ReplayCatalogService(DemosPathService paths, IReplayFileSystem fileSystem)

    {

        _paths = paths;

        _fileSystem = fileSystem;

    }



    public Task<IReadOnlyList<MappedReplay>> ListMappedAsync(AppMode mode, CancellationToken cancellationToken = default)

        => Task.Run(() => ListMapped(mode), cancellationToken);



    public Task<IReadOnlyList<ReplayEntry>> ListOriginalsAsync(CancellationToken cancellationToken = default)

        => Task.Run(ListOriginals, cancellationToken);



    private IReadOnlyList<MappedReplay> ListMapped(AppMode mode)

    {

        var results = new List<(MappedReplay Mapped, DateTimeOffset Sort)>();



        if (mode == AppMode.Full)

        {

            foreach (var entryPath in _fileSystem.EnumerateDemosEntries(_paths.DemosDirectory))

            {

                if (!entryPath.EndsWith(".replay", StringComparison.OrdinalIgnoreCase))

                {

                    continue;

                }



                if (!_fileSystem.IsSymlink(entryPath))

                {

                    continue;

                }



                var target = _fileSystem.GetSymlinkTarget(entryPath);

                if (string.IsNullOrEmpty(target) || !_fileSystem.FileExists(target))

                {

                    continue;

                }



                var slotFileName = Path.GetFileName(entryPath);

                var entry = ResolveOriginalEntry(target);

                ReplayEntry? slotEntry = null;

                if (ReplayFilenameParser.TryParse(entryPath, out var slotParsed) && slotParsed is not null)

                {

                    slotEntry = slotParsed;

                }



                var mapped = new MappedReplay

                {

                    SlotFileName = slotFileName,

                    SlotEntry = slotEntry,

                    SymlinkPath = entryPath,

                    TargetPath = target,

                    Entry = entry,

                };

                results.Add((mapped, ReplaySortKey.FromReplay(entryPath, slotEntry?.UnixTime ?? entry.UnixTime)));

            }

        }

        else

        {

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entryPath in _fileSystem.EnumerateDemosEntries(_paths.DemosDirectory))

            {

                if (!entryPath.EndsWith(".replay", StringComparison.OrdinalIgnoreCase))

                {

                    continue;

                }



                if (_fileSystem.IsSymlink(entryPath))

                {

                    continue;

                }



                AddViewOnlyEntry(entryPath, results, seen);

            }



            foreach (var originalsPath in _fileSystem.EnumerateFiles(_paths.OriginalsDirectory, "*.replay"))

            {

                AddViewOnlyEntry(originalsPath, results, seen);

            }

        }



        return results

            .OrderByDescending(static r => r.Sort)

            .Select(static r => r.Mapped)

            .ToList();

    }



    private void AddViewOnlyEntry(

        string filePath,

        List<(MappedReplay Mapped, DateTimeOffset Sort)> results,

        HashSet<string> seen)

    {

        var fileName = Path.GetFileName(filePath);

        if (!seen.Add(fileName))

        {

            return;

        }



        var entry = ResolveOriginalEntry(filePath);

        ReplayEntry? slotEntry = null;

        if (ReplayFilenameParser.TryParse(fileName, out var slotParsed) && slotParsed is not null)

        {

            slotEntry = slotParsed;

        }



        var mapped = new MappedReplay

        {

            SlotFileName = fileName,

            SlotEntry = slotEntry,

            SymlinkPath = _paths.GetDemosLinkPath(fileName),

            TargetPath = filePath,

            Entry = entry,

        };

        results.Add((mapped, ReplaySortKey.FromReplay(filePath, slotEntry?.UnixTime ?? entry.UnixTime)));

    }



    private ReplayEntry ResolveOriginalEntry(string path)

    {

        if (ReplayFilenameParser.TryParse(path, out var parsed) && parsed is not null)

        {

            return parsed with { IsImportOnly = _fileSystem.IsMarkedImported(path) };

        }



        return ReplayEntry.FromUnparsedPath(path, _fileSystem.IsMarkedImported(path));

    }



    private IReadOnlyList<ReplayEntry> ListOriginals()

    {

        var entries = new List<(ReplayEntry Entry, DateTimeOffset Sort)>();



        foreach (var path in _fileSystem.EnumerateFiles(_paths.OriginalsDirectory, "*.replay"))

        {

            var entry = ResolveOriginalEntry(path);

            entries.Add((entry, ReplaySortKey.FromReplay(path, entry.UnixTime)));

        }



        return entries

            .OrderByDescending(static e => e.Sort)

            .Select(static e => e.Entry)

            .ToList();

    }

}



