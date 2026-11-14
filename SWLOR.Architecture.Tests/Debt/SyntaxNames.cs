using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>Names and line counts read from syntax alone.</summary>
internal static class SyntaxNames
{
    /// <summary>
    /// The full name of a type declaration as the baselines show it: namespace, containing types and the type, with its
    /// type parameters (<c>SWLOR.Game.Server.Domain.UI.GuiViewModelBase&lt;TViewModel, TDefinition&gt;</c>). Every partial
    /// declaration of a type has the same name.
    /// </summary>
    public static string FullName(BaseTypeDeclarationSyntax type)
    {
        var name = NameWithTypeParameters(type);
        for (var parent = type.Parent; parent != null; parent = parent.Parent)
        {
            switch (parent)
            {
                case BaseTypeDeclarationSyntax outer:
                    name = NameWithTypeParameters(outer) + "." + name;
                    break;
                case BaseNamespaceDeclarationSyntax space:
                    name = space.Name.ToString() + "." + name;
                    break;
            }
        }

        return name;
    }

    /// <summary>The outermost type declaration that contains <paramref name="node"/>, or null for code outside any type.</summary>
    public static BaseTypeDeclarationSyntax? TopLevelType(SyntaxNode node)
    {
        BaseTypeDeclarationSyntax? outermost = null;
        for (var current = node; current != null; current = current.Parent)
        {
            if (current is BaseTypeDeclarationSyntax type)
                outermost = type;
        }

        return outermost;
    }

    /// <summary>The type declarations of a file that are not nested in another type.</summary>
    public static IEnumerable<BaseTypeDeclarationSyntax> TopLevelTypes(SyntaxNode root) =>
        root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .OfType<BaseTypeDeclarationSyntax>();

    /// <summary>
    /// The lines a declaration spans, from its first attribute or modifier to its last token. Leading comments and XML
    /// documentation above it are not counted; comments and blank lines inside it are.
    /// </summary>
    public static int Lines(SyntaxNode node)
    {
        var span = node.SyntaxTree.GetLineSpan(node.Span);
        return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
    }

    /// <summary>The lines of a file as an editor numbers them: a final line break does not start another line.</summary>
    public static int FileLines(SyntaxTree tree)
    {
        var lines = tree.GetText().Lines;
        return lines.Count > 1 && lines[^1].Span.IsEmpty ? lines.Count - 1 : lines.Count;
    }

    /// <summary>True when one of the type's declared bases is named <paramref name="baseName"/> (any namespace, any type arguments).</summary>
    public static bool ListsBase(BaseTypeDeclarationSyntax type, string baseName) =>
        type.BaseList?.Types.Any(baseType => RightmostName(baseType.Type) == baseName) == true;

    /// <summary>The name a member of a type is listed under: <c>Build</c>, <c>this[]</c>, <c>operator +</c>, <c>.ctor</c>, <c>.cctor</c>, <c>~Type</c>.</summary>
    public static string? MemberName(MemberDeclarationSyntax member) => member switch
    {
        MethodDeclarationSyntax method => method.Identifier.ValueText + TypeParameters(method.TypeParameterList),
        ConstructorDeclarationSyntax constructor => constructor.Modifiers.Any(SyntaxKind.StaticKeyword) ? ".cctor" : ".ctor",
        DestructorDeclarationSyntax destructor => "~" + destructor.Identifier.ValueText,
        OperatorDeclarationSyntax op => "operator " + op.OperatorToken.ValueText,
        ConversionOperatorDeclarationSyntax conversion => conversion.ImplicitOrExplicitKeyword.ValueText + " operator " + conversion.Type,
        PropertyDeclarationSyntax property => property.Identifier.ValueText,
        IndexerDeclarationSyntax => "this[]",
        EventDeclarationSyntax @event => @event.Identifier.ValueText,
        _ => null
    };

    /// <summary>The parameter types of a member as written, for telling overloads apart: <c>(uint, int)</c>.</summary>
    public static string ParameterTypes(MemberDeclarationSyntax member)
    {
        var parameters = member switch
        {
            BaseMethodDeclarationSyntax method => method.ParameterList.Parameters,
            IndexerDeclarationSyntax indexer => indexer.ParameterList.Parameters,
            _ => default
        };

        return "(" + string.Join(", ", parameters.Select(parameter => parameter.Type?.ToString() ?? parameter.Identifier.ValueText)) + ")";
    }

    private static string NameWithTypeParameters(BaseTypeDeclarationSyntax type) =>
        type.Identifier.ValueText + (type is TypeDeclarationSyntax declaration ? TypeParameters(declaration.TypeParameterList) : string.Empty);

    private static string TypeParameters(TypeParameterListSyntax? list) =>
        list is null || list.Parameters.Count == 0
            ? string.Empty
            : "<" + string.Join(", ", list.Parameters.Select(parameter => parameter.Identifier.ValueText)) + ">";

    private static string RightmostName(TypeSyntax type) => type switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        _ => type.ToString()
    };
}
