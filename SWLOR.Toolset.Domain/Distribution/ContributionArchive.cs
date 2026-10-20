using SWLOR.NWN.Formats.Common;
using System.IO.Compression;
using System.Text.Json;

namespace SWLOR.Toolset.Domain.Distribution;

/// <summary>Exports byte-preserving changes and refuses imports over a different file generation.</summary>
public static class ContributionArchive
{
    public const string ManifestName = "contribution.json";
    private const long MaximumBytes = 512L * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Export(string root, string destination)
    {
        using var conversationLease = ModuleWriteLock.Acquire(Path.Combine(root, "SWLOR.Game.Server", "ConversationData"));
        using var moduleLease = ModuleWriteLock.Acquire(Path.Combine(root, "Module"));
        var workspace = WorkspaceManifest.Load(root);
        var current = ContentPaths.Capture(root);
        var manifest = new ContributionManifest { ContentRevision = workspace.ContentRevision, AssetRevision = workspace.AssetRevision };
        foreach (var path in workspace.Baseline.Keys.Union(current.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.Ordinal))
        {
            workspace.Baseline.TryGetValue(path, out var before);
            current.TryGetValue(path, out var after);
            if (!string.Equals(before, after, StringComparison.OrdinalIgnoreCase))
                manifest.Changes.Add(new ContributionChange(path, before, after));
        }
        if (manifest.Changes.Count == 0) throw new InvalidOperationException("There are no saved changes to export.");
        if (File.Exists(destination)) throw new IOException("Choose a new contribution file; the destination already exists.");
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var zip = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                using (var writer = new StreamWriter(zip.CreateEntry(ManifestName).Open()))
                    writer.Write(JsonSerializer.Serialize(manifest, JsonOptions));
                long total = 0;
                foreach (var change in manifest.Changes.Where(change => change.AfterHash != null))
                {
                    var bytes = File.ReadAllBytes(ContentPaths.Resolve(root, change.Path));
                    total += bytes.LongLength;
                    if (total > MaximumBytes) throw new InvalidDataException("This contribution exceeds 512 MiB. Export smaller sets of changes.");
                    if (ContentPaths.Hash(bytes) != change.AfterHash) throw new IOException($"{change.Path} changed during export. Save and try again.");
                    using var stream = zip.CreateEntry("files/" + change.Path, CompressionLevel.Optimal).Open();
                    stream.Write(bytes);
                }
            }
            File.Move(temporary, destination);
            return manifest.Changes.Count;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    /// <summary>Checks the entire archive before any writes. Run in a clean review branch.</summary>
    public static int Import(string archive, string root, bool apply)
    {
        using var conversationLease = ModuleWriteLock.Acquire(Path.Combine(root, "SWLOR.Game.Server", "ConversationData"));
        using var moduleLease = ModuleWriteLock.Acquire(Path.Combine(root, "Module"));
        using var zip = ZipFile.OpenRead(archive);
        if (zip.Entries.Count > 50001 || zip.Entries.Sum(entry => entry.Length) > MaximumBytes)
            throw new InvalidDataException("The contribution is too large.");
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
            if (!entries.TryAdd(entry.FullName, entry)) throw new InvalidDataException("Duplicate archive entry.");
        if (!entries.TryGetValue(ManifestName, out var manifestEntry) || manifestEntry.Length > 16 * 1024 * 1024)
            throw new InvalidDataException("Contribution manifest is missing or too large.");
        using var input = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<ContributionManifest>(input) ?? throw new InvalidDataException("Empty contribution manifest.");
        if (manifest.SchemaVersion != 1 || !ContentPaths.IsRevision(manifest.ContentRevision) ||
            !ContentPaths.IsRevision(manifest.AssetRevision) || manifest.Changes == null || manifest.Changes.Count == 0)
            throw new InvalidDataException("Unsupported contribution manifest.");
        var expectedEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ManifestName };
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prepared = new List<(string Path, byte[]? Before, byte[]? After)>();
        foreach (var change in manifest.Changes)
        {
            ContentPaths.RequireContentPath(change.Path);
            if (!paths.Add(change.Path) || change.BeforeHash == change.AfterHash ||
                (change.BeforeHash != null && !ContentPaths.IsHash(change.BeforeHash)) ||
                (change.AfterHash != null && !ContentPaths.IsHash(change.AfterHash)))
                throw new InvalidDataException($"Invalid change: {change.Path}");
            var target = ContentPaths.Resolve(root, change.Path);
            var before = File.Exists(target) ? File.ReadAllBytes(target) : null;
            var actual = before == null ? null : ContentPaths.Hash(before);
            if (!string.Equals(actual, change.BeforeHash, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"Conflict: {change.Path} has changed since the contributor's snapshot. No files were imported.");
            byte[]? after = null;
            if (change.AfterHash != null)
            {
                var name = "files/" + change.Path;
                expectedEntries.Add(name);
                if (!entries.TryGetValue(name, out var entry)) throw new InvalidDataException($"Missing payload: {change.Path}");
                using var payload = entry.Open();
                using var bytes = new MemoryStream();
                payload.CopyTo(bytes);
                after = bytes.ToArray();
                if (!string.Equals(ContentPaths.Hash(after), change.AfterHash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Payload checksum failed: {change.Path}");
            }
            prepared.Add((target, before, after));
        }
        if (!expectedEntries.SetEquals(entries.Keys)) throw new InvalidDataException("The contribution contains unexpected files.");
        if (!apply) return prepared.Count;
        var changed = new List<(string Path, byte[]? Before)>();
        try
        {
            foreach (var item in prepared)
            {
                // Recheck after reading all payloads; a concurrent edit is never silently replaced.
                var current = File.Exists(item.Path) ? File.ReadAllBytes(item.Path) : null;
                if (!(current == null ? item.Before == null : item.Before != null && current.AsSpan().SequenceEqual(item.Before)))
                    throw new IOException($"{item.Path} changed during import.");
                changed.Add((item.Path, item.Before));
                Replace(item.Path, item.After);
            }
        }
        catch
        {
            foreach (var item in changed.AsEnumerable().Reverse()) Replace(item.Path, item.Before);
            throw;
        }
        return prepared.Count;
    }

    private static void Replace(string path, byte[]? bytes)
    {
        if (bytes == null) { File.Delete(path); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, path, overwrite: true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
