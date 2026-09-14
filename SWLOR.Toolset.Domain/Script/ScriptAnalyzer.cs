using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>Everything the editor knows about one script at a moment in time.</summary>
    /// <param name="Outline">Declarations found in the file.</param>
    /// <param name="Diagnostics">Advisory findings from this project's own analysis.</param>
    public sealed record ScriptAnalysis(ScriptOutline Outline, IReadOnlyList<ScriptAnalysisDiagnostic> Diagnostics);

    /// <summary>
    /// Tier-1 analysis: fast, advisory findings for squiggles as the author types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Conservative by construction.</b> The vendored compiler is the authority on what is valid
    /// (see <c>Compile/ScriptCompiler</c>); this pass exists only to surface the obvious immediately.
    /// So it reports something only when it is certain, and stays silent everywhere else. A gap here
    /// degrades to "no squiggle", never to a false error on valid code — which is the failure mode
    /// that makes an editor annoying enough to abandon, and the reason the acceptance gate is
    /// "zero diagnostics across all 87 known-good module scripts".
    /// </para>
    /// <para>
    /// That rule is why the checks are so narrow. Unbalanced brackets and unterminated literals are
    /// unambiguous. Unknown identifiers are <b>not</b> reported at all: an identifier can come from
    /// any include, and this pass does not resolve include contents, so flagging them would light up
    /// every legacy file in the module. That check belongs with a real binder over a resolved include
    /// set, and until then silence is the correct answer.
    /// </para>
    /// </remarks>
    public sealed class ScriptAnalyzer
    {
        private enum LiteralArgumentType
        {
            Integer,
            Float,
            String
        }

        private readonly record struct ArgumentRange(int Start, int End);

        private readonly record struct LiteralArgument(
            LiteralArgumentType Type,
            int Start,
            int Length);

        private readonly EngineSymbolDatabase _engine;
        private readonly ScriptBinder? _binder;

        /// <param name="engine">The engine header's symbols.</param>
        /// <param name="readInclude">
        /// Resolves an include to its source. When supplied, unknown-identifier checking is enabled —
        /// but only for files whose entire include set resolves. Without it that check stays off, which
        /// is the behaviour this analyzer shipped with.
        /// </param>
        public ScriptAnalyzer(EngineSymbolDatabase engine, Func<string, string?>? readInclude = null)
        {
            _engine = engine;
            _binder = readInclude != null ? new ScriptBinder(engine, readInclude) : null;
        }

        public ScriptAnalysis Analyze(string source)
        {
            var outline = ScriptOutline.Build(source);
            var tokens = ScriptLexer.Tokenize(source);
            var diagnostics = new List<ScriptAnalysisDiagnostic>();

            CheckUnterminated(source, tokens, diagnostics);
            CheckBalanced(source, tokens, diagnostics);
            CheckKnownCallArity(source, tokens, diagnostics);
            CheckDuplicateDefinitions(source, outline, diagnostics);

            // Only meaningful once every include resolves; the binder returns nothing otherwise. A
            // file with unbalanced brackets is skipped too — the token stream past the mismatch is
            // not trustworthy, and one structural error should not spray name errors after it.
            if (_binder != null && diagnostics.Count == 0)
            {
                var scope = _binder.BuildScope(source);
                if (scope.Complete)
                {
                    CheckLiteralArgumentTypes(source, tokens, diagnostics);

                    if (diagnostics.Count == 0)
                        diagnostics.AddRange(_binder.FindUnknownIdentifiers(source, scope));
                }
            }

            return new ScriptAnalysis(outline, diagnostics
                .OrderBy(d => d.Start)
                .ToList());
        }

        /// <summary>An unterminated block comment or string literal. Unambiguous.</summary>
        private static void CheckUnterminated(
            string source, IReadOnlyList<ScriptToken> tokens, List<ScriptAnalysisDiagnostic> diagnostics)
        {
            foreach (var token in tokens)
            {
                var text = token.Text(source);

                if (token.Kind == ScriptTokenKind.BlockComment &&
                    !(text.Length >= 4 && text.EndsWith("*/", StringComparison.Ordinal)))
                {
                    diagnostics.Add(Make("Unterminated block comment", token, source, ScriptDiagnosticSeverity.Warning));
                }

                if (token.Kind == ScriptTokenKind.String &&
                    !(text.Length >= 2 && text[^1] == '"'))
                {
                    diagnostics.Add(Make("Unterminated string literal", token, source, ScriptDiagnosticSeverity.Error));
                }
            }
        }

        /// <summary>Bracket balance. Reported once, at the first offender, to avoid a cascade.</summary>
        private static void CheckBalanced(
            string source, IReadOnlyList<ScriptToken> tokens, List<ScriptAnalysisDiagnostic> diagnostics)
        {
            var stack = new Stack<ScriptToken>();

            foreach (var token in tokens)
            {
                if (token.Kind != ScriptTokenKind.Operator)
                    continue;

                var c = source[token.Start];
                if (c is '(' or '{' or '[')
                {
                    stack.Push(token);
                    continue;
                }

                if (c is not (')' or '}' or ']'))
                    continue;

                if (stack.Count == 0)
                {
                    diagnostics.Add(Make($"Unmatched '{c}'", token, source, ScriptDiagnosticSeverity.Error));
                    return;
                }

                var open = source[stack.Pop().Start];
                var expected = open switch { '(' => ')', '{' => '}', _ => ']' };
                if (c != expected)
                {
                    diagnostics.Add(Make($"Expected '{expected}' but found '{c}'", token, source, ScriptDiagnosticSeverity.Error));
                    return;
                }
            }

            if (stack.Count > 0)
            {
                var unclosed = stack.Peek();
                diagnostics.Add(Make($"Unclosed '{source[unclosed.Start]}'", unclosed, source, ScriptDiagnosticSeverity.Error));
            }
        }

        /// <summary>
        /// Too many arguments to a known engine function. Only the upper bound is checked: a
        /// too-short call is usually a half-typed one, and reporting it would squiggle the author's
        /// cursor while they type.
        /// </summary>
        private void CheckKnownCallArity(
            string source, IReadOnlyList<ScriptToken> tokens, List<ScriptAnalysisDiagnostic> diagnostics)
        {
            var code = tokens.Where(t => !t.IsTrivia).ToList();

            for (var i = 0; i < code.Count - 1; i++)
            {
                if (code[i].Kind != ScriptTokenKind.Identifier || code[i + 1].ToText(source) != "(")
                    continue;

                var fn = _engine.FindFunction(code[i].ToText(source));
                if (fn == null)
                    continue;

                var close = MatchParen(code, source, i + 1);
                if (close < 0)
                    continue;

                // A declaration, not a call: "void Foo(int n)" has a type before the name.
                if (i > 0 && code[i - 1].Kind == ScriptTokenKind.TypeKeyword)
                    continue;

                var arguments = CountArguments(code, source, i + 1, close);
                if (arguments > fn.Parameters.Count)
                {
                    diagnostics.Add(Make(
                        $"{fn.Name} takes at most {fn.Parameters.Count} argument(s) but {arguments} were given",
                        code[i], source, ScriptDiagnosticSeverity.Error));
                }

                i = close;
            }
        }

        /// <summary>
        /// Literal argument type mismatches for known engine functions. This intentionally does not
        /// infer expression types: only a single string/number literal is certain enough for tier 1.
        /// </summary>
        private void CheckLiteralArgumentTypes(
            string source, IReadOnlyList<ScriptToken> tokens, List<ScriptAnalysisDiagnostic> diagnostics)
        {
            var code = tokens.Where(t => !t.IsTrivia).ToList();

            for (var i = 0; i < code.Count - 1; i++)
            {
                if (code[i].Kind != ScriptTokenKind.Identifier || code[i + 1].ToText(source) != "(")
                    continue;

                var fn = _engine.FindFunction(code[i].ToText(source));
                if (fn == null)
                    continue;

                var close = MatchParen(code, source, i + 1);
                if (close < 0)
                    continue;

                // A declaration, not a call: "void Foo(int n)" has a type before the name.
                if (i > 0 && code[i - 1].Kind == ScriptTokenKind.TypeKeyword)
                    continue;

                var arguments = ArgumentRanges(code, source, i + 1, close);
                for (var argIndex = 0; argIndex < arguments.Count && argIndex < fn.Parameters.Count; argIndex++)
                {
                    var literal = TryLiteralArgument(code, source, arguments[argIndex]);
                    if (literal == null)
                        continue;

                    var expected = fn.Parameters[argIndex].Type;
                    if (LiteralCanFlowTo(expected, literal.Value.Type))
                        continue;

                    diagnostics.Add(new ScriptAnalysisDiagnostic(
                        $"Cannot pass {LiteralName(literal.Value.Type)} literal to {expected} parameter '{fn.Parameters[argIndex].Name}' of {fn.Name}",
                        literal.Value.Start, literal.Value.Length,
                        ScriptDiagnosticSeverity.Error, ScriptDiagnosticSource.Editor,
                        ScriptOutline.LineOf(source, literal.Value.Start)));
                }

                i = close;
            }
        }

        /// <summary>Two definitions of the same function in one file.</summary>
        private static void CheckDuplicateDefinitions(
            string source, ScriptOutline outline, List<ScriptAnalysisDiagnostic> diagnostics)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var fn in outline.Functions.Where(f => f.IsDefinition))
            {
                if (seen.Add(fn.Name))
                    continue;

                diagnostics.Add(new ScriptAnalysisDiagnostic(
                    $"'{fn.Name}' is already defined in this file",
                    fn.Offset, fn.Name.Length,
                    ScriptDiagnosticSeverity.Error, ScriptDiagnosticSource.Editor, fn.Line));
            }
        }

        private static int CountArguments(IReadOnlyList<ScriptToken> code, string source, int open, int close)
        {
            var depth = 0;
            var count = 0;
            var sawContent = false;

            for (var i = open; i <= close; i++)
            {
                var text = code[i].ToText(source);

                if (text is "(" or "[")
                {
                    depth++;
                    continue;
                }

                if (text is ")" or "]")
                {
                    depth--;
                    continue;
                }

                if (depth == 1 && text == ",")
                {
                    count++;
                    continue;
                }

                if (depth >= 1)
                    sawContent = true;
            }

            return sawContent ? count + 1 : 0;
        }

        private static IReadOnlyList<ArgumentRange> ArgumentRanges(
            IReadOnlyList<ScriptToken> code, string source, int open, int close)
        {
            var ranges = new List<ArgumentRange>();
            var depth = 0;
            var start = open + 1;

            for (var i = open + 1; i < close; i++)
            {
                var text = code[i].ToText(source);

                if (text is "(" or "[")
                {
                    depth++;
                    continue;
                }

                if (text is ")" or "]")
                {
                    depth--;
                    continue;
                }

                if (depth == 0 && text == ",")
                {
                    AddNonEmptyRange(ranges, start, i);
                    start = i + 1;
                }
            }

            AddNonEmptyRange(ranges, start, close);
            return ranges;
        }

        private static void AddNonEmptyRange(List<ArgumentRange> ranges, int start, int end)
        {
            if (start < end)
                ranges.Add(new ArgumentRange(start, end));
        }

        private static LiteralArgument? TryLiteralArgument(
            IReadOnlyList<ScriptToken> code, string source, ArgumentRange range)
        {
            if (range.End == range.Start + 1)
                return LiteralFromToken(code[range.Start], source);

            if (range.End == range.Start + 2 &&
                code[range.Start].ToText(source) == "-" &&
                code[range.Start + 1].Kind == ScriptTokenKind.Number)
            {
                var number = LiteralFromToken(code[range.Start + 1], source);
                if (number == null)
                    return null;

                return number.Value with
                {
                    Start = code[range.Start].Start,
                    Length = code[range.Start + 1].End - code[range.Start].Start
                };
            }

            return null;
        }

        private static LiteralArgument? LiteralFromToken(ScriptToken token, string source)
        {
            if (token.Kind == ScriptTokenKind.String)
                return new LiteralArgument(LiteralArgumentType.String, token.Start, token.Length);

            if (token.Kind != ScriptTokenKind.Number)
                return null;

            var text = token.ToText(source);
            var type = text.Contains('.') || text.EndsWith("f", StringComparison.OrdinalIgnoreCase)
                ? LiteralArgumentType.Float
                : LiteralArgumentType.Integer;

            return new LiteralArgument(type, token.Start, token.Length);
        }

        private static bool LiteralCanFlowTo(string expectedType, LiteralArgumentType literalType)
        {
            var expected = expectedType.Trim().ToLowerInvariant();

            if (expected is "int" or "float")
                return literalType is LiteralArgumentType.Integer or LiteralArgumentType.Float;

            var literalTypeName = literalType switch
            {
                LiteralArgumentType.String => "string",
                _ => string.Empty
            };

            return expected == literalTypeName;
        }

        private static string LiteralName(LiteralArgumentType type) =>
            type switch
            {
                LiteralArgumentType.Integer => "int",
                LiteralArgumentType.Float => "float",
                _ => "string"
            };

        private static int MatchParen(IReadOnlyList<ScriptToken> code, string source, int openIndex)
        {
            var depth = 0;
            for (var i = openIndex; i < code.Count; i++)
            {
                var text = code[i].ToText(source);
                if (text == "(")
                    depth++;
                else if (text == ")" && --depth == 0)
                    return i;
            }

            return -1;
        }

        private static ScriptAnalysisDiagnostic Make(
            string message, ScriptToken token, string source, ScriptDiagnosticSeverity severity) =>
            new(message, token.Start, Math.Max(1, token.Length), severity,
                ScriptDiagnosticSource.Editor, ScriptOutline.LineOf(source, token.Start));
    }
}
