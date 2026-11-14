using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.ContentInjection;

/// <summary>Syntax questions the analysis and the rewriters share. Syntax only: the tool runs on trees that need not compile.</summary>
internal static class SyntaxHelpers
{
    public const string AccessorClass = "GameServices";

    private static readonly HashSet<string> Predefined = new(StringComparer.Ordinal)
    {
        "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint", "long", "ulong", "short", "ushort", "object", "string", "nint", "nuint"
    };

    /// <summary>A class declared directly in a namespace or the file, not nested in another type.</summary>
    public static bool IsTopLevel(ClassDeclarationSyntax syntax) => syntax.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax;

    /// <summary>The namespace a type is declared in.</summary>
    public static string NamespaceOf(SyntaxNode node)
    {
        var parts = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(space => space.Name.ToString()).Reverse();
        return string.Join(".", parts);
    }

    /// <summary>The rightmost identifier of a type name (<c>Feature.X</c> and <c>X</c> are <c>X</c>), or null for any other type.</summary>
    public static string? SimpleName(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        QualifiedNameSyntax qualified => SimpleName(qualified.Right),
        AliasQualifiedNameSyntax alias => SimpleName(alias.Name),
        _ => null
    };

    /// <summary><c>GameServices</c>, <c>Composition.GameServices</c> or <c>SWLOR.Game.Server.Composition.GameServices</c>.</summary>
    public static bool IsAccessor(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText == AccessorClass,
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText == AccessorClass,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText == AccessorClass,
        _ => false
    };

    /// <summary>True when <paramref name="expression"/> is the target of a call.</summary>
    public static bool IsInvoked(ExpressionSyntax expression) => expression.Parent is InvocationExpressionSyntax invocation && invocation.Expression == expression;

    /// <summary>True when <paramref name="identifier"/> is the left side of <c>X.Y</c> or <c>X?.Y</c>.</summary>
    public static bool IsReceiver(IdentifierNameSyntax identifier) =>
        identifier.Parent is MemberAccessExpressionSyntax access && access.Expression == identifier ||
        identifier.Parent is ConditionalAccessExpressionSyntax conditional && conditional.Expression == identifier;

    public static bool InsideNameof(SyntaxNode node) =>
        node.Ancestors().OfType<InvocationExpressionSyntax>().Any(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" });

    /// <summary>Names a member declares for itself: parameters, locals, pattern and loop variables, lambda and query parameters.</summary>
    public static HashSet<string> DeclaredLocals(SyntaxNode member)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in member.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case ParameterSyntax parameter:
                    names.Add(parameter.Identifier.ValueText);
                    break;
                case VariableDeclaratorSyntax variable when variable.Parent?.Parent is not FieldDeclarationSyntax and not EventFieldDeclarationSyntax:
                    names.Add(variable.Identifier.ValueText);
                    break;
                case SingleVariableDesignationSyntax designation:
                    names.Add(designation.Identifier.ValueText);
                    break;
                case ForEachStatementSyntax loop:
                    names.Add(loop.Identifier.ValueText);
                    break;
                case CatchDeclarationSyntax { Identifier.ValueText.Length: > 0 } caught:
                    names.Add(caught.Identifier.ValueText);
                    break;
                case FromClauseSyntax from:
                    names.Add(from.Identifier.ValueText);
                    break;
                case LetClauseSyntax let:
                    names.Add(let.Identifier.ValueText);
                    break;
                case JoinClauseSyntax join:
                    names.Add(join.Identifier.ValueText);
                    break;
                case QueryContinuationSyntax continuation:
                    names.Add(continuation.Identifier.ValueText);
                    break;
                case LocalFunctionStatementSyntax function:
                    names.Add(function.Identifier.ValueText);
                    break;
                case TypeParameterSyntax typeParameter:
                    names.Add(typeParameter.Identifier.ValueText);
                    break;
            }
        }

        return names;
    }

    /// <summary>The names a member declares on its class.</summary>
    public static IEnumerable<string> MemberNames(MemberDeclarationSyntax member) => member switch
    {
        MethodDeclarationSyntax method => new[] { method.Identifier.ValueText },
        PropertyDeclarationSyntax property => new[] { property.Identifier.ValueText },
        FieldDeclarationSyntax field => field.Declaration.Variables.Select(variable => variable.Identifier.ValueText),
        EventFieldDeclarationSyntax field => field.Declaration.Variables.Select(variable => variable.Identifier.ValueText),
        EventDeclarationSyntax e => new[] { e.Identifier.ValueText },
        BaseTypeDeclarationSyntax type => new[] { type.Identifier.ValueText },
        DelegateDeclarationSyntax d => new[] { d.Identifier.ValueText },
        _ => Array.Empty<string>()
    };

    public static bool IsStatic(MemberDeclarationSyntax member) =>
        member.Modifiers.Any(SyntaxKind.StaticKeyword) || member.Modifiers.Any(SyntaxKind.ConstKeyword);

    /// <summary>The parameter name for a property: <c>Stat</c> is <c>stat</c>, <c>AI</c> is <c>ai</c>, <c>DMPresence</c> is <c>dmPresence</c>.</summary>
    public static string ParameterName(string property)
    {
        var upper = 0;
        while (upper < property.Length && char.IsUpper(property[upper]))
            upper++;

        string name;
        if (upper == 0)
            name = property;
        else if (upper == property.Length)
            name = property.ToLowerInvariant();
        else if (upper == 1)
            name = char.ToLowerInvariant(property[0]) + property[1..];
        else
            name = property[..(upper - 1)].ToLowerInvariant() + property[(upper - 1)..];

        return Microsoft.CodeAnalysis.CSharp.SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    }

    /// <summary>True for a type the creator can always name (<c>int</c>, <c>string?</c>, <c>uint[]</c>).</summary>
    public static bool IsPredefined(string type) => Predefined.Contains(type.TrimEnd('?', '[', ']'));

    /// <summary>The leading whitespace of the line that holds <paramref name="position"/>.</summary>
    public static string IndentAt(string text, int position)
    {
        var start = LineStart(text, position);
        var end = start;
        while (end < text.Length && (text[end] == ' ' || text[end] == '\t'))
            end++;
        return text[start..end];
    }

    public static int LineStart(string text, int position)
    {
        var start = position;
        while (start > 0 && text[start - 1] != '\n')
            start--;
        return start;
    }

    /// <summary>The position just after the end of the line that holds <paramref name="position"/>.</summary>
    public static int NextLineStart(string text, int position)
    {
        var end = text.IndexOf('\n', position);
        return end < 0 ? text.Length : end + 1;
    }
}
