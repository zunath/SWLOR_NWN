using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.TypeMoves;

/// <summary>
/// Turns the findings of one file into its edits: the qualifier rewrites, the namespace line of a moved file, the using of
/// a source namespace dropped where nothing binds through it any more and the data lets it go (and a moved file's using of
/// its own new namespace), and a using of each new namespace the file needs that is not already in scope.
/// </summary>
internal static class EditPlanner
{
    /// <param name="text">The file's text.</param>
    /// <param name="root">Its tree before the move.</param>
    /// <param name="findings">What the analysis found in it.</param>
    /// <param name="context">The move.</param>
    /// <param name="movedFile">The move of a moved file, or null for a file that stays.</param>
    /// <param name="projectImports">The namespaces the file's project imports with <c>global using</c> after the move.</param>
    /// <param name="droppedGlobalImports">The source namespaces whose <c>global using</c> goes from this project.</param>
    /// <param name="added">Receives the namespaces the file now imports.</param>
    /// <param name="removed">Counts the using directives dropped.</param>
    public static List<TextEdit> Plan(
        string text,
        CompilationUnitSyntax root,
        FileFindings findings,
        MoveContext context,
        TypeMove? movedFile,
        IReadOnlySet<string> projectImports,
        IReadOnlySet<string> droppedGlobalImports,
        ISet<string> added,
        ref int removed)
    {
        var edits = new List<TextEdit>(findings.QualifierEdits.Values);
        IReadOnlyList<string> declared;
        if (movedFile != null)
        {
            foreach (var declaration in root.DescendantNodes(node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<BaseNamespaceDeclarationSyntax>())
            {
                if (SyntaxQueries.FullName(declaration) == movedFile.From)
                    edits.Add(new TextEdit(declaration.Name.SpanStart, declaration.Name.Span.Length, movedFile.To));
            }

            declared = [movedFile.To];
        }
        else
        {
            declared = SyntaxQueries.DeclaredNamespaces(root);
        }

        var removals = new List<UsingDirectiveSyntax>();
        foreach (var directive in SyntaxQueries.AllUsings(root).Where(SyntaxQueries.IsPlain))
        {
            var name = SyntaxQueries.Dotted(directive.Name!);
            if (movedFile != null && name == movedFile.To && !SyntaxQueries.IsGlobal(directive))
            {
                removals.Add(directive); // a moved file's using of its own new namespace (which may also be a source)
            }
            else if (context.Data.Sources.Contains(name))
            {
                if (context.MayDropImport(name) && (SyntaxQueries.IsGlobal(directive) ? droppedGlobalImports.Contains(name) : !findings.KeptImports.Contains(name)))
                    removals.Add(directive);
            }
        }

        var existing = SyntaxQueries.AllUsings(root)
            .Where(directive => SyntaxQueries.IsPlain(directive) && !removals.Contains(directive))
            .Select(directive => SyntaxQueries.Dotted(directive.Name!))
            .ToHashSet(StringComparer.Ordinal);
        var wanted = new SortedSet<string>(findings.Imports, StringComparer.Ordinal);
        wanted.UnionWith(findings.NeededSources);

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
