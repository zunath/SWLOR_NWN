// The P5-1 domain order extractor (tools/Refactor/DomainOrder/README.md). Compiles SWLOR.Game.Server from source, binds every
// name the way the compiler does (accessibility respected) and writes two files into <out>:
//   refs.tsv  : one line per distinct (file, source top type, source member, target type, mode, target member, in attribute)
//   types.tsv : one line per type declared in the server (nested included) with its shape
// The guard (SWLOR.Architecture.Tests, Debt/SourceReferenceScanner.cs) records the same references; keep the two in step.
// Usage: dotnet run -c Release --project tools/Refactor/DomainOrder/EdgeExtract -- <repository root> <out> [configuration]
// The server must be built in <configuration> (default Release): its bin folder supplies the package references.
using System.Collections.Concurrent;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: EdgeExtract <repository root> <out folder> [configuration]");
    return 2;
}

var repo = Path.GetFullPath(args[0]);
var outDir = Path.GetFullPath(args[1]);
var configuration = args.Length > 2 ? args[2] : "Release";
var serverDir = Path.Combine(repo, "SWLOR.Game.Server");
var binDir = Path.Combine(serverDir, "bin", configuration, "net10.0");
if (!Directory.Exists(binDir))
{
    Console.Error.WriteLine($"{binDir} does not exist: build SWLOR.Game.Server in {configuration} first.");
    return 2;
}

Directory.CreateDirectory(outDir);

var separator = Path.DirectorySeparatorChar;
var files = Directory.EnumerateFiles(serverDir, "*.cs", SearchOption.AllDirectories)
    .Where(f => !f.Contains(separator + "bin" + separator) && !f.Contains(separator + "obj" + separator))
    .OrderBy(f => f, StringComparer.Ordinal).ToList();
// The server is LangVersion 10 and has no #if on the configuration, so the trees are the same in Debug and Release.
var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp10, preprocessorSymbols: ["TRACE", "RELEASE"]);
var trees = files.AsParallel().AsOrdered().Select(f =>
{
    using var s = File.OpenRead(f);
    return CSharpSyntaxTree.ParseText(SourceText.From(s), parseOptions, Path.GetRelativePath(repo, f).Replace('\\', '/'));
}).ToList();

var refs = new Dictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);
foreach (var p in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator))
    refs.TryAdd(Path.GetFileName(p), MetadataReference.CreateFromFile(p));
foreach (var p in Directory.EnumerateFiles(binDir, "*.dll"))
{
    var n = Path.GetFileName(p);
    if (n.Equals("SWLOR.Game.Server.dll", StringComparison.OrdinalIgnoreCase)) continue;
    if (!HasMetadata(p)) continue;
    refs[n] = MetadataReference.CreateFromFile(p);
}

var compilation = CSharpCompilation.Create("SWLOR.Game.Server", trees, refs.Values,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)
        .WithMetadataImportOptions(MetadataImportOptions.All).WithConcurrentBuild(true));
var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
Console.WriteLine($"files {trees.Count}, compile errors {errors.Count}");
foreach (var e in errors.Take(10)) Console.WriteLine("  " + e);

var assembly = compilation.Assembly;
var lines = new ConcurrentBag<string>();
Parallel.ForEach(trees, tree =>
{
    // Accessibility respected, as the compiler binds: ignoring it lets internal framework types (System.Currency) clash.
    var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);
    var seen = new HashSet<string>(StringComparer.Ordinal);
    foreach (var node in tree.GetRoot().DescendantNodes())
    {
        switch (node)
        {
            case SimpleNameSyntax name:
                HandleName(model, name, seen);
                break;
            case ImplicitObjectCreationExpressionSyntax:
                if (model.GetSymbolInfo(node).Symbol is IMethodSymbol ctor)
                    Add(node, ctor.ContainingType, "new", ".ctor", seen);
                break;
            case ElementAccessExpressionSyntax:
                if (model.GetSymbolInfo(node).Symbol is IPropertySymbol indexer)
                    Add(node, indexer.ContainingType, "member", "this[]", seen);
                break;
            case AnonymousFunctionExpressionSyntax:
                if (model.GetSymbolInfo(node).Symbol is IMethodSymbol fn)
                {
                    foreach (var p in fn.Parameters) AddTypeDeep(node, p.Type, "lambda", seen);
                    AddTypeDeep(node, fn.ReturnType, "lambda", seen);
                }
                break;
            case ForEachStatementSyntax forEach:
                if (model.GetForEachStatementInfo(forEach).ElementType is { } et) AddTypeDeep(node, et, "lambda", seen);
                break;
        }
    }
    foreach (var l in seen) lines.Add(l);
});

