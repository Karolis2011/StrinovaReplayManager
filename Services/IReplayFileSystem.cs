namespace StrinovaReplayManager.Services;

public interface IReplayFileSystem
{
    bool IsSymlink(string path);

    string? GetSymlinkTarget(string path);

    void CreateSymlink(string linkPath, string targetPath);

    void MoveFile(string source, string destination);

    void DeleteFile(string path);

    bool FileExists(string path);

    IEnumerable<string> EnumerateFiles(string directory, string searchPattern);

    IEnumerable<string> EnumerateDemosEntries(string demosDirectory);

    void EnsureDirectory(string path);

    bool IsMarkedImported(string filePath);

    void MarkImported(string filePath);
}

