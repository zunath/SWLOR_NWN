namespace SWLOR.Toolset.Domain.Script.Syntax
{
    /// <summary>
    /// Splits NWScript source into tokens. Total and lossless: every character of the input lands in
    /// exactly one token, including whitespace, comments and characters the language does not define.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Losslessness is the whole design. It gives the syntax highlighter a span for every character
    /// with no gaps to paper over, and it makes the correctness gate trivial to state and impossible
    /// to fudge: concatenating every token's text must reproduce the file byte for byte. That single
    /// assertion catches essentially every lexer bug, and it is the same shape as the round-trip gate
    /// the GFF layer already lives by.
    /// </para>
    /// <para>
    /// Nothing here throws. An unterminated string or block comment runs to end of file and an
    /// unrecognised character becomes <see cref="ScriptTokenKind.Unknown"/>, because the editor lexes
    /// half-typed lines constantly and a lexer that threw would make the buffer unhighlightable
    /// exactly while it was being written.
    /// </para>
    /// </remarks>
    public static class ScriptLexer
    {
        /// <summary>
        /// NWScript's built-in types. The eight engine structures come from nwscript.nss's own
        /// ENGINE_STRUCTURE_0..7 block; they are types even though they are not primitives.
        /// </summary>
        private static readonly HashSet<string> TypeKeywords = new(StringComparer.Ordinal)
        {
            "void", "int", "float", "string", "object", "struct", "vector",
            "effect", "event", "location", "talent", "itemproperty", "sqlquery", "cassowary", "json"
        };

        private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
        {
            "if", "else", "for", "while", "do", "switch", "case", "default",
            "break", "continue", "return", "const"
        };

        /// <summary>Lexes <paramref name="source"/> into a list covering every character exactly once.</summary>
        public static IReadOnlyList<ScriptToken> Tokenize(string source)
        {
            var tokens = new List<ScriptToken>(Math.Max(16, source.Length / 4));
            var i = 0;

            while (i < source.Length)
            {
                var start = i;
                var c = source[i];

                if (char.IsWhiteSpace(c))
                {
                    while (i < source.Length && char.IsWhiteSpace(source[i]))
                        i++;
                    tokens.Add(new ScriptToken(ScriptTokenKind.Whitespace, start, i - start));
                    continue;
                }

                if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
                {
                    while (i < source.Length && source[i] != '\n')
                        i++;
                    tokens.Add(new ScriptToken(ScriptTokenKind.LineComment, start, i - start));
                    continue;
                }

                if (c == '/' && i + 1 < source.Length && source[i + 1] == '*')
                {
                    i += 2;
                    while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length && source[i + 1] == '/'))
                        i++;

                    // Unterminated: run to EOF rather than throwing. Half-typed /* is normal while editing.
                    i = i < source.Length ? i + 2 : source.Length;
                    tokens.Add(new ScriptToken(ScriptTokenKind.BlockComment, start, i - start));
                    continue;
                }

                if (c == '"')
                {
                    // NWScript has NO escape sequences: a backslash inside a string is a literal
                    // backslash, and the string ends at the very next quote. Treating '\"' as an
                    // escape (the C habit) makes the lexer run straight past the closing quote of
                    // real corpus lines like  return "/\/\\";  in dmfi_plychat_exe.nss - ASCII art,
                    // not an escape - and swallow the rest of the file as one string.
                    i++;
                    while (i < source.Length && source[i] != '"')
                    {
                        // A string never spans a line; stopping here keeps one stray quote from
                        // colouring the rest of the file as string.
                        if (source[i] == '\n')
                            break;

                        i++;
                    }

                    if (i < source.Length && source[i] == '"')
                        i++;

                    tokens.Add(new ScriptToken(ScriptTokenKind.String, start, i - start));
                    continue;
                }

                if (c == '#')
                {
                    i++;
                    while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_'))
                        i++;
                    tokens.Add(new ScriptToken(ScriptTokenKind.Preprocessor, start, i - start));
                    continue;
                }

                if (char.IsDigit(c) || c == '.' && i + 1 < source.Length && char.IsDigit(source[i + 1]))
                {
                    i = ScanNumber(source, i);
                    tokens.Add(new ScriptToken(ScriptTokenKind.Number, start, i - start));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_'))
                        i++;

                    var word = source.Substring(start, i - start);
                    var kind = TypeKeywords.Contains(word) ? ScriptTokenKind.TypeKeyword
                        : Keywords.Contains(word) ? ScriptTokenKind.Keyword
                        : ScriptTokenKind.Identifier;

                    tokens.Add(new ScriptToken(kind, start, i - start));
                    continue;
                }

                if (IsOperatorChar(c))
                {
                    // Operators are lexed one character at a time. Multi-character forms like "+="
                    // and "&&" need no special handling here: nothing downstream distinguishes them,
                    // and splitting them keeps the table small and the round trip exact.
                    i++;
                    tokens.Add(new ScriptToken(ScriptTokenKind.Operator, start, 1));
                    continue;
                }

                i++;
                tokens.Add(new ScriptToken(ScriptTokenKind.Unknown, start, 1));
            }

            return tokens;
        }

        /// <summary>Tokens with whitespace and comments removed — what a parser or scanner wants.</summary>
        public static IReadOnlyList<ScriptToken> TokenizeCode(string source) =>
            Tokenize(source).Where(t => !t.IsTrivia).ToList();

        private static int ScanNumber(string source, int i)
        {
            if (source[i] == '0' && i + 1 < source.Length && (source[i + 1] == 'x' || source[i + 1] == 'X'))
            {
                i += 2;
                while (i < source.Length && Uri.IsHexDigit(source[i]))
                    i++;
                return i;
            }

            while (i < source.Length && char.IsDigit(source[i]))
                i++;

            if (i < source.Length && source[i] == '.')
            {
                i++;
                while (i < source.Length && char.IsDigit(source[i]))
                    i++;
            }

            // A trailing 'f' is accepted by the compiler on float literals.
            if (i < source.Length && (source[i] == 'f' || source[i] == 'F'))
                i++;

            return i;
        }

        private static bool IsOperatorChar(char c) =>
            c is '{' or '}' or '(' or ')' or '[' or ']' or ';' or ',' or '.' or ':' or '?'
                or '+' or '-' or '*' or '/' or '%' or '=' or '!' or '<' or '>' or '&' or '|' or '^' or '~';
    }
}
