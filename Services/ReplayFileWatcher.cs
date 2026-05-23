namespace StrinovaReplayManager.Services;

public sealed class ReplayFileWatcher : IDisposable
{
    private readonly DemosPathService _paths;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private bool _disposed;

    public event EventHandler? Changed;

    public ReplayFileWatcher(DemosPathService paths)
    {
        _paths = paths;
    }

    public void Start()
    {
        if (_watcher is not null)
        {
            return;
        }

        Directory.CreateDirectory(_paths.DemosDirectory);
        _watcher = new FileSystemWatcher(_paths.DemosDirectory)
        {
            Filter = "*.replay",
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
        };
        _watcher.Created += OnWatcherEvent;
        _watcher.Changed += OnWatcherEvent;
        _watcher.Deleted += OnWatcherEvent;
        _watcher.Renamed += OnWatcherEvent;
        _watcher.EnableRaisingEvents = true;
    }

    public void Stop()
    {
        if (_watcher is null)
        {
            return;
        }

        _watcher.EnableRaisingEvents = false;
        _watcher.Created -= OnWatcherEvent;
        _watcher.Changed -= OnWatcherEvent;
        _watcher.Deleted -= OnWatcherEvent;
        _watcher.Renamed -= OnWatcherEvent;
        _watcher.Dispose();
        _watcher = null;
        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }

    public async Task RunSerializedAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private void OnWatcherEvent(object sender, FileSystemEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(
            _ => Changed?.Invoke(this, EventArgs.Empty),
            null,
            TimeSpan.FromMilliseconds(300),
            Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
        _gate.Dispose();
    }
}

