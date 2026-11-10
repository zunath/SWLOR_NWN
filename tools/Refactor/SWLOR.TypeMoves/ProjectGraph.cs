namespace SWLOR.TypeMoves;

/// <summary>
/// The projects of a repository the tool compiles: the server, every project that references it (directly or through
/// another project), and the projects those reference, in dependency order. Only the server and the projects that see it
/// can name a moved type; the others are compiled so the ones that see it bind like the real build.
/// </summary>
internal sealed class ProjectGraph
{
    public const string ServerProjectPath = "SWLOR.Game.Server/SWLOR.Game.Server.csproj";

    /// <summary>Folders below the repository root that never hold a project of the build.</summary>
    private static readonly HashSet<string> SkippedRootFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "SWLOR_Haks", "Module", "design", "packages"
    };

    private ProjectGraph(IReadOnlyList<ProjectInfo> ordered, IReadOnlySet<ProjectInfo> seeServer, ProjectInfo server)
    {
        Ordered = ordered;
        SeeServer = seeServer;
        Server = server;
    }

    /// <summary>Every compiled project, each after the projects it references.</summary>
    public IReadOnlyList<ProjectInfo> Ordered { get; }

    /// <summary>The server and the projects that reference it: the ones whose files the tool rewrites.</summary>
    public IReadOnlySet<ProjectInfo> SeeServer { get; }

    public ProjectInfo Server { get; }

    public static ProjectGraph Load(string root)
    {
        var serverFile = Path.GetFullPath(Path.Combine(root, ServerProjectPath));
        if (!File.Exists(serverFile))
            throw new TypeMoveException($"{ServerProjectPath} does not exist under {root}.");

        var projects = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in ProjectFiles(root))
            projects[file] = ProjectLoader.Load(file);

        var sees = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        bool SeesServer(string file)
        {
            if (sees.TryGetValue(file, out var known))
                return known;

            sees[file] = false; // a reference cycle ends here
            var result = string.Equals(file, serverFile, StringComparison.OrdinalIgnoreCase) ||
                         (projects.TryGetValue(file, out var project) && project.ProjectReferences.Any(SeesServer));
            sees[file] = result;
            return result;
        }

        var seeServer = projects.Keys.Where(SeesServer).ToList();
        var needed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>(seeServer);
        while (pending.Count > 0)
        {
            var file = pending.Pop();
            if (!projects.ContainsKey(file) || !needed.Add(file))
                continue;
            foreach (var reference in projects[file].ProjectReferences)
                pending.Push(reference);
        }

        var ordered = new List<ProjectInfo>();
        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        void Visit(string file)
        {
            if (state.TryGetValue(file, out var mark))
            {
                if (mark == 1)
                    throw new TypeMoveException($"The project references form a cycle through {file}.");
                return;
            }

            state[file] = 1;
            foreach (var reference in projects[file].ProjectReferences.Where(needed.Contains))
                Visit(reference);
            state[file] = 2;
            ordered.Add(projects[file]);
        }

        foreach (var file in needed.OrderBy(file => file, StringComparer.OrdinalIgnoreCase))
            Visit(file);

        var seeSet = seeServer.Select(file => projects[file]).ToHashSet();
        return new ProjectGraph(ordered, seeSet, projects[serverFile]);
    }

    private static IEnumerable<string> ProjectFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        var result = new List<string>();
        while (pending.Count > 0)
        {
            var folder = pending.Pop();
            result.AddRange(Directory.EnumerateFiles(folder, "*.csproj").Select(Path.GetFullPath));
            foreach (var child in Directory.EnumerateDirectories(folder))
            {
                var info = new DirectoryInfo(child);
                if (ProjectLoader.SkippedFolders.Contains(info.Name) || info.Name.StartsWith('.') || (info.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    (string.Equals(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) &&
                     SkippedRootFolders.Contains(info.Name)))
                    continue;
                pending.Push(child);
            }
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }
}
