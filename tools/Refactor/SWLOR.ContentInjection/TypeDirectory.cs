using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.ContentInjection;

/// <summary>
/// Where the server's types are declared, by simple name, and which namespaces a file sees, so a value type taken from a made type's
/// constructor is written where the creator can name it (<c>Content.Create&lt;X, ItemType&gt;</c> or the full name).
/// </summary>
internal sealed class TypeDirectory
{
    private readonly Dictionary<string, HashSet<string>> _namespaces = new(StringComparer.Ordinal);

    public TypeDirectory(IEnumerable<TreeFile> serverFiles)
    {
        foreach (var file in serverFiles)
        {
            foreach (var type in file.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Where(type => type.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax))
                Add(type.Identifier.ValueText, SyntaxHelpers.NamespaceOf(type));
            foreach (var type in file.Root.DescendantNodes().OfType<DelegateDeclarationSyntax>().Where(type => type.Parent is BaseNamespaceDeclarationSyntax or CompilationUnitSyntax))
                Add(type.Identifier.ValueText, SyntaxHelpers.NamespaceOf(type));
        }
    }

    /// <summary>The namespaces <paramref name="file"/> sees: its usings and the namespaces it is declared in, with their parents.</summary>
    public static HashSet<string> VisibleNamespaces(TreeFile file)
    {
        var visible = new HashSet<string>(StringComparer.Ordinal);
        foreach (var directive in file.Root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive.Alias == null && directive.StaticKeyword.ValueText.Length == 0 && directive.Name != null)
                visible.Add(directive.Name.ToString());
        }

        foreach (var space in file.Root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
        {
            var outer = SyntaxHelpers.NamespaceOf(space);
            var name = outer.Length == 0 ? space.Name.ToString() : outer + "." + space.Name;
            for (; name.Length > 0; name = name.Contains('.') ? name[..name.LastIndexOf('.')] : string.Empty)
                visible.Add(name);
        }

        return visible;
    }

    /// <summary>
    /// <paramref name="typeText"/>, a type written in <paramref name="declaringFile"/>, as <paramref name="creator"/> must write it: as is
    /// for a predefined or generic type or one the creator sees, else with the namespace the declaring file resolves it to.
    /// </summary>
    /// <returns>Null when the type cannot be told apart from another of its name.</returns>
    public string? Qualify(string typeText, TreeFile declaringFile, TreeFile creator)
    {
        if (SyntaxHelpers.IsPredefined(typeText) || !IsSimple(typeText.TrimEnd('?', '[', ']')))
            return typeText;

        var full = FullName(typeText.TrimEnd('?', '[', ']'), declaringFile);
        if (full == null)
            return null;

        var space = full[..Math.Max(0, full.LastIndexOf('.'))];
        return space.Length == 0 || VisibleNamespaces(creator).Contains(space) ? typeText : space + "." + typeText;
    }

    /// <summary>The full name of the simple type <paramref name="name"/> as <paramref name="file"/> sees it, or null when it is unknown or ambiguous.</summary>
    public string? FullName(string name, TreeFile file)
    {
        if (!_namespaces.TryGetValue(name, out var spaces))
            return null;

        var visible = VisibleNamespaces(file);
        var seen = spaces.Where(visible.Contains).ToArray();
        if (seen.Length == 1)
            return seen[0] + "." + name;
        return seen.Length == 0 && spaces.Count == 1 ? spaces.Single() + "." + name : null;
    }

    private static bool IsSimple(string text) => text.Length > 0 && text.All(character => char.IsLetterOrDigit(character) || character == '_');

    private void Add(string name, string space)
    {
        if (!_namespaces.TryGetValue(name, out var spaces))
            _namespaces[name] = spaces = new HashSet<string>(StringComparer.Ordinal);
        spaces.Add(space);
    }
}
