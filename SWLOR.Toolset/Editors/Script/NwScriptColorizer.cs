using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Editors.Script
{
    /// <summary>
    /// Colours NWScript by running the same lexer the rest of the language service uses, rather than
    /// a separate .xshd grammar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One lexer means highlighting can never drift from completion: a token the parser treats as a
    /// string is painted as a string, always. A second grammar would be a second definition of the
    /// language, and the two would disagree eventually.
    /// </para>
    /// <para>
    /// The palette is derived from ToolsetTheme.axaml rather than imported from another editor.
    /// Seven of the nine slots are existing theme tokens; only the type and number hues are new, and
    /// both are analogous to the hyperspace-blue accent. The load-bearing contrast is engine
    /// functions (gold) against the author's own locals (plain ink) - reading unfamiliar legacy
    /// NWScript, the first thing you want to see is how much of a line is engine surface.
    /// </para>
    /// </remarks>
    public sealed class NwScriptColorizer : DocumentColorizingTransformer
    {
        private static readonly IBrush Comment = New("#6C7683");      // InkTertiaryBrush
        private static readonly IBrush Keyword = New("#5B9BF5");      // AccentBrush
        private static readonly IBrush TypeName = New("#8FC8E8");     // new, analogous to the accent
        private static readonly IBrush EngineFunction = New("#E4CE9A"); // GoldBrush
        private static readonly IBrush Constant = New("#D9A155");     // AmberBrush
        private static readonly IBrush StringLiteral = New("#5FBE8C");// GreenBrush
        private static readonly IBrush Number = New("#B49BE0");       // new
        private static readonly IBrush Preprocessor = New("#98A2B0"); // InkSecondaryBrush
        private static readonly IBrush Identifier = New("#DDE3EB");   // InkBrush

        private string _cachedText = string.Empty;
        private IReadOnlyList<ScriptToken> _cachedTokens = Array.Empty<ScriptToken>();

        /// <summary>Engine function names, so a call can be told from a local. Set by the editor view.</summary>
        public Func<string, bool>? IsEngineFunction { get; set; }

        /// <summary>Engine constant names, likewise.</summary>
        public Func<string, bool>? IsEngineConstant { get; set; }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (line.Length == 0)
                return;

            var text = CurrentContext.Document.Text;

            // Lexing the whole document per line would be quadratic on a 13,000-line file; the token
            // list is rebuilt only when the text actually changes, then shared across every line of
            // that render pass.
            if (!ReferenceEquals(text, _cachedText) && text != _cachedText)
            {
                _cachedText = text;
                _cachedTokens = ScriptLexer.Tokenize(text);
            }

            var lineStart = line.Offset;
            var lineEnd = line.EndOffset;

            foreach (var token in _cachedTokens)
            {
                if (token.End <= lineStart)
                    continue;

                if (token.Start >= lineEnd)
                    break;

                var brush = BrushFor(token, text);
                if (brush == null)
                    continue;

                var start = Math.Max(token.Start, lineStart);
                var end = Math.Min(token.End, lineEnd);
                if (start >= end)
                    continue;

                var isComment = token.Kind is ScriptTokenKind.LineComment or ScriptTokenKind.BlockComment;
                ChangeLinePart(start, end, element =>
                {
                    element.TextRunProperties.SetForegroundBrush(brush);
                    if (isComment)
                        element.TextRunProperties.SetTypeface(new Typeface(
                            element.TextRunProperties.Typeface.FontFamily, FontStyle.Italic));
                });
            }
        }

        private IBrush? BrushFor(ScriptToken token, string source) => token.Kind switch
        {
            ScriptTokenKind.LineComment or ScriptTokenKind.BlockComment => Comment,
            ScriptTokenKind.Keyword => Keyword,
            ScriptTokenKind.TypeKeyword => TypeName,
            ScriptTokenKind.String => StringLiteral,
            ScriptTokenKind.Number => Number,
            ScriptTokenKind.Preprocessor => Preprocessor,
            ScriptTokenKind.Identifier => IdentifierBrush(token.ToText(source)),
            _ => null
        };

        private IBrush IdentifierBrush(string name)
        {
            if (IsEngineConstant?.Invoke(name) == true)
                return Constant;

            if (IsEngineFunction?.Invoke(name) == true)
                return EngineFunction;

            // A SCREAMING_CASE name the header does not know is still almost certainly a constant -
            // module includes define their own - so treat the casing as the hint.
            if (name.Length > 1 && name.All(c => char.IsUpper(c) || char.IsDigit(c) || c == '_'))
                return Constant;

            return Identifier;
        }

        private static IBrush New(string hex) => new SolidColorBrush(Color.Parse(hex));
    }
}
