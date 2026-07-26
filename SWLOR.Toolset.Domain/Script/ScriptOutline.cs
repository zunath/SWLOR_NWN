using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>A function defined in the file being edited.</summary>
    /// <param name="Name">Function name.</param>
    /// <param name="ReturnType">Declared return type.</param>
    /// <param name="Parameters">Parameter list as written, for display.</param>
    /// <param name="Offset">Offset of the name token.</param>
    /// <param name="Line">1-based line of the declaration.</param>
    /// <param name="IsDefinition">True when a body follows; false for a forward declaration.</param>
    public sealed record ScriptFunctionDeclaration(
        string Name, string ReturnType, string Parameters, int Offset, int Line, bool IsDefinition)
    {
        public string Display => $"{Name}({Parameters})";
    }

    /// <summary>
    /// What a single file declares: its includes, its functions and its variables. Built straight
    /// from the token stream rather than from a parse tree.
    /// </summary>
    /// <remarks>
    /// A full recursive-descent parser is the eventual home for this (WPS1.2), but the editor needs
    /// only names and positions to drive the outline, go-to-definition and completion, and a token
    /// scan delivers those on half-typed source without any error-recovery machinery. Where the scan
    /// cannot be certain it stays silent, which matches the plan's rule that tier-1 language services
    /// degrade to "no answer" rather than to a wrong one.
    /// </remarks>
    public sealed class ScriptOutline
    {
        private ScriptOutline(
            IReadOnlyList<ScriptFunctionDeclaration> functions,
            IReadOnlyList<string> includes,
            IReadOnlyList<string> variables)
        {
            Functions = functions;
            Includes = includes;
            Variables = variables;
        }

        public IReadOnlyList<ScriptFunctionDeclaration> Functions { get; }

        /// <summary>Resrefs named by <c>#include "x"</c>, in file order.</summary>
        public IReadOnlyList<string> Includes { get; }

        /// <summary>Every declared variable name — globals, locals and parameters, undifferentiated.</summary>
        public IReadOnlyList<string> Variables { get; }

        public static ScriptOutline Empty { get; } =
            new(Array.Empty<ScriptFunctionDeclaration>(), Array.Empty<string>(), Array.Empty<string>());

        public static ScriptOutline Build(string source)
        {
            var tokens = ScriptLexer.TokenizeCode(source);
            var functions = new List<ScriptFunctionDeclaration>();
            var includes = new List<string>();
            var variables = new List<string>();

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token.Kind == ScriptTokenKind.Preprocessor &&
                    token.Text(source).SequenceEqual("#include") &&
                    i + 1 < tokens.Count && tokens[i + 1].Kind == ScriptTokenKind.String)
                {
                    includes.Add(Unquote(tokens[i + 1].ToText(source)));
                    i++;
                    continue;
                }

                // A declaration is <type> <identifier> followed by '(' for a function or by ';' / '='
                // / ',' for a variable. Anything else is a call, an expression or noise.
                if (token.Kind is not (ScriptTokenKind.TypeKeyword or ScriptTokenKind.Identifier))
                    continue;

                if (i + 1 >= tokens.Count || tokens[i + 1].Kind != ScriptTokenKind.Identifier)
                    continue;

                // Only a real type keyword starts a declaration. Allowing any identifier here would
                // read "GetName (x)" as declaring something.
                if (token.Kind != ScriptTokenKind.TypeKeyword)
                    continue;

                var nameToken = tokens[i + 1];
                var name = nameToken.ToText(source);

                if (i + 2 < tokens.Count && tokens[i + 2].ToText(source) == "(")
                {
                    var close = FindMatchingParen(tokens, source, i + 2);
                    if (close < 0)
                        continue;

                    var argStart = tokens[i + 2].End;
                    var argText = source[argStart..tokens[close].Start].Trim();
                    var isDefinition = close + 1 < tokens.Count && tokens[close + 1].ToText(source) == "{";

                    functions.Add(new ScriptFunctionDeclaration(
                        name, token.ToText(source), argText, nameToken.Start,
                        LineOf(source, nameToken.Start), isDefinition));

                    // Parameter names are variables in scope for the body.
                    foreach (var p in SplitNames(argText))
                        variables.Add(p);

                    i = close;
                    continue;
                }

                if (i + 2 < tokens.Count && tokens[i + 2].ToText(source) is ";" or "=" or ",")
                {
                    variables.Add(name);

                    // One declaration can introduce several names: "int i, iBegin, iEnd;" is common in
                    // this module's legacy scripts, and recording only the first left the rest looking
                    // undefined to the binder. Walk the declarator list to its semicolon, taking the
                    // identifier after each top-level comma and skipping any initialiser expression.
                    var depth = 0;
                    var expectName = false;
                    for (var j = i + 2; j < tokens.Count; j++)
                    {
                        var text = tokens[j].ToText(source);

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

                        if (depth > 0)
                            continue;

                        if (text == ";")
                        {
                            i = j;
                            break;
                        }

                        if (text == ",")
                        {
                            expectName = true;
                            continue;
                        }

                        if (expectName && tokens[j].Kind == ScriptTokenKind.Identifier)
                        {
                            variables.Add(text);
                            expectName = false;
                        }
                        else if (text != "=")
                        {
                            // Inside an initialiser; the next comma still starts a new declarator.
                            expectName = false;
                        }
                    }
                }
            }

            return new ScriptOutline(functions, includes, variables.Distinct(StringComparer.Ordinal).ToList());
        }

        /// <summary>1-based line number for an offset.</summary>
        public static int LineOf(string source, int offset)
        {
            var line = 1;
            for (var i = 0; i < offset && i < source.Length; i++)
                if (source[i] == '\n')
                    line++;

            return line;
        }

        private static int FindMatchingParen(IReadOnlyList<ScriptToken> tokens, string source, int openIndex)
        {
            var depth = 0;
            for (var i = openIndex; i < tokens.Count; i++)
            {
                var text = tokens[i].ToText(source);
                if (text == "(")
                    depth++;
                else if (text == ")")
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return -1;
        }

        private static IEnumerable<string> SplitNames(string parameterList)
        {
            foreach (var part in parameterList.Split(','))
            {
                var text = part.Trim();
                var eq = text.IndexOf('=');
                if (eq >= 0)
                    text = text[..eq].Trim();

                var space = text.LastIndexOf(' ');
                if (space > 0)
                    yield return text[(space + 1)..].Trim();
            }
        }

        private static string Unquote(string text) =>
            text.Length >= 2 && text[0] == '"' ? text.Trim('"') : text;
    }
}
