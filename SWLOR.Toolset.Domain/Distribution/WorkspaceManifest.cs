using System.Text.Json;

namespace SWLOR.Toolset.Domain.Distribution;

/// <summary>Identifies a content snapshot without tying it to an application installation.</summary>
public sealed class WorkspaceManifest
{
    public const string FileName = "swlor-workspace.json";
    public int SchemaVersion { get; set; } = 1;
    public string ContentRevision { get; set; } = string.Empty;
    public string AssetRevision { get; set; } = string.Empty;
    public Dictionary<string, string> Baseline { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static WorkspaceManifest Load(string root)
    {
        var result = JsonSerializer.Deserialize<WorkspaceManifest>(File.ReadAllText(Path.Combine(root, FileName)))
            ?? throw new InvalidDataException("The workspace manifest is empty.");
        if (result.SchemaVersion != 1 || !ContentPaths.IsRevision(result.ContentRevision) ||
            !ContentPaths.IsRevision(result.AssetRevision) || result.Baseline == null)
            throw new InvalidDataException("This workspace has an unsupported or invalid manifest.");
        var baseline = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (path, hash) in result.Baseline)
        {
            ContentPaths.RequireContentPath(path);
            if (!ContentPaths.IsHash(hash) || !baseline.TryAdd(path, hash))
                throw new InvalidDataException($"Invalid baseline entry: {path}");
        }
        result.Baseline = baseline;
        return result;
    }

    public static string? FindRoot(string? moduleRoot)
    {
        if (string.IsNullOrWhiteSpace(moduleRoot)) return null;
        var parent = Directory.GetParent(Path.GetFullPath(moduleRoot))?.FullName;
        return parent != null && File.Exists(Path.Combine(parent, FileName)) ? parent : null;
    }
}
