using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.EngineSeamGenerator;

/// <summary>Reads the members of one generated class or interface part back with Roslyn, for round-trip checks.</summary>
internal static class GeneratedTypeReader
{
    public static IReadOnlyList<GeneratedMember> Read(string path, string text, string typeName, List<string> errors)
    {
        var tree = CSharpSyntaxTree.ParseText(text);
        foreach (var diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            errors.Add($"{path}: syntax error {diagnostic}");
        }

        var types = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
            .Where(t => t.Identifier.Text == typeName)
            .ToList();
        if (types.Count != 1)
        {
            errors.Add($"{path}: expected exactly one declaration of {typeName}, found {types.Count}.");
            return Array.Empty<GeneratedMember>();
        }

        var members = new List<GeneratedMember>();
        foreach (var member in types[0].Members)
        {
            switch (member)
            {
                case MethodDeclarationSyntax method:
                {
                    var parameters = method.ParameterList.Parameters;
                    var key = method.Identifier.Text + "(" + string.Join(",", parameters.Select(p => WrapperParser.RemoveWhitespace(p.Type!.ToString()))) + ")";
                    var signature = $"{method.ReturnType} {method.Identifier.Text}({string.Join(", ", parameters.Select(p => p.ToString()))})";
                    string? body = method.Body == null ? null : WrapperParser.Normalize(text.Substring(method.Body.Span.Start, method.Body.Span.Length));
                    members.Add(new GeneratedMember(MemberKind.Method, key, signature, body));
                    break;
                }
                case PropertyDeclarationSyntax property:
                {
                    string? body = property.ExpressionBody == null
                        ? null
                        : WrapperParser.Normalize(text.Substring(property.ExpressionBody.Expression.Span.Start, property.ExpressionBody.Expression.Span.Length));
                    members.Add(new GeneratedMember(MemberKind.Property, property.Identifier.Text, $"{property.Type} {property.Identifier.Text}", body));
                    break;
                }
                default:
                    errors.Add($"{path}: unexpected {member.Kind()} in {typeName}.");
                    break;
            }
        }

        return members;
    }
}
