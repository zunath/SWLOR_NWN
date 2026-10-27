using System.Text;

namespace SWLOR.EngineSeamGenerator;

/// <summary>Reads and writes files below a repository root as UTF-8 without a byte order mark.</summary>
internal sealed class DiskFileSystem(string root) : IGeneratorFileSystem
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public IReadOnlyList<string> ListFiles(string directory, bool recursive)
    {
        var full = Resolve(directory);
        if (!Directory.Exists(full)) return Array.Empty<string>();

        return Directory.GetFiles(full, "*.cs", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    public string? ReadAllText(string path)
    {
        var full = Resolve(path);
        if (!File.Exists(full)) return null;

        var bytes = File.ReadAllBytes(full);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            throw new GeneratorException($"{path}: files with a byte order mark are not supported, so every file can be written back byte for byte.");
        }

        return Utf8.GetString(bytes);
    }

    public void WriteAllText(string path, string content)
    {
        var full = Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, Utf8.GetBytes(content));
    }

    public void Delete(string path)
    {
        var full = Resolve(path);
        if (File.Exists(full)) File.Delete(full);
    }

    private string Resolve(string path) => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
}
