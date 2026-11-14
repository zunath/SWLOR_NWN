using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// The reviewed exemptions of the Phase 5 debt measures, read from <c>Baseline/Phase5/Exemptions.json</c>: what the size and
/// services measures skip (owner decision 1) and the static entry points the owner approved (owner decision 3).
/// </summary>
internal sealed class ExemptionList
{
    public const string TypeSize = "typeSize";
    public const string MethodSize = "methodSize";
    public const string ServicesPerType = "servicesPerType";

    private const string FolderMatch = "folder";
    private const string FileMatch = "file";
    private const string TypeMatch = "type";
    private const string MemberMatch = "member";
    private const string GeneratedHeaderMatch = "generatedHeader";

    private static readonly string[] Matches = [FolderMatch, FileMatch, TypeMatch, MemberMatch, GeneratedHeaderMatch];
    private static readonly string[] KnownMeasures = [TypeSize, MethodSize, ServicesPerType];

    private readonly List<string> _problems = new();

    private ExemptionList(IReadOnlyList<Exemption> exemptions, IReadOnlyList<ApprovedEntryPoint> approved)
    {
        Exemptions = exemptions;
        ApprovedEntryPoints = approved;
    }

    public IReadOnlyList<Exemption> Exemptions { get; }

    public IReadOnlyList<ApprovedEntryPoint> ApprovedEntryPoints { get; }

    /// <summary>Mistakes in the file itself: an unknown match or measure, a missing reason, an entry listed twice.</summary>
    public IReadOnlyList<string> Problems => _problems;

    public static ExemptionList Empty { get; } = new([], []);

    public static ExemptionList Load(string path) => Parse(File.ReadAllText(path));

    public static ExemptionList Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var root = document.RootElement;

        var exemptions = root.GetProperty("exemptions").EnumerateArray()
            .Select(element => new Exemption(
                Text(element, "match"),
                Text(element, "value"),
                element.TryGetProperty("measures", out var measures) ? measures.EnumerateArray().Select(m => m.GetString() ?? string.Empty).ToList() : [],
                Text(element, "category"),
                Text(element, "reason")))
            .ToList();
        var approved = root.GetProperty("approvedStaticEntryPoints").EnumerateArray()
            .Select(element => new ApprovedEntryPoint(Text(element, "type"), Text(element, "reason")))
            .ToList();

        var list = new ExemptionList(exemptions, approved);
        list.Validate();
        return list;
    }

    /// <summary>The exemption that takes a whole file out of <paramref name="measure"/>: its folder, the file, or a generated header.</summary>
    public Exemption? ForFile(SyntaxTree tree, string measure) =>
        Exemptions.FirstOrDefault(exemption => exemption.Measures.Contains(measure) && exemption.Match switch
        {
            FolderMatch => tree.FilePath.StartsWith(exemption.Value, StringComparison.Ordinal),
            FileMatch => tree.FilePath == exemption.Value,
            GeneratedHeaderMatch => Header(tree).Contains(exemption.Value, StringComparison.Ordinal),
            _ => false
        });

    /// <summary>The exemption that takes a type out of <paramref name="measure"/>.</summary>
    public Exemption? ForType(string typeName, string measure) =>
        Exemptions.FirstOrDefault(exemption => exemption.Measures.Contains(measure) && exemption.Match == TypeMatch && exemption.Value == typeName);

    /// <summary>The exemption that takes a member out of <paramref name="measure"/> (and its lines out of its type's size).</summary>
    public Exemption? ForMember(string typeName, string memberName, string measure) =>
        Exemptions.FirstOrDefault(exemption => exemption.Measures.Contains(measure) && exemption.Match == MemberMatch && exemption.Value == typeName + "." + memberName);

    public ApprovedEntryPoint? Approval(string typeName) => ApprovedEntryPoints.FirstOrDefault(approved => approved.Type == typeName);

    /// <summary>
    /// Exemptions and approvals that name nothing in the server, so a rename or deletion cannot leave one behind silently:
    /// a folder or file without source, a generated header no file has, a type or member that is not declared.
    /// </summary>
    public IReadOnlyList<string> Unmatched(ParsedProject server)
    {
        var typeNames = new HashSet<string>(StringComparer.Ordinal);
        var memberNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tree in server.Trees)
        {
            foreach (var type in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                var name = SyntaxNames.FullName(type);
                typeNames.Add(name);
                if (type is TypeDeclarationSyntax withMembers)
                {
                    foreach (var member in withMembers.Members)
                    {
                        if (SyntaxNames.MemberName(member) is { } memberName)
                            memberNames.Add(name + "." + memberName);
                        if (member is FieldDeclarationSyntax field)
                            memberNames.UnionWith(field.Declaration.Variables.Select(variable => name + "." + variable.Identifier.ValueText));
                    }
                }
            }
        }

        bool Matches(Exemption exemption) => exemption.Match switch
        {
            FolderMatch => server.Trees.Any(tree => tree.FilePath.StartsWith(exemption.Value, StringComparison.Ordinal)),
            FileMatch => server.Trees.Any(tree => tree.FilePath == exemption.Value),
            GeneratedHeaderMatch => server.Trees.Any(tree => Header(tree).Contains(exemption.Value, StringComparison.Ordinal)),
            TypeMatch => typeNames.Contains(exemption.Value),
            MemberMatch => memberNames.Contains(exemption.Value),
            _ => true
        };

        return Exemptions.Where(exemption => !Matches(exemption)).Select(exemption => $"exemption {exemption} names nothing in the server")
            .Concat(ApprovedEntryPoints.Where(approved => !typeNames.Contains(approved.Type)).Select(approved => $"approved entry point {approved.Type} is not declared in the server"))
            .ToList();
    }

    /// <summary>The text of a file before its first declaration: usings and header comments.</summary>
    public static string Header(SyntaxTree tree)
    {
        var root = (CompilationUnitSyntax)tree.GetRoot();
        var end = root.Members.Count > 0 ? root.Members[0].SpanStart : root.FullSpan.End;
        return tree.GetText().ToString(new Microsoft.CodeAnalysis.Text.TextSpan(0, end));
    }

    private void Validate()
    {
        foreach (var exemption in Exemptions)
        {
            if (!Matches.Contains(exemption.Match))
                _problems.Add($"{exemption}: match must be one of {string.Join(", ", Matches)}.");
            if (exemption.Value.Length == 0)
                _problems.Add($"{exemption}: value is empty.");
            if (exemption.Match == FolderMatch && !exemption.Value.EndsWith('/'))
                _problems.Add($"{exemption}: a folder ends with '/'.");
            if (exemption.Measures.Count == 0 || exemption.Measures.Any(measure => !KnownMeasures.Contains(measure)))
                _problems.Add($"{exemption}: measures must be some of {string.Join(", ", KnownMeasures)}.");
            if (exemption.Category.Length == 0 || exemption.Reason.Length == 0)
                _problems.Add($"{exemption}: every exemption needs a category and a reason.");
        }

        foreach (var duplicate in Exemptions.GroupBy(exemption => exemption.ToString()).Where(group => group.Count() > 1))
            _problems.Add($"{duplicate.Key} is listed {duplicate.Count()} times.");

        foreach (var approved in ApprovedEntryPoints.Where(approved => approved.Type.Length == 0 || approved.Reason.Length == 0))
            _problems.Add($"approved entry point '{approved.Type}' needs a type and a reason.");
    }

    private static string Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
