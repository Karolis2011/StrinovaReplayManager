using System.Text.Json;
using Microsoft.Win32;
using StrinovaReplayManager.Models;

namespace StrinovaReplayManager.Services;

public sealed class SettingsService
{
    private const string RegistryPath = @"Software\StrinovaReplayManager";
    private const string AlwaysElevateValue = "AlwaysElevate";
    private const string AppModeValue = "AppMode";
    private const string ImportOnlyValue = "ImportOnlyFiles";
    private const string DeletedReplaysValue = "DeletedReplays";

    private static readonly JsonSerializerOptions DeletedReplayJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public bool LoadAlwaysElevate()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key?.GetValue(AlwaysElevateValue) is int value)
        {
            return value != 0;
        }

        return false;
    }

    public void SaveAlwaysElevate(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.SetValue(AlwaysElevateValue, enabled ? 1 : 0, RegistryValueKind.DWord);
    }

    public AppMode? LoadPersistedAppMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key?.GetValue(AppModeValue) is string mode &&
            Enum.TryParse<AppMode>(mode, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    public void SaveAppMode(AppMode mode)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.SetValue(AppModeValue, mode.ToString(), RegistryValueKind.String);
    }

    internal IReadOnlySet<string> LoadLegacyImportOnlyFileNames()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key?.GetValue(ImportOnlyValue) is not string json || string.IsNullOrWhiteSpace(json))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? [];
            return list.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    internal void ClearLegacyImportOnlyFileNames()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.DeleteValue(ImportOnlyValue, throwOnMissingValue: false);
    }

    public IReadOnlyList<DeletedReplaySnapshot> LoadDeletedReplays()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key?.GetValue(DeletedReplaysValue) is not string json || string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<DeletedReplaySnapshot>>(json, DeletedReplayJsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SaveDeletedReplays(IReadOnlyList<DeletedReplaySnapshot> snapshots)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        var json = JsonSerializer.Serialize(snapshots, DeletedReplayJsonOptions);
        key.SetValue(DeletedReplaysValue, json, RegistryValueKind.String);
    }
}

