using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>What a completion item is, which drives its glyph and colour.</summary>
    public enum CompletionItemKind
    {
        Keyword,
        Type,
        EngineFunction,
        LocalFunction,
        Constant,
        Variable,
        IncludeFile
    }

    /// <summary>One offered completion. Plain data: the app layer maps this to AvaloniaEdit's ICompletionData.</summary>
    /// <param name="Text">What gets inserted.</param>
    /// <param name="Kind">Drives glyph and colour.</param>
    /// <param name="Detail">Right-aligned hint — a constant's value, a function's return type.</param>
    /// <param name="Documentation">Tooltip body.</param>
    public sealed record CompletionItem(
        string Text,
        CompletionItemKind Kind,
        string? Detail = null,
        string? Documentation = null);

    /// <summary>Where the caret is, which decides what gets offered.</summary>
    public enum CompletionContextKind
    {
        /// <summary>Ordinary code position.</summary>
        General,

        /// <summary>Inside the quotes of an <c>#include</c>.</summary>
        IncludePath,

        /// <summary>Inside an ordinary string literal. Nothing is offered here.</summary>
        StringLiteral,

        /// <summary>Inside a call's argument list.</summary>
        Argument
    }

    /// <summary>The resolved caret context.</summary>
    /// <param name="Kind">Which situation the caret is in.</param>
    /// <param name="Prefix">The partial word being typed, possibly empty.</param>
    /// <param name="PrefixStart">Offset the prefix starts at, so the caller can replace it.</param>
    /// <param name="FunctionName">Enclosing call, when <see cref="Kind"/> is Argument.</param>
    /// <param name="ArgumentIndex">0-based argument position, when Kind is Argument.</param>
    public sealed record CompletionContext(
        CompletionContextKind Kind,
        string Prefix,
        int PrefixStart,
        string? FunctionName = null,
        int ArgumentIndex = 0);

    /// <summary>
    /// Builds the completion list for a caret position.
    /// </summary>
    /// <remarks>
    /// The ordering is the point, and it is why this lives in Domain where it can be tested as
    /// "caret position + source → expected ordered items". Two rules do most of the work:
    /// <list type="bullet">
    /// <item>In an argument position whose parameter documents a <c>FOO_*</c> family, that family
    /// comes first. This is the feature Aurora never had — 12 constants instead of 6,201.</item>
    /// <item>Locals and parameters always outrank the 6,201 engine constants. A variable the author
    /// just declared is nearer to hand than anything in the header.</item>
    /// </list>
    /// Matching is prefix-first, then substring, then subsequence ("gnc" → GetNearestCreature),
    /// because NWScript names are long and typing them out defeats the purpose.
    /// </remarks>
    public sealed class ScriptCompletionEngine
    {
        private static readonly string[] Keywords =
        {
            "if", "else", "for", "while", "do", "switch", "case", "default",
            "break", "continue", "return", "const", "struct"
        };

        private static readonly string[] Types =
        {
            "void", "int", "float", "string", "object", "vector",
            "effect", "event", "location", "talent", "itemproperty", "sqlquery", "cassowary", "json"
        };

        private readonly EngineSymbolDatabase _engine;

        public ScriptCompletionEngine(EngineSymbolDatabase engine) => _engine = engine;

        /// <summary>Resrefs offered after <c>#include "</c>. Set by the app from the module workspace.</summary>
        public IReadOnlyList<string> AvailableIncludes { get; set; } = Array.Empty<string>();

        /// <summary>Works out what the caret is in the middle of.</summary>
        public static CompletionContext DescribeContext(string source, int caret)
        {
            caret = Math.Clamp(caret, 0, source.Length);

            var prefixStart = caret;
            while (prefixStart > 0 && (char.IsLetterOrDigit(source[prefixStart - 1]) || source[prefixStart - 1] == '_'))
                prefixStart--;

            var prefix = source[prefixStart..caret];

            var tokens = ScriptLexer.TokenizeCode(source);

            // Inside an #include's string? The lexer gives the whole literal one token, so the test is
            // simply "the caret sits within a string token that follows #include".
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Kind != ScriptTokenKind.String)
                    continue;

                if (caret <= tokens[i].Start || caret > tokens[i].End)
                    continue;

                var isInclude = i > 0 && tokens[i - 1].Kind == ScriptTokenKind.Preprocessor &&
                    tokens[i - 1].Text(source).SequenceEqual("#include");

                var literalStart = tokens[i].Start + 1;
                var typed = source[literalStart..Math.Max(literalStart, caret)];

                return isInclude
                    ? new CompletionContext(CompletionContextKind.IncludePath, typed, literalStart)
                    // Inside an ordinary string literal nothing is offered; an identifier list popping
                    // up while typing prose is pure noise.
                    : new CompletionContext(CompletionContextKind.StringLiteral, string.Empty, caret);
            }

            var call = FindEnclosingCall(tokens, source, caret);
            return call != null
                ? new CompletionContext(CompletionContextKind.Argument, prefix, prefixStart, call.Value.Name, call.Value.Index)
                : new CompletionContext(CompletionContextKind.General, prefix, prefixStart);
        }

        /// <summary>The ranked completion list for a caret position.</summary>
        public IReadOnlyList<CompletionItem> GetCompletions(string source, int caret, ScriptOutline? outline = null)
        {
            var context = DescribeContext(source, caret);
            outline ??= ScriptOutline.Build(source);

            if (context.Kind == CompletionContextKind.StringLiteral)
                return Array.Empty<CompletionItem>();

            if (context.Kind == CompletionContextKind.IncludePath)
            {
                return Rank(AvailableIncludes
                        .Select(r => new CompletionItem(r, CompletionItemKind.IncludeFile,
                            r.EndsWith("_inc", StringComparison.OrdinalIgnoreCase) ? "include" : null))
                        // An _inc header is what an #include almost always wants.
                        .OrderByDescending(r => r.Detail != null)
                        .ToList(),
                    context.Prefix);
            }

            var items = new List<CompletionItem>();

            // Argument position with a documented constant family: that family leads.
            if (context.Kind == CompletionContextKind.Argument && context.FunctionName != null)
            {
                var fn = _engine.FindFunction(context.FunctionName);
                var family = fn != null && context.ArgumentIndex < fn.Parameters.Count
                    ? fn.Parameters[context.ArgumentIndex].ConstantFamily
                    : null;

                if (family != null)
                {
                    items.AddRange(_engine.ConstantsInFamily(family)
                        .Select(c => new CompletionItem(c.Name, CompletionItemKind.Constant, c.Value,
                            $"{c.Type} {c.Name} = {c.Value}")));
                }
            }

            // Locals before anything from the header.
            items.AddRange(outline.Variables.Select(v => new CompletionItem(v, CompletionItemKind.Variable)));
            items.AddRange(outline.Functions.Select(f =>
                new CompletionItem(f.Name, CompletionItemKind.LocalFunction, f.ReturnType, f.Display)));

            items.AddRange(Keywords.Select(k => new CompletionItem(k, CompletionItemKind.Keyword)));
            items.AddRange(Types.Select(t => new CompletionItem(t, CompletionItemKind.Type)));

            items.AddRange(_engine.Functions.Select(f =>
                new CompletionItem(f.Name, CompletionItemKind.EngineFunction, f.ReturnType, f.Summary ?? f.Signature)));

            items.AddRange(_engine.Constants.Select(c =>
                new CompletionItem(c.Name, CompletionItemKind.Constant, c.Value, $"{c.Type} {c.Name} = {c.Value}")));

            // Distinct keeps a name that is both a local and an engine symbol from appearing twice;
            // the earlier (more local) entry wins because DistinctBy keeps the first.
            return Rank(items.DistinctBy(i => i.Text, StringComparer.Ordinal).ToList(), context.Prefix);
        }

        /// <summary>Filters to what matches the prefix and orders by how well it matches.</summary>
        private static IReadOnlyList<CompletionItem> Rank(IReadOnlyList<CompletionItem> items, string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return items;

            var scored = new List<(CompletionItem Item, int Score, int Order)>();
            for (var i = 0; i < items.Count; i++)
            {
                var score = Score(items[i].Text, prefix);
                if (score > 0)
                    scored.Add((items[i], score, i));
            }

            return scored
                .OrderByDescending(s => s.Score)
                // Ties keep the order they were added in, which is where the "family first, then
                // locals, then engine" intent actually lives.
                .ThenBy(s => s.Order)
                .Select(s => s.Item)
                .ToList();
        }

        private static int Score(string candidate, string prefix)
        {
            if (candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return 3;

            if (candidate.Contains(prefix, StringComparison.OrdinalIgnoreCase))
                return 2;

            return IsSubsequence(candidate, prefix) ? 1 : 0;
        }

        /// <summary>"gnc" matches GetNearestCreature.</summary>
        private static bool IsSubsequence(string candidate, string prefix)
        {
            var j = 0;
            foreach (var c in candidate)
            {
                if (j < prefix.Length && char.ToLowerInvariant(c) == char.ToLowerInvariant(prefix[j]))
                    j++;
            }

            return j == prefix.Length;
        }

        /// <summary>
        /// Walks back from the caret to the call it sits inside, counting commas at depth 1 to get
        /// the argument index. Returns null when the caret is not inside a call's parentheses.
        /// </summary>
        internal static (string Name, int Index)? FindEnclosingCall(
            IReadOnlyList<ScriptToken> tokens, string source, int caret)
        {
            var depth = 0;
            var commas = 0;

            var last = tokens.Count - 1;
            while (last >= 0 && tokens[last].Start >= caret)
                last--;

            for (var i = last; i >= 0; i--)
            {
                var text = tokens[i].ToText(source);

                if (text == ")")
                {
                    depth++;
                    continue;
                }

                if (text == ",")
                {
                    if (depth == 0)
                        commas++;

                    continue;
                }

                if (text != "(")
                    continue;

                if (depth > 0)
                {
                    depth--;
                    continue;
                }

                // An unmatched '(' at depth 0 is the call we are inside. It is a call only if an
                // identifier precedes it - otherwise it is a grouping or a control-flow condition.
                if (i > 0 && tokens[i - 1].Kind == ScriptTokenKind.Identifier)
                    return (tokens[i - 1].ToText(source), commas);

                return null;
            }

            return null;
        }
    }
}
