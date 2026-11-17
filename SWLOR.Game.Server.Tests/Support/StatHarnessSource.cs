using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Methods and fields of the stat instance class for the Roslyn harnesses that compile them against their own stand-ins: as
/// static members, with each injected service spelled as the static stand-in the harness declares. They are read through
/// <see cref="StatSource"/>, so a member a split part holds is found in its part file, and a call on another part has lost
/// its receiver as it had before the move.
/// </summary>
internal static class StatHarnessSource
{
    public static IEnumerable<string> Methods(Func<MethodDeclarationSyntax, bool> select) =>
        StatSource.Nodes<MethodDeclarationSyntax>()
            .Where(select)
            .Select(method => MakeStatic(method.WithAttributeLists(default)).ToFullString())
            .Select(Respell);

    /// <summary>The field declarations that declare one of the names <paramref name="select"/> accepts, as they are.</summary>
    public static IEnumerable<FieldDeclarationSyntax> Fields(Func<string, bool> select) =>
        StatSource.Nodes<FieldDeclarationSyntax>()
            .Where(field => field.Declaration.Variables.Any(variable => select(variable.Identifier.ValueText)));

    private static string Respell(string source) => source
        .Replace("_db.", "DB.")
        .Replace("_combatRecorder.", "CombatRecorder.")
        .Replace("_playerFeedback.", "PlayerFeedback.")
        .Replace("_companions.", "BeastMastery.");

    private static MethodDeclarationSyntax MakeStatic(MethodDeclarationSyntax method) =>
        method.Modifiers.Any(SyntaxKind.StaticKeyword)
            ? method
            : method.AddModifiers(SyntaxFactory.Token(SyntaxKind.StaticKeyword).WithTrailingTrivia(SyntaxFactory.Space));
}
