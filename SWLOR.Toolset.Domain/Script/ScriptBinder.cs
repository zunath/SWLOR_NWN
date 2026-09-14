using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>Everything nameable from one file, once its includes have been resolved.</summary>
    /// <param name="Functions">Function names declared here or in any resolved include.</param>
    /// <param name="Variables">Globals, locals and parameters declared here or in any resolved include.</param>
    /// <param name="Complete">
    /// False when any include could not be read. The whole point of the binder is that it must not
    /// guess: with an unresolved include, an unknown name might be perfectly valid, so nothing is
    /// reported at all.
    /// </param>
    /// <param name="MissingIncludes">Includes that could not be resolved, for the "why" message.</param>
    public sealed record ScriptScope(
        IReadOnlySet<string> Functions,
        IReadOnlySet<string> Variables,
        bool Complete,
        IReadOnlyList<string> MissingIncludes);

    /// <summary>
    /// Resolves the names a script can legally use, across its transitive include set plus the
    /// engine header.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes unknown-identifier reporting safe. The earlier analyzer deliberately never
    /// reported unknown names, because an identifier can come from any include and nothing resolved
    /// include contents — flagging them would have lit up every legacy file in the module. With the
    /// include graph in place the set is knowable, so the check becomes possible.
    /// </para>
    /// <para>
    /// It stays conservative in one specific way that matters more than the feature: <b>if any
    /// include fails to resolve, the scope is marked incomplete and no unknown-identifier diagnostic
    /// is emitted at all.</b> Sixteen of this module's scripts include base-game headers that live
    /// only in an NWN installation's KEY/BIF, so an incomplete scope is the normal case for a builder
    /// without one — and reporting hundreds of false errors there would be far worse than reporting
    /// nothing.
    /// </para>
    /// </remarks>
    public sealed class ScriptBinder
    {
        private readonly EngineSymbolDatabase _engine;
        private readonly Func<string, string?>? _readInclude;

        /// <param name="engine">The engine header's symbols.</param>
        /// <param name="readInclude">Resolves an include resref to its source, or null if unavailable.</param>
        public ScriptBinder(EngineSymbolDatabase engine, Func<string, string?>? readInclude = null)
        {
            _engine = engine;
            _readInclude = readInclude;
        }

        /// <summary>Collects every name in scope for <paramref name="source"/>.</summary>
        public ScriptScope BuildScope(string source)
        {
            var functions = new HashSet<string>(StringComparer.Ordinal);
            var variables = new HashSet<string>(StringComparer.Ordinal);
            var missing = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Collect(source, functions, variables, missing, visited, depth: 0);

            return new ScriptScope(functions, variables, missing.Count == 0, missing);
        }

        private void Collect(
            string source,
            HashSet<string> functions,
            HashSet<string> variables,
            List<string> missing,
            HashSet<string> visited,
            int depth)
        {
            if (depth >= ScriptIncludeGraph.MaxIncludeDepth)
                return;

            var outline = ScriptOutline.Build(source);

            foreach (var fn in outline.Functions)
                functions.Add(fn.Name);

            foreach (var variable in outline.Variables)
                variables.Add(variable);

            foreach (var include in outline.Includes)
            {
                if (!visited.Add(include))
                    continue;

                var text = _readInclude?.Invoke(include);
                if (text == null)
                {
                    missing.Add(include);
                    continue;
                }

                Collect(text, functions, variables, missing, visited, depth + 1);
            }
        }

        /// <summary>
        /// Identifiers used in <paramref name="source"/> that resolve to nothing. Empty whenever the
        /// scope is incomplete.
        /// </summary>
        public IReadOnlyList<ScriptAnalysisDiagnostic> FindUnknownIdentifiers(string source)
        {
            var scope = BuildScope(source);
            return FindUnknownIdentifiers(source, scope);
        }

        public IReadOnlyList<ScriptAnalysisDiagnostic> FindUnknownIdentifiers(string source, ScriptScope scope)
        {
            if (!scope.Complete)
                return Array.Empty<ScriptAnalysisDiagnostic>();

            var tokens = ScriptLexer.Tokenize(source);
            var code = tokens.Where(t => !t.IsTrivia).ToList();
            var results = new List<ScriptAnalysisDiagnostic>();
            var reported = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < code.Count; i++)
            {
                var token = code[i];
                if (token.Kind != ScriptTokenKind.Identifier)
                    continue;

                var name = token.ToText(source);

                // A declaration introduces the name rather than using it, and struct member access
                // (after '.') resolves against the struct, which this pass does not model.
                if (i > 0 && (code[i - 1].Kind == ScriptTokenKind.TypeKeyword || code[i - 1].ToText(source) == "."))
                    continue;

                if (scope.Functions.Contains(name) || scope.Variables.Contains(name))
                    continue;

                if (_engine.FindFunction(name) != null || _engine.FindConstant(name) != null)
                    continue;

                // Struct tag names appear as bare identifiers in declarations; treating an unknown
                // SCREAMING_CASE name as a constant likewise avoids flagging module-defined ones.
                if (name.All(c => char.IsUpper(c) || char.IsDigit(c) || c == '_'))
                    continue;

                // One report per name: a variable used ten times is one mistake, not ten.
                if (!reported.Add(name))
                    continue;

                results.Add(new ScriptAnalysisDiagnostic(
                    $"'{name}' is not defined in this script or anything it includes",
                    token.Start, token.Length,
                    ScriptDiagnosticSeverity.Error, ScriptDiagnosticSource.Editor,
                    ScriptOutline.LineOf(source, token.Start)));
            }

            return results;
        }
    }
}
