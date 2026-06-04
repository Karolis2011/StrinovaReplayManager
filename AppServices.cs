using Microsoft.UI.Xaml.Controls;
using StrinovaReplayManager.Helpers;
using StrinovaReplayManager.Models;
using StrinovaReplayManager.Services;
using StrinovaReplayManager.ViewModels;

namespace StrinovaReplayManager;

public sealed class AppServices
{
    public AppServices(Func<IntPtr> getWindowHandle)
    {
        FileSystem = new ReplayFileSystemHelper();
        Paths = new DemosPathService();
        Settings = new SettingsService();
        Dialogs = new ReplayDialogService();
        GameProcess = new GameProcessService();
        SymlinkCapability = new SymlinkCapabilityService(Paths, FileSystem, Settings);
        ImportedReplayMigration.MigrateRegistryMarksToStreams(Settings, Paths, FileSystem);
        Ingest = new ReplayIngestService(Paths, FileSystem);
        Catalog = new ReplayCatalogService(Paths, FileSystem);
        Remap = new ReplayRemapService(Paths, FileSystem);
        Watcher = new ReplayFileWatcher(Paths);
        Export = new ReplayExportService(Paths, getWindowHandle);
        Clipboard = new ReplayClipboardService(Paths);
        Import = new ReplayImportService(Paths, FileSystem, getWindowHandle);
        var httpClient = new HttpClient();
        CloudDownload = new ReplayCloudDownloadService(httpClient);
        DeletedReplayTracker = new DeletedReplayTracker(Settings);

        SetupViewModel = new SetupViewModel(this);
        MainViewModel = new MainViewModel(this);
        RemapViewModel = new RemapViewModel(this);
    }

    public IReplayFileSystem FileSystem { get; }

    public DemosPathService Paths { get; }

    public SettingsService Settings { get; }

    public IReplayDialogService Dialogs { get; }

    public GameProcessService GameProcess { get; }

    public SymlinkCapabilityService SymlinkCapability { get; }

    public ReplayIngestService Ingest { get; }

    public ReplayCatalogService Catalog { get; }

    public ReplayRemapService Remap { get; }

    public ReplayFileWatcher Watcher { get; }

    public IReplayExportService Export { get; }

    public IReplayClipboardService Clipboard { get; }

    public IReplayImportService Import { get; }

    public IReplayCloudDownloadService CloudDownload { get; }

    public DeletedReplayTracker DeletedReplayTracker { get; }

    public SetupViewModel SetupViewModel { get; }

    public MainViewModel MainViewModel { get; }

    public RemapViewModel RemapViewModel { get; }

    public AppMode CurrentMode { get; set; } = AppMode.ViewOnly;

    public MainWindow? MainWindow { get; set; }

    public Frame? RootFrame => MainWindow?.RootFrame;
}

