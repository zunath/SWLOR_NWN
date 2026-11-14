using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.TypeMoves;

/// <summary>
/// Turns the findings of one file into its edits: the qualifier rewrites, the namespace line of a moved file, the using of
/// the old namespace dropped where nothing binds through it any more (and a moved file's using of its own new namespace),
/// and a using of each new namespace the file needs that is not already in scope.
/// </summary>
internal static class EditPlanner
{
    /// <param name="text">The file's text.</param>
    /// <param name="root">Its tree before the move.</param>
    /// <param name="findings">What the analysis found in it.</param>
    /// <param name="context">The move.</param>
    /// <param name="newNamespace">The namespace a moved file joins, or null for a file that stays.</param>
    /// <param name="projectImports">The namespaces the file's project imports with <c>global using</c> after the move.</param>
    /// <param name="dropGlobalImport">True when a <c>global using</c> of the old namespace in this file goes.</param>
    /// <param name="added">Receives the namespaces the file now imports.</param>
    /// <param name="removed">Counts the using directives dropped.</param>
    public static List<TextEdit> Plan(
        string text,
        CompilationUnitSyntax root,
        FileFindings findings,
        MoveContext context,
        string? newNamespace,
        IReadOnlySet<string> projectImports,
        bool dropGlobalImport,
        ISet<string> added,
        ref int removed)
    {
        var edits = new List<TextEdit>(findings.QualifierEdits.Values);
        IReadOnlyList<string> declared;
        if (newNamespace != null)
        {
            foreach (var declaration in root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<BaseNamespaceDeclarationSyntax>())
            {
                if (SyntaxQueries.FullName(declaration) == context.From)
                    edits.Add(new TextEdit(declaration.Name.SpanStart, declaration.Name.Span.Length, newNamespace));
            }

            declared = [newNamespace];
        }
        else
        {
            declared = SyntaxQueries.DeclaredNamespaces(root);
        }

        var removals = new List<UsingDirectiveSyntax>();
        foreach (var directive in SyntaxQueries.AllUsings(root).Where(SyntaxQueries.IsPlain))
        {
            var name = SyntaxQueries.Dotted(directive.Name!);
            if (name == context.From)
            {
                if (SyntaxQueries.IsGlobal(directive) ? dropGlobalImport : !findings.KeepsImport)
                    removals.Add(directive);
            }
            else if (newNamespace != null && name == newNamespace && !SyntaxQueries.IsGlobal(directive))
            {
                removals.Add(directive);
            }
        }

        var existing = SyntaxQueries.AllUsings(root)
            .Where(directive => SyntaxQueries.IsPlain(directive) && !removals.Contains(directive))
            .Select(directive => SyntaxQueries.Dotted(directive.Name!))
            .ToHashSet(StringComparer.Ordinal);
        var wanted = new SortedSet<string>(findings.Imports, StringComparer.Ordinal);
        if (findings.NeedsOldNamespace)
            wanted.Add(context.From);

        var additions = wanted
            .Where(@namespace => !existing.Contains(@namespace) && !projectImports.Contains(@namespace) &&
                                 !declared.Any(own => SyntaxQueries.IsWithin(own, @namespace)))
            .ToList();
        foreach (var @namespace in additions)
            added.Add(@namespace);
        removed += removals.Count;

        if (additions.Count > 0 || removals.Count > 0)
            edits.AddRange(UsingEditor.Plan(text, root, additions, removals));

        return edits;
    }
}
