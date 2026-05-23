using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StrinovaReplayManager.Models;
using StrinovaReplayManager.Views;
using WinRT.Interop;

namespace StrinovaReplayManager;

public sealed partial class MainWindow : Window
{
    public AppServices Services { get; }

    public Frame RootFrame => NavigationFrame;

    public MainWindow()
    {
        Services = new AppServices(GetWindowHandle);
        Services.MainWindow = this;
        InitializeComponent();

        AppWindow.Title = "Strinova Replay Manager";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (File.Exists("Assets/AppIcon.ico"))
        {
            AppWindow.SetIcon("Assets/AppIcon.ico");
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(960, 720));
    }

    public async Task InitializeNavigationAsync()
    {
        if (await Services.SymlinkCapability.MaybeAutoElevateOnStartupAsync().ConfigureAwait(true))
        {
            Application.Current.Exit();
            return;
        }

        if (await Services.SymlinkCapability.ProbeAsync().ConfigureAwait(true))
        {
            Services.CurrentMode = AppMode.Full;
            RootFrame.Navigate(typeof(MainPage), AppMode.Full);
            return;
        }

        var persisted = Services.Settings.LoadPersistedAppMode();
        if (persisted == AppMode.ViewOnly)
        {
            Services.CurrentMode = AppMode.ViewOnly;
            RootFrame.Navigate(typeof(MainPage), AppMode.ViewOnly);
        }
        else
        {
            RootFrame.Navigate(typeof(SetupPage));
        }
    }

    private IntPtr GetWindowHandle() => WindowNative.GetWindowHandle(this);
}

