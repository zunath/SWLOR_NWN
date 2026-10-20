using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SWLOR.Toolset.Domain.Distribution;

/// <summary>Restricts contribution archives to authored content, never executable or configuration files.</summary>
public static class ContentPaths
{
    public static bool IsRevision(string value) => Regex.IsMatch(value ?? "", "^[a-f0-9]{40}([a-f0-9]{24})?$", RegexOptions.CultureInvariant);
    public static bool IsHash(string value) => Regex.IsMatch(value ?? "", "^[A-Fa-f0-9]{64}$", RegexOptions.CultureInvariant);
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    public static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static void RequireContentPath(string path)
    {
        var valid = path == "toolset/categories.json" ||
            Regex.IsMatch(path, @"^Module/(are|dlg|fac|gic|git|ifo|itp|jrl|utc|utd|ute|uti|utm|utp|uts|utt|utw)/[A-Za-z0-9_]+\.[a-z]+\.json$") ||
            Regex.IsMatch(path, @"^Module/(nss|ncs)/[A-Za-z0-9_]+\.(nss|ncs)$") ||
            Regex.IsMatch(path, @"^SWLOR\.Game\.Server/ConversationData/[A-Za-z0-9_]+\.conversation\.json$");
        if (!valid) throw new InvalidDataException($"Unsupported contribution path: {path}");
    }

    public static string Resolve(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative) || relative.Contains('\\') || relative.Contains(':') ||
            relative.StartsWith('/') || relative.Split('/').Any(part => part is "" or "." or ".."))
            throw new InvalidDataException($"Invalid archive path: {relative}");
        root = Path.GetFullPath(root);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Path escapes the workspace: {relative}");
        for (FileSystemInfo? entry = Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path); entry != null; entry = entry is FileInfo file ? file.Directory : ((DirectoryInfo)entry).Parent)
        {
            if (entry.Exists && (entry.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Linked paths are not supported: {relative}");
            if (string.Equals(entry.FullName, root, StringComparison.OrdinalIgnoreCase)) break;
        }
        return path;
    }

    public static Dictionary<string, string> Capture(string root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in new[] { "Module", "SWLOR.Game.Server/ConversationData", "toolset" })
        {
            var directory = Resolve(root, folder);
            if (!Directory.Exists(directory)) continue;
            foreach (var file in Directory.EnumerateFiles(directory, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                try { RequireContentPath(relative); }
                catch (InvalidDataException) { continue; }
                if (!result.TryAdd(relative, HashFile(file))) throw new InvalidDataException($"Duplicate content path: {relative}");
            }
        }
        return result;
    }
}
