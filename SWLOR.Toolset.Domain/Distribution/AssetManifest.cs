using System.Text.Json;

namespace SWLOR.Toolset.Domain.Distribution;

public sealed class AssetManifest
{
    public const string FileName = "asset-manifest.json";
    public int SchemaVersion { get; set; } = 1;
    public string AssetRevision { get; set; } = string.Empty;
    public Dictionary<string, string> Files { get; set; } = new();

    public static AssetManifest Load(string root, string revision, bool verifyFiles)
    {
        var manifest = JsonSerializer.Deserialize<AssetManifest>(File.ReadAllText(Path.Combine(root, FileName)))
            ?? throw new InvalidDataException("Empty asset manifest.");
        if (manifest.SchemaVersion != 1 || manifest.AssetRevision != revision || !ContentPaths.IsRevision(revision) || manifest.Files == null || manifest.Files.Count == 0 ||
            !manifest.Files.ContainsKey("tlk/sw_tlk.tlk") || !manifest.Files.ContainsKey("tlk/sw_tlk.tlk.json"))
            throw new InvalidDataException("The asset pack does not match this workspace.");
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (relative, hash) in manifest.Files)
        {
            RequireAssetPath(relative);
            var path = ContentPaths.Resolve(root, relative);
            if (!paths.Add(relative) || !ContentPaths.IsHash(hash) || !File.Exists(path) ||
                (verifyFiles && !string.Equals(ContentPaths.HashFile(path), hash, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException($"Missing or damaged asset: {relative}. Download the asset pack again.");
        }
        return manifest;
    }

    public static void RequireAssetPath(string path)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(path, @"^hak/[a-zA-Z0-9_]+\.hak$") &&
            path is not ("tlk/sw_tlk.tlk" or "tlk/sw_tlk.tlk.json"))
            throw new InvalidDataException($"Unsupported asset path: {path}");
    }
}
