using System.Runtime.InteropServices;

namespace StrinovaReplayManager.Helpers;

internal static class Win32Interop
{
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr ShellExecute(
        IntPtr hwnd,
        string lpOperation,
        string lpFile,
        string? lpParameters,
        string? lpDirectory,
        int nShowCmd);

    public static bool IsProcessElevated()
    {
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryRelaunchElevated(string executablePath, string arguments)
    {
        var result = ShellExecute(IntPtr.Zero, "runas", executablePath, arguments, null, 1);
        return (nint)result > 32;
    }
}

