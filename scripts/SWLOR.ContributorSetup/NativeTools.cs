using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace SWLOR.ContributorSetup;

internal class NativeTools
{
    private readonly string _root;
    private readonly CommandRunner _runner;
    private readonly NativeToolConfiguration _configuration;
    private readonly bool _windows;
    private readonly Func<string, bool> _libraryAvailable;
    internal static readonly IReadOnlyDictionary<string, string> DesktopLibraries = new Dictionary<string, string>
    {
        ["libfontconfig.so.1"] = "libfontconfig1", ["libX11.so.6"] = "libx11-6",
        ["libXrandr.so.2"] = "libxrandr2", ["libXfixes.so.3"] = "libxfixes3",
        ["libXi.so.6"] = "libxi6", ["libXcursor.so.1"] = "libxcursor1",
        ["libICE.so.6"] = "libice6", ["libSM.so.6"] = "libsm6",
        ["libglib-2.0.so.0"] = "libglib2.0-0t64", ["libGL.so.1"] = "libgl1"
    };
    internal string CacheDirectory { get; }

    public NativeTools(string root, CommandRunner runner, NativeToolConfiguration? configuration = null,
        string? cacheDirectory = null, bool? windows = null, Func<string, bool>? libraryAvailable = null)
    {
        _root = root;
        _runner = runner;
        _configuration = configuration ?? NativeToolConfiguration.Load(root);
        _windows = windows ?? OperatingSystem.IsWindows();
        _libraryAvailable = libraryAvailable ?? CanLoad;
        CacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
            "SWLOR.Toolset", "NativeTools", _windows ? "win-x64" : "linux-x64");
    }

    public virtual IEnumerable<string> CheckPrerequisites(string profile)
    {
        if (_windows)
        {
            foreach (var name in _configuration.WindowsFiles)
                if (!File.Exists(Path.Combine(_root, "tools", "SWLOR.CLI", name)))
                    yield return "Restore the bundled utility " + name + " from Git.";
            yield break;
        }
        if (new[] { "libpcre.so.3", "libsqlite3.so.0" }.Any(name => !_libraryAvailable(name)))
            yield return "Install libpcre3 and libsqlite3-0 (Ubuntu: sudo apt-get install libpcre3 libsqlite3-0).";
        if (profile == "Toolset")
        {
            var missing = DesktopLibraries.Keys.Where(name => !_libraryAvailable(name)).ToArray();
            if (missing.Length != 0)
                yield return "Install the desktop libraries: " + string.Join(" ", DesktopLibraries.Values) +
                    " libgl1-mesa-dri (Ubuntu: sudo apt-get install <packages>). Missing libraries: " + string.Join(", ", missing) + ".";
        }
    }

    private static bool CanLoad(string name)
    {
        if (!NativeLibrary.TryLoad(name, out var library)) return false;
        NativeLibrary.Free(library);
        return true;
    }

    public virtual bool IsPrepared()
    {
        if (_windows) return !CheckPrerequisites("Toolset").Any();
        try
        {
            var receipt = JsonSerializer.Deserialize<NativeToolReceipt>(File.ReadAllText(Path.Combine(CacheDirectory, "native-tools.json")));
            var required = _configuration.NativeNames.Concat(new[] { "libnwnscriptcomp.so", "libpcre.so.3", "libsqlite3.so.0" });
            if (receipt?.Version != _configuration.Version || receipt.Files == null ||
                !required.All(receipt.Files.ContainsKey)) return false;
            foreach (var pair in receipt.Files)
            {
                if (string.IsNullOrEmpty(pair.Key) || pair.Key is "." or ".." || pair.Key.Contains('/') || pair.Key.Contains('\\'))
                    return false;
                var path = Path.Combine(CacheDirectory, pair.Key);
                if (!File.Exists(path) || !Hash(path).Equals(pair.Value, StringComparison.OrdinalIgnoreCase)) return false;
            }
            if (OperatingSystem.IsLinux())
                foreach (var name in _configuration.NativeNames)
                    if ((File.GetUnixFileMode(Path.Combine(CacheDirectory, name)) & UnixFileMode.UserExecute) == 0)
                        return false;
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    public virtual async Task<string> PrepareAsync(CancellationToken cancellation)
    {
        if (_windows) return Path.Combine(_root, "tools", "SWLOR.CLI");
        if (IsPrepared()) return CacheDirectory;
        if (Directory.Exists(CacheDirectory))
            throw new InvalidOperationException($"The native tool cache is incomplete. Close the toolset, delete {CacheDirectory}, and rerun setup.");

        var ldconfig = _runner.Exists("ldconfig") ? "ldconfig" :
            new[] { "/sbin/ldconfig", "/usr/sbin/ldconfig" }.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException("Cannot find ldconfig to locate Linux native libraries.");
        var listing = await _runner.CheckedAsync(ldconfig, new[] { "-p" }, _root,
            capture: true, timeout: TimeSpan.FromSeconds(30), cancellation: cancellation);
        var libraries = new[] { "libpcre.so.3", "libsqlite3.so.0" }.ToDictionary(name => name, name =>
        {
            var line = listing.Split('\n').FirstOrDefault(line => line.TrimStart().StartsWith(name + " ", StringComparison.Ordinal) &&
                line.Contains("x86-64", StringComparison.Ordinal) && line.Contains(" => ", StringComparison.Ordinal));
            return line?.Split(" => ", 2)[1].Trim() ?? throw new FileNotFoundException("Cannot locate x64 native library " + name);
        });

        var parent = Path.GetDirectoryName(CacheDirectory)!;
        Directory.CreateDirectory(parent);
        var temporary = Path.Combine(parent, ".swlor-native-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            var archive = Path.Combine(temporary, "neverwinter.zip");
            Console.WriteLine($"Downloading verified neverwinter.nim {_configuration.Version} Linux tools...");
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            using (var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan })
            {
                // Bound the entire transfer, including the streamed response body.
                deadline.CancelAfter(TimeSpan.FromMinutes(5));
                try
                {
                    using var response = await client.GetAsync(_configuration.LinuxUrl, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                    response.EnsureSuccessStatusCode();
                    await using var target = File.Create(archive);
                    await response.Content.CopyToAsync(target, deadline.Token);
                }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
                {
                    throw new TimeoutException("The native tools download timed out. Rerun setup to try again.");
                }
            }
            var staging = Path.Combine(temporary, "tools");
            ExtractVerifiedArchive(archive, staging);
            foreach (var pair in libraries) File.Copy(pair.Value, Path.Combine(staging, pair.Key));
            foreach (var package in new[] { "libpcre3", "libsqlite3-0" })
            {
                var notice = Path.Combine("/usr/share/doc", package, "copyright");
                if (File.Exists(notice)) File.Copy(notice, Path.Combine(staging, package + "-copyright.txt"));
            }
            await _runner.CheckedAsync(Path.Combine(staging, "nwn_script_comp"), new[] { "--help" }, _root,
                EnvironmentFor(staging), capture: true, timeout: TimeSpan.FromSeconds(30), cancellation: cancellation);
            var hashes = Directory.GetFiles(staging).ToDictionary(path => Path.GetFileName(path), Hash);
            var receipt = new NativeToolReceipt(_configuration.Version, _configuration.LinuxUrl, _configuration.LinuxSha256, hashes);
            await File.WriteAllTextAsync(Path.Combine(staging, "native-tools.json"),
                JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true }) + "\n", cancellation);
            cancellation.ThrowIfCancellationRequested();
            try { Directory.Move(staging, CacheDirectory); }
            catch (IOException) when (IsPrepared()) { } // Another setup process finished the same cache.
            return CacheDirectory;
        }
        finally { Directory.Delete(temporary, recursive: true); }
    }

    internal void ExtractVerifiedArchive(string archive, string staging)
    {
        if (!Hash(archive).Equals(_configuration.LinuxSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The native tools download failed its SHA-256 check.");
        using var bundle = ZipFile.OpenRead(archive);
        Directory.CreateDirectory(staging);
        foreach (var name in _configuration.NativeNames.Append("libnwnscriptcomp.so"))
        {
            var entry = bundle.GetEntry(name) ?? throw new InvalidDataException("Missing native utility " + name);
            entry.ExtractToFile(Path.Combine(staging, name));
            if (OperatingSystem.IsLinux())
                File.SetUnixFileMode(Path.Combine(staging, name),
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }

    public virtual IReadOnlyDictionary<string, string> EnvironmentFor(string directory)
    {
        var environment = new Dictionary<string, string>
        {
            ["SWLOR_NATIVE_TOOLS"] = directory,
            ["PATH"] = directory + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH")
        };
        if (!_windows)
        {
            var previous = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
            environment["LD_LIBRARY_PATH"] = directory + (string.IsNullOrEmpty(previous) ? "" : ":" + previous);
        }
        return environment;
    }

    internal static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}
