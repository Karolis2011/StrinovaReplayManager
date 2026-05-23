using StrinovaReplayManager.Services;

namespace StrinovaReplayManager.Helpers;

public sealed class ReplayFileSystemHelper : IReplayFileSystem
{
    public bool IsSymlink(string path)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
    }

    public string? GetSymlinkTarget(string path)
    {
        if (!IsSymlink(path))
        {
            return null;
        }

        try
        {
            var target = File.ResolveLinkTarget(path, returnFinalTarget: true);
            return target?.FullName;
        }
        catch
        {
            return null;
        }
    }

    public void CreateSymlink(string linkPath, string targetPath)
    {
        if (File.Exists(linkPath) || Directory.Exists(linkPath))
        {
            DeleteFile(linkPath);
        }

        var targetInfo = new FileInfo(targetPath);
        var linkDir = Path.GetDirectoryName(linkPath) ?? throw new InvalidOperationException("Invalid link path.");
        var relativeTarget = Path.GetRelativePath(linkDir, targetInfo.FullName);
        File.CreateSymbolicLink(linkPath, relativeTarget);
    }

    public void MoveFile(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination))
        {
            File.Delete(destination);
        }

        File.Move(source, destination);
    }

    public void DeleteFile(string path)
    {
        if (IsSymlink(path) || File.Exists(path))
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }

            File.Delete(path);
            return;
        }

        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<string> EnumerateFiles(string directory, string searchPattern)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(directory, searchPattern))
        {
            yield return file;
        }
    }

    public IEnumerable<string> EnumerateDemosEntries(string demosDirectory)
    {
        if (!Directory.Exists(demosDirectory))
        {
            yield break;
        }

        foreach (var entry in Directory.EnumerateFileSystemEntries(demosDirectory))
        {
            var name = Path.GetFileName(entry);
            if (name.Equals(".original", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return entry;
        }
    }

    public void EnsureDirectory(string path) => Directory.CreateDirectory(path);

    public bool IsMarkedImported(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        try
        {
            return File.Exists(ImportedReplayStream.GetPath(filePath));
        }
        catch
        {
            return false;
        }
    }

    public void MarkImported(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Replay file not found.", filePath);
        }

        File.WriteAllText(ImportedReplayStream.GetPath(filePath), "1");
    }
}

