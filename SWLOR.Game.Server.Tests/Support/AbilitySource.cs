using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of <c>AbilityService</c> wherever it lives, for the tests that guard a rule by reading the code. The ability
/// split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) moves the text of <c>Domain/Abilities/AbilityService.cs</c>
/// into part files in Abilities, Combat and Character; each pull request that moves a part adds its files to
/// <see cref="PartFiles"/>, and a test calls <see cref="Read"/> (or <see cref="Method"/>, <see cref="Nodes{TNode}"/>) and keeps
/// its needles.
/// <para>
/// The text is combined as it is, apart from one rewrite: a call on an injected part (<c>_impactState.GetFoo(x)</c>, through a
/// field declared in the same file with one of the <see cref="PartInterfaces"/>) loses its receiver (<c>GetFoo(x)</c>),
/// because the code called that member on its own instance before the move. A call on any other injected service keeps its
/// receiver. Nothing else is rewritten, so a changed condition, a removed or reordered call, or a different argument still
/// fails a needle. <see cref="Normalise"/> is public so the tests of this helper can show that.
/// </para>
/// </summary>
public static class AbilitySource
{
    /// <summary>
    /// The files that hold the code of <c>AbilityService.cs</c> as it was before the split, repository-relative, in reading
    /// order: the service itself first, then each part in the order the split moved it.
    /// </summary>
    public static readonly IReadOnlyList<string> PartFiles = new[]
    {
        "SWLOR.Game.Server/Domain/Abilities/AbilityService.cs",
        // P5-3b: the catalog, the impact state store and its tracker, the geometry and the magnitude.
        "SWLOR.Game.Server/Domain/Abilities/AbilityCatalogService.cs",
        "SWLOR.Game.Server/Domain/Combat/AbilityImpactStore.cs",
        "SWLOR.Game.Server/Domain/Combat/AbilityImpactTracker.cs",
        "SWLOR.Game.Server/Domain/Combat/PendingDamageEffect.cs",
        "SWLOR.Game.Server/Domain/Combat/CombatImpactGeometryService.cs",
        "SWLOR.Game.Server/Domain/Combat/CombatAbilityMagnitudeService.cs",
    };

    /// <summary>
    /// The interfaces of the parts: a call through a field of one of these types was a call on the same instance before
    /// the move.
    /// </summary>
    public static readonly IReadOnlySet<string> PartInterfaces = new HashSet<string>(StringComparer.Ordinal)
    {
        "IAbilityCatalog", "IAbilityCatalogService", "IAbilityImpactState", "ICombatImpactGeometryService", "ICombatAbilityMagnitudeService"
    };

    private static readonly Lazy<IReadOnlyList<SourceFile>> Loaded = new(() => Load(FindRepositoryRoot()));

    private static readonly Regex InjectedField = new(
        @"^\s*(?:private|protected|internal|public)\s+(?:readonly\s+)?(?<type>I\w+)\s+(?<name>_\w+)\s*(?:;|=)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>One file of the combined source: its full path and its (normalised) text.</summary>
    public sealed record SourceFile(string FullPath, string Text);

    /// <summary>The combined source of <c>AbilityService</c>: the part files, in reading order.</summary>
    public static string Read() => Combine(Loaded.Value);

    /// <summary>The full paths of the files <see cref="Read"/> combines, in the same order.</summary>
    public static IReadOnlyList<string> Paths() => Loaded.Value.Select(file => file.FullPath).ToArray();

    /// <summary>One syntax tree per file, for tests that look at members instead of text.</summary>
    public static IReadOnlyList<SyntaxTree> Trees() => Trees(Loaded.Value);

    /// <summary>Every node of a type across the files, for tests that find a method or a call by name.</summary>
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

    /// <summary>Whether a path is one of the files <see cref="Read"/> combines.</summary>
    public static bool IsAbilityFile(string path) =>
        Paths().Contains(Path.GetFullPath(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>The files of <c>AbilityService</c> under a repository root, normalised, in reading order.</summary>
    public static IReadOnlyList<SourceFile> Load(string repositoryRoot) =>
        PartFiles
            .Select(relative => Path.GetFullPath(Path.Combine(repositoryRoot, relative)))
            .Select(path => new SourceFile(path, Normalise(File.ReadAllText(path))))
            .ToArray();

    /// <summary>Joins files in order; each file ends with a line break so a needle never spans two files.</summary>
    public static string Combine(IEnumerable<SourceFile> files)
    {
        var builder = new StringBuilder();
        foreach (var file in files)
        {
            builder.Append(file.Text);
            if (!file.Text.EndsWith('\n'))
                builder.Append("\r\n");
        }

        return builder.ToString();
    }

    /// <summary>One syntax tree per file.</summary>
    public static IReadOnlyList<SyntaxTree> Trees(IEnumerable<SourceFile> files) =>
        files.Select(file => CSharpSyntaxTree.ParseText(file.Text, path: file.FullPath)).ToArray();

    /// <summary>The text with no receiver on a call through a field that holds one of <see cref="PartInterfaces"/>.</summary>
    public static string Normalise(string text) => Normalise(text, PartInterfaces);

    /// <summary>
    /// The text with no receiver on a call through a field declared in it with one of <paramref name="partInterfaces"/>.
    /// Anything else stays as it is.
    /// </summary>
    public static string Normalise(string text, IReadOnlySet<string> partInterfaces)
    {
        var injected = InjectedField.Matches(text)
            .Where(match => partInterfaces.Contains(match.Groups["type"].Value))
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);
        if (injected.Count == 0)
            return text;

        // Only the receiver of a call is removed (_fooState.GetFoo(x) and _fooState.GetFoo<T>(x)); a property read
        // (_fooState.Count), a method group and text inside a string or comment keep it. The syntax tree finds the calls.
        var receivers = CSharpSyntaxTree.ParseText(text).GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => invocation.Expression)
            .OfType<MemberAccessExpressionSyntax>()
            .Where(access => access.Expression is IdentifierNameSyntax receiver && injected.Contains(receiver.Identifier.ValueText))
            .Select(access => (Start: access.Expression.SpanStart, End: access.OperatorToken.Span.End))
            .OrderByDescending(span => span.Start)
            .ToArray();

        var builder = new StringBuilder(text);
        foreach (var (start, end) in receivers)
            builder.Remove(start, end - start);

        return builder.ToString();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }
}
