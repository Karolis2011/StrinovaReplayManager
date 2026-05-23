using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace StrinovaReplayManager;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();
        Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.Initialize(0x00020000);
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