var sb = new StringBuilder();
sb.AppendLine("file\tfromNs\tfromTop\tmember\ttoNs\ttoTop\ttoType\tmode\ttoMember\tinAttribute");
foreach (var l in lines.OrderBy(x => x, StringComparer.Ordinal)) sb.AppendLine(l);
File.WriteAllText(Path.Combine(outDir, "refs.tsv"), sb.ToString());
Console.WriteLine($"refs {lines.Count}");

var tsb = new StringBuilder();
tsb.AppendLine("ns\ttop\ttype\tkind\trecord\tstatic\tabstract\tsealed\tattribute\tbases\tinterfaces\tmethods\tprops\tconstFields\tstaticReadonly\tstaticMutable\tinstanceFields\tevents\tfiles\tlines");
foreach (var t in AllTypes(assembly.GlobalNamespace).Where(t => SymbolEqualityComparer.Default.Equals(t.ContainingAssembly, assembly)))
{
    var top = t; while (top.ContainingType is { } c) top = c;
    var bases = new List<string>();
    for (var b = t.BaseType; b is not null; b = b.BaseType)
        bases.Add(SymbolEqualityComparer.Default.Equals(b.ContainingAssembly, assembly) ? Full(b.OriginalDefinition) : b.OriginalDefinition.ToDisplayString());
    var ifaces = t.AllInterfaces.Select(i => Full(i.OriginalDefinition)).Distinct().ToList();
    var members = t.GetMembers();
    int methods = members.OfType<IMethodSymbol>().Count(m => m.MethodKind is MethodKind.Ordinary or MethodKind.UserDefinedOperator or MethodKind.Conversion && !m.IsImplicitlyDeclared
        && !(m.IsOverride && m.Name is "ToString" or "Equals" or "GetHashCode"));
    int props = members.OfType<IPropertySymbol>().Count(p => !p.IsImplicitlyDeclared);
    var fields = members.OfType<IFieldSymbol>().Where(f => !f.IsImplicitlyDeclared && f.AssociatedSymbol is null).ToList();
    int consts = fields.Count(f => f.IsConst);
    int sro = fields.Count(f => f.IsStatic && f.IsReadOnly && !f.IsConst);
    int smut = fields.Count(f => f.IsStatic && !f.IsReadOnly && !f.IsConst);
    int inst = fields.Count(f => !f.IsStatic);
    int evs = members.OfType<IEventSymbol>().Count();
    var fileList = string.Join(";", t.Locations.Where(l => l.IsInSource).Select(l => l.SourceTree!.FilePath).Distinct());
    int lineCount = t.DeclaringSyntaxReferences.Sum(r => { var span = r.SyntaxTree.GetLineSpan(r.Span); return span.EndLinePosition.Line - span.StartLinePosition.Line + 1; });
    bool isAttr = false; for (var b = t.BaseType; b is not null; b = b.BaseType) if (b.ToDisplayString() == "System.Attribute") isAttr = true;
    tsb.Append(top.ContainingNamespace.ToDisplayString()).Append('\t').Append(top.MetadataName.Split('`')[0]).Append('\t').Append(Full(t)).Append('\t')
       .Append(t.TypeKind).Append('\t').Append(t.IsRecord).Append('\t').Append(t.IsStatic).Append('\t').Append(t.IsAbstract).Append('\t').Append(t.IsSealed).Append('\t').Append(isAttr).Append('\t')
       .Append(string.Join(";", bases)).Append('\t').Append(string.Join(";", ifaces)).Append('\t')
       .Append(methods).Append('\t').Append(props).Append('\t').Append(consts).Append('\t').Append(sro).Append('\t').Append(smut).Append('\t').Append(inst).Append('\t').Append(evs).Append('\t')
       .Append(fileList).Append('\t').Append(lineCount).AppendLine();
}
File.WriteAllText(Path.Combine(outDir, "types.tsv"), tsb.ToString());
Console.WriteLine("done");
return 0;

