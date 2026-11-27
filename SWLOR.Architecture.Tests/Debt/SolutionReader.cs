using System.Text.RegularExpressions;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>Lists the C# projects of a solution file and the source files of each.</summary>
internal static class SolutionReader
{
    private static readonly Regex ProjectLine = new(
        @"^Project\(""\{[^}]+\}""\)\s*=\s*""(?<name>[^""]+)"",\s*""(?<path>[^""]+\.csproj)""",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly string[] SkippedFolders = ["bin", "obj"];

    /// <summary>The solution's C# projects in file order; solution folders are skipped.</summary>
    public static IReadOnlyList<SolutionProject> Read(string repositoryRoot, string solutionFileName)
    {
        var text = File.ReadAllText(Path.Combine(repositoryRoot, solutionFileName));
        var projects = ProjectLine.Matches(text)
            .Select(match => (Name: match.Groups["name"].Value,
                              Directory: Path.GetFullPath(Path.GetDirectoryName(Path.Combine(repositoryRoot, match.Groups["path"].Value.Replace('\\', '/')))!)))
            .ToList();
        var projectFolders = projects.Select(project => project.Directory).ToList();

        return projects
            .Select(project => new SolutionProject(project.Name, project.Directory, SourceFiles(repositoryRoot, project.Directory, projectFolders)))
            .ToList();
    }

    /// <summary>A path relative to the repository root with forward slashes.</summary>
    public static string Relative(string repositoryRoot, string path) =>
        Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

    private static IReadOnlyList<string> SourceFiles(string repositoryRoot, string projectFolder, IReadOnlyList<string> projectFolders)
    {
        var files = new List<string>();
        if (!System.IO.Directory.Exists(projectFolder))
            return files;

        var pending = new Stack<string>();
        pending.Push(projectFolder);
        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            files.AddRange(System.IO.Directory.EnumerateFiles(folder, "*.cs").Select(file => Relative(repositoryRoot, file)));
            foreach (var child in System.IO.Directory.EnumerateDirectories(folder))
            {
                var name = Path.GetFileName(child);
                if (SkippedFolders.Contains(name, StringComparer.OrdinalIgnoreCase) || name.StartsWith('.'))
                    continue;
                if (projectFolders.Any(other => string.Equals(Path.GetFullPath(child), other, StringComparison.OrdinalIgnoreCase)))
                    continue;

                pending.Push(child);
            }
        }

        files.Sort(StringComparer.Ordinal);
        return files;
    }
}
