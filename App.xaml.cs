using Microsoft.UI.Xaml;
using StrinovaReplayManager.Models;
using StrinovaReplayManager.Views;

namespace StrinovaReplayManager;

public partial class App : Application
{
    private MainWindow? _window;

    public static AppServices Services { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        Services = _window.Services;
        _window.Activate();
        _ = _window.InitializeNavigationAsync();
    }
}