void HandleName(SemanticModel model, SimpleNameSyntax name, HashSet<string> seen)
{
    var info = model.GetSymbolInfo(name);
    var symbol = info.Symbol ?? (info.CandidateSymbols.Length == 1 ? info.CandidateSymbols[0] : null);
    if (name.IsVar && symbol is ITypeSymbol inferred)
    {
        // A var has no syntax for what it infers (an array's element type, type arguments), so they come from the inferred
        // type (same rule as the guard's SourceReferenceScanner).
        AddTypeDeep(name, inferred, "type", seen);
        return;
    }

    var inNameof = IsInNameof(model, name);
    switch (symbol)
    {
        case INamedTypeSymbol type:
            // A type that only qualifies a member access is covered by the member (so a constant stays a constant).
            if (IsQualifierOfMember(model, name)) return;
            if (name.Ancestors().Any(a => a is UsingDirectiveSyntax)) return;
            Add(name, type, inNameof ? "nameof" : "type", "", seen);
            break;
        case IMethodSymbol method:
            var m = method.ReducedFrom ?? method;
            Add(name, m.ContainingType, inNameof ? "nameof" : "call", m.MethodKind == MethodKind.Constructor ? ".ctor" : m.Name, seen);
            foreach (var ta in method.TypeArguments) AddTypeDeep(name, ta, "typearg", seen);
            if (m.ContainingType is { IsGenericType: true } ct) foreach (var ta in ct.TypeArguments) AddTypeDeep(name, ta, "typearg", seen);
            break;
        case IFieldSymbol field:
            var mode = field.ContainingType.TypeKind == TypeKind.Enum ? "enum" : field.IsConst ? "const" : "member";
            Add(name, field.ContainingType, inNameof ? "nameof" : mode, field.Name, seen);
            if (field.ContainingType is { IsGenericType: true } fct) foreach (var ta in fct.TypeArguments) AddTypeDeep(name, ta, "typearg", seen);
            break;
        case IPropertySymbol property:
            Add(name, property.ContainingType, inNameof ? "nameof" : "member", property.Name, seen);
            if (property.ContainingType is { IsGenericType: true } pct) foreach (var ta in pct.TypeArguments) AddTypeDeep(name, ta, "typearg", seen);
            break;
        case IEventSymbol ev:
            Add(name, ev.ContainingType, inNameof ? "nameof" : "member", ev.Name, seen);
            break;
    }
}

bool IsQualifierOfMember(SemanticModel model, SimpleNameSyntax name)
{
    SimpleNameSyntax? right = name.Parent switch
    {
        MemberAccessExpressionSyntax ma when ma.Expression == name => ma.Name,
        QualifiedNameSyntax qn when qn.Left == name => qn.Right,
        _ => null
    };
    return right is not null && model.GetSymbolInfo(right).Symbol is { } symbol && symbol is not INamespaceSymbol;
}

bool IsInNameof(SemanticModel model, SyntaxNode node)
{
    foreach (var a in node.Ancestors())
    {
        if (a is InvocationExpressionSyntax inv && inv.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
            && model.GetSymbolInfo(inv).Symbol is null)
            return true;
        if (a is StatementSyntax or MemberDeclarationSyntax) return false;
    }
    return false;
}

