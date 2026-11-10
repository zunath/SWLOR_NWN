using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.TypeMoves;

/// <summary>
/// One run over a repository: moves each listed file whose old path exists, rewrites every reference in the projects that
/// see the server (proving with a second compilation that no name binds to anything else, and qualifying the names that
/// would), rewrites the names and paths in other text, renames the moved handlers in the manifest and records them in
/// <c>HandlerRenames.txt</c>. Nothing is written until every step succeeded; a check writes nothing.
/// </summary>
internal static class TypeMoveRunner
{
    /// <summary>Success (and, for a check, nothing to change).</summary>
    public const int Ok = 0;

    /// <summary>A check found something <see cref="RunMode.Apply"/> would change.</summary>
    public const int Differs = 1;

    /// <summary>The input could not be processed; nothing was written.</summary>
    public const int Failed = 2;

    public const string ManifestPath = "SWLOR.Game.Server/Core/ScriptHandlerOrder.txt";

    public const string RenamesPath = "tools/Refactor/HandlerRenames.txt";

    /// <summary>Rounds of qualifying names whose binding would change (a second round writes <c>global::</c>).</summary>
    private const int Rounds = 3;

    public static int Run(RunOptions options, TextWriter log)
    {
        try
        {
            return Execute(options, log);
        }
        catch (TypeMoveException exception)
        {
            log.WriteLine("Nothing was written: " + exception.Message);
            return Failed;
        }
    }

    private static int Execute(RunOptions options, TextWriter log)
    {
        var clock = Stopwatch.StartNew();
        var root = Path.GetFullPath(options.Root);
        var data = TypeMoveData.Load(options.DataPath);

        // Which files still have to move, which moved already (a later run, or the sync after a master merge).
        var pending = new Dictionary<string, TypeMove>(StringComparer.OrdinalIgnoreCase);
        var done = new List<TypeMove>();
        foreach (var file in data.Files)
        {
            var move = file.First();
            var source = Full(root, move.SourcePath);
            var target = Full(root, move.TargetPath);
            var hasSource = File.Exists(source);
            var hasTarget = File.Exists(target);
            if (hasSource && hasTarget)
                throw new TypeMoveException($"{move.SourcePath} and {move.TargetPath} both exist: keep one by hand (a master change to the old file goes into the new one).");
            if (hasSource)
                pending[source] = move;
            else if (hasTarget)
                done.Add(move);
            else
                log.WriteLine($"Skipped {string.Join(", ", file.Select(entry => entry.Name))}: neither {move.SourcePath} nor {move.TargetPath} exists in this tree.");
        }

        var graph = ProjectGraph.Load(root);
        var warnings = new SortedSet<string>(StringComparer.Ordinal);
        var resolver = new ReferenceResolver();
        var sources = new Dictionary<string, SourceFile>(StringComparer.OrdinalIgnoreCase);
        var trees = new Dictionary<ProjectInfo, Dictionary<string, SyntaxTree>>();
        foreach (var project in graph.Ordered)
        {
            var map = new Dictionary<string, SyntaxTree>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in project.SourceFiles)
            {
                if (!sources.TryGetValue(path, out var source))
                    sources[path] = source = SourceFile.Read(path);
                map[path] = CSharpSyntaxTree.ParseText(source.Text, project.ParseOptions, path);
            }

            trees[project] = map;
        }

        foreach (var (path, move) in pending)
        {
            if (!graph.Server.SourceFiles.Contains(path, StringComparer.OrdinalIgnoreCase))
                throw new TypeMoveException($"{move.SourcePath} is not compiled by the server project.");
            CheckDeclarations(trees[graph.Server][path], data, move);
        }

        // A type that moved already has a stand-in in the old namespace, so code written for the old place (master's code
        // after a merge) still binds to it and is moved like the rest.
        var ghosts = done.Select(move => Ghost(sources.TryGetValue(Full(root, move.TargetPath), out var source) ? source.Text : SourceFile.Read(Full(root, move.TargetPath)).Text, move, graph.Server.ParseOptions)).ToList();
        var before = CompilationSet.Create(graph, project => project == graph.Server ? trees[project].Values.Concat(ghosts) : trees[project].Values, resolver, warnings);

