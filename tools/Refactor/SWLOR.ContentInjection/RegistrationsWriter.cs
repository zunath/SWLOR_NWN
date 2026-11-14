using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.ContentInjection;

/// <summary>
/// Writes <c>SWLOR.Game.Server/Composition/ContentRegistrations.cs</c>, which the tool owns: one registration per content singleton,
/// sorted by name, and every <c>Content.Create</c> signature of the server's code (the made type and its value types), which the
/// reload validation checks resolve to exactly one constructor.
/// </summary>
internal static class RegistrationsWriter
{
    public static string Write(IEnumerable<string> singletons, IEnumerable<string> signatures, string eol)
    {
        var text = new StringBuilder();
        text.Append("// Written by tools/Refactor/SWLOR.ContentInjection (Apply-ContentInjection.ps1); do not edit by hand.\n");
        text.Append("using System.Collections.Generic;\n");
        text.Append("using Microsoft.Extensions.DependencyInjection;\n");
        text.Append('\n');
        text.Append("namespace SWLOR.Game.Server.Composition\n");
        text.Append("{\n");
        text.Append("    /// <summary>\n");
        text.Append("    /// The content the container knows (MaintainabilityRefactorContentInjection.md): every content singleton (one instance per build,\n");
        text.Append("    /// built like a service), one line each, sorted by name, and every <c>Content.Create</c> signature of the game's code, which the\n");
        text.Append("    /// reload validation checks. Made content is never registered: <see cref=\"ContentFactory\"/> builds it.\n");
        text.Append("    /// </summary>\n");
        text.Append("    internal static class ContentRegistrations\n");
        text.Append("    {\n");
        text.Append("        internal static void Register(IServiceCollection services)\n");
        text.Append("        {\n");
        foreach (var singleton in singletons.Distinct().OrderBy(name => name[(name.LastIndexOf('.') + 1)..], StringComparer.Ordinal).ThenBy(name => name, StringComparer.Ordinal))
            text.Append("            services.AddSingleton<").Append(singleton).Append(">();\n");
        text.Append("        }\n");
        text.Append('\n');
        text.Append("        internal static IEnumerable<ContentSignature> Signatures()\n");
        text.Append("        {\n");
        var lines = signatures.Distinct().OrderBy(line => line, StringComparer.Ordinal).ToList();
        if (lines.Count == 0)
            text.Append("            yield break;\n");
        foreach (var signature in lines)
            text.Append("            yield return new ContentSignature(").Append(signature).Append(");\n");
        text.Append("        }\n");
        text.Append("    }\n");
        text.Append("}\n");
        return eol == "\n" ? text.ToString() : text.ToString().Replace("\n", eol, StringComparison.Ordinal);
    }

    /// <summary>
    /// The arguments of each <c>ContentSignature</c> line for the <c>Content.Create</c> calls of <paramref name="file"/> (a server file):
    /// <c>typeof(Full.Made), typeof(int)</c>. A type the tool cannot name in full is refused.
    /// </summary>
    public static IEnumerable<string> Signatures(TreeFile file, string text, ContentAnalysis analysis, Func<TypeDirectory> types, List<Refusal> refusals)
    {
        if (!text.Contains(".Create<", StringComparison.Ordinal))
            yield break;

        var parsed = new TreeFile(file.RelativePath, file.Kind, file.ImportsGameServicesGlobally, new SourceFile(file.Source.Bom, text));
        foreach (var invocation in parsed.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: ServiceName.ContentName or "_content" },
                    Name: GenericNameSyntax { Identifier.ValueText: "Create" } create
                } || invocation.ArgumentList.Arguments.Count != create.TypeArgumentList.Arguments.Count - 1)
            {
                continue;
            }

            var names = new List<string>();
            foreach (var argument in create.TypeArgumentList.Arguments)
            {
                var written = argument.ToString();
                var simple = SyntaxHelpers.SimpleName(argument);
                var full = SyntaxHelpers.IsPredefined(written) ? written
                    : simple != null && analysis.ByName.TryGetValue(simple, out var made) ? made.FullName
                    : written.Contains('.') ? written
                    : simple != null ? types().FullName(simple, parsed) : null;
                if (full == null)
                {
                    refusals.Add(parsed.Refuse(invocation, $"The tool cannot name {written} in full to record this Content.Create signature: write its line in ContentRegistrations by hand."));
                    names.Clear();
                    break;
                }

                names.Add("typeof(" + full + (written.EndsWith('?') && !full.EndsWith('?') ? "?" : string.Empty) + ")");
            }

            if (names.Count > 0)
                yield return string.Join(", ", names);
        }
    }
}
