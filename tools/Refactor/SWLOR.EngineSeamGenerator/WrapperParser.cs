using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Reads a hand-written wrapper source file with Roslyn syntax only. Anything the generator cannot move behind an
/// interface (generic methods, `ref` parameters, attributes, mutable statics, nested types that hold code) is reported
/// as an error, so a new kind of member never slips past the seam unnoticed.
/// </summary>
internal static class WrapperParser
{
    public static WrapperSourceFile Parse(string path, string text, SeamDefinition definition, List<string> errors)
    {
        var tree = CSharpSyntaxTree.ParseText(text);
        foreach (var diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
        {
            errors.Add($"{path}: syntax error {diagnostic}");
        }

        var members = new List<WrapperMember>();
        var constants = new List<string>();
        var nestedTypes = new List<NestedTypeInfo>();
        var root = tree.GetCompilationUnitRoot();
        var usings = root.Usings.Select(u => u.ToString()).ToList();
        var eol = DetectEol(text);

        var classDeclaration = FindClass(path, root, definition, errors);
        if (classDeclaration != null)
        {
            foreach (var member in classDeclaration.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax method:
                        AddIfValid(members, ParseMethod(path, text, method, errors));
                        break;
                    case PropertyDeclarationSyntax property:
                        AddIfValid(members, ParseProperty(path, text, property, errors));
                        break;
                    case FieldDeclarationSyntax field when IsPublicConstant(field):
                        constants.AddRange(field.Declaration.Variables.Select(v => v.Identifier.Text));
                        break;
                    case BaseTypeDeclarationSyntax nested when definition.AllowDataOnlyNestedTypes:
                        if (IsDataOnlyNestedType(path, nested, errors))
                        {
                            nestedTypes.Add(new NestedTypeInfo(nested.Identifier.Text, Normalize(nested.ToString())));
                        }

                        break;
                    default:
                        errors.Add($"{path}: unsupported member `{FirstLine(member)}` ({member.Kind()}). The generator moves public " +
                                   "static methods with block bodies and get-only expression-bodied properties, and leaves public constants" +
                                   (definition.AllowDataOnlyNestedTypes ? " and data-only nested types" : string.Empty) + " static.");
                        break;
                }
            }
        }

        return new WrapperSourceFile
        {
            Path = path,
            Text = text,
            Eol = eol,
            Usings = usings,
            Members = members,
            Constants = constants,
            NestedTypes = nestedTypes
        };
    }

    public static string DetectEol(string text)
    {
        var crlf = 0;
        var lf = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n') continue;
            if (i > 0 && text[i - 1] == '\r') crlf++;
            else lf++;
        }

