using StrinovaReplayManager.Helpers;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public sealed class DeletedReplayTracker
{
    private const int MaxEntries = 50;

    private readonly SettingsService _settings;
    private List<DeletedReplaySnapshot> _entries = [];

    public DeletedReplayTracker(SettingsService settings)
    {
        _settings = settings;
        Reload();
    }

    public bool HasRecoverableEntries => ListRecoverable().Count > 0;

    public void Reload()
    {
        _entries = _settings.LoadDeletedReplays().ToList();
        Prune();
    }

    public void RecordDeleted(ReplayEntry entry)
    {
        var snapshot = DeletedReplaySnapshot.FromEntry(entry, DateTimeOffset.UtcNow);
        _entries.RemoveAll(e => e.FileName.Equals(entry.FileName, StringComparison.OrdinalIgnoreCase));
        _entries.Insert(0, snapshot);

        while (_entries.Count > MaxEntries)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        Prune();
        Persist();
    }

    public void Remove(string fileName)
    {
        var removed = _entries.RemoveAll(e => e.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            Persist();
        }
    }

    public IReadOnlyList<DeletedReplaySnapshot> ListRecoverable()
    {
        Prune();
        return _entries
            .OrderByDescending(static e => e.DeletedAtUtc)
            .ToList();
    }

    private void Prune()
    {
        var cutoff = DateTimeOffset.UtcNow - ReplayCloudRetention.MaxServerAge;
        var changed = _entries.RemoveAll(e => e.DeletedAtUtc < cutoff) > 0;
        if (changed)
        {
            Persist();
        }
    }

    private void Persist()
        => _settings.SaveDeletedReplays(_entries);
}
