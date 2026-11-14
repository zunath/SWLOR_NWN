using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.TypeMoves;

/// <summary>
/// Finds, with the semantic model of the tree before the move, how one file reaches the moved types and the types that stay:
/// <list type="bullet">
/// <item>a qualified name whose qualifier spells a moved type's source namespace (<c>Service.Log</c>,
/// <c>SWLOR.Game.Server.Service.Log</c>, <c>Crafting.RecipeType</c>, in code, <c>nameof</c>, <c>cref</c>, <c>using static</c> and
/// alias directives): the segments that spell the source become the target;</item>
/// <item>a simple name that binds to a moved type (through a using of the source namespace, a <c>global using</c>, or the
/// enclosing namespace), also in a <c>cref</c> or <c>nameof</c>, or a call of an extension method a moved class declares: the
/// file imports the new namespace;</item>
/// <item>a simple name (or extension method) that binds to a type that stays in a source namespace: the file keeps its using
/// of that namespace, a moved file imports it, and a reference through a <c>global using</c> keeps that one.</item>
/// </list>
/// A name reached through an alias is left alone (the alias directive is rewritten instead).
/// </summary>
internal static class ReferenceAnalyzer
{
    /// <returns>The source namespaces a staying type binds through by a <c>global using</c>.</returns>
    public static IReadOnlySet<string> Analyze(SemanticModel model, SyntaxNode root, MoveContext context, bool movedFile, FileFindings findings)
    {
        var throughGlobal = new HashSet<string>(StringComparer.Ordinal);
        var names = movedFile ? context.InterestingNamesInMovedFiles : context.InterestingNames;
        foreach (var name in SyntaxQueries.SimpleNames(root))
        {
            var text = name.Identifier.ValueText;
            if (!names.Contains(text) || SyntaxQueries.InNamespaceName(name))
                continue;

            var qualifier = SyntaxQueries.Qualifier(name);
            if (qualifier != null)
            {
                if (context.ExtensionNames.Contains(text) && name.Parent is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)
                {
                    var symbols = Binding.Of(model, name).Symbols;
                    if (symbols.Select(context.MovedExtension).FirstOrDefault(move => move != null) is { } extension)
                    {
                        findings.Imports.Add(extension.To);
                        findings.MovedReferences++;
                    }
                    else if (symbols.Select(context.StayingExtensionSource).FirstOrDefault(source => source != null) is { } source &&
                             RouteStaying(name, source, movedFile, findings))
                    {
                        throughGlobal.Add(source);
                    }
                }

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
            else if (!binding.Ambiguous && binding.Symbols.Select(context.StayingSource).FirstOrDefault(source => source != null) is { } source &&
                     RouteStaying(name, source, movedFile, findings))
            {
                throughGlobal.Add(source);
            }
        }

        return throughGlobal;
    }

    /// <summary>
    /// A qualifier that spells the moved type's source namespace (not an alias of it) gets the target instead: the segments
    /// that spell <see cref="TypeMove.RelativeSource"/> become <see cref="TypeMove.RelativeTarget"/>; a shorter qualifier (one
    /// that relied on the enclosing namespace or a using to reach the source) is replaced whole by the relative target, and the
    /// check writes the full namespace when that would bind to something else.
    /// </summary>
    private static void AddQualifierEdit(SemanticModel model, SimpleNameSyntax name, SyntaxNode qualifier, MoveContext context, FileFindings findings)
    {
        var move = Binding.Of(model, name).Symbols.Select(context.MovedFrom).FirstOrDefault(found => found != null);
        if (move == null)
            return; // the tree has no such type (master without it): nothing to move
        if (model.GetSymbolInfo(qualifier).Symbol is not INamespaceSymbol @namespace || @namespace.ToDisplayString() != move.From)
            return;
        if (SyntaxQueries.Segments(qualifier) is not { } segments)
            return;

        var from = move.From.Split('.');
        if (segments.Count > from.Length || !segments.Select(token => token.ValueText).SequenceEqual(from[^segments.Count..], StringComparer.Ordinal))
            return; // an alias of the namespace: the alias directive names it

        var source = move.RelativeSource.Split('.').Length;
        var start = segments.Count >= source ? segments[^source].SpanStart : segments[0].SpanStart;
        var end = segments[^1].Span.End;
        findings.QualifierEdits[start] = new TextEdit(start, end - start, move.RelativeTarget);
        findings.Qualifiers[name.SpanStart] = new QualifierSpan(qualifier.SpanStart, end, move);
    }

    /// <returns>True when the reference goes through a <c>global using</c>.</returns>
    private static bool RouteStaying(SyntaxNode node, string source, bool movedFile, FileFindings findings)
    {
        if (SyntaxQueries.IsWithin(SyntaxQueries.NamespaceOf(node), source))
        {
            if (movedFile)
                findings.NeededSources.Add(source);
            return false;
        }

        if (SyntaxQueries.HasFileImport(node, source))
        {
            findings.KeptImports.Add(source);
            return false;
        }

        return true;
    }
}