        var server = before[graph.Server];
        foreach (var move in pending.Values.Distinct())
        {
            foreach (var entry in data.Moves.Where(entry => entry.File == move.File))
            {
                if (FindNamespace(server.GlobalNamespace, entry.To)?.GetTypeMembers(entry.Name).Length > 0)
                    throw new TypeMoveException($"{entry.To} already declares a type {entry.Name}.");
            }
        }

        var staying = FindNamespace(server.GlobalNamespace, data.From)?.GetTypeMembers().Where(type => !data.ByName.ContainsKey(type.Name)).ToList() ?? [];
        var stayingExtensions = staying
            .Where(type => type.MightContainExtensionMethods)
            .SelectMany(type => type.GetMembers().OfType<IMethodSymbol>().Where(method => method.IsExtensionMethod))
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);
        var context = new MoveContext(data, staying.Select(type => type.Name).ToHashSet(StringComparer.Ordinal), stayingExtensions);

        // Analysis: how each file reaches the moved types and the staying ones.
        var findings = new Dictionary<string, FileFindings>(StringComparer.OrdinalIgnoreCase);
        var home = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
        var keepsGlobal = new HashSet<ProjectInfo>();
        var globalImports = new Dictionary<ProjectInfo, HashSet<string>>();
        var mentions = WordRegex(context.InterestingNames);
        var fromUsing = new Regex(@"\busing\s+(global::)?" + Regex.Escape(data.From) + @"\s*;", RegexOptions.CultureInvariant);
        var work = new List<(ProjectInfo Project, string Path, SyntaxTree Tree, bool Moved)>();
        foreach (var project in graph.Ordered.Where(graph.SeeServer.Contains))
        {
            globalImports[project] = GlobalImports(project, trees[project].Values);
            var globalFrom = globalImports[project].Contains(data.From);
            foreach (var (path, tree) in trees[project])
            {
                var text = sources[path].Text;
                var moved = pending.ContainsKey(path);
                var importsFrom = fromUsing.IsMatch(text);
                if (!moved && !importsFrom && !(text.Contains(context.FromLeaf, StringComparison.Ordinal) || globalFrom))
                    continue;
                if (!moved && !importsFrom && !mentions.IsMatch(text))
                    continue;

                if (!home.ContainsKey(path) || IsBelow(path, project.Folder))
                    home[path] = project;
                if (!findings.ContainsKey(path))
                    findings[path] = new FileFindings();
                work.Add((project, path, tree, moved));
            }
        }

        log.WriteLine($"Read {graph.Ordered.Count} project(s), {sources.Count} file(s) in {clock.Elapsed.TotalSeconds:0.0} s; analyzing {work.Count}.");
        var models = new System.Collections.Concurrent.ConcurrentDictionary<(ProjectInfo, string), SemanticModel>();
        Parallel.ForEach(work, item =>
        {
            var model = before[item.Project].GetSemanticModel(item.Tree);
            models[(item.Project, item.Path)] = model;
            var local = new FileFindings();
            var usesGlobal = ReferenceAnalyzer.Analyze(model, item.Tree.GetRoot(), context, item.Moved, local);
            lock (findings[item.Path])
                findings[item.Path].Merge(local);
            if (usesGlobal)
            {
                lock (keepsGlobal)
                    keepsGlobal.Add(item.Project);
            }
        });
        log.WriteLine($"Analyzed in {clock.Elapsed.TotalSeconds:0.0} s.");

        // Edits, file by file.
        var edits = new Dictionary<string, List<TextEdit>>(StringComparer.OrdinalIgnoreCase);
        var added = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var usingsAdded = 0;
        var usingsRemoved = 0;
        foreach (var (path, found) in findings)
        {
            var project = home[path];
            var imports = new HashSet<string>(globalImports[project], StringComparer.Ordinal);
            if (!keepsGlobal.Contains(project))
                imports.Remove(data.From);

            var tree = trees[project][path];
            var addedHere = new HashSet<string>(StringComparer.Ordinal);
            var list = EditPlanner.Plan(sources[path].Text, (CompilationUnitSyntax)tree.GetRoot(), found, context,
                pending.TryGetValue(path, out var move) ? move.To : null, imports, !keepsGlobal.Contains(project), addedHere, ref usingsRemoved);
            usingsAdded += addedHere.Count;
            added[path] = addedHere;
            if (list.Count > 0)
                edits[path] = list;
        }

        // The check: rebuild with the edits and compare bindings; qualify names that would bind to something else.
        var qualified = 0;
        if (edits.Count > 0 || pending.Count > 0)
        {
            qualified = CheckBindings(root, graph, trees, sources, before, models, ghosts, pending, edits, added, globalImports, context, log);
            log.WriteLine($"Checked the bindings in {clock.Elapsed.TotalSeconds:0.0} s.");
        }

        // The new text of every C# file that changed, by its new repository path.
        var newTexts = new Dictionary<string, (string OldPath, SourceFile Source, string Text)>(StringComparer.Ordinal);
        foreach (var (path, list) in edits)
        {
            var newPath = pending.TryGetValue(path, out var move) ? move.TargetPath : Relative(root, path);
            newTexts[newPath] = (Relative(root, path), sources[path], TextEdit.Apply(sources[path].Text, list));
        }

        // The text rewrite: names and paths in comments, literals and every other text file.
        var rewriter = new TextRewriter(data);
        var textRewrites = 0;
        var output = new Dictionary<string, (string OldPath, SourceFile Source, string Text)>(StringComparer.Ordinal);
        var listing = RepositoryFiles.List(root);
        foreach (var relative in listing)
        {
            var full = Full(root, relative);
            var newPath = pending.TryGetValue(full, out var move) ? move.TargetPath : relative;
            SourceFile source;
            string text;
            if (newTexts.TryGetValue(newPath, out var changed))
            {
                source = changed.Source;
                text = changed.Text;
            }
            else
            {
                source = sources.TryGetValue(full, out var known) ? known : SourceFile.Read(full);
                text = source.Text;
            }

            // HandlerRenames.txt keeps master's names on the left; its lines are written below, never rewritten.
            var rewritten = data.IsExcluded(relative) || relative == RenamesPath ? text
                : relative.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? rewriter.RewriteCSharp(text, ref textRewrites)
                : rewriter.RewriteText(text, ref textRewrites);
            if (rewritten != source.Text || newPath != relative)
                output[newPath] = (relative, source, rewritten);
        }

        foreach (var (newPath, entry) in newTexts)
        {
            if (!output.ContainsKey(newPath) && (entry.Text != entry.Source.Text || newPath != entry.OldPath))
                output[newPath] = entry; // a compiled file the listing does not hold (outside the text scope)
        }

        // Handlers of moved types: their manifest lines were renamed by the text rewrite; record each rename.
        var handlerRenames = 0;
        if (output.TryGetValue(ManifestPath, out var manifest) && File.Exists(Full(root, RenamesPath)))
        {
            var pairs = HandlerRenames.Pairs(manifest.Source.Text, manifest.Text);
            if (pairs.Count > 0)
            {
                var renames = SourceFile.Read(Full(root, RenamesPath));
                var updated = HandlerRenames.Update(renames.Text, pairs);
                if (updated != renames.Text)
                    output[RenamesPath] = (RenamesPath, renames, updated);
                handlerRenames = pairs.Count;
            }
        }

        foreach (var warning in warnings)
            log.WriteLine("Warning: " + warning);

        var moves = output.Where(entry => entry.Key != entry.Value.OldPath).OrderBy(entry => entry.Key, StringComparer.Ordinal).ToList();
        var editsOnly = output.Where(entry => entry.Key == entry.Value.OldPath).OrderBy(entry => entry.Key, StringComparer.Ordinal).ToList();
        var verb = options.Mode == RunMode.Apply ? "Moved" : "Would move";
        log.WriteLine($"{verb} {moves.Count} file(s) ({pending.Values.Sum(move => data.Moves.Count(entry => entry.File == move.File))} type(s)); " +
                      $"{(options.Mode == RunMode.Apply ? "rewrote" : "would rewrite")} {editsOnly.Count} other file(s): " +
                      $"{findings.Values.Sum(found => found.QualifierEdits.Count)} qualified name(s), {usingsAdded} using line(s) added, {usingsRemoved} removed, " +
                      $"{qualified} name(s) qualified to keep their binding, {textRewrites} text rewrite(s), {handlerRenames} handler rename(s); {clock.Elapsed.TotalSeconds:0.0} s.");

        if (options.Mode == RunMode.Check)
        {
            foreach (var (newPath, entry) in moves)
                log.WriteLine($"  move  {entry.OldPath} -> {newPath}");
            foreach (var (newPath, _) in editsOnly)
                log.WriteLine($"  edit  {newPath}");
            if (output.Count == 0)
                log.WriteLine("Nothing to change.");
            return output.Count > 0 ? Differs : Ok;
        }

        var touched = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (newPath, entry) in output)
        {
            var target = Full(root, newPath);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, entry.Source.Encode(entry.Text));
            touched.Add(newPath);
            if (newPath != entry.OldPath)
            {
                File.Delete(Full(root, entry.OldPath));
                touched.Add(entry.OldPath);
                RemoveEmptyFolders(root, Path.GetDirectoryName(Full(root, entry.OldPath))!);
            }
        }

        RepositoryFiles.Stage(root, touched);
        return Ok;
    }

    /// <summary>
    /// Compiles the edited files, compares every name of interest with its binding before the edits, and writes a
    /// qualifier before each leading name that would change (on a second round with <c>global::</c>).
    /// </summary>
    /// <returns>The number of names qualified.</returns>
    private static int CheckBindings(
        string root,
        ProjectGraph graph,
        Dictionary<ProjectInfo, Dictionary<string, SyntaxTree>> trees,
        Dictionary<string, SourceFile> sources,
        CompilationSet before,
        IReadOnlyDictionary<(ProjectInfo, string), SemanticModel> models,
        IReadOnlyList<SyntaxTree> ghosts,
        IReadOnlyDictionary<string, TypeMove> pending,
        Dictionary<string, List<TextEdit>> edits,
        IReadOnlyDictionary<string, HashSet<string>> added,
        IReadOnlyDictionary<ProjectInfo, HashSet<string>> globalImports,
        MoveContext context,
        TextWriter log)
    {
        var targets = context.Data.Moves.Select(move => move.To).ToHashSet(StringComparer.Ordinal);
        var moved = WordRegex(context.MovedNames);
        var targetUsing = new Regex(@"\busing\s+(global::)?(" + string.Join("|", targets.Select(Regex.Escape)) + @")\s*;", RegexOptions.CultureInvariant);

        // The files to compare, with the names to compare in each (null: every name, for a moved file).
        var checks = new Dictionary<string, HashSet<string>?>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in graph.Ordered.Where(graph.SeeServer.Contains))
        {
            var projectTargets = globalImports[project].Overlaps(targets);
            foreach (var (path, tree) in trees[project])
            {
                if (checks.ContainsKey(path))
                    continue;
                if (pending.ContainsKey(path))
                {
                    checks[path] = null;
                    continue;
                }

                var text = sources[path].Text;
                if (edits.ContainsKey(path) || (moved.IsMatch(text) &&
                    (projectTargets || targetUsing.IsMatch(text) || SyntaxQueries.DeclaredNamespaces(tree.GetRoot()).Any(own => targets.Any(target => SyntaxQueries.IsWithin(own, target))))))
                    checks[path] = new HashSet<string>(context.MovedNames, StringComparer.Ordinal);
            }
        }

        var qualified = 0;
        var attempts = new Dictionary<(string, int), int>();
        var pendingChecks = checks.Keys.OrderBy(path => path, StringComparer.Ordinal).ToList();
        var after = Rebuild(root, graph, trees, sources, before, ghosts, pending, edits, edits.Keys.Concat(pending.Keys));
        for (var round = 0; round < Rounds && pendingChecks.Count > 0; round++)
        {
            var afterServer = after.Compilations[graph.Server];
            foreach (var path in pendingChecks)
            {
                var names = checks[path];
                if (names == null || !added.TryGetValue(path, out var imported))
                    continue;

                foreach (var @namespace in imported)
                {
                    foreach (var type in FindNamespace(afterServer.GlobalNamespace, @namespace)?.GetTypeMembers() ?? [])
                        names.Add(type.Name);
                }
            }

            var jobs = pendingChecks
                .SelectMany(path => graph.Ordered.Where(project => graph.SeeServer.Contains(project) && trees[project].ContainsKey(path)).Select(project => (Project: project, Path: path)))
                .ToList();
            var found = new System.Collections.Concurrent.ConcurrentBag<(string Path, ProjectInfo Project, BindingCheck.Change Change)>();
            var current = after;
            Parallel.ForEach(jobs, job =>
            {
                var treeBefore = trees[job.Project][job.Path];
                var treeAfter = current.Trees.TryGetValue((job.Project, job.Path), out var rebuilt) ? rebuilt : treeBefore;
                var modelBefore = models.TryGetValue((job.Project, job.Path), out var cached) ? cached : before[job.Project].GetSemanticModel(treeBefore);
                var list = edits.TryGetValue(job.Path, out var fileEdits) ? fileEdits : [];
                foreach (var change in BindingCheck.Compare(modelBefore, treeBefore.GetRoot(), current.Compilations[job.Project].GetSemanticModel(treeAfter), treeAfter.GetRoot(), list, checks[job.Path], context))
                    found.Add((job.Path, job.Project, change));
            });

            var fixedPaths = new SortedSet<string>(StringComparer.Ordinal);
            var failures = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var group in found.GroupBy(item => (item.Path, item.Change.Position)).OrderBy(group => group.Key.Path, StringComparer.Ordinal).ThenBy(group => group.Key.Position))
            {
                var (path, project, change) = group.First();
                var treeBefore = trees[project][path];
                var token = treeBefore.GetRoot().FindToken(change.Position);
                var line = treeBefore.GetLineSpan(token.Span).StartLinePosition.Line + 1;
                attempts.TryGetValue(group.Key, out var tries);
                var fileNamespace = pending.TryGetValue(path, out var move) ? move.To : SyntaxQueries.NamespaceOf(token.Parent!);
                var qualifier = change.Leading && tries < 2 ? BindingCheck.Qualifier(change.Meant, context, fileNamespace, global: tries > 0) : null;
                if (qualifier == null)
                {
                    failures.Add($"{Relative(root, path)} ({project.AssemblyName}) line {line}: {change.Name} meant {change.Before} and would mean {change.After}.");
                    continue;
                }

                attempts[group.Key] = tries + 1;
                if (!edits.TryGetValue(path, out var list))
                    edits[path] = list = new List<TextEdit>();
                list.RemoveAll(edit => edit.Start == change.Position && edit.Length == 0);
                list.Add(new TextEdit(change.Position, 0, qualifier));
                if (tries == 0)
                    qualified++;
                log.WriteLine($"Qualified {Relative(root, path)} line {line}: {qualifier}{change.Name} (it meant {change.Before}; unqualified it would mean {change.After}).");
                fixedPaths.Add(path);
            }

            if (failures.Count > 0)
                throw new TypeMoveException("These names would bind to something else after the move and cannot be qualified:" + Environment.NewLine + "  " + string.Join(Environment.NewLine + "  ", failures));

            pendingChecks = fixedPaths.ToList();
            if (pendingChecks.Count > 0)
                after = Rebuild(root, graph, trees, sources, before, ghosts, pending, edits, pendingChecks, after);
        }

        if (pendingChecks.Count > 0)
            throw new TypeMoveException($"Qualifying names did not settle their bindings in {Rounds} rounds: {string.Join(", ", pendingChecks.Select(path => Relative(root, path)))}.");

        return qualified;
    }

    /// <summary>The compilations and trees after the edits.</summary>
    private sealed record AfterState(CompilationSet Compilations, Dictionary<(ProjectInfo, string), SyntaxTree> Trees);

    private static AfterState Rebuild(
        string root,
        ProjectGraph graph,
        Dictionary<ProjectInfo, Dictionary<string, SyntaxTree>> trees,
        Dictionary<string, SourceFile> sources,
        CompilationSet before,
        IReadOnlyList<SyntaxTree> ghosts,
        IReadOnlyDictionary<string, TypeMove> pending,
        IReadOnlyDictionary<string, List<TextEdit>> edits,
        IEnumerable<string> paths,
        AfterState? previous = null)
    {
        var changed = paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newTrees = previous == null ? new Dictionary<(ProjectInfo, string), SyntaxTree>() : new Dictionary<(ProjectInfo, string), SyntaxTree>(previous.Trees);
        var changes = new Dictionary<ProjectInfo, List<(SyntaxTree?, SyntaxTree?)>>();
        foreach (var project in graph.Ordered)
        {
            var list = new List<(SyntaxTree?, SyntaxTree?)>();
            if (previous == null && project == graph.Server)
                list.AddRange(ghosts.Select(ghost => ((SyntaxTree?)ghost, (SyntaxTree?)null)));

            foreach (var path in changed)
            {
                if (!trees[project].TryGetValue(path, out var original))
                    continue;

                var text = TextEdit.Apply(sources[path].Text, edits.TryGetValue(path, out var list2) ? list2 : []);
                var newPath = pending.TryGetValue(path, out var move) ? Full(root, move.TargetPath) : path;
                var tree = CSharpSyntaxTree.ParseText(text, project.ParseOptions, newPath);
                var old = newTrees.TryGetValue((project, path), out var current) ? current : original;
                newTrees[(project, path)] = tree;
                list.Add((old, tree));
            }

            if (list.Count > 0)
                changes[project] = list;
        }

        var start = previous?.Compilations ?? before;
        return new AfterState(start.Replace(changes), newTrees);
    }

    /// <summary>
    /// A stand-in for a file that already moved: its text with the namespace set back to the old one (and a using of the new
    /// one, so its own names still bind), compiled beside the real file only for the analysis.
    /// </summary>
    private static SyntaxTree Ghost(string text, TypeMove move, CSharpParseOptions options)
    {
        var root = CSharpSyntaxTree.ParseText(text, options).GetRoot();
        var edits = root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Where(declaration => SyntaxQueries.FullName(declaration) == move.To)
            .Select(declaration => new TextEdit(declaration.Name.SpanStart, declaration.Name.Span.Length, move.From))
            .Append(new TextEdit(0, 0, "using " + move.To + ";\n"))
            .ToList();
        return CSharpSyntaxTree.ParseText(TextEdit.Apply(text, edits), options, Path.Combine("TypeMoves.Ghost", move.File));
    }

    /// <summary>The file declares exactly the types the data lists for it, directly in the old namespace.</summary>
    private static void CheckDeclarations(SyntaxTree tree, TypeMoveData data, TypeMove move)
    {
        var root = tree.GetRoot();
        var declared = root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<BaseNamespaceDeclarationSyntax>()
            .ToList();
        if (declared.Count != 1 || SyntaxQueries.FullName(declared[0]) != data.From)
            throw new TypeMoveException($"{move.SourcePath} must declare exactly one namespace, {data.From}.");

        var types = declared[0].Members.OfType<BaseTypeDeclarationSyntax>().Select(type => type.Identifier.ValueText)
            .Concat(declared[0].Members.OfType<DelegateDeclarationSyntax>().Select(@delegate => @delegate.Identifier.ValueText))
            .ToHashSet(StringComparer.Ordinal);
        var listed = data.Moves.Where(entry => entry.File == move.File).Select(entry => entry.Name).ToHashSet(StringComparer.Ordinal);
        if (!types.SetEquals(listed))
            throw new TypeMoveException($"{move.SourcePath} declares {string.Join(", ", types.OrderBy(name => name, StringComparer.Ordinal))}, but the data lists {string.Join(", ", listed.OrderBy(name => name, StringComparer.Ordinal))} for it.");
    }

    /// <summary>The namespaces a project imports with <c>global using</c> (its files and what MSBuild generates).</summary>
    private static HashSet<string> GlobalImports(ProjectInfo project, IEnumerable<SyntaxTree> trees)
    {
        var generated = CSharpSyntaxTree.ParseText(project.GeneratedSource, project.ParseOptions);
        return trees.Append(generated)
            .SelectMany(tree => ((CompilationUnitSyntax)tree.GetRoot()).Usings)
            .Where(directive => SyntaxQueries.IsGlobal(directive) && SyntaxQueries.IsPlain(directive))
            .Select(directive => SyntaxQueries.Dotted(directive.Name!))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static INamespaceSymbol? FindNamespace(INamespaceSymbol global, string name)
    {
        var current = global;
        foreach (var segment in name.Split('.'))
        {
            current = current.GetNamespaceMembers().FirstOrDefault(member => member.Name == segment);
            if (current == null)
                return null;
        }

        return current;
    }

    private static Regex WordRegex(IEnumerable<string> words) =>
        new(@"(?<![A-Za-z0-9_])(" + string.Join("|", words.OrderByDescending(word => word.Length).Select(Regex.Escape)) + @")(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);

    private static void RemoveEmptyFolders(string root, string folder)
    {
        var stop = Full(root, TypeMove.ServerRoot);
        var current = folder;
        while (current.Length > stop.Length && Directory.Exists(current) && !Directory.EnumerateFileSystemEntries(current).Any())
        {
            Directory.Delete(current);
            current = Path.GetDirectoryName(current)!;
        }
    }

    private static bool IsBelow(string path, string folder) =>
        path.StartsWith(folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static string Full(string root, string relative) => Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
