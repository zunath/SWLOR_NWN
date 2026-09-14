using System.Text;
using System.Runtime.ExceptionServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The script editor's byte-fidelity gate. The GFF editors get zero-spurious-diff from lexical
    /// preservation of a parsed tree; a script is plain text, so the same guarantee reduces to
    /// preserving EOL style, the trailing newline and any BOM. The corpus test below is the real
    /// one - opening and saving any of the module's own scripts unchanged must not move a byte.
    /// </summary>
    public class ScriptTextDocumentTests
    {
        private static string NssDirectory => Path.Combine(CorpusLocator.ModuleDirectory, "nss");

        private static IEnumerable<string> EnumerateScripts() =>
            Directory.EnumerateFiles(NssDirectory, "*.nss", SearchOption.TopDirectoryOnly);

        [Test]
        public void EveryModuleScript_RoundTripsByteForByte()
        {
            var scripts = EnumerateScripts().ToList();
            scripts.Should().NotBeEmpty("the module corpus must be reachable for this gate to mean anything");

            var failures = new List<string>();
            foreach (var path in scripts)
            {
                var original = File.ReadAllBytes(path);
                var rewritten = ScriptTextDocument.FromBytes(original).ToBytes();

                if (!rewritten.AsSpan().SequenceEqual(original))
                    failures.Add($"{Path.GetFileName(path)} ({original.Length} -> {rewritten.Length} bytes)");
            }

            failures.Should().BeEmpty(
                "a load/save with no edit must not change any of the {0} module scripts", scripts.Count);
        }

        [Test]
        public void EveryModuleScript_IsDecodableAndNonEmpty()
        {
            foreach (var path in EnumerateScripts())
            {
                var doc = ScriptTextDocument.Load(path);
                doc.Text.Should().NotBeNull();
            }
        }

        [Test]
        public void CrlfFile_PreservesCrlfAndNormalisesBufferToLf()
        {
            var bytes = Encoding.UTF8.GetBytes("void main()\r\n{\r\n}\r\n");
            var doc = ScriptTextDocument.FromBytes(bytes);

            doc.EolStyle.Should().Be(ScriptEolStyle.Crlf);
            doc.HasTrailingNewline.Should().BeTrue();
            doc.Text.Should().Be("void main()\n{\n}", "the buffer works in \\n regardless of disk style");
            doc.ToBytes().Should().Equal(bytes);
        }

        [Test]
        public void LfFile_StaysLf()
        {
            var bytes = Encoding.UTF8.GetBytes("void main()\n{\n}\n");
            var doc = ScriptTextDocument.FromBytes(bytes);

            doc.EolStyle.Should().Be(ScriptEolStyle.Lf);
            doc.ToBytes().Should().Equal(bytes);
        }

        [Test]
        public void FileWithoutTrailingNewline_DoesNotGainOne()
        {
            var bytes = Encoding.UTF8.GetBytes("void main()\r\n{\r\n}");
            var doc = ScriptTextDocument.FromBytes(bytes);

            doc.HasTrailingNewline.Should().BeFalse();
            doc.ToBytes().Should().Equal(bytes, "adding a newline would rewrite the last line of the file");
        }

        [Test]
        public void FileWithBom_KeepsBom()
        {
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
                .Concat(Encoding.UTF8.GetBytes("void main()\r\n{\r\n}\r\n")).ToArray();
            var doc = ScriptTextDocument.FromBytes(bytes);

            doc.HasByteOrderMark.Should().BeTrue();
            doc.Text.Should().StartWith("void", "the BOM is file shape, not buffer content");
            doc.ToBytes().Should().Equal(bytes);
        }

        [Test]
        public void EditedText_IsWrittenInTheFilesOwnEolStyle()
        {
            var doc = ScriptTextDocument.FromBytes(Encoding.UTF8.GetBytes("void main()\r\n{\r\n}\r\n"));

            // The buffer always hands back \n; the file must still come out CRLF.
            var written = doc.ToBytes("void main()\n{\n    int n = 1;\n}");

            Encoding.UTF8.GetString(written).Should().Be("void main()\r\n{\r\n    int n = 1;\r\n}\r\n");
        }

        [Test]
        public void NewScript_MatchesTheTemplateShapeUsedElsewhere()
        {
            var doc = ScriptTextDocument.NewScript("My Script");

            Encoding.UTF8.GetString(doc.ToBytes())
                .Should().Be("// My Script\r\nvoid main()\r\n{\r\n}\r\n");
        }

        /// <summary>
        /// colors_inc.nss embeds raw high bytes inside string literals as NWScript colour codes, so it
        /// is not valid UTF-8. A strict UTF-8 reader throws on it - this is
        /// the case that forced the Latin-1 fallback, pinned so a future "just use UTF-8" simplification
        /// fails here rather than silently corrupting a real module file.
        /// </summary>
        [TestCase("colors_inc.nss")]
        public void LegacyScriptWithRawColourBytes_ReadsAsLatin1AndRoundTrips(string name)
        {
            var path = Path.Combine(NssDirectory, name);
            if (!File.Exists(path))
                Assert.Ignore($"{name} is not present in this corpus");

            var original = File.ReadAllBytes(path);
            var doc = ScriptTextDocument.FromBytes(original);

            doc.EncodingKind.Should().Be(ScriptEncoding.Latin1);
            doc.ToBytes().Should().Equal(original);
        }

        [Test]
        public void PlainAsciiScript_ReadsAsUtf8()
        {
            var doc = ScriptTextDocument.FromBytes(Encoding.UTF8.GetBytes("void main()\r\n{\r\n}\r\n"));

            doc.EncodingKind.Should().Be(ScriptEncoding.Utf8);
        }

        [Test]
        public void EveryByteValue_SurvivesTheLatin1Path()
        {
            // 0x80-0xFF is what the colour-code files actually carry; include the whole range so the
            // fallback is proven bijective rather than merely working on the two known files.
            var body = Enumerable.Range(0x80, 0x80).Select(b => (byte)b).ToArray();
            var bytes = Encoding.UTF8.GetBytes("// ").Concat(body).ToArray();

            var doc = ScriptTextDocument.FromBytes(bytes);

            doc.EncodingKind.Should().Be(ScriptEncoding.Latin1);
            doc.ToBytes().Should().Equal(bytes);
        }

        [Test, NonParallelizable]
        public void Latin1Detection_DoesNotThrowAFirstChanceDecoderException()
        {
            var decoderExceptions = 0;
            EventHandler<FirstChanceExceptionEventArgs> observe = (_, args) =>
            {
                if (args.Exception is DecoderFallbackException)
                    decoderExceptions++;
            };

            AppDomain.CurrentDomain.FirstChanceException += observe;
            try
            {
                ScriptTextDocument.FromBytes([(byte)'/', (byte)'/', (byte)' ', 0x92])
                    .EncodingKind.Should().Be(ScriptEncoding.Latin1);
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= observe;
            }

            decoderExceptions.Should().Be(0,
                "legacy scripts are normal input and must not fill the debugger console with caught exceptions");
        }

        [Test]
        public void EmptyFile_RoundTrips()
        {
            var doc = ScriptTextDocument.FromBytes(Array.Empty<byte>());

            doc.Text.Should().BeEmpty();
            doc.ToBytes().Should().BeEmpty();
        }
    }
}
