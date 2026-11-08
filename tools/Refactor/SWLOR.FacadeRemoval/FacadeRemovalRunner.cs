using System.Text.RegularExpressions;

namespace SWLOR.FacadeRemoval;

/// <summary>
/// Runs the tool over a tree: every <c>.cs</c> file outside build output and data folders, the global usings of projects
/// that import <c>SWLOR.Game.Server.Service</c> globally, and the handler order manifest. What the command line and the
/// tests call.
/// </summary>
internal static class FacadeRemovalRunner
{
    /// <summary>Success (and, for a check, nothing to change).</summary>
    public const int Ok = 0;

    /// <summary>A check found something <see cref="RunMode.Apply"/> would change.</summary>
    public const int Differs = 1;

    /// <summary>The input could not be processed; nothing was written.</summary>
    public const int Failed = 2;

    public const string ManifestPath = "SWLOR.Game.Server/Core/ScriptHandlerOrder.txt";

    /// <summary>The accessor's main file. A tree without it is not the branch, and a run there changes nothing.</summary>
    public const string AccessorPath = "SWLOR.Game.Server/Composition/GameServices.cs";

    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", ".claude", "node_modules", "packages", "TestResults", "SWLOR_Haks", "Module"
    };

    private static readonly Regex GlobalServiceImport = new(@"^[ \t]*global[ \t]+using[ \t]+SWLOR\.Game\.Server\.Service[ \t]*;[^\n]*\n?", RegexOptions.Multiline);

    public static int Run(RunOptions options, TextWriter log)
    {
        try
        {
            var facades = RemovedFacadeList.Load(options.DataPath);
            return options.Mode == RunMode.Reverse ? Reverse(options, facades, log) : ApplyOrCheck(options, facades, log);
        }
        catch (FacadeRemovalException exception)
        {
            log.WriteLine("Nothing was written: " + exception.Message);
            return Failed;
        }
    }

    private static int ApplyOrCheck(RunOptions options, IReadOnlyList<RemovedFacade> facades, TextWriter log)
    {
        var root = options.Root;
        var active = new List<RemovedFacade>();
        if (!File.Exists(Path.Combine(root, AccessorPath)))
        {
            // Master (or the parity copy of it) has no accessor, and some facades were born on the branch, so a missing
            // facade file there does not mean the facade was removed.
            log.WriteLine($"Skipped every facade: this tree has no {AccessorPath}, so it is not the branch (master, or the parity copy of it).");
        }
        else
        {
            foreach (var facade in facades)
            {
                if (File.Exists(Path.Combine(root, facade.SourcePath)))
                    log.WriteLine($"Skipped {facade.Facade}: {facade.SourcePath} still exists in this tree, so its facade is not removed here.");
                else
                    active.Add(facade);
            }
        }

        var changes = new List<(string Path, byte[] Bytes)>();
        var usings = 0;
        var rewrites = 0;
        var projects = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var needGlobal = new SortedSet<string>(StringComparer.Ordinal);

        if (active.Count > 0)
        {
            foreach (var path in SourceFiles(root))
            {
                var project = ProjectOf(Path.GetDirectoryName(path)!, root, projects);
                var imported = project != null && ImportsServiceGlobally(project);
                var source = SourceFile.Read(path);
                var result = CallerRewriter.Apply(source.Text, active, imported);
                if (result.NeedsAccessorImport && imported)
                    needGlobal.Add(project!);
                if (!result.Changed(source.Text))
                    continue;

                changes.Add((path, source.Encode(result.Text)));
                usings += result.UsingsAdded;
                rewrites += result.Rewrites;
                log.WriteLine($"{Relative(root, path)}: {Describe(result.UsingsAdded, result.Rewrites)}");
            }

            foreach (var project in needGlobal)
            {
                var path = Path.Combine(project, "GlobalUsings.cs");
                var source = SourceFile.Read(path);
                if (source.Text.Contains(CallerRewriter.GlobalUsingStatic, StringComparison.Ordinal))
                    continue;

                var match = GlobalServiceImport.Match(source.Text);
                var eol = source.LineEnding;
                var insert = (match.Value.EndsWith('\n') ? string.Empty : eol) + CallerRewriter.GlobalUsingStatic + (match.Value.EndsWith('\n') ? eol : string.Empty);
                var text = source.Text.Insert(match.Index + match.Length, insert);
                changes.Add((path, source.Encode(text)));
                usings++;
                log.WriteLine($"{Relative(root, path)}: the project imports SWLOR.Game.Server.Service globally, so it imports GameServices globally too");
            }
        }

        var renamed = 0;
        var manifestPath = Path.Combine(root, ManifestPath);
        if (active.Count > 0 && File.Exists(manifestPath))
        {
            var renames = File.Exists(options.RenamesPath)
                ? ManifestRenamer.ReadRenames(File.ReadAllText(options.RenamesPath), options.RenamesPath)
                : new Dictionary<string, string>();
            var manifest = SourceFile.Read(manifestPath);
            var (text, count) = ManifestRenamer.Apply(manifest.Text, renames, active);
            if (count > 0)
            {
                changes.Add((manifestPath, manifest.Encode(text)));
                renamed = count;
                log.WriteLine($"{ManifestPath}: {count} handler line(s) renamed to the instance method");
            }
        }

        if (options.Mode == RunMode.Apply)
        {
            foreach (var (path, bytes) in changes)
                File.WriteAllBytes(path, bytes);
        }

        var verb = options.Mode == RunMode.Apply ? "Changed" : "Would change";
        log.WriteLine($"{verb} {changes.Count} file(s) for {active.Count} removed facade(s): {usings} using line(s) added, {rewrites} call site(s) rewritten, {renamed} manifest line(s) renamed.");
        return options.Mode == RunMode.Check && changes.Count > 0 ? Differs : Ok;
    }

    private static int Reverse(RunOptions options, IReadOnlyList<RemovedFacade> facades, TextWriter log)
    {
        if (!Directory.Exists(options.Root))
            throw new FacadeRemovalException($"The folder {options.Root} does not exist.");

        var files = 0;
        var usings = 0;
        var rewrites = 0;
        foreach (var path in SourceFiles(options.Root))
        {
            var source = SourceFile.Read(path);
            var result = CallerRewriter.Reverse(source.Text, facades);
            if (!result.Changed(source.Text))
                continue;

            File.WriteAllBytes(path, source.Encode(result.Text));
            files++;
            usings += result.UsingsAdded;
            rewrites += result.Rewrites;
            log.WriteLine($"{Relative(options.Root, path)}: {result.UsingsAdded} using line(s) removed, {result.Rewrites} call site(s) rewritten");
        }

        log.WriteLine($"Reversed {files} file(s): {usings} using line(s) removed, {rewrites} call site(s) rewritten.");
        return Ok;
    }

    /// <summary>Every <c>.cs</c> file under <paramref name="root"/>, in ordinal order, outside build output and data folders.</summary>
    public static IEnumerable<string> SourceFiles(string root)
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

    private static string? ProjectOf(string folder, string root, Dictionary<string, string?> cache)
    {
        if (cache.TryGetValue(folder, out var known))
            return known;

        string? project = null;
        if (Directory.EnumerateFiles(folder, "*.csproj").Any())
            project = folder;
        else if (!string.Equals(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) &&
                 Path.GetDirectoryName(folder) is { } parent)
            project = ProjectOf(parent, root, cache);

        cache[folder] = project;
        return project;
    }

    private static bool ImportsServiceGlobally(string project)
    {
        var path = Path.Combine(project, "GlobalUsings.cs");
        return File.Exists(path) && GlobalServiceImport.IsMatch(SourceFile.Read(path).Text);
    }

    private static string Describe(int usings, int rewrites)
    {
        var parts = new List<string>();
        if (usings > 0)
            parts.Add($"{usings} using line(s) added");
        if (rewrites > 0)
            parts.Add($"{rewrites} call site(s) rewritten");
        return string.Join(", ", parts);
    }

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
