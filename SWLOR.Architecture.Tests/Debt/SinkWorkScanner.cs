using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// The "sink does no work" measure (owner decision 12): <c>NativeSinks</c> counts as wiring in the domain order only
/// because a sink forwards a hook to the code that handles it, as a trampoline forwards to its sink. Read from source (syntax,
/// and the compiler's binding for what a constructor assigns), so it does not depend on the build configuration.
/// </summary>
/// <remarks>
/// <para>Every type of the sink namespace is measured except <see cref="FactoryType"/>, which only creates the sinks. A member
/// does no work when it is</para>
/// <list type="bullet">
/// <item>a method without a body (an interface or abstract member);</item>
/// <item>a method whose body is one call, written as an expression body, <c>return call;</c> or <c>call;</c>, whose target is a
/// name or a chain of member names (<c>_service.Handle</c>, <c>Native.Rules.Resolve</c>) and whose arguments are the method's own
/// parameters, each passed as written;</item>
/// <item>a <c>readonly</c> instance field without an initializer (what a constructor was given);</item>
/// <item>an instance constructor without a <c>base</c> or <c>this</c> initializer whose body only assigns its own parameters,
/// as written, to instance fields (its type's or inherited); a property (its setter can do work), a static field or an event
/// as the target is work;</item>
/// <item>a primary constructor of a class or struct that passes nothing to its base type (its parameters are only captured).</item>
/// </list>
/// <para>Every other member is work and counts with all its lines (<see cref="SyntaxNames.Lines"/>): bodies that branch, catch,
/// log, call the engine function or anything else, fields with an initializer (state), static fields, properties, nested
/// types and helper methods. A primary constructor that passes arguments to its base type, like an ordinary constructor with a
/// <c>base</c> initializer, and a record's parameter list (it declares properties) count as <c>.ctor</c> with the lines from
/// the parameter list to the end of the base arguments.</para>
/// </remarks>
internal static class SinkWorkScanner
{
    public const string SinkNamespace = "SWLOR.Game.Server.NativeSinks";
    public const string FactoryType = SinkNamespace + ".NativeSinkFactory";

    public static IReadOnlyList<SinkWork> Scan(SourceSnapshot snapshot)
    {
        var compilation = snapshot.CompilationOf(snapshot.Server);
        var types = new SortedDictionary<string, Measured>(StringComparer.Ordinal);
        foreach (var tree in snapshot.Server.Trees)
        {
            SemanticModel? model = null;
            SemanticModel Model() => model ??= SourceSnapshot.Bind(compilation, tree);
            foreach (var declaration in SyntaxNames.TopLevelTypes(tree.GetRoot()))
            {
                var name = SyntaxNames.FullName(declaration);
                if (!name.StartsWith(SinkNamespace + ".", StringComparison.Ordinal) || name == FactoryType)
                    continue;

                if (!types.TryGetValue(name, out var measured))
                {
                    measured = new Measured();
                    types[name] = measured;
                }

                measured.Files.Add(tree.FilePath);
                if (declaration is not TypeDeclarationSyntax type)
                {
                    // An enum in the sink namespace is not a sink: all of it is work.
                    measured.Lines += SyntaxNames.Lines(declaration);
                    measured.Working.Add(declaration.Identifier.ValueText);
                    continue;
                }

                if (type.ParameterList is { } primary)
                {
                    var baseArguments = type.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>().FirstOrDefault()?.ArgumentList;
                    if (baseArguments is null && type is not RecordDeclarationSyntax)
                        measured.Forwarding++;
                    else
                    {
                        measured.Lines += LinesFrom(primary, baseArguments ?? (SyntaxNode)primary);
                        measured.Working.Add(".ctor");
                    }
                }

                foreach (var member in type.Members)
                {
                    if (DoesNoWork(member, Model))
                    {
                        measured.Forwarding++;
                        continue;
                    }

                    measured.Lines += SyntaxNames.Lines(member);
                    measured.Working.Add(NameOf(member));
                }
            }
        }

        return types
            .Select(pair => new SinkWork(pair.Key, pair.Value.Files.ToList(), pair.Value.Lines, pair.Value.Working, pair.Value.Forwarding))
            .ToList();
    }

