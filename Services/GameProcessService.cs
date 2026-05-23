using System.Diagnostics;

namespace StrinovaReplayManager.Services;

public sealed class GameProcessService
{
    public const string ProcessName = "Strinova-Win64-Shipping";

    public bool IsGameRunning()
    {
        return Process.GetProcessesByName(ProcessName).Length > 0;
    }
}
