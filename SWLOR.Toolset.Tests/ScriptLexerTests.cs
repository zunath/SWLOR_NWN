using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Syntax;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The lexer's correctness gate is losslessness: concatenating every token's text must reproduce
    /// the input exactly. Stated that way it is impossible to fudge and catches essentially every
    /// lexer bug - the same shape as the GFF layer's round-trip gate.
    /// </summary>
    public class ScriptLexerTests
    {
        private static string NssDirectory => Path.Combine(CorpusLocator.ModuleDirectory, "nss");

        private static string EngineHeaderPath => Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.NWN.API", "NWN", "nwscript-8193.37.nss");

        private static string Concat(string source) =>
            string.Concat(ScriptLexer.Tokenize(source).Select(t => t.ToText(source)));

        [Test]
        public void EveryModuleScript_LexesLosslessly()
        {
            var scripts = Directory.EnumerateFiles(NssDirectory, "*.nss").ToList();
            scripts.Should().NotBeEmpty();

            var failures = new List<string>();
            foreach (var path in scripts)
            {
                var source = ScriptTextDocument.Load(path).Text;
                if (Concat(source) != source)
                    failures.Add(Path.GetFileName(path));
            }

            failures.Should().BeEmpty("every one of the {0} module scripts must round-trip through the lexer", scripts.Count);
        }

        [Test]
        public void TheEngineHeader_LexesLosslessly()
        {
            if (!File.Exists(EngineHeaderPath))
                Assert.Ignore("engine header not present");

            var source = ScriptTextDocument.Load(EngineHeaderPath).Text;
            Concat(source).Should().Be(source, "the 13,870-line header is the largest input the lexer sees");
        }

        [Test]
        public void EveryModuleScript_HasNoUnknownTokens()
        {
            var offenders = new List<string>();
            foreach (var path in Directory.EnumerateFiles(NssDirectory, "*.nss"))
            {
                var source = ScriptTextDocument.Load(path).Text;
                var unknown = ScriptLexer.Tokenize(source).Where(t => t.Kind == ScriptTokenKind.Unknown).ToList();

                // colors_inc.nss legitimately carries raw colour bytes inside string literals; those
                // sit inside String tokens, so they must not surface as Unknown.
                if (unknown.Count > 0)
                    offenders.Add($"{Path.GetFileName(path)}: {unknown.Count}");
            }

            offenders.Should().BeEmpty("known-good source should lex entirely into known token kinds");
        }

        [Test]
        public void TokensCoverEveryCharacterExactlyOnce()
        {
            const string src = "void main()\n{\n    int n = 0x1F; // hi\n}";
            var tokens = ScriptLexer.Tokenize(src);

            var offset = 0;
            foreach (var t in tokens)
            {
                t.Start.Should().Be(offset, "tokens must be contiguous with no gaps or overlaps");
                offset += t.Length;
            }

            offset.Should().Be(src.Length);
        }

        [Test]
        public void ClassifiesTheBasicShapes()
        {
            const string src = "#include \"x\"\nvoid main() { int n = 1; if (n) return; }";
            var code = ScriptLexer.TokenizeCode(src);

            code[0].Kind.Should().Be(ScriptTokenKind.Preprocessor);
            code[0].ToText(src).Should().Be("#include");
            code[1].Kind.Should().Be(ScriptTokenKind.String);
            code[2].Kind.Should().Be(ScriptTokenKind.TypeKeyword, "void is a type");
            code[3].Kind.Should().Be(ScriptTokenKind.Identifier, "main is not a keyword");

            code.Should().Contain(t => t.Kind == ScriptTokenKind.Keyword && t.ToText(src) == "if");
            code.Should().Contain(t => t.Kind == ScriptTokenKind.Number && t.ToText(src) == "1");
        }

        [TestCase("0x1F")]
        [TestCase("1.5")]
        [TestCase("1.5f")]
        [TestCase("42")]
        [TestCase(".5")]
        public void LexesNumberForms(string literal)
        {
            var src = $"int n = {literal};";
            var code = ScriptLexer.TokenizeCode(src);

            code.Should().ContainSingle(t => t.Kind == ScriptTokenKind.Number && t.ToText(src) == literal);
        }

        [Test]
        public void EngineStructureTypes_AreTypes()
        {
            const string src = "effect e; location l; itemproperty ip; json j; sqlquery q; cassowary c; talent t; event ev;";
            var code = ScriptLexer.TokenizeCode(src);

            code.Where(t => t.Kind == ScriptTokenKind.TypeKeyword)
                .Should().HaveCount(8, "all eight ENGINE_STRUCTURE types are types");
        }

        [Test]
        public void UnterminatedBlockComment_RunsToEndOfFileWithoutThrowing()
        {
            const string src = "void main() { } /* never closed";
            var tokens = ScriptLexer.Tokenize(src);

            tokens[^1].Kind.Should().Be(ScriptTokenKind.BlockComment);
            Concat(src).Should().Be(src);
        }

        [Test]
        public void UnterminatedString_StopsAtEndOfLine()
        {
            const string src = "string s = \"oops\nint n = 1;";
            var tokens = ScriptLexer.Tokenize(src);

            // Stopping at the newline keeps one stray quote from colouring the rest of the file.
            tokens.Should().Contain(t => t.Kind == ScriptTokenKind.Number);
            Concat(src).Should().Be(src);
        }

        /// <summary>
        /// NWScript has no escape sequences. A backslash is a literal character and the string ends
        /// at the very next quote. Treating '\"' as a C-style escape made the lexer run past the
        /// closing quote of real corpus lines like <c>return "/\/\\";</c> in dmfi_plychat_exe.nss —
        /// ASCII art, not an escape — and swallow the rest of the file as one string. Found by the
        /// zero-false-positives gate, not by reasoning.
        /// </summary>
        [Test]
        public void BackslashInsideAStringIsLiteral()
        {
            const string src = "string s = \"/\\/\\\\\";\nint n = 1;";
            var code = ScriptLexer.TokenizeCode(src);

            var str = code.First(t => t.Kind == ScriptTokenKind.String);
            str.ToText(src).Should().Be("\"/\\/\\\\\"", "the string ends at the next quote, escapes or not");
            code.Should().Contain(t => t.Kind == ScriptTokenKind.Number,
                "the rest of the file must still lex as code");
        }

        [Test]
        public void EveryModuleScript_LexesTheSameNumberOfQuotesAsStringTokens()
        {
            // A cross-check on the rule above: with no escapes, every pair of quotes on a line is
            // exactly one string token, so a lexer that swallows past a closing quote shows up here.
            foreach (var path in Directory.EnumerateFiles(NssDirectory, "*.nss"))
            {
                var source = ScriptTextDocument.Load(path).Text;
                var tokens = ScriptLexer.Tokenize(source);

                foreach (var token in tokens.Where(t => t.Kind == ScriptTokenKind.String))
                {
                    var text = token.ToText(source);
                    text.Should().NotContain("\n", "a string token must never span a line");
                }
            }
        }

        [Test]
        public void HalfTypedInput_DoesNotThrowAndStaysLossless()
        {
            // Completion lexes broken code constantly; truncation must never throw.
            const string full = "void main()\n{\n    object o = GetNearestCreature(CREATURE_TYPE_PLAYER_CHAR,\n}";
            for (var cut = 0; cut <= full.Length; cut++)
            {
                var src = full[..cut];
                Concat(src).Should().Be(src, "truncation at {0} must stay lossless", cut);
            }
        }

        [Test]
        public void TokenizeCode_DropsTriviaOnly()
        {
            const string src = "int /* c */ n; // trailing";
            var all = ScriptLexer.Tokenize(src);
            var code = ScriptLexer.TokenizeCode(src);

            code.Should().OnlyContain(t => !t.IsTrivia);
            code.Count.Should().BeLessThan(all.Count);
        }

        [Test]
        public void RawHighBytesInsideAStringStayInsideIt()
        {
            // colors_inc.nss's actual shape: colour codes embedded in a literal.
            var src = "string COLOR = \"" + (char)0x80 + (char)0x81 + "\";";
            var code = ScriptLexer.TokenizeCode(src);

            code.Should().ContainSingle(t => t.Kind == ScriptTokenKind.String);
            code.Should().NotContain(t => t.Kind == ScriptTokenKind.Unknown);
        }
    }
}