void AddTypeDeep(SyntaxNode node, ITypeSymbol type, string mode, HashSet<string> seen)
{
    switch (type)
    {
        case IArrayTypeSymbol array: AddTypeDeep(node, array.ElementType, mode, seen); break;
        case IPointerTypeSymbol pointer: AddTypeDeep(node, pointer.PointedAtType, mode, seen); break;
        case INamedTypeSymbol named:
            Add(node, named, mode, "", seen);
            foreach (var ta in named.TypeArguments) AddTypeDeep(node, ta, mode, seen);
            break;
    }
}

void Add(SyntaxNode node, INamedTypeSymbol? target, string mode, string member, HashSet<string> seen)
{
    if (target is null) return;
    target = target.OriginalDefinition;
    if (!SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, assembly)) return;
    var top = target; while (top.ContainingType is { } c) top = c;

    var (srcNs, srcTop, memberId) = Source(node);
    if (srcTop is null) return;
    var toNs = top.ContainingNamespace.ToDisplayString();
    var toTop = top.MetadataName.Split('`')[0];
    if (srcNs == toNs && srcTop == toTop) return;
    var inAttr = node.Ancestors().Any(a => a is AttributeSyntax);
    seen.Add(string.Join('\t', node.SyntaxTree.FilePath, srcNs, srcTop, memberId, toNs, toTop, Full(target), mode, member, inAttr ? "1" : "0"));
}

// The top-level type a node is in (namespace and name) and the member around it: "Outer+Nested::Member", "(type)" outside members.
(string Ns, string? Top, string Member) Source(SyntaxNode node)
{
    var typeDecls = node.AncestorsAndSelf().Where(a => a is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax).ToList();
    if (typeDecls.Count == 0) return ("", null, "");
    var outer = typeDecls[^1];
    var ns = string.Join(".", outer.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(n => n.Name.ToString()).Reverse());
    var path = string.Join("+", typeDecls.AsEnumerable().Reverse().Select(Identifier));
    var member = "(type)";
    foreach (var a in node.AncestorsAndSelf())
    {
        if (a is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax) break;
        string? found = a switch
        {
            MethodDeclarationSyntax md => md.Identifier.ValueText,
            ConstructorDeclarationSyntax cd => cd.Modifiers.Any(SyntaxKind.StaticKeyword) ? ".cctor" : ".ctor",
            DestructorDeclarationSyntax => "Finalize",
            PropertyDeclarationSyntax pd => pd.Identifier.ValueText,
            IndexerDeclarationSyntax => "Item",
            EventDeclarationSyntax ed => ed.Identifier.ValueText,
            EventFieldDeclarationSyntax ef => ef.Declaration.Variables.First().Identifier.ValueText,
            FieldDeclarationSyntax fd => fd.Declaration.Variables.First().Identifier.ValueText,
            OperatorDeclarationSyntax od => "op_" + od.OperatorToken.ValueText,
            ConversionOperatorDeclarationSyntax => "op_Conversion",
            EnumMemberDeclarationSyntax em => em.Identifier.ValueText,
            _ => null
        };
        if (found is not null) { member = found; break; }
    }
    return (ns, Identifier(outer), path + "::" + member);
}

static string Identifier(SyntaxNode type) => type switch
{
    BaseTypeDeclarationSyntax b => b.Identifier.ValueText,
    DelegateDeclarationSyntax d => d.Identifier.ValueText,
    _ => "?"
};

static string Full(INamedTypeSymbol t) => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted).WithGenericsOptions(SymbolDisplayGenericsOptions.None));

static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol ns)
{
    foreach (var t in ns.GetTypeMembers()) foreach (var x in WithNested(t)) yield return x;
    foreach (var child in ns.GetNamespaceMembers()) foreach (var x in AllTypes(child)) yield return x;
}

static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol t)
{
    yield return t;
    foreach (var n in t.GetTypeMembers()) foreach (var x in WithNested(n)) yield return x;
}

static bool HasMetadata(string path)
{
    try { using var s = File.OpenRead(path); using var r = new PEReader(s); return r.HasMetadata; }
    catch { return false; }
}
