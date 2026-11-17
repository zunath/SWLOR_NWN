using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The source of <c>StatService</c> wherever it lives, for the tests that guard a rule by reading the code. The stat split
/// (<c>Readmes/MaintainabilityRefactorStatSplit.md</c>) moves the text of <c>Domain/Character/StatService.cs</c> into part
/// files in Character and Combat; each pull request that moves a part adds its files to <see cref="PartFiles"/>
/// (and its interface to <see cref="InterfaceFiles"/> and <see cref="PartInterfaces"/>), and a test calls <see cref="Read"/>
/// (or <see cref="Method"/>, <see cref="Members"/>, <see cref="Nodes{TNode}"/>) and keeps its needles.
/// <para>
/// The text is combined as it is, apart from one rewrite: a call on an injected part (<c>_pool.GetMaxFP(x)</c>, through a
/// field declared in the same file with one of the <see cref="PartInterfaces"/>) loses its receiver (<c>GetMaxFP(x)</c>),
/// because the code called that member on its own instance before the move. A call on any other injected service keeps its
/// receiver. Nothing else is rewritten, so a changed condition, a removed or reordered call, or a different argument still
/// fails a needle. <see cref="Normalise(string, IReadOnlySet{string})"/> is public so the tests of this helper can show that.
/// </para>
/// </summary>
public static class StatSource
{
    /// <summary>
    /// The files that hold the code of <c>StatService.cs</c> as it was before the split, repository-relative, in reading
    /// order: the service itself first, then each part in the order the split moves it.
    /// </summary>
    public static readonly IReadOnlyList<string> PartFiles = new[]
    {
        "SWLOR.Game.Server/Domain/Character/StatService.cs",
        "SWLOR.Game.Server/Domain/Combat/CombatRatingService.cs",
        "SWLOR.Game.Server/Domain/Combat/CombatDeflectionReactionService.cs",
        // Part 7, the healing adjustments, joined the rest of the resources service (P5-4d).
        "SWLOR.Game.Server/Domain/Combat/CombatResourcesService.cs",
    };

    /// <summary>
    /// The interface files of the parts, repository-relative: <c>IStatService.cs</c> first, then each part's interface in
    /// the order the split moves it.
    /// </summary>
    public static readonly IReadOnlyList<string> InterfaceFiles = new[]
    {
        "SWLOR.Game.Server/Domain/Character/IStatService.cs",
        "SWLOR.Game.Server/Domain/Combat/ICombatRatingService.cs",
        "SWLOR.Game.Server/Domain/Combat/ICombatDeflectionReactionService.cs",
        "SWLOR.Game.Server/Domain/Combat/ICombatResourcesService.cs",
    };

    /// <summary>
    /// The interfaces of the parts: a call through a field of one of these types was a call on the same instance before
    /// the move.
    /// </summary>
    public static readonly IReadOnlySet<string> PartInterfaces = new HashSet<string>(StringComparer.Ordinal)
    {
        "IStatService",
        "ICombatRatingService",
        "ICombatDeflectionReactionService",
        "ICombatResourcesService"
    };

    private static readonly Lazy<IReadOnlyList<SourceFile>> Loaded = new(() => Load(FindRepositoryRoot()));

    private static readonly Lazy<IReadOnlyList<SourceFile>> LoadedInterfaces = new(() => LoadAsIs(FindRepositoryRoot(), InterfaceFiles));

    private static readonly Regex InjectedField = new(
        @"^\s*(?:private|protected|internal|public)\s+(?:readonly\s+)?(?<type>I\w+)\s+(?<name>_\w+)\s*(?:;|=)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>One file of the combined source: its full path and its (normalised) text.</summary>
    public sealed record SourceFile(string FullPath, string Text);

    /// <summary>The combined source of <c>StatService</c>: the part files, in reading order.</summary>
    public static string Read() => Combine(Loaded.Value);

    /// <summary>The interfaces of the parts, in reading order, as they are (an interface has no call to rewrite).</summary>
    public static string ReadInterfaces() => Combine(LoadedInterfaces.Value);

    /// <summary>The full paths of the files <see cref="Read"/> combines, in the same order.</summary>
    public static IReadOnlyList<string> Paths() => Loaded.Value.Select(file => file.FullPath).ToArray();

    /// <summary>One syntax tree per file, for tests that look at members instead of text.</summary>
    public static IReadOnlyList<SyntaxTree> Trees() => Trees(Loaded.Value);

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

    /// <summary>
    /// The text of the method of this name that <paramref name="typeName"/> declares (the longest, when it has overloads), for a
    /// name two parts declare.
    /// </summary>
    public static string Member(string typeName, string methodName) =>
        Nodes<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.Text == methodName &&
                method.Parent is TypeDeclarationSyntax type && type.Identifier.Text == typeName)
            .OrderByDescending(method => method.FullSpan.Length)
            .First()
            .ToString();

