using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace SWLOR.TypeMoves;

/// <summary>
/// What the analysis of one tree needs to know about the move: which types move where, and which other types stay in the
/// namespace they leave (a file that still uses one of those keeps importing that namespace).
/// </summary>
internal sealed class MoveContext
{
    private readonly Regex _oldNames;

    public MoveContext(TypeMoveData data, IReadOnlySet<string> otherTypes, IReadOnlySet<string> otherExtensionMethods)
    {
        Data = data;
        OtherTypes = otherTypes;
        OtherExtensionMethods = otherExtensionMethods;
        MovedNames = data.Moves.Select(move => move.Name).ToHashSet(StringComparer.Ordinal);
        InterestingNames = MovedNames.Concat(otherTypes).Concat(otherExtensionMethods).ToHashSet(StringComparer.Ordinal);
        var names = string.Join("|", data.Moves.Select(move => move.Name).OrderByDescending(name => name.Length).Select(Regex.Escape));
        _oldNames = new Regex(Regex.Escape(data.From) + @"\.(?<name>" + names + @")(?![A-Za-z0-9_])", RegexOptions.CultureInvariant);
    }

    public TypeMoveData Data { get; }

    public string From => Data.From;

    /// <summary>The last segment of <see cref="From"/> (<c>Service</c>): how a qualifier that names it ends.</summary>
    public string FromLeaf => TypeMove.Leaf(Data.From);

    public IReadOnlySet<string> MovedNames { get; }

    /// <summary>The types that stay directly in <see cref="From"/> (on master: the facades).</summary>
    public IReadOnlySet<string> OtherTypes { get; }

    /// <summary>The extension methods of the static classes that stay in <see cref="From"/>.</summary>
    public IReadOnlySet<string> OtherExtensionMethods { get; }

    /// <summary>The names whose references the analysis binds: the moved types, the types that stay, their extension methods.</summary>
    public IReadOnlySet<string> InterestingNames { get; }

    /// <summary>
    /// The move of a type declared directly in <see cref="From"/> (the type itself before the move, or its stand-in copy after
    /// it), or null for any other symbol.
    /// </summary>
    public TypeMove? MovedFrom(ISymbol? symbol)
    {
        var type = symbol == null ? null : Binding.NamedType(symbol)?.OriginalDefinition;
        if (type is not { ContainingType: null } || type.ContainingNamespace.ToDisplayString() != From)
            return null;

        return Data.ByName.TryGetValue(type.Name, out var move) ? move : null;
    }

    /// <summary>True for a type that stays directly in <see cref="From"/>.</summary>
    public bool IsStayingType(ISymbol symbol) =>
        Binding.NamedType(symbol)?.OriginalDefinition is { ContainingType: null } type &&
        type.ContainingNamespace.ToDisplayString() == From && !Data.ByName.ContainsKey(type.Name);

    /// <summary>True for an extension method of a static class that stays in <see cref="From"/>.</summary>
    public bool IsStayingExtension(ISymbol symbol) =>
        symbol is IMethodSymbol { ReducedFrom: { } method } &&
        method.ContainingType is { ContainingType: null } owner &&
        owner.ContainingNamespace.ToDisplayString() == From && !Data.ByName.ContainsKey(owner.Name);

    /// <summary>A symbol key (<see cref="Binding.Key"/>) with each moved type's old full name replaced by its new one.</summary>
    public string MapKey(string key) =>
        _oldNames.Replace(key, match => Data.ByName[match.Groups["name"].Value].NewFullName);
}
