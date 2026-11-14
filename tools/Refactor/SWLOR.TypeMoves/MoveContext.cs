using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace SWLOR.TypeMoves;

/// <summary>
/// What the analysis of one tree needs to know about the move: which types move where, which other types stay in the
/// namespaces they leave (a file that still uses one of those keeps importing that namespace), and which names to bind.
/// </summary>
internal sealed class MoveContext
{
    private const string AttributeSuffix = "Attribute";

    private readonly Regex? _oldNames;

    /// <param name="data">The moves this tree holds.</param>
    /// <param name="stayingTypes">For each source namespace, the types declared directly in it that do not move.</param>
    /// <param name="stayingExtensions">For each source namespace, the extension methods of its static classes that do not move.</param>
    /// <param name="movedExtensions">The extension methods declared by the moved static classes.</param>
    public MoveContext(
        TypeMoveData data,
        IReadOnlyDictionary<string, IReadOnlySet<string>> stayingTypes,
        IReadOnlyDictionary<string, IReadOnlySet<string>> stayingExtensions,
        IReadOnlySet<string> movedExtensions)
    {
        Data = data;
        StayingTypes = stayingTypes;
        MovedNames = WithAttributeStems(data.Moves.Select(move => move.Name));
        MovedExtensionNames = movedExtensions;
        ExtensionNames = movedExtensions.Concat(stayingExtensions.Values.SelectMany(names => names)).ToHashSet(StringComparer.Ordinal);

        var droppable = data.Sources.Where(MayDropImport).ToList();
        InterestingNames = MovedNames.Concat(movedExtensions)
            .Concat(droppable.SelectMany(source => WithAttributeStems(Staying(stayingTypes, source))))
            .Concat(droppable.SelectMany(source => Staying(stayingExtensions, source)))
            .ToHashSet(StringComparer.Ordinal);
        InterestingNamesInMovedFiles = InterestingNames
            .Concat(data.Sources.SelectMany(source => WithAttributeStems(Staying(stayingTypes, source))))
            .Concat(data.Sources.SelectMany(source => Staying(stayingExtensions, source)))
            .ToHashSet(StringComparer.Ordinal);

        if (data.Moves.Count > 0)
        {
            var names = string.Join("|", data.Moves.Select(move => move.OldFullName).OrderByDescending(name => name.Length).Select(Regex.Escape));
            _oldNames = new Regex("(?<old>" + names + @")(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
        }
    }

    public TypeMoveData Data { get; }

    /// <summary>The names of the moved types, with the short name of each moved attribute (<c>Skill</c> for <c>SkillAttribute</c>).</summary>
    public IReadOnlySet<string> MovedNames { get; }

    /// <summary>The extension methods the moved static classes declare: a call through one needs the new namespace.</summary>
    public IReadOnlySet<string> MovedExtensionNames { get; }

    /// <summary>Every extension method name of interest: the moved ones and those of the static classes that stay.</summary>
    public IReadOnlySet<string> ExtensionNames { get; }

    /// <summary>For each source namespace, the types that stay directly in it (on master: the facades of <c>Service</c>).</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> StayingTypes { get; }

    /// <summary>
    /// The names whose references the analysis binds in a file that does not move: the moved types and their extension
    /// methods, and the types (and extension methods) that stay in a source namespace whose using may go.
    /// </summary>
    public IReadOnlySet<string> InterestingNames { get; }

    /// <summary><see cref="InterestingNames"/> and every type that stays in a source namespace: a moved file saw those from inside.</summary>
    public IReadOnlySet<string> InterestingNamesInMovedFiles { get; }

    /// <summary>
    /// True when a using of the source namespace may go from a file where nothing binds through it any more: the data asks
    /// for that, or no type is left there (a using of a namespace without types does not compile).
    /// </summary>
    public bool MayDropImport(string source) =>
        Data.RemoveUnusedSourceUsings || !StayingTypes.TryGetValue(source, out var staying) || staying.Count == 0;

    /// <summary>
    /// The move of a type declared directly in its source namespace (the type itself before the move, or its stand-in copy
    /// after it), or null for any other symbol.
    /// </summary>
    public TypeMove? MovedFrom(ISymbol? symbol)
    {
        var type = symbol == null ? null : Binding.NamedType(symbol)?.OriginalDefinition;
        if (type is not { ContainingType: null })
            return null;

        return Data.ByOldFullName.TryGetValue(type.ContainingNamespace.ToDisplayString() + "." + type.Name, out var move) ? move : null;
    }

    /// <summary>The move of the static class that declares an extension method called through it, or null.</summary>
    public TypeMove? MovedExtension(ISymbol symbol) =>
        symbol is IMethodSymbol { ReducedFrom: { } method } && method.ContainingType is { ContainingType: null } owner
            ? Data.ByOldFullName.GetValueOrDefault(owner.OriginalDefinition.ContainingNamespace.ToDisplayString() + "." + owner.Name)
            : null;

    /// <summary>The source namespace a type that stays there is declared in directly, or null.</summary>
    public string? StayingSource(ISymbol symbol) =>
        Binding.NamedType(symbol)?.OriginalDefinition is { ContainingType: null } type ? Staying(type) : null;

    /// <summary>The source namespace of the static class that declares an extension method that stays, or null.</summary>
    public string? StayingExtensionSource(ISymbol symbol) =>
        symbol is IMethodSymbol { ReducedFrom: { } method } && method.ContainingType is { ContainingType: null } owner ? Staying(owner) : null;

    /// <summary>A symbol key (<see cref="Binding.Key"/>) with each moved type's old full name replaced by its new one.</summary>
    public string MapKey(string key) =>
        _oldNames == null ? key : _oldNames.Replace(key, match => Data.ByOldFullName[match.Groups["old"].Value].NewFullName);

    /// <summary>The names with the short name of each attribute class among them (<c>SkillAttribute</c> gives <c>Skill</c> too).</summary>
    public static IReadOnlySet<string> WithAttributeStems(IEnumerable<string> names)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            result.Add(name);
            if (name.Length > AttributeSuffix.Length && name.EndsWith(AttributeSuffix, StringComparison.Ordinal))
                result.Add(name[..^AttributeSuffix.Length]);
        }

        return result;
    }

    private string? Staying(INamedTypeSymbol type)
    {
        var @namespace = type.OriginalDefinition.ContainingNamespace.ToDisplayString();
        return Data.Sources.Contains(@namespace) && !Data.ByOldFullName.ContainsKey(@namespace + "." + type.Name) ? @namespace : null;
    }

    private static IEnumerable<string> Staying(IReadOnlyDictionary<string, IReadOnlySet<string>> map, string source) =>
        map.TryGetValue(source, out var names) ? names : [];
}
