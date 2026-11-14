using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace SWLOR.ContentInjection;

/// <summary>A <c>.cs</c> file of the tree with what its project is, parsed on first use.</summary>
internal sealed class TreeFile
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview, DocumentationMode.None);

    private SyntaxNode? _root;

    public TreeFile(string relativePath, ProjectKind kind, bool importsGameServicesGlobally, SourceFile source)
    {
        RelativePath = relativePath;
        Kind = kind;
        ImportsGameServicesGlobally = importsGameServicesGlobally;
        Source = source;
    }

    /// <summary>Relative to the root, with forward slashes.</summary>
    public string RelativePath { get; }

    public ProjectKind Kind { get; }

    /// <summary>True when the project's <c>GlobalUsings.cs</c> imports the <c>GameServices</c> properties.</summary>
    public bool ImportsGameServicesGlobally { get; }

    public SourceFile Source { get; }

    public string Text => Source.Text;

    public SyntaxNode Root => _root ??= Parse(Text);

    /// <summary>True for a server file in <c>Domain/</c> or <c>Composition/</c>: a class the container builds, which takes <c>_content</c> by hand.</summary>
    public bool IsServiceCode =>
        Kind == ProjectKind.Server &&
        (RelativePath.StartsWith("SWLOR.Game.Server/Domain/", StringComparison.Ordinal) ||
         RelativePath.StartsWith("SWLOR.Game.Server/Composition/", StringComparison.Ordinal));

    public static SyntaxNode Parse(string text) => CSharpSyntaxTree.ParseText(text, ParseOptions).GetRoot();

    /// <summary>The 1-based line of <paramref name="position"/>.</summary>
    public int LineOf(int position) => Root.SyntaxTree.GetLineSpan(new TextSpan(position, 0)).StartLinePosition.Line + 1;

    public Refusal Refuse(SyntaxNode node, string reason) => new(RelativePath, LineOf(node.SpanStart), reason);
}
