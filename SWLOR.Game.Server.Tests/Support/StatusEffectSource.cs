using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of <c>StatusEffectService</c> wherever it lives, for the tests that guard a rule by reading the code. The status
/// effect split (<c>Readmes/MaintainabilityRefactorStatusEffectSplit.md</c>) moves the text of
/// <c>Domain/Character/StatusEffectService.cs</c> into part files; each pull request that moves a part adds its file to
/// <see cref="PartFiles"/> (and its interface to <see cref="InterfaceFiles"/> and <see cref="PartInterfaces"/>), and a test calls
/// <see cref="Read"/> (or <see cref="Method"/>, <see cref="Members"/>, <see cref="Nodes{TNode}"/>) and keeps its needles.
/// <para>
/// As in <see cref="StatSource"/>, the text is as it is apart from one rewrite: a call through a field of a part interface
/// (<c>_native.OnApplyNWNStatusEffect(x)</c>) loses its receiver, because the code called that member on its own instance before
/// the move. A call on any other injected service keeps its receiver, so a changed condition, a removed or reordered call, or
/// a different argument still fails a needle. The statics the service uses (<c>StatusEffectRules.cs</c>, the icon map, the Leadership rules) are part files, as their members were part of it.
/// </para>
/// </summary>
public static class StatusEffectSource
{
    /// <summary>
    /// The files that hold the code of <c>StatusEffectService.cs</c> as it was before the split, repository-relative, in reading
    /// order: the service itself first, then each part in the order the split moves it.
    /// </summary>
    public static readonly IReadOnlyList<string> PartFiles = new[]
    {
        "SWLOR.Game.Server/Domain/Character/StatusEffectService.cs",
        "SWLOR.Game.Server/Domain/Character/StatusEffectCatalogService.cs",
        "SWLOR.Game.Server/Domain/Character/StatusEffectApplicationService.cs",
        "SWLOR.Game.Server/Domain/Character/StatusEffectDurationService.cs",
        "SWLOR.Game.Server/Domain/Character/StatusEffectCombatEventService.cs",
        "SWLOR.Game.Server/Domain/Character/StatusEffectNativeService.cs",
        "SWLOR.Game.Server/Domain/Character/StatusEffectSessionService.cs",
        "SWLOR.Game.Server/Domain/Character/StatusEffectIconMap.cs",
        "SWLOR.Game.Server/Domain/Character/LeadershipDamageReductionRules.cs",
        "SWLOR.Game.Server/Domain/Character/StatusEffectRules.cs",
    };

    /// <summary>
    /// The interface files of the parts, repository-relative: <c>IStatusEffectService.cs</c> first, then each part's interface in
    /// the order the split moves it.
    /// </summary>
    public static readonly IReadOnlyList<string> InterfaceFiles = new[]
    {
        "SWLOR.Game.Server/Domain/Character/IStatusEffectService.cs",
        "SWLOR.Game.Server/Domain/Character/IStatusEffectCatalogService.cs",
        "SWLOR.Game.Server/Domain/Character/IStatusEffectApplicationService.cs",
        "SWLOR.Game.Server/Domain/Character/IStatusEffectDurationService.cs",
        "SWLOR.Game.Server/Domain/Character/IStatusEffectCombatEventService.cs",
        "SWLOR.Game.Server/Domain/Character/IStatusEffectNativeService.cs",
        "SWLOR.Game.Server/Domain/Character/IStatusEffectSessionService.cs",
    };

    /// <summary>
    /// The interfaces of the parts: a call through a field of one of these types was a call on the same instance before the move.
    /// </summary>
    public static readonly IReadOnlySet<string> PartInterfaces = new HashSet<string>(StringComparer.Ordinal)
    {
        "IStatusEffectService",
        "IStatusEffectCatalogService",
        "IStatusEffectApplicationService",
        "IStatusEffectDurationService",
        "IStatusEffectCombatEventService",
        "IStatusEffectNativeService",
        "IStatusEffectSessionService",
    };

    private static readonly Lazy<IReadOnlyList<StatSource.SourceFile>> Loaded = new(() => Load(FindRepositoryRoot()));

    private static readonly Lazy<IReadOnlyList<StatSource.SourceFile>> LoadedInterfaces = new(() =>
        InterfaceFiles
            .Select(relative => Path.GetFullPath(Path.Combine(FindRepositoryRoot(), relative)))
            .Select(path => new StatSource.SourceFile(path, File.ReadAllText(path)))
            .ToArray());

    /// <summary>The combined source of <c>StatusEffectService</c>: the part files, in reading order.</summary>
    public static string Read() => StatSource.Combine(Loaded.Value);

    /// <summary>The interfaces of the parts, in reading order, as they are.</summary>
    public static string ReadInterfaces() => StatSource.Combine(LoadedInterfaces.Value);

    /// <summary>The full paths of the files <see cref="Read"/> combines, in the same order.</summary>
    public static IReadOnlyList<string> Paths() => Loaded.Value.Select(file => file.FullPath).ToArray();

    /// <summary>One syntax tree per file, for tests that look at members instead of text.</summary>
    public static IReadOnlyList<SyntaxTree> Trees() => StatSource.Trees(Loaded.Value);

    /// <summary>Every node of a type across the files, for tests that find a method, a field or a call by name.</summary>
    public static IEnumerable<TNode> Nodes<TNode>()
        where TNode : SyntaxNode =>
        Trees().SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TNode>());

    /// <summary>The method of this name that holds the code: the declaration with the longest text, when a name has several overloads.</summary>
    public static MethodDeclarationSyntax Method(string name) =>
        Nodes<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.Text == name)
            .OrderByDescending(method => method.FullSpan.Length)
            .First();

    /// <summary>The text of every method with one of these names (all overloads), in the order the names are given.</summary>
    public static string Members(params string[] names) =>
        string.Join('\n', names.SelectMany(name => Nodes<MethodDeclarationSyntax>().Where(method => method.Identifier.Text == name)).Select(method => method.ToString()));

    /// <summary>The files of <c>StatusEffectService</c> under a repository root, normalised, in reading order.</summary>
    public static IReadOnlyList<StatSource.SourceFile> Load(string repositoryRoot) =>
        PartFiles
            .Select(relative => Path.GetFullPath(Path.Combine(repositoryRoot, relative)))
            .Select(path => new StatSource.SourceFile(path, StatSource.Normalise(File.ReadAllText(path), PartInterfaces)))
            .ToArray();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }
}
