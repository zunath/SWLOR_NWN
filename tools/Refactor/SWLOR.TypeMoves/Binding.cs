using Microsoft.CodeAnalysis;

namespace SWLOR.TypeMoves;

/// <summary>What a name binds to: one symbol, or the candidates of an ambiguous or unresolved overload.</summary>
/// <param name="Symbols">The symbol, or the candidates. Empty when the name binds to nothing.</param>
/// <param name="Ambiguous">True when the compiler found several symbols of that name in one scope.</param>
internal sealed record Binding(IReadOnlyList<ISymbol> Symbols, bool Ambiguous)
{
    public static readonly Binding None = new([], false);

    public bool IsEmpty => Symbols.Count == 0;

    public static Binding Of(SemanticModel model, SyntaxNode node)
    {
        var info = model.GetSymbolInfo(node);
        if (info.Symbol != null)
            return new Binding([info.Symbol], false);
        if (info.CandidateSymbols.Length == 0)
            return None;

        return new Binding(info.CandidateSymbols.ToList(), info.CandidateReason == CandidateReason.Ambiguous);
    }

    /// <summary>
    /// A stable name for a symbol that survives a recompilation: its documentation id (<c>T:NS.Type</c>, <c>M:NS.Type.M(System.Int32)</c>),
    /// or its kind and name for a local, a parameter, a range variable or a label; null for an error type.
    /// </summary>
    public static string? Key(ISymbol symbol)
    {
        switch (symbol)
        {
            case ILocalSymbol or IParameterSymbol or IRangeVariableSymbol or ILabelSymbol or IDiscardSymbol:
                return symbol.Kind + ":" + symbol.Name;
            case IAliasSymbol alias:
                return "alias:" + alias.Name + "=" + Key(alias.Target);
            case IErrorTypeSymbol:
                return null;
            case ITypeParameterSymbol parameter:
                return "typeparameter:" + parameter.Name;
        }

        var definition = symbol.OriginalDefinition;
        return definition.GetDocumentationCommentId() ?? symbol.Kind + ":" + definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>The type a symbol names: the type itself, or the type of a constructor (an attribute name binds to one).</summary>
    public static INamedTypeSymbol? NamedType(ISymbol symbol) => symbol switch
    {
        INamedTypeSymbol type => type,
        IMethodSymbol { MethodKind: MethodKind.Constructor } constructor => constructor.ContainingType,
        _ => null
    };
}
