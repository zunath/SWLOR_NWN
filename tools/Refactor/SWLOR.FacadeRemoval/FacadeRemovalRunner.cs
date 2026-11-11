using System.Text.RegularExpressions;

namespace SWLOR.FacadeRemoval;

/// <summary>
/// Runs the tool over a tree: every <c>.cs</c> file outside build output and data folders (except in a project that neither
/// is the server nor references it), the global usings of projects that import <c>SWLOR.Game.Server.Service</c> globally,
/// and the handler order manifest. What the command line and the tests call.
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

    /// <summary>The server's source folder below a repository root.</summary>
    private const string ServerFolder = "SWLOR.Game.Server";

    private static readonly Regex NamespaceDeclaration = new(@"^[ \t]*namespace[ \t]+([A-Za-z_][A-Za-z0-9_.]*)", RegexOptions.Multiline);

    /// <summary>The server's project file: a project that neither is it nor references it cannot call a removed facade.</summary>
    private const string ServerProjectFile = "SWLOR.Game.Server.csproj";

    private static readonly Regex ProjectReference = new(@"<ProjectReference\s+Include\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);

    private static readonly Regex GlobalServiceImport =new(@"^[ \t]*global[ \t]+using[ \t]+SWLOR\.Game\.Server\.Service[ \t]*;[^\n]*\n?", RegexOptions.Multiline);

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
        var seesServer = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var needGlobal = new SortedSet<string>(StringComparer.Ordinal);

        if (active.Count > 0)
        {
            foreach (var path in SourceFiles(root))
            {
                var project = ProjectOf(Path.GetDirectoryName(path)!, root, projects);

                // Only the server and the projects that reference it can call a removed facade; a name in any other
                // project that matches one (SWLOR.BackgroundServices' own BackgroundJob) is that project's own.
                if (project != null && !SeesServer(project, seesServer))
                    continue;

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
                : new Dictionary<string, IReadOnlyList<string>>();
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

        // The tree the folder is reversed against: the repository above it (master's parity copy), whose server sources
        // decide which branch-only imports still name a namespace there.
        var repository = RepositoryLocator.Find(options.Root);
        var declared = repository == null ? null : DeclaredNamespaces(Path.Combine(repository, ServerFolder));

        var files = 0;
        var usings = 0;
        var rewrites = 0;
        foreach (var path in SourceFiles(options.Root))
        {
            var source = SourceFile.Read(path);
            var result = CallerRewriter.Reverse(source.Text, facades, declared);
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
    /// <summary>
    /// The namespaces the <c>.cs</c> files under <paramref name="serverFolder"/> declare, with every enclosing namespace
    /// (a using of <c>A.B</c> is valid when only <c>A.B.C</c> declares types). Empty when the folder does not exist. The server
    /// declares one namespace per file (file-scoped or a block), never a nested one.
    /// </summary>
    public static IReadOnlySet<string> DeclaredNamespaces(string serverFolder)
    {
        var declared = new HashSet<string>(StringComparer.Ordinal);
        if (!Directory.Exists(serverFolder))
            return declared;

        foreach (var path in SourceFiles(serverFolder))
        {
            foreach (Match match in NamespaceDeclaration.Matches(SourceFile.Read(path).Text))
            {
                var name = match.Groups[1].Value;
                while (declared.Add(name) && name.Contains('.'))
                    name = name[..name.LastIndexOf('.')];
            }
        }

        return declared;
    }

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

    /// <summary>True when a project file in <paramref name="project"/> is the server's or references it, directly or through another project.</summary>
    private static bool SeesServer(string project, Dictionary<string, bool> cache) =>
        Directory.EnumerateFiles(project, "*.csproj").Any(file => ReferencesServer(Path.GetFullPath(file), cache));

    private static bool ReferencesServer(string projectFile, Dictionary<string, bool> cache)
    {
        if (cache.TryGetValue(projectFile, out var known))
            return known;

        cache[projectFile] = false; // a reference cycle ends here
        var result = string.Equals(Path.GetFileName(projectFile), ServerProjectFile, StringComparison.OrdinalIgnoreCase);
        if (!result)
        {
            foreach (Match match in ProjectReference.Matches(File.ReadAllText(projectFile)))
            {
                var include = match.Groups[1].Value;
                if (include.Contains("$(", StringComparison.Ordinal))
                    continue; // a path built from an MSBuild property points outside this tree (the external toolset sources)

                var referenced = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectFile)!, include.Replace('\\', Path.DirectorySeparatorChar)));
                if (File.Exists(referenced) && ReferencesServer(referenced, cache))
                {
                    result = true;
                    break;
                }
            }
        }

        cache[projectFile] = result;
        return result;
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
