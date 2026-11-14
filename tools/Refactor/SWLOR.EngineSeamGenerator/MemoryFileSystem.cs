namespace SWLOR.EngineSeamGenerator;

/// <summary>An in-memory file set: used to prove a second run is a fixed point, and by the generator's own tests.</summary>
internal sealed class MemoryFileSystem : IGeneratorFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.Ordinal);

    public MemoryFileSystem()
    {
    }

    public MemoryFileSystem(IEnumerable<KeyValuePair<string, string>> files)
    {
        foreach (var (path, content) in files)
        {
            _files[path] = content;
        }
    }

    public IReadOnlyDictionary<string, string> Files => _files;

    public IReadOnlyList<string> ListFiles(string directory, bool recursive)
    {
        var prefix = directory + "/";
        return _files.Keys
            .Where(path => path.StartsWith(prefix, StringComparison.Ordinal))
            .Where(path => recursive || path.IndexOf('/', prefix.Length) < 0)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    public string? ReadAllText(string path) => _files.GetValueOrDefault(path);

    public void WriteAllText(string path, string content) => _files[path] = content;

    public void Delete(string path) => _files.Remove(path);
}
