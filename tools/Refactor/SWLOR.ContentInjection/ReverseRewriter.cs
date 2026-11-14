using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.ContentInjection;

/// <summary>
/// Turns converted text back into master's form (MaintainabilityRefactorContentInjection.md, "The round trip"):
/// <c>Content.Create&lt;X, A...&gt;(args)</c> becomes <c>new X(args)</c> with the arguments as written, and the fixture properties the
/// forward run adds (<c>Content</c> from a container or <c>TestContent.WithoutServices</c>, a singleton from a container) go, with the
/// blank line the forward run wrote after each. Needs nothing of the tree: <c>MasterCopy.ps1</c> runs it on the characterization
/// project copied into master's tree, which has no <c>Core/IContentFactory.cs</c>.
/// </summary>
internal static class ReverseRewriter
{
    public static (string Text, int Rewrites) Apply(string text)
    {
        if (!text.Contains(ServiceName.ContentName, StringComparison.Ordinal) && !text.Contains(".Get<", StringComparison.Ordinal))
            return (text, 0);

        var root = TreeFile.Parse(text);
        var edits = new List<TextEdit>();
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: ServiceName.ContentName },
                    Name: GenericNameSyntax { Identifier.ValueText: "Create" } create
                })
            {
                continue;
            }

            var types = create.TypeArgumentList.Arguments;
            if (types.Count == 0 || invocation.ArgumentList.Arguments.Count != types.Count - 1)
                continue;

            edits.Add(new TextEdit(invocation.SpanStart, create.Span.End - invocation.SpanStart, "new " + types[0]));
        }

        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Where(IsFixtureProperty))
        {
            var start = SyntaxHelpers.LineStart(text, property.SpanStart);
            var end = SyntaxHelpers.NextLineStart(text, property.Span.End);
            if (end < text.Length && IsBlankLine(text, end))
                end = SyntaxHelpers.NextLineStart(text, end);
            edits.Add(new TextEdit(start, end - start, string.Empty));
        }

        return edits.Count == 0 ? (text, 0) : (TextEdit.Apply(text, edits), edits.Count);
    }

    /// <summary>A property the forward run writes into a test fixture.</summary>
    public static bool IsFixtureProperty(PropertyDeclarationSyntax property)
    {
        if (property.Modifiers.Count != 1 || property.Modifiers[0].ValueText != "private")
            return false;

        var type = property.Type.ToString();
        var name = property.Identifier.ValueText;
        if (name == ServiceName.ContentName && (type == ServiceName.ContentType || type == ServiceName.ContentNamespace + "." + ServiceName.ContentType))
        {
            return property.ExpressionBody?.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: ServiceName.ContentName, Expression: IdentifierNameSyntax } ||
                   property.Initializer?.Value is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WithoutServices" } withoutServices &&
                   withoutServices.Expression.ToString() is "TestContent" or ContentRewriter.TestContentNamespace + ".TestContent";
        }

        return SyntaxHelpers.SimpleName(property.Type) == name &&
               property.ExpressionBody?.Expression is InvocationExpressionSyntax
               {
                   ArgumentList.Arguments.Count: 0,
                   Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax, Name: GenericNameSyntax { Identifier.ValueText: "Get", TypeArgumentList.Arguments: [var got] } }
               } && got.ToString() == type;
    }

    private static bool IsBlankLine(string text, int start)
    {
        var position = start;
        while (position < text.Length && (text[position] == ' ' || text[position] == '\t'))
            position++;
        return position < text.Length && (text[position] == '\n' || text[position] == '\r');
    }
}
