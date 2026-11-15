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

        return new Binding(info.CandidateSymbols.ToList(), info.CandidateReason == CandidateReason.Ambiguous)
        {
            Failed = info.CandidateReason != CandidateReason.Ambiguous
        };
    }

    /// <summary>True when the compiler found only candidates because it could not resolve the name (an overload that failed).</summary>
    public bool Failed { get; init; }

    /// <summary>
    /// True when the name bound to a symbol whose own type or whose containing type has an error type in it. That happens
    /// in the analysis only when a stand-in for a type that already moved sits beside the real type and a file imports
    /// both namespaces: the ambiguous name becomes an error type, which changes how a same-named property, a tuple element
    /// or an overload binds. Such a binding says nothing about what the name meant.
    /// </summary>
    public bool IsUnreliable => Failed || Symbols.Any(HasError);

    private static bool HasError(ISymbol symbol) => symbol switch
    {
        IErrorTypeSymbol => true,
        INamedTypeSymbol type => type.TypeArguments.Any(HasError),
        IPropertySymbol property => HasError(property.Type) || HasError(property.ContainingType),
        IFieldSymbol field => HasError(field.Type) || HasError(field.ContainingType),
        IMethodSymbol method => HasError(method.ReturnType) || method.Parameters.Any(parameter => HasError(parameter.Type)) || HasError(method.ContainingType),
        _ => false
    };

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
