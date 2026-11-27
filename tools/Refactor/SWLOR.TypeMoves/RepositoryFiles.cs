using System.Diagnostics;

namespace SWLOR.TypeMoves;

/// <summary>
/// The text files of a repository the tool's text rewrite reads (code, scripts, project files, manifests, path lists, JSON and
/// guides; the extensions <c>Move-SupportNamespaces.ps1</c> rewrites), outside <c>Module/</c>, <c>SWLOR_Haks/</c>, <c>design/</c> and
/// build output. In a git checkout the tracked and new files (<c>git ls-files</c>), else every file below the root.
/// </summary>
internal static class RepositoryFiles
{
    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".razor", ".cshtml", ".csx", ".ps1", ".py", ".sh", ".cmd", ".csproj", ".props", ".targets", ".txt", ".json", ".md",
        ".yml", ".yaml", ".runsettings", ".xml"
    };

    private static readonly string[] SkippedRoots = ["Module/", "SWLOR_Haks/", "design/", ".git/", ".claude/", "node_modules/"];

    private const long MaximumSize = 8 * 1024 * 1024;

    /// <summary>Repository-relative paths (forward slashes), ordinal order.</summary>
    public static List<string> List(string root)
    {
        var paths = Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git"))
            ? GitFiles(root)
            : WalkedFiles(root);
        var result = paths.Where(path => InScope(root, path)).Distinct(StringComparer.Ordinal).ToList();
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    public static bool InScope(string root, string path)
    {
        if (SkippedRoots.Any(skipped => path.StartsWith(skipped, StringComparison.Ordinal)) ||
            path.Contains("/bin/", StringComparison.Ordinal) || path.Contains("/obj/", StringComparison.Ordinal) ||
            path.StartsWith("bin/", StringComparison.Ordinal) || path.StartsWith("obj/", StringComparison.Ordinal))
            return false;

        if (!TextExtensions.Contains(Path.GetExtension(path)))
            return false;

        var info = new FileInfo(Path.Combine(root, path));
        return info.Exists && info.Length > 0 && info.Length <= MaximumSize;
    }

    private static IEnumerable<string> GitFiles(string root)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in new[] { "-c", "core.quotepath=off", "ls-files", "-z", "--cached", "--others", "--exclude-standard" })
            start.ArgumentList.Add(argument);

        using var process = Process.Start(start) ?? throw new TypeMoveException("git could not be started.");
        var errors = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new TypeMoveException("git ls-files failed: " + errors.Result.Trim());

        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    private static IEnumerable<string> WalkedFiles(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            yield return Path.GetRelativePath(root, file).Replace('\\', '/');
    }

    /// <summary>Stages the given paths (moves and edits) in a git checkout, so a move reads as a rename; does nothing elsewhere.</summary>
    public static void Stage(string root, IReadOnlyCollection<string> paths)
    {
        if (paths.Count == 0 || !(Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git"))))
            return;

        foreach (var chunk in paths.Chunk(100))
        {
            var start = new ProcessStartInfo("git")
            {
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var argument in new[] { "--literal-pathspecs", "add", "-A", "--" }.Concat(chunk))
                start.ArgumentList.Add(argument);

            using var process = Process.Start(start) ?? throw new TypeMoveException("git could not be started.");
            var errors = process.StandardError.ReadToEndAsync();
            process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new TypeMoveException("git add failed: " + errors.Result.Trim());
        }
    }
}