        return crlf > lf ? "\r\n" : "\n";
    }

    public static string RemoveWhitespace(string value) => string.Concat(value.Where(c => !char.IsWhiteSpace(c)));

    public static string Normalize(string value) => value.Replace("\r\n", "\n");

    private static void AddIfValid(List<WrapperMember> members, WrapperMember? member)
    {
        if (member != null) members.Add(member);
    }

    private static ClassDeclarationSyntax? FindClass(string path, CompilationUnitSyntax root, SeamDefinition definition, List<string> errors)
    {
        if (root.Members.Count != 1 || root.Members[0] is not NamespaceDeclarationSyntax ns || ns.Name.ToString() != definition.Namespace)
        {
            errors.Add($"{path}: expected exactly one block-scoped `namespace {definition.Namespace}` declaration and nothing else.");
            return null;
        }

        if (ns.Usings.Count > 0 || ns.Externs.Count > 0)
        {
            errors.Add($"{path}: using directives inside the namespace are not supported; put them at the top of the file.");
            return null;
        }

        if (ns.Members.Count != 1 || ns.Members[0] is not ClassDeclarationSyntax cls || cls.Identifier.Text != definition.StaticClassName ||
            cls.TypeParameterList != null || cls.BaseList != null ||
            !HasExactModifiers(cls.Modifiers, definition.StaticClassModifiers.ToArray()))
        {
            errors.Add($"{path}: expected exactly one `{string.Join(" ", definition.StaticClassModifiers)} class {definition.StaticClassName}` in the namespace.");
            return null;
        }

        return cls;
    }

    private static WrapperMember? ParseMethod(string path, string text, MethodDeclarationSyntax method, List<string> errors)
    {
        var name = method.Identifier.Text;
        var where = $"{path}: {name}";
        var valid = true;

        if (!HasExactModifiers(method.Modifiers, "public", "static"))
        {
            errors.Add($"{where}: only `public static` methods are supported (found `{string.Join(" ", method.Modifiers.Select(m => m.Text))}`).");
            valid = false;
        }

        if (method.AttributeLists.Count > 0 || method.TypeParameterList != null || method.ConstraintClauses.Count > 0 ||
            method.ExplicitInterfaceSpecifier != null)
        {
            errors.Add($"{where}: attributes, generics and constraints are not supported.");
            valid = false;
        }

        if (method.Body == null || method.ExpressionBody != null)
        {
            errors.Add($"{where}: only block bodies are supported.");
            valid = false;
        }

        var parameters = new List<WrapperParameter>();
        foreach (var parameter in method.ParameterList.Parameters)
        {
            var modifiers = parameter.Modifiers.Select(m => m.Text).ToList();
            if (modifiers.Any(m => m != "params") || parameter.AttributeLists.Count > 0 || parameter.Type == null)
            {
                errors.Add($"{where}: parameter `{parameter}` uses a modifier or attribute the interface cannot express (only `params` is supported).");
                valid = false;
                continue;
            }

            var withoutDefault = string.Join(" ", modifiers.Concat(new[] { parameter.Type.ToString(), parameter.Identifier.Text }));
            parameters.Add(new WrapperParameter(parameter.Identifier.Text, parameter.Type.ToString(), parameter.ToString(), withoutDefault));
        }

        if (!valid || method.Body == null) return null;

        var indent = ReadIndent(path, text, method.GetFirstToken().SpanStart, name, errors);
        if (indent == null) return null;

        var span = method.Body.Span;
        return new WrapperMember
        {
            Kind = MemberKind.Method,
            Name = name,
            ReturnType = method.ReturnType.ToString(),
            Parameters = parameters,
            Key = name + "(" + string.Join(",", parameters.Select(p => RemoveWhitespace(p.Type))) + ")",
            DocLines = ReadDocLines(method),
            Indent = indent,
            BodyStart = span.Start,
            BodyLength = span.Length,
            BodyText = Normalize(text.Substring(span.Start, span.Length))
        };
    }

    private static WrapperMember? ParseProperty(string path, string text, PropertyDeclarationSyntax property, List<string> errors)
    {
        var name = property.Identifier.Text;
        var where = $"{path}: {name}";

        if (!HasExactModifiers(property.Modifiers, "public", "static") || property.AttributeLists.Count > 0 ||
            property.ExpressionBody == null || property.AccessorList != null || property.Initializer != null)
        {
            errors.Add($"{where}: only `public static T Name => expression;` properties are supported.");
            return null;
        }

        var indent = ReadIndent(path, text, property.GetFirstToken().SpanStart, name, errors);
        if (indent == null) return null;

        var span = property.ExpressionBody.Expression.Span;
        return new WrapperMember
        {
            Kind = MemberKind.Property,
            Name = name,
            ReturnType = property.Type.ToString(),
            Parameters = Array.Empty<WrapperParameter>(),
            Key = name,
            DocLines = ReadDocLines(property),
            Indent = indent,
            BodyStart = span.Start,
            BodyLength = span.Length,
            BodyText = Normalize(text.Substring(span.Start, span.Length))
        };
    }

    private static string? ReadIndent(string path, string text, int tokenStart, string name, List<string> errors)
    {
        var lineStart = text.LastIndexOf('\n', Math.Max(tokenStart - 1, 0)) + 1;
        var indent = text.Substring(lineStart, tokenStart - lineStart);
        if (indent.Any(c => c != ' ' && c != '\t'))
        {
            errors.Add($"{path}: {name}: the declaration must start its own line.");
            return null;
        }

        return indent;
    }

    private static IReadOnlyList<string> ReadDocLines(MemberDeclarationSyntax member)
    {
        return member.GetLeadingTrivia()
            .Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            .SelectMany(t => Normalize(t.ToFullString()).Split('\n'))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    /// <summary>
    /// A nested type stays where it is, so it must hold data and nothing else: a public enum, or a public struct, class or
    /// record whose members are fields and auto-implemented properties without initializers. Code in it could reach the
    /// engine without passing through the seam.
    /// </summary>
    private static bool IsDataOnlyNestedType(string path, BaseTypeDeclarationSyntax nested, List<string> errors)
    {
        var name = nested.Identifier.Text;
        var where = $"{path}: nested type {name}";
        var valid = true;

        if (!HasExactModifiers(nested.Modifiers, "public") || nested.AttributeLists.Count > 0)
        {
            errors.Add($"{where}: only public nested types without attributes are supported.");
            valid = false;
        }

        if (nested is TypeDeclarationSyntax type)
        {
            if (type.TypeParameterList != null || type.ConstraintClauses.Count > 0 || type.BaseList != null ||
                type is RecordDeclarationSyntax { ParameterList: not null })
            {
                errors.Add($"{where}: generics, base types and primary constructors are not supported.");
                valid = false;
            }

            foreach (var member in type.Members)
            {
                var dataOnly = member switch
                {
                    FieldDeclarationSyntax field => field.AttributeLists.Count == 0 && field.Declaration.Variables.All(v => v.Initializer == null),
                    PropertyDeclarationSyntax property => property.AttributeLists.Count == 0 && property.ExpressionBody == null &&
                                                          property.Initializer == null && property.AccessorList != null &&
                                                          property.AccessorList.Accessors.All(a => a.Body == null && a.ExpressionBody == null),
                    _ => false
                };
                if (!dataOnly)
                {
                    errors.Add($"{where}: member `{FirstLine(member)}` holds code or an initializer. Nested types stay in the static class, " +
                               "so they may only declare fields and auto-implemented properties.");
                    valid = false;
                }
            }
        }

        return valid;
    }

    private static bool IsPublicConstant(FieldDeclarationSyntax field)
    {
        var modifiers = field.Modifiers.Select(m => m.Text).ToList();
        return modifiers.Count == 2 && modifiers.Contains("public") && modifiers.Contains("const");
    }

    private static bool HasExactModifiers(SyntaxTokenList modifiers, params string[] expected)
    {
        var actual = modifiers.Select(m => m.Text).OrderBy(m => m, StringComparer.Ordinal);
        return actual.SequenceEqual(expected.OrderBy(m => m, StringComparer.Ordinal));
    }

    private static string FirstLine(SyntaxNode node) => node.ToString().Split('\n')[0].Trim();
}
