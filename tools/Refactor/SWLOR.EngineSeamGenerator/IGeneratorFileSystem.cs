namespace SWLOR.EngineSeamGenerator;

/// <summary>The files the generator reads and writes, addressed by repository-relative '/' paths.</summary>
internal interface IGeneratorFileSystem
{
    /// <summary>The `.cs` files below a directory (all depths), sorted ordinally. Empty when the directory is missing.</summary>
    IReadOnlyList<string> ListFiles(string directory, bool recursive);

    /// <summary>The text of a file, or null when it does not exist.</summary>
    string? ReadAllText(string path);

    void WriteAllText(string path, string content);

    void Delete(string path);
}
