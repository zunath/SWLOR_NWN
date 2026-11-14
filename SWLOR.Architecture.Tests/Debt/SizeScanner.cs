using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// Measures the server's types and members from syntax (Phase 5 targets 2 and 3). A type is measured by its files, every
/// partial file together ("all partial files of a type together", the approved target): a file that declares only that
/// top-level type counts all its lines, a file that declares several counts the lines each declaration spans. A member is
/// measured from its first attribute or modifier to its last token, so the XML documentation above it is not counted.
/// </summary>
internal static class SizeScanner
{
    /// <summary>The interface every GUI window definition implements; its members are layout.</summary>
    public const string LayoutInterface = "IGuiWindowDefinition";

    public static IReadOnlyList<TypeSizeFact> Types(ParsedProject server, ExemptionList exemptions)
    {
        var layoutTypes = LayoutTypes(server);
        // Lines of the files an exemption covers (a generated partial) are kept apart: they count only when every file of the
        // type is exempt, so the report can show the exempt type's size.
        var types = new Dictionary<string, (int Measured, int Exempt, SortedSet<string> Files, Exemption? FileExemption)>(StringComparer.Ordinal);

        foreach (var tree in server.Trees)
        {
            var fileExemption = exemptions.ForFile(tree, ExemptionList.TypeSize);
            var declarations = SyntaxNames.TopLevelTypes(tree.GetRoot()).ToList();
            foreach (var declaration in declarations)
            {
                var name = SyntaxNames.FullName(declaration);
                var span = declarations.Count == 1 ? SyntaxNames.FileLines(tree) : SyntaxNames.Lines(declaration);
                var lines = span - ExemptMemberLines(declaration, exemptions);
                var current = types.TryGetValue(name, out var found) ? found : (0, 0, new SortedSet<string>(StringComparer.Ordinal), null);
                if (fileExemption is null)
                    current.Files.Add(tree.FilePath);
                types[name] = fileExemption is null
                    ? (current.Measured + lines, current.Exempt, current.Files, current.FileExemption)
                    : (current.Measured, current.Exempt + lines, current.Files, current.FileExemption ?? fileExemption);
            }
        }

        return types
            .Select(pair =>
            {
                var allExempt = pair.Value.Files.Count == 0;
                return new TypeSizeFact(
                    pair.Key,
                    allExempt ? pair.Value.Exempt : pair.Value.Measured,
                    pair.Value.Files.ToList(),
                    layoutTypes.Contains(pair.Key),
                    exemptions.ForType(pair.Key, ExemptionList.TypeSize) ?? (allExempt ? pair.Value.FileExemption : null));
            })
            .OrderBy(fact => fact.Type, StringComparer.Ordinal)
            .ToList();
    }

    public static IReadOnlyList<MemberSizeFact> Members(ParsedProject server, ExemptionList exemptions, int methodLimit, int layoutLimit)
    {
        var layoutTypes = LayoutTypes(server);
        var declarations = server.Trees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Select(type => (Tree: tree, Type: type)))
            .ToList();

        var members = declarations
            .SelectMany(declaration => declaration.Type.Members
                .Where(IsMeasured)
                .Select(member => (declaration.Tree, TypeName: SyntaxNames.FullName(declaration.Type), Member: member, Name: SyntaxNames.MemberName(member)!,
                                   TopLevel: SyntaxNames.FullName(SyntaxNames.TopLevelType(declaration.Type)!))))
            .ToList();
        var overloaded = members
            .GroupBy(member => (member.TypeName, member.Name))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();

        return members
            .Select(member =>
            {
                var key = $"{member.TypeName}.{member.Name}" +
                          (overloaded.Contains((member.TypeName, member.Name)) ? SyntaxNames.ParameterTypes(member.Member) : string.Empty);
                var exemption = exemptions.ForMember(member.TypeName, member.Name, ExemptionList.MethodSize)
                                ?? exemptions.ForType(member.TypeName, ExemptionList.MethodSize)
                                ?? exemptions.ForType(member.TopLevel, ExemptionList.MethodSize)
                                ?? exemptions.ForFile(member.Tree, ExemptionList.MethodSize);
                var limit = layoutTypes.Contains(member.TopLevel) ? layoutLimit : methodLimit;
                return new MemberSizeFact(key, member.TypeName, member.Tree.FilePath, SyntaxNames.Lines(member.Member), limit, exemption);
            })
            .OrderBy(fact => fact.Key, StringComparer.Ordinal)
            .ThenBy(fact => fact.File, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Methods, constructors, destructors, operators, properties, indexers and events with accessors. Fields and nested types are not members here.</summary>
    private static bool IsMeasured(MemberDeclarationSyntax member) =>
        member is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax;

    /// <summary>The top-level types that implement the GUI window definition interface in any of their declarations.</summary>
    private static HashSet<string> LayoutTypes(ParsedProject server) =>
        server.Trees
            .SelectMany(tree => SyntaxNames.TopLevelTypes(tree.GetRoot()))
            .Where(type => SyntaxNames.ListsBase(type, LayoutInterface))
            .Select(SyntaxNames.FullName)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>The lines of the members of a declaration (nested types included) that an exemption takes out of their type's size.</summary>
    private static int ExemptMemberLines(BaseTypeDeclarationSyntax declaration, ExemptionList exemptions)
    {
        var lines = 0;
        foreach (var type in declaration.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
        {
            var typeName = SyntaxNames.FullName(type);
            foreach (var member in type.Members)
            {
                var names = member is FieldDeclarationSyntax field
                    ? field.Declaration.Variables.Select(variable => variable.Identifier.ValueText)
                    : SyntaxNames.MemberName(member) is { } name ? [name] : [];
                if (names.Any(name => exemptions.ForMember(typeName, name, ExemptionList.TypeSize) != null))
                    lines += SyntaxNames.Lines(member);
            }
        }

        return lines;
    }
}
