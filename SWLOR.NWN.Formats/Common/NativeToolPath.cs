using System.Diagnostics;

namespace SWLOR.NWN.Formats.Common;

/// <summary>Resolves host-native NWN utilities without trying Windows binaries on Linux.</summary>
public static class NativeToolPath
{
    public static string ExecutableName(string name) => ExecutableName(name, OperatingSystem.IsWindows());
    public static string ExecutableName(string name, bool windows)
    {
        var stem = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
        return windows ? stem + ".exe" : stem;
    }

    public static string CacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SWLOR.Toolset", "NativeTools", OperatingSystem.IsWindows() ? "win-x64" : "linux-x64");

    public static string? FindInDirectory(string directory, string name)
    {
        var native = Path.Combine(directory, ExecutableName(name));
        if (File.Exists(native)) return native;
        // Existing Linux server deployments used .exe symlinks to the native utilities.
        var legacy = Path.Combine(directory, ExecutableName(name, windows: true));
        return !OperatingSystem.IsWindows() && File.Exists(legacy) && new FileInfo(legacy).LinkTarget != null
            ? legacy : null;
    }

    public static string? Find(string name, params string?[] directories)
    {
        var candidates = new[] { Environment.GetEnvironmentVariable("SWLOR_NATIVE_TOOLS") }
            .Concat(directories)
            .Concat(new[] { AppContext.BaseDirectory, Path.Combine(AppContext.BaseDirectory, "tools", "SWLOR.CLI"), CacheDirectory });
        foreach (var directory in candidates.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var path = FindInDirectory(directory!, name);
            if (path != null) return path;
        }
        return null;
    }

    public static void ConfigureLibraryPath(ProcessStartInfo startInfo)
    {
        if (!OperatingSystem.IsLinux()) return;
        var directory = Path.GetDirectoryName(Path.GetFullPath(startInfo.FileName))!;
        startInfo.Environment.TryGetValue("LD_LIBRARY_PATH", out var previous);
        startInfo.Environment["LD_LIBRARY_PATH"] = string.IsNullOrEmpty(previous) ? directory : directory + ":" + previous;
    }
}
