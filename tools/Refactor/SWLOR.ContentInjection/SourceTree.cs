using System.Text.RegularExpressions;

namespace SWLOR.ContentInjection;

/// <summary>
/// The <c>.cs</c> files of a tree that the conversion may touch: the server's and those of every project that references it, outside
/// build output and data folders, with the kind of their project.
/// </summary>
internal static class SourceTree
{
    public const string ServerProject = "SWLOR.Game.Server";
    public const string EngineTestProject = "SWLOR.Game.Server.EngineTests";

    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", ".claude", "node_modules", "packages", "TestResults", "SWLOR_Haks", "Module"
    };

    private static readonly Regex ProjectReference = new(@"<ProjectReference\s+Include\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);

    /// <summary>Every file under <paramref name="root"/> whose project is the server or references it, in ordinal path order.</summary>
    public static IReadOnlyList<TreeFile> Load(string root)
    {
        var projects = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var references = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var globals = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var files = new List<TreeFile>();
        foreach (var path in Files(root))
        {
            var project = ProjectFileOf(Path.GetDirectoryName(path)!, root, projects);
            var kind = project == null ? ProjectKind.Unrelated : KindOf(project, references);
            if (kind == ProjectKind.Unrelated)
                continue;

            if (!globals.TryGetValue(project!, out var imports))
            {
                var globalUsings = Path.Combine(Path.GetDirectoryName(project!)!, "GlobalUsings.cs");
                imports = File.Exists(globalUsings) &&
                          SourceFile.Read(globalUsings).Text.Contains("global " + ContentRewriter.UsingStatic, StringComparison.Ordinal);
                globals[project!] = imports;
            }

            files.Add(new TreeFile(Relative(root, path), kind, imports, SourceFile.Read(path)));
        }

        return files;
    }

    /// <summary>Every <c>.cs</c> file under <paramref name="root"/>, in ordinal order, outside build output and data folders.</summary>
    public static IReadOnlyList<string> Files(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        var result = new List<string>();
        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            result.AddRange(Directory.EnumerateFiles(folder, "*.cs"));
            foreach (var child in Directory.EnumerateDirectories(folder))
            {
                var info = new DirectoryInfo(child);
                if (SkippedFolders.Contains(info.Name) || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
                pending.Push(child);
            }
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    public static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    private static ProjectKind KindOf(string projectFile, Dictionary<string, bool> references)
    {
        var name = Path.GetFileNameWithoutExtension(projectFile);
        if (name == ServerProject)
            return ProjectKind.Server;
        if (!ReferencesServer(projectFile, references))
            return ProjectKind.Unrelated;
        if (name == EngineTestProject)
            return ProjectKind.EngineTests;
        return name.EndsWith("Tests", StringComparison.Ordinal) ? ProjectKind.UnitTests : ProjectKind.Tool;
    }

    private static string? ProjectFileOf(string folder, string root, Dictionary<string, string?> cache)
    {
        if (cache.TryGetValue(folder, out var known))
            return known;

        var project = Directory.EnumerateFiles(folder, "*.csproj").OrderBy(file => file, StringComparer.Ordinal).FirstOrDefault();
        if (project == null &&
            !string.Equals(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) &&
            Path.GetDirectoryName(folder) is { } parent)
        {
            project = ProjectFileOf(parent, root, cache);
        }

        cache[folder] = project;
        return project;
    }

    private static bool ReferencesServer(string projectFile, Dictionary<string, bool> cache)
    {
        var full = Path.GetFullPath(projectFile);
        if (cache.TryGetValue(full, out var known))
            return known;

        cache[full] = false; // a reference cycle ends here
        var result = string.Equals(Path.GetFileNameWithoutExtension(full), ServerProject, StringComparison.OrdinalIgnoreCase);
        if (!result)
        {
            foreach (Match match in ProjectReference.Matches(File.ReadAllText(full)))
            {
                var include = match.Groups[1].Value;
                if (include.Contains("$(", StringComparison.Ordinal))
                    continue;

                var referenced = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(full)!, include.Replace('\\', Path.DirectorySeparatorChar)));
                if (File.Exists(referenced) && ReferencesServer(referenced, cache))
                {
                    result = true;
                    break;
                }
            }
        }

        cache[full] = result;
        return result;
    }
}
