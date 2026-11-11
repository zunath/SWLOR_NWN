using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SWLOR.TypeMoves;

/// <summary>
/// One in-memory compilation per project of a <see cref="ProjectGraph"/>, each referencing the compilations of the projects it
/// references, so a name in a test binds to the server's source as the real build binds it to the server's assembly.
/// Immutable: <see cref="Replace"/> returns a new set that shares everything that did not change.
/// </summary>
internal sealed class CompilationSet
{
    private readonly ProjectGraph _graph;
    private readonly Dictionary<ProjectInfo, CSharpCompilation> _compilations;
    private readonly Dictionary<ProjectInfo, MetadataReference> _asReference;

    private CompilationSet(ProjectGraph graph, Dictionary<ProjectInfo, CSharpCompilation> compilations, Dictionary<ProjectInfo, MetadataReference> asReference)
    {
        _graph = graph;
        _compilations = compilations;
        _asReference = asReference;
    }

    public CSharpCompilation this[ProjectInfo project] => _compilations[project];

    public static CompilationSet Create(ProjectGraph graph, Func<ProjectInfo, IEnumerable<SyntaxTree>> trees, ReferenceResolver resolver, ICollection<string> warnings)
    {
        var compilations = new Dictionary<ProjectInfo, CSharpCompilation>();
        var asReference = new Dictionary<ProjectInfo, MetadataReference>();
        var byFile = graph.Ordered.ToDictionary(project => project.ProjectFile, StringComparer.OrdinalIgnoreCase);
        foreach (var project in graph.Ordered)
        {
            var references = new List<MetadataReference>(resolver.For(project, warnings));
            foreach (var other in Referenced(project, byFile))
                references.Add(asReference[other]);

            var generated = CSharpSyntaxTree.ParseText(project.GeneratedSource, project.ParseOptions, Path.Combine(project.Folder, "obj", "TypeMoves.Generated.cs"));
            var options = new CSharpCompilationOptions(
                project.OutputKind,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Enable,
                concurrentBuild: true);
            var compilation = CSharpCompilation.Create(project.AssemblyName, trees(project).Append(generated), references, options);
            compilations[project] = compilation;
            asReference[project] = compilation.ToMetadataReference();
        }

        return new CompilationSet(graph, compilations, asReference);
    }

    /// <summary>
    /// A set with the given trees replaced, removed (new tree null) or added (old tree null), and every project that
    /// references a changed one pointed at its new compilation.
    /// </summary>
    public CompilationSet Replace(IReadOnlyDictionary<ProjectInfo, List<(SyntaxTree? Old, SyntaxTree? New)>> changes)
    {
        var compilations = new Dictionary<ProjectInfo, CSharpCompilation>();
        var asReference = new Dictionary<ProjectInfo, MetadataReference>();
        var byFile = _graph.Ordered.ToDictionary(project => project.ProjectFile, StringComparer.OrdinalIgnoreCase);
        foreach (var project in _graph.Ordered)
        {
            var compilation = _compilations[project];
            if (changes.TryGetValue(project, out var list))
            {
                foreach (var (old, @new) in list)
                {
                    if (old != null && @new != null)
                        compilation = compilation.ReplaceSyntaxTree(old, @new);
                    else if (old != null)
                        compilation = compilation.RemoveSyntaxTrees(old);
                    else if (@new != null)
                        compilation = compilation.AddSyntaxTrees(@new);
                }
            }

            foreach (var other in Referenced(project, byFile))
            {
                if (!ReferenceEquals(asReference[other], _asReference[other]))
                    compilation = compilation.ReplaceReference(_asReference[other], asReference[other]);
            }

            compilations[project] = compilation;
            asReference[project] = ReferenceEquals(compilation, _compilations[project]) ? _asReference[project] : compilation.ToMetadataReference();
        }

        return new CompilationSet(_graph, compilations, asReference);
    }

    /// <summary>
    /// The compiled projects a project references, directly or through another project (an SDK project reference flows on,
    /// so a test project of the simulator sees the server), each once.
    /// </summary>
    private static List<ProjectInfo> Referenced(ProjectInfo project, IReadOnlyDictionary<string, ProjectInfo> byFile)
    {
        var result = new List<ProjectInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>(project.ProjectReferences);
        while (pending.Count > 0)
        {
            var file = pending.Pop();
            if (!seen.Add(file) || !byFile.TryGetValue(file, out var other))
                continue;

            result.Add(other);
            foreach (var next in other.ProjectReferences)
                pending.Push(next);
        }

        return result;
    }
}
