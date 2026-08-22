// SPDX-License-Identifier: MIT

using System.Text.Json;
using SWLOR.NWN.Formats.Bif;
using SWLOR.NWN.Formats.Key;

namespace SWLOR.NWN.Formats.Corpus.Tests;

internal static class LicensedCorpus
{
    internal sealed record Resource(
        string KeyName,
        string ResRef,
        ushort ResourceType,
        string BifPath,
        uint Offset,
        uint Size);

    public static string RepositoryRoot
    {
        get
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")))
                    return current.FullName;
            }
            throw new DirectoryNotFoundException("Could not locate the repository root.");
        }
    }

    public static string HaksRoot
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("SWLOR_HAKS_CORPUS");
            var path = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(RepositoryRoot, "SWLOR_Haks")
                : Path.GetFullPath(configured);
            if (!Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any())
            {
                throw new DirectoryNotFoundException(
                    "Licensed corpus is required, but SWLOR_Haks is not initialized. " +
                    "Initialize only that submodule or set SWLOR_HAKS_CORPUS.");
            }
            return path;
        }
    }

    public static string InstallRoot
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("NWN_INSTALL_ROOT");
            if (!string.IsNullOrWhiteSpace(configured) &&
                Directory.Exists(Path.Combine(configured, "data")))
            {
                return Path.GetFullPath(configured);
            }

            var candidates = new[]
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\Neverwinter Nights",
                @"C:\Program Files\Steam\steamapps\common\Neverwinter Nights",
                @"C:\GOG Games\Neverwinter Nights Enhanced Edition",
                @"C:\Program Files (x86)\GOG Galaxy\Games\Neverwinter Nights Enhanced Edition",
                @"C:\Program Files\GOG Galaxy\Games\Neverwinter Nights Enhanced Edition",
                @"C:\Program Files (x86)\Beamdog Library\00785",
                @"C:\Program Files\Beamdog Library\00785"
            };
            return candidates.FirstOrDefault(path => Directory.Exists(Path.Combine(path, "data"))) ??
                   throw new DirectoryNotFoundException(
                       "Licensed corpus is required, but no NWN:EE data directory was found.");
        }
    }

    public static string DataDirectory => Path.Combine(InstallRoot, "data");

    public static IEnumerable<string> HakSourceDirectories()
    {
        var configPath = Path.Combine(RepositoryRoot, "Build", "hakbuilder.json");
        using var document = JsonDocument.Parse(File.ReadAllBytes(configPath));
        foreach (var hak in document.RootElement.GetProperty("HakList").EnumerateArray())
        {
            var configured = hak.GetProperty("Path").GetString();
            if (string.IsNullOrWhiteSpace(configured))
                continue;

            var directoryName = Path.GetFileName(
                configured.TrimEnd('/', '\\').Replace('\\', Path.DirectorySeparatorChar));
            var path = Path.Combine(HaksRoot, directoryName);
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException($"Required hak source directory '{path}' is absent.");
            yield return path;
        }
    }

    public static IReadOnlyList<Resource> KeyResources(ushort? resourceType = null)
    {
        var resources = new List<Resource>();
        foreach (var keyPath in Directory.EnumerateFiles(DataDirectory, "*.key", SearchOption.TopDirectoryOnly)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var key = KeyReader.Read(keyPath);
            var bifs = new Dictionary<int, (string Path, BifFile File)>();
            foreach (var resource in key.ResourceEntries)
            {
                if (resourceType.HasValue && resource.ResourceType != resourceType.Value)
                    continue;
                if (!bifs.TryGetValue(resource.BifIndex, out var bif))
                {
                    var descriptor = key.GetBifForResource(resource) ??
                                     throw new InvalidDataException($"{Path.GetFileName(keyPath)} has a missing BIF.");
                    var bifPath = ResolveBifPath(DataDirectory, descriptor.Filename);
                    bif = (bifPath, BifReader.ReadMetadataOnly(bifPath));
                    bifs.Add(resource.BifIndex, bif);
                }

                if (resource.VariableTableIndex < 0 ||
                    resource.VariableTableIndex >= bif.File.VariableResources.Count)
                {
                    throw new InvalidDataException(
                        $"{Path.GetFileName(keyPath)}:{resource.ResRef} has an invalid BIF resource index.");
                }

                var entry = bif.File.VariableResources[resource.VariableTableIndex];
                resources.Add(new Resource(
                    Path.GetFileName(keyPath),
                    resource.ResRef,
                    resource.ResourceType,
                    bif.Path,
                    entry.Offset,
                    entry.Size));
            }
        }
        return resources;
    }

    public static byte[] Read(Resource resource)
    {
        using var stream = new FileStream(
            resource.BifPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.RandomAccess);
        stream.Seek(resource.Offset, SeekOrigin.Begin);
        var bytes = new byte[checked((int)resource.Size)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    public static byte[] ReadPrefix(Resource resource, int count)
    {
        if (resource.Size < count)
            throw new InvalidDataException($"{resource.ResRef} is shorter than {count} bytes.");
        using var stream = new FileStream(
            resource.BifPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.RandomAccess);
        stream.Seek(resource.Offset, SeekOrigin.Begin);
        var bytes = new byte[count];
        stream.ReadExactly(bytes);
        return bytes;
    }

    public static byte[] ReadPrefix(string path, int count)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.SequentialScan);
        if (stream.Length < count)
            throw new InvalidDataException($"{path} is shorter than {count} bytes.");
        var bytes = new byte[count];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static string ResolveBifPath(string dataDirectory, string filename)
    {
        var normalized = filename
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        var installRoot = Path.GetDirectoryName(dataDirectory) ?? dataDirectory;
        var fullInstallRoot = Path.GetFullPath(installRoot);
        var fromInstallRoot = Path.GetFullPath(normalized, fullInstallRoot);
        var allowedRoot = fullInstallRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                          Path.DirectorySeparatorChar;
        if (!fromInstallRoot.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"BIF path '{filename}' escapes the NWN installation.");
        if (File.Exists(fromInstallRoot))
            return fromInstallRoot;

        var fromDataDirectory = Path.Combine(dataDirectory, Path.GetFileName(normalized));
        if (File.Exists(fromDataDirectory))
            return fromDataDirectory;
        throw new FileNotFoundException($"BIF '{filename}' referenced by a KEY was not found.");
    }
}
