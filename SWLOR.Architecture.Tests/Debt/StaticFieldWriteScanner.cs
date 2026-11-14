using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// Tells static fields that hold runtime state from tables that are only built once: a field is written when code other
/// than its own initializer and its type's static constructor assigns it (<c>=</c>, <c>+=</c>, <c>??=</c>, <c>++</c>, <c>ref</c>
/// or <c>out</c>), assigns one of its elements or members (<c>_stores[id] = ...</c>), or calls a mutating method on it
/// (<c>Add</c>, <c>Remove</c>, <c>Clear</c>, <c>TryAdd</c>, <c>Enqueue</c>, ...). Names are bound like the compiler binds them.
/// Information for the report: a table filled by a helper the initializer calls counts as written, and a collection
/// changed only through a method it is passed to does not.
/// </summary>
internal static class StaticFieldWriteScanner
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.Ordinal)
    {
        "Add", "AddFirst", "AddLast", "AddOrUpdate", "AddRange", "Clear", "Dequeue", "Enqueue", "ExceptWith", "GetOrAdd",
        "Insert", "InsertRange", "IntersectWith", "Pop", "Push", "Remove", "RemoveAll", "RemoveAt", "RemoveFirst", "RemoveLast",
        "RemoveRange", "RemoveWhere", "Reset", "Restart", "Reverse", "Set", "Sort", "Start", "Stop", "SymmetricExceptWith",
        "TryAdd", "TryDequeue", "TryPop", "TryRemove", "TryTake", "TryUpdate", "UnionWith"
    };

    /// <summary>The fields of <paramref name="fields"/> that code writes outside their initializer and static constructor.</summary>
    public static IReadOnlySet<StaticFieldFact> Written(SourceSnapshot snapshot, IReadOnlyCollection<StaticFieldFact> fields)
    {
        var compilation = snapshot.CompilationOf(snapshot.Server);
        var declarations = snapshot.Server.Trees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().Select(type => (Tree: tree, Type: type)))
            .GroupBy(pair => IlName(pair.Type))
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var textOf = snapshot.Server.Trees.ToDictionary(tree => tree, tree => tree.GetText().ToString());
        var treesOfTopLevel = declarations.Values
            .SelectMany(list => list)
            .GroupBy(pair => TopLevelIlName(pair.Type))
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Tree).Distinct().ToList(), StringComparer.Ordinal);

        var written = new HashSet<StaticFieldFact>();
        foreach (var field in fields)
        {
            if (!declarations.TryGetValue($"{field.Owner.Namespace}|{field.Declaring}", out var types))
                continue;

            var symbol = DeclaredSymbol(compilation, types, field.Name);
            if (symbol is null)
                continue;

            var topLevel = field.Owner.ToString();
            var searched = symbol.DeclaredAccessibility == Accessibility.Private
                ? treesOfTopLevel.GetValueOrDefault(topLevel) ?? []
                : snapshot.Server.Trees;

            foreach (var tree in searched.Where(tree => textOf[tree].Contains(field.Name, StringComparison.Ordinal)))
            {
                if (IsWrittenIn(compilation, tree, symbol))
                {
                    written.Add(field);
                    break;
                }
            }
        }

        return written;
    }

    private static bool IsWrittenIn(CSharpCompilation compilation, SyntaxTree tree, ISymbol field)
    {
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
        foreach (var name in tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == field.Name))
        {
            if (!IsWriteContext(name) || IsInitialization(name, field))
                continue;

            var info = model.GetSymbolInfo(name);
            if (SymbolEqualityComparer.Default.Equals(info.Symbol?.OriginalDefinition, field))
                return true;
        }

        return false;
    }

    /// <summary>True when the name is assigned, incremented, passed by reference, or has an element or member assigned or a mutating method called.</summary>
    private static bool IsWriteContext(IdentifierNameSyntax name)
    {
        // The field itself: X, Type.X or this-less X.
        ExpressionSyntax field = name.Parent is MemberAccessExpressionSyntax qualified && qualified.Name == name ? qualified : name;
        var parent = field.Parent;

        switch (parent)
        {
            case AssignmentExpressionSyntax assignment when assignment.Left == field:
            case PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.PreIncrementExpression or (int)SyntaxKind.PreDecrementExpression }:
            case PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.PostIncrementExpression or (int)SyntaxKind.PostDecrementExpression }:
                return true;
            case ArgumentSyntax argument when !argument.RefKindKeyword.IsKind(SyntaxKind.None) && !argument.RefKindKeyword.IsKind(SyntaxKind.InKeyword):
                return true;
            case ElementAccessExpressionSyntax element when element.Expression == field:
                return element.Parent is AssignmentExpressionSyntax elementAssignment && elementAssignment.Left == element;
            case MemberAccessExpressionSyntax member when member.Expression == field:
                return member.Parent is InvocationExpressionSyntax invocation && invocation.Expression == member && MutatingMethods.Contains(member.Name.Identifier.ValueText)
                       || member.Parent is AssignmentExpressionSyntax memberAssignment && memberAssignment.Left == member;
            case ConditionalAccessExpressionSyntax conditional when conditional.Expression == field:
                return conditional.WhenNotNull is InvocationExpressionSyntax { Expression: MemberBindingExpressionSyntax binding } && MutatingMethods.Contains(binding.Name.Identifier.ValueText);
            default:
                return false;
        }
    }

    /// <summary>True inside the field's own initializer or a static constructor of the type that declares it.</summary>
    private static bool IsInitialization(SyntaxNode node, ISymbol field)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case VariableDeclaratorSyntax declarator when declarator.Identifier.ValueText == field.Name && declarator.Parent?.Parent is FieldDeclarationSyntax:
                    return true;
                case PropertyDeclarationSyntax property when property.Identifier.ValueText == field.Name:
                    return property.Initializer?.Span.Contains(node.Span) == true;
                case ConstructorDeclarationSyntax constructor when constructor.Modifiers.Any(SyntaxKind.StaticKeyword):
                    return constructor.Parent is TypeDeclarationSyntax type && type.Identifier.ValueText == field.ContainingType.Name;
                case BaseTypeDeclarationSyntax:
                    return false;
            }
        }

        return false;
    }

    private static ISymbol? DeclaredSymbol(CSharpCompilation compilation, IReadOnlyList<(SyntaxTree Tree, TypeDeclarationSyntax Type)> types, string name)
    {
        foreach (var (tree, type) in types)
        {
            foreach (var member in type.Members)
            {
                SyntaxNode? declared = member switch
                {
                    FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault(variable => variable.Identifier.ValueText == name),
                    PropertyDeclarationSyntax property when property.Identifier.ValueText == name => property,
                    EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables.FirstOrDefault(variable => variable.Identifier.ValueText == name),
                    _ => null
                };
                if (declared != null)
                    return compilation.GetSemanticModel(tree, ignoreAccessibility: true).GetDeclaredSymbol(declared);
            }
        }

        return null;
    }

    /// <summary>A type declaration named the way the IL scanner names it: <c>Namespace|Outer+Nested</c>, without type parameters.</summary>
    private static string IlName(TypeDeclarationSyntax type)
    {
        var names = type.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().Select(declaration => declaration.Identifier.ValueText).Reverse();
        return $"{Namespace(type)}|{string.Join("+", names)}";
    }

    private static string TopLevelIlName(TypeDeclarationSyntax type) =>
        $"{Namespace(type)}.{SyntaxNames.TopLevelType(type)!.Identifier.ValueText}";

    private static string Namespace(SyntaxNode node) =>
        string.Join(".", node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(space => space.Name.ToString()).Reverse());
}
