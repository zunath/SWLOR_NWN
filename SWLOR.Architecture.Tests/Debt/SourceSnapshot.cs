using System.Collections.Concurrent;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// The C# source of every project of the solution, parsed once, and the compilations the semantic measures bind names
/// in. The server compiles from its source; every other project compiles from its own source against the server assembly
/// built next to these tests, so a name that reaches the server binds exactly as the compiler binds it.
/// </summary>
/// <remarks>
/// References come from the test process (the framework and the packages the tests and the server use). A type of a
/// package only another project uses does not resolve, which leaves a name of that project unbound only when it could also
/// mean something of that package; the lookup measure binds names of <c>GameServices</c> members, which no package declares.
/// </remarks>
internal sealed class SourceSnapshot
{
    public const string ServerProjectName = "SWLOR.Game.Server";

    private const string ServerAssemblyFileName = "SWLOR.Game.Server.dll";
    private const string TestAssemblyFileName = "SWLOR.Architecture.Tests.dll";

    private static readonly Regex LangVersion = new(@"<LangVersion>\s*(?<version>[^<\s]+)\s*</LangVersion>", RegexOptions.Compiled);

    // Shared by every snapshot of the process: the rule tests build many small ones.
    private static readonly Lazy<IReadOnlyList<MetadataReference>> PackageReferenceList = new(() => PackageReferences(includeServer: false));
    private static readonly Lazy<IReadOnlyList<MetadataReference>> ReferencesWithServerList = new(() => PackageReferences(includeServer: true));

    private readonly ConcurrentDictionary<string, CSharpCompilation> _compilations = new(StringComparer.Ordinal);

    private SourceSnapshot(string repositoryRoot, IReadOnlyList<ParsedProject> projects)
    {
        RepositoryRoot = repositoryRoot;
        Projects = projects;
        Server = projects.Single(project => project.Name == ServerProjectName);
    }

    public string RepositoryRoot { get; }

    /// <summary>Every C# project of the solution, in solution order.</summary>
    public IReadOnlyList<ParsedProject> Projects { get; }

    /// <summary>The game server, the project every size and dependency measure reads.</summary>
    public ParsedProject Server { get; }

    /// <summary>Reads and parses every project of the solution (in parallel).</summary>
    public static SourceSnapshot Load(string repositoryRoot, string solutionFileName)
    {
        var projects = SolutionReader.Read(repositoryRoot, solutionFileName);
        var parsed = projects
            .AsParallel()
            .AsOrdered()
            .Select(project => Parse(repositoryRoot, project))
            .ToList();
        return new SourceSnapshot(repositoryRoot, parsed);
    }

    /// <summary>Builds a snapshot from source text, for the rule tests: one project per entry, file path to text.</summary>
    public static SourceSnapshot FromText(IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> projects)
    {
        var parsed = projects
            .Select(project => new ParsedProject(
                new SolutionProject(project.Key, project.Key, project.Value.Keys.OrderBy(path => path, StringComparer.Ordinal).ToList()),
                project.Value
                    .OrderBy(file => file.Key, StringComparer.Ordinal)
                    .Select(file => CSharpSyntaxTree.ParseText(file.Value, CSharpParseOptions.Default, file.Key))
                    .ToList()))
            .ToList();
        return new SourceSnapshot(string.Empty, parsed);
    }

    /// <summary>
    /// The compilation names of <paramref name="project"/> bind in: the server from its source; any other project from its
    /// source plus the server built from source in the same snapshot (rule tests) or the built server assembly (the real
    /// solution).
    /// </summary>
    public CSharpCompilation CompilationOf(ParsedProject project) =>
        _compilations.GetOrAdd(project.Name, _ => Compile(project));

    private CSharpCompilation Compile(ParsedProject project)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)
            .WithMetadataImportOptions(MetadataImportOptions.All)
            .WithConcurrentBuild(true);

        if (project == Server)
            return CSharpCompilation.Create(project.Name, project.Trees, PackageReferenceList.Value, options);

        if (RepositoryRoot.Length == 0)
        {
            var server = CompilationOf(Server);
            return CSharpCompilation.Create(project.Name, project.Trees, PackageReferenceList.Value.Append(server.ToMetadataReference()), options);
        }

        return CSharpCompilation.Create(project.Name, project.Trees, ReferencesWithServerList.Value, options);
    }

    private static ParsedProject Parse(string repositoryRoot, SolutionProject project)
    {
        var options = new CSharpParseOptions(LanguageVersionOf(project), preprocessorSymbols: ["TRACE"]);
        var trees = new SyntaxTree[project.Files.Count];
        Parallel.For(0, project.Files.Count, index =>
        {
            var path = project.Files[index];
            using var stream = File.OpenRead(Path.Combine(repositoryRoot, path));
            trees[index] = CSharpSyntaxTree.ParseText(SourceText.From(stream), options, path);
        });

        return new ParsedProject(project, trees);
    }

    private static LanguageVersion LanguageVersionOf(SolutionProject project)
    {
        var projectFile = Path.Combine(project.Directory, project.Name + ".csproj");
        if (!File.Exists(projectFile))
            return LanguageVersion.Default;

        var match = LangVersion.Match(File.ReadAllText(projectFile));
        return match.Success && LanguageVersionFacts.TryParse(match.Groups["version"].Value, out var version)
            ? version
            : LanguageVersion.Default;
    }

    /// <summary>
    /// The framework and package assemblies of the test process, one per file name. The built server assembly is
    /// included only for projects that reference it; the tests' own assembly never is.
    /// </summary>
    private static IReadOnlyList<MetadataReference> PackageReferences(bool includeServer)
    {
        var trusted = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        var local = System.IO.Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll");

        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in trusted.Concat(local))
        {
            var name = Path.GetFileName(path);
            if (name.Equals(TestAssemblyFileName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!includeServer && name.Equals(ServerAssemblyFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            byName.TryAdd(name, path);
        }

        return byName.Values
            .Where(HasMetadata)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }

    private static bool HasMetadata(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new PEReader(stream);
            return reader.HasMetadata;
        }
        catch (BadImageFormatException)
        {
            return false;
        }
    }
}
