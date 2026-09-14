namespace SWLOR.Toolset.Domain.Script.Syntax
{
    /// <summary>What a lexed span of NWScript source is.</summary>
    public enum ScriptTokenKind
    {
        /// <summary>Spaces, tabs and newlines.</summary>
        Whitespace,

        /// <summary>A <c>//</c> line comment, including the leading slashes.</summary>
        LineComment,

        /// <summary>A <c>/* */</c> block comment. Unterminated ones run to end of file.</summary>
        BlockComment,

        /// <summary>A language keyword — control flow, <c>struct</c>, <c>const</c>, and so on.</summary>
        Keyword,

        /// <summary>A built-in type name: int, float, string, object, void, and the engine structures.</summary>
        TypeKeyword,

        /// <summary>Any other identifier: a function name, variable, struct tag or constant.</summary>
        Identifier,

        /// <summary>An integer, float or hex literal.</summary>
        Number,

        /// <summary>A double-quoted string. NWScript has no escape sequences other than <c>\"</c>.</summary>
        String,

        /// <summary>A preprocessor directive word, i.e. <c>#include</c> or <c>#define</c>.</summary>
        Preprocessor,

        /// <summary>Punctuation and operators.</summary>
        Operator,

        /// <summary>A character the lexer does not recognise. Never dropped — see the round-trip gate.</summary>
        Unknown
    }

    /// <summary>
    /// One lexed span. Positions are absolute offsets into the source so a token maps straight onto
    /// an editor document range.
    /// </summary>
    /// <param name="Kind">What this span is.</param>
    /// <param name="Start">Absolute offset of the first character.</param>
    /// <param name="Length">Length in characters.</param>
    public readonly record struct ScriptToken(ScriptTokenKind Kind, int Start, int Length)
    {
        /// <summary>One past the last character.</summary>
        public int End => Start + Length;

        /// <summary>The source text this token covers.</summary>
        public ReadOnlySpan<char> Text(string source) => source.AsSpan(Start, Length);

        /// <summary>The source text this token covers, as a string.</summary>
        public string ToText(string source) => source.Substring(Start, Length);

        /// <summary>True for whitespace and both comment forms — the spans a parser skips.</summary>
        public bool IsTrivia =>
            Kind is ScriptTokenKind.Whitespace or ScriptTokenKind.LineComment or ScriptTokenKind.BlockComment;
    }
}
