using System.Diagnostics;
using System.Text;

namespace SWLOR.EngineSeamGenerator;

/// <summary>A read-only view of the repository as committed at a git revision.</summary>
internal sealed class GitFileSystem(string root, string revision) : IGeneratorFileSystem
{
    public IReadOnlyList<string> ListFiles(string directory, bool recursive)
    {
        var output = Git("ls-tree", "-r", "--name-only", revision, "--", directory + "/") ?? string.Empty;
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(path => path.EndsWith(".cs", StringComparison.Ordinal))
            .Where(path => recursive || path.IndexOf('/', directory.Length + 1) < 0)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    public string? ReadAllText(string path) => Git("show", $"{revision}:{path}");

    public void WriteAllText(string path, string content) => throw new InvalidOperationException("The git view is read-only.");

    public void Delete(string path) => throw new InvalidOperationException("The git view is read-only.");

    private string? Git(params string[] arguments)
    {
        var info = new ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            UseShellExecute = false
        };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);

        using var process = Process.Start(info) ?? throw new GeneratorException("git could not be started.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode == 0) return output;
        if (error.Contains("does not exist") || error.Contains("exists on disk, but not in")) return null;
        throw new GeneratorException($"git {string.Join(' ', arguments)} failed: {error.Trim()}");
    }
}
