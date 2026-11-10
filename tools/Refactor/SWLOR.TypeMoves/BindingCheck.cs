using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.TypeMoves;

/// <summary>
/// Proves the edits change no binding: every name of interest in a file must bind after the move to what it bound to
/// before (a moved type to itself under its new name). A name that would bind to something else, or become ambiguous, is
/// written qualified with the namespace or type of what it meant (<c>Random</c> that meant <c>System.Random</c> inside the
/// namespace <c>Random</c> joins becomes <c>System.Random</c>); <see cref="Qualifier"/> gives that text.
/// </summary>
internal static class BindingCheck
{
    /// <summary>A name whose binding changed.</summary>
    /// <param name="Position">The name's position in the text before the edits.</param>
    /// <param name="Name">The name.</param>
    /// <param name="Meant">What it bound to before (a moved type: its old symbol).</param>
    /// <param name="Before">The keys it bound to before, mapped to the new names.</param>
    /// <param name="After">The keys it binds to after the edits.</param>
    /// <param name="Leading">True when the name stands first (not the right side of a qualified name or member access), so a qualifier can be written before it.</param>
    internal sealed record Change(int Position, string Name, ISymbol Meant, string Before, string After, bool Leading);

    public static List<Change> Compare(
        SemanticModel before,
        SyntaxNode rootBefore,
        SemanticModel after,
        SyntaxNode rootAfter,
        IReadOnlyList<TextEdit> edits,
        IReadOnlySet<string>? names,
        MoveContext context)
    {
        var changes = new List<Change>();
        foreach (var token in rootBefore.DescendantTokens())
        {
            if (!token.IsKind(SyntaxKind.IdentifierToken) || token.Parent is not SimpleNameSyntax name ||
                (names != null && !names.Contains(token.ValueText)) || SyntaxQueries.InNamespaceName(name))
                continue;

            var position = TextEdit.MapPosition(token.SpanStart, edits);
            if (position < 0)
                continue; // an edit replaced it (a qualifier segment)

            var bindingBefore = Binding.Of(before, name);
            var meant = Meant(bindingBefore, context);
            if (meant == null)
                continue; // bound to nothing before: nothing to keep

            var keysBefore = Keys(bindingBefore, context, meant);
            var tokenAfter = rootAfter.FindToken(position);
            if (tokenAfter.SpanStart != position || tokenAfter.ValueText != token.ValueText || tokenAfter.Parent is not SimpleNameSyntax nameAfter)
            {
                changes.Add(new Change(token.SpanStart, token.ValueText, meant, keysBefore, "(the name moved)", false));
                continue;
            }

            var bindingAfter = Binding.Of(after, nameAfter);
            var keysAfter = bindingAfter.Ambiguous ? "ambiguous: " + Keys(bindingAfter, null, null) : Keys(bindingAfter, null, null);
            if (keysAfter != keysBefore)
                changes.Add(new Change(token.SpanStart, token.ValueText, meant, keysBefore, keysAfter.Length == 0 ? "nothing" : keysAfter, SyntaxQueries.Qualifier(name) == null));
        }

        return changes;
    }

    /// <summary>
    /// The text to write before a leading name so it binds to <paramref name="meant"/> again (<c>System.</c>,
    /// <c>Domain.Foundation.</c>), on the second try with <c>global::</c>; null when a qualifier cannot help (a local, a member).
    /// </summary>
    public static string? Qualifier(ISymbol meant, MoveContext context, string fileNamespace, bool global)
    {
        if (context.MovedFrom(meant) is { } move)
        {
            if (global)
                return "global::" + move.To + ".";
            return SyntaxQueries.IsWithin(fileNamespace, TypeMove.Parent(move.From)) ? move.RelativeTarget + "." : move.To + ".";
        }

        var owner = meant switch
        {
            IMethodSymbol { MethodKind: MethodKind.Constructor } constructor => (ISymbol?)Container(constructor.ContainingType),
            INamedTypeSymbol type => Container(type),
            IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol when meant.IsStatic => meant.ContainingType,
            _ => null
        };

        switch (owner)
        {
            case INamespaceSymbol { IsGlobalNamespace: true }:
                return "global::";
            case INamespaceSymbol or INamedTypeSymbol:
                var full = owner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (full.StartsWith("global::", StringComparison.Ordinal) && !global)
                    full = full["global::".Length..];
                return full + ".";
            default:
                return null;
        }
    }

    private static ISymbol? Container(INamedTypeSymbol type) =>
        type.ContainingType != null ? type.ContainingType : type.ContainingNamespace;

    /// <summary>What a name meant before: its symbol, or the moved type among ambiguous candidates; null when it bound to nothing.</summary>
    private static ISymbol? Meant(Binding binding, MoveContext context)
    {
        if (binding.IsEmpty)
            return null;

        var moved = binding.Symbols.FirstOrDefault(symbol => context.MovedFrom(symbol) != null);
        if (moved != null)
            return moved;

        return binding.Ambiguous ? null : binding.Symbols[0];
    }

    private static string Keys(Binding binding, MoveContext? context, ISymbol? meant)
    {
        var symbols = meant != null && context?.MovedFrom(meant) != null ? [meant] : binding.Symbols;
        var keys = symbols
            .Select(Binding.Key)
            .Where(key => key != null)
            .Select(key => context == null ? key! : context.MapKey(key!))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal);
        return string.Join(" | ", keys);
    }
}
