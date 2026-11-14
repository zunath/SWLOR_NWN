using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// The "sink does no work" measure (owner decision 12, P5-1b): <c>NativeSinks</c> counts as wiring in the domain order only
/// because a sink forwards a hook to the code that handles it, as a trampoline forwards to its sink. Read from syntax, so it
/// does not depend on the build configuration.
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
/// <item>an instance constructor whose body only assigns its parameters to fields.</item>
/// </list>
/// <para>Every other member is work and counts with all its lines (<see cref="SyntaxNames.Lines"/>): bodies that branch, catch,
/// log, call the engine function or anything else, fields with an initializer (state), static fields, properties, nested
/// types and helper methods.</para>
/// </remarks>
internal static class SinkWorkScanner
{
    public const string SinkNamespace = "SWLOR.Game.Server.NativeSinks";
    public const string FactoryType = SinkNamespace + ".NativeSinkFactory";

    public static IReadOnlyList<SinkWork> Scan(ParsedProject server)
    {
        var types = new SortedDictionary<string, Measured>(StringComparer.Ordinal);
        foreach (var tree in server.Trees)
        {
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

                foreach (var member in type.Members)
                {
                    if (DoesNoWork(member))
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

    private static bool DoesNoWork(MemberDeclarationSyntax member) => member switch
    {
        MethodDeclarationSyntax method => IsForwardingMethod(method),
        FieldDeclarationSyntax field => field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) && !field.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                                        field.Declaration.Variables.All(variable => variable.Initializer is null),
        ConstructorDeclarationSyntax constructor => IsAssigningConstructor(constructor),
        _ => false
    };

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

    private static bool IsAssigningConstructor(ConstructorDeclarationSyntax constructor)
    {
        if (constructor.Modifiers.Any(SyntaxKind.StaticKeyword) || constructor.Initializer != null || constructor.ExpressionBody != null || constructor.Body is null)
            return false;

        var parameters = constructor.ParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        return constructor.Body.Statements.All(statement =>
            statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment } &&
            assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
            assignment.Left is IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax } &&
            assignment.Right is IdentifierNameSyntax value && parameters.Contains(value.Identifier.ValueText));
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
