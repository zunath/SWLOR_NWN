using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.TypeMoves;

/// <summary>
/// Finds, with the semantic model of the tree before the move, how one file reaches the moved types and the types that stay:
/// <list type="bullet">
/// <item>a qualified name whose qualifier binds to the old namespace (<c>Service.Log</c>, <c>SWLOR.Game.Server.Service.Log</c>,
/// in code, <c>using static</c> and alias directives): the qualifier's last segment becomes the new namespace;</item>
/// <item>a simple name that binds to a moved type (through a using of the old namespace, a <c>global using</c>, or the
/// enclosing namespace): the file imports the new namespace;</item>
/// <item>a simple name (or extension method) that binds to a type that stays: the file keeps its using of the old namespace,
/// a moved file imports it, and a reference through a <c>global using</c> keeps that one.</item>
/// </list>
/// A name reached through an alias is left alone (the alias directive is rewritten instead).
/// </summary>
internal static class ReferenceAnalyzer
{
    /// <returns>True when a staying type binds through a <c>global using</c> of the old namespace.</returns>
    public static bool Analyze(SemanticModel model, SyntaxNode root, MoveContext context, bool movedFile, FileFindings findings)
    {
        var usesGlobalImport = false;
        foreach (var name in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            var text = name.Identifier.ValueText;
            if (!context.InterestingNames.Contains(text) || SyntaxQueries.InNamespaceName(name))
                continue;

            var qualifier = SyntaxQueries.Qualifier(name);
            if (qualifier != null)
            {
                if (context.OtherExtensionMethods.Contains(text) && name.Parent is MemberAccessExpressionSyntax &&
                    Binding.Of(model, name).Symbols.Any(context.IsStayingExtension))
                    usesGlobalImport |= RouteStaying(name, context, movedFile, findings);

                if (context.MovedNames.Contains(text))
                    AddQualifierEdit(model, name, qualifier, context, findings);
                continue;
            }

            if (model.GetAliasInfo(name) != null)
                continue;

            var binding = Binding.Of(model, name);
            var moved = binding.Symbols.Select(context.MovedFrom).FirstOrDefault(move => move != null);
            if (moved != null)
            {
                findings.Imports.Add(moved.To);
                findings.MovedReferences++;
            }
            else if (!binding.Ambiguous && binding.Symbols.Any(context.IsStayingType))
            {
                usesGlobalImport |= RouteStaying(name, context, movedFile, findings);
            }
        }

        return usesGlobalImport;
    }

    private static void AddQualifierEdit(SemanticModel model, SimpleNameSyntax name, SyntaxNode qualifier, MoveContext context, FileFindings findings)
    {
        if (SyntaxQueries.LastIdentifier(qualifier) is not { } last || last.ValueText != context.FromLeaf)
            return;
        if (model.GetSymbolInfo(qualifier).Symbol is not INamespaceSymbol @namespace || @namespace.ToDisplayString() != context.From)
            return;

        var move = Binding.Of(model, name).Symbols.Select(context.MovedFrom).FirstOrDefault(found => found != null);
        if (move == null)
            return; // the tree has no such type (master without it): nothing to move

        findings.QualifierEdits[last.SpanStart] = new TextEdit(last.SpanStart, last.Span.Length, move.RelativeTarget);
    }

    /// <returns>True when the reference goes through a <c>global using</c>.</returns>
    private static bool RouteStaying(SyntaxNode node, MoveContext context, bool movedFile, FileFindings findings)
    {
        if (SyntaxQueries.IsWithin(SyntaxQueries.NamespaceOf(node), context.From))
        {
            if (movedFile)
                findings.NeedsOldNamespace = true;
            return false;
        }

        if (SyntaxQueries.HasFileImport(node, context.From))
        {
            findings.KeepsImport = true;
            return false;
        }

        return true;
    }
}