    /// <summary>The text of every method with one of these names (all overloads), in the order the names are given.</summary>
    public static string Members(params string[] names) =>
        string.Join('\n', names.SelectMany(name => Nodes<MethodDeclarationSyntax>().Where(method => method.Identifier.Text == name)).Select(method => method.ToString()));

    /// <summary>Whether a path is one of the files <see cref="Read"/> combines.</summary>
    public static bool IsStatFile(string path) =>
        Paths().Contains(Path.GetFullPath(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>The files of <c>StatService</c> under a repository root, normalised, in reading order.</summary>
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

        // Only the receiver of a call on the field is removed (_pool.GetMaxFP(x), this._pool.GetMaxFP(x), _pool.Get<T>(x)); a
        // property read (_pool.Count), a method group, text inside a string or comment, and a call through a parameter, local,
        // lambda parameter or pattern variable that shadows the field keep it. The syntax tree finds the calls.
        var receivers = CSharpSyntaxTree.ParseText(text).GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => invocation.Expression)
            .OfType<MemberAccessExpressionSyntax>()
            .Where(access => IsInjectedFieldReceiver(access.Expression, injected))
            .Select(access => (Start: access.Expression.SpanStart, End: access.OperatorToken.Span.End))
            .OrderByDescending(span => span.Start)
            .ToArray();

        var builder = new StringBuilder(text);
        foreach (var (start, end) in receivers)
            builder.Remove(start, end - start);

        return builder.ToString();
    }

    /// <summary>
    /// Whether a receiver is one of the injected fields: <c>this._pool</c> always is; a bare <c>_pool</c> is when no
    /// declaration of that name is in scope between it and its type (a parameter of an enclosing method, local function or
    /// lambda, a local, a pattern or <c>out</c> variable, a loop, <c>using</c>, <c>catch</c> or query variable). The check reads
    /// syntax only and errs towards a declaration being in scope, which keeps the receiver, so a needle fails rather than
    /// matching a different call.
    /// </summary>
    private static bool IsInjectedFieldReceiver(ExpressionSyntax receiver, IReadOnlySet<string> injected)
    {
        if (receiver is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax field })
            return injected.Contains(field.Identifier.ValueText);

        if (receiver is not IdentifierNameSyntax name || !injected.Contains(name.Identifier.ValueText))
            return false;

        var text = name.Identifier.ValueText;
        for (var scope = name.Parent; scope != null && scope is not BaseTypeDeclarationSyntax; scope = scope.Parent)
        {
            // A nested scope that does not hold the receiver cannot shadow it; the scopes that do hold it are visited as
            // ancestors in turn.
            var scopeNode = scope;
            var declares = scopeNode
                .DescendantNodesAndSelf(node => node == scopeNode || !IsScopeBoundary(node))
                .Any(node => DeclaredName(node) == text);
            if (declares)
                return false;
        }

        return true;
    }

    private static bool IsScopeBoundary(SyntaxNode node) => node is BlockSyntax
        or AnonymousFunctionExpressionSyntax
        or LocalFunctionStatementSyntax
        or BaseMethodDeclarationSyntax
        or AccessorDeclarationSyntax
        or ForStatementSyntax
        or CommonForEachStatementSyntax
        or UsingStatementSyntax
        or FixedStatementSyntax
        or CatchClauseSyntax
        or WhileStatementSyntax
        or DoStatementSyntax
        or SwitchStatementSyntax
        or SwitchExpressionArmSyntax
        or QueryExpressionSyntax;

    private static string? DeclaredName(SyntaxNode node) => node switch
    {
        ParameterSyntax parameter => parameter.Identifier.ValueText,
        VariableDeclaratorSyntax variable when variable.Parent?.Parent is not FieldDeclarationSyntax => variable.Identifier.ValueText,
        SingleVariableDesignationSyntax designation => designation.Identifier.ValueText,
        ForEachStatementSyntax loop => loop.Identifier.ValueText,
        CatchDeclarationSyntax declaration => declaration.Identifier.ValueText,
        FromClauseSyntax from => from.Identifier.ValueText,
        LetClauseSyntax let => let.Identifier.ValueText,
        JoinClauseSyntax join => join.Identifier.ValueText,
        JoinIntoClauseSyntax into => into.Identifier.ValueText,
        QueryContinuationSyntax continuation => continuation.Identifier.ValueText,
        _ => null
    };

    private static IReadOnlyList<SourceFile> LoadAsIs(string repositoryRoot, IEnumerable<string> relativePaths) =>
        relativePaths
            .Select(relative => Path.GetFullPath(Path.Combine(repositoryRoot, relative)))
            .Select(path => new SourceFile(path, File.ReadAllText(path)))
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