    private static bool DoesNoWork(MemberDeclarationSyntax member, Func<SemanticModel> model) => member switch
    {
        MethodDeclarationSyntax method => IsForwardingMethod(method),
        FieldDeclarationSyntax field => field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) && !field.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                                        field.Declaration.Variables.All(variable => variable.Initializer is null),
        ConstructorDeclarationSyntax constructor => IsAssigningConstructor(constructor, model),
        _ => false
    };

    /// <summary>The lines from the start of <paramref name="first"/> to the end of <paramref name="last"/>.</summary>
    private static int LinesFrom(SyntaxNode first, SyntaxNode last)
    {
        var tree = first.SyntaxTree;
        return tree.GetLineSpan(last.Span).EndLinePosition.Line - tree.GetLineSpan(first.Span).StartLinePosition.Line + 1;
    }

    private static bool IsForwardingMethod(MethodDeclarationSyntax method)
    {
        // An interface or abstract member declares and does nothing; an extern one runs native code.
        if (method.Body is null && method.ExpressionBody is null)
            return !method.Modifiers.Any(SyntaxKind.ExternKeyword);

        var expression = method.ExpressionBody?.Expression ?? (method.Body?.Statements is [var statement] ? statement switch
        {
            ReturnStatementSyntax returned => returned.Expression,
            ExpressionStatementSyntax plain => plain.Expression,
            _ => null
        } : null);
        if (expression is not InvocationExpressionSyntax call || !IsNameChain(call.Expression))
            return false;

        var parameters = method.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        return call.ArgumentList.Arguments.All(argument => argument.NameColon is null && argument.Expression is IdentifierNameSyntax name && parameters.Contains(name.Identifier.ValueText));
    }

    private static bool IsNameChain(ExpressionSyntax expression) => expression switch
    {
        SimpleNameSyntax => true,
        MemberAccessExpressionSyntax access => access.IsKind(SyntaxKind.SimpleMemberAccessExpression) && IsNameChain(access.Expression),
        AliasQualifiedNameSyntax => true,
        _ => false
    };

    private static bool IsAssigningConstructor(ConstructorDeclarationSyntax constructor, Func<SemanticModel> model)
    {
        if (constructor.Modifiers.Any(SyntaxKind.StaticKeyword) || constructor.Initializer != null || constructor.ExpressionBody != null || constructor.Body is null)
            return false;

        foreach (var statement in constructor.Body.Statements)
        {
            if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment } ||
                !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) ||
                assignment.Left is not (IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax, Name: IdentifierNameSyntax }) ||
                assignment.Right is not IdentifierNameSyntax value)
            {
                return false;
            }

            // Bound, not read from the names: the target must be an instance field (a property's setter can do work, a static
            // field is shared state), and the value one of this constructor's own parameters.
            var bound = model();
            if (bound.GetSymbolInfo(assignment.Left).Symbol is not IFieldSymbol { IsStatic: false, IsConst: false } ||
                bound.GetSymbolInfo(value).Symbol is not IParameterSymbol parameter ||
                !constructor.ParameterList.Parameters.Any(declared => SymbolEqualityComparer.Default.Equals(bound.GetDeclaredSymbol(declared), parameter)))
            {
                return false;
            }
        }

        return true;
    }

    private static string NameOf(MemberDeclarationSyntax member) => member switch
    {
        BaseFieldDeclarationSyntax field => field.Declaration.Variables.First().Identifier.ValueText,
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
        _ => SyntaxNames.MemberName(member) ?? member.Kind().ToString()
    };

    private sealed class Measured
    {
        public SortedSet<string> Files { get; } = new(StringComparer.Ordinal);

        public List<string> Working { get; } = new();

        public int Lines { get; set; }

        public int Forwarding { get; set; }
    }
}
