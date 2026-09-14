using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Editing;
using System.Runtime.ExceptionServices;
using System.Text;

namespace SWLOR.Toolset.Tests
{
    public class JsonStringCodecTests
    {
        [Test]
        public void EditableNwnText_RoundTripsNativeWindows1252Bytes()
        {
            var rawToken = new byte[] { (byte)'"', (byte)'I', (byte)'t', 0x92, (byte)'s', (byte)'"' };

            var decoded = JsonStringCodec.Decode(rawToken);

            decoded.Should().Be("It’s");
            JsonStringCodec.Encode(decoded).Should().Equal(rawToken);
        }

        [Test, NonParallelizable]
        public void LegacyEncodingDetection_DoesNotThrowAFirstChanceDecoderException()
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
                JsonStringCodec.Decode([(byte)'"', (byte)'I', (byte)'t', 0x92, (byte)'s', (byte)'"'])
                    .Should().Be("It’s");
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= observe;
            }

            decoderExceptions.Should().Be(0,
                "legacy text is normal input and must not fill the debugger console with caught exceptions");
        }

        [Test]
        public void UnicodeEscape_DecodesAsUnicodeWhileRawBytesUseWindows1252()
        {
            var escaped = System.Text.Encoding.ASCII.GetBytes("\"It\\u2019s\"");

            JsonStringCodec.Decode(escaped).Should().Be("It’s");
        }

        [Test]
        public void EditableNwnText_RejectsCharactersOutsideWindows1252()
        {
            var act = () => JsonStringCodec.Encode("Not representable: 😀");

            act.Should().Throw<EncoderFallbackException>();
        }

        /// <summary>First assignments must materialize valid JSON tokens for newly created text fields.</summary>
        [TestCase("")]
        [TestCase("New text")]
        public void TextSettersInitializeEmptySourceTokens(string text)
        {
            var localized = new LocStringEntry("0", []);
            var scalar = JsonGffField.CreateScalar(GffFieldType.CExoString, []);

            localized.SetText(text);
            scalar.SetString(text);

            localized.RawText.Should().Equal(JsonStringCodec.Encode(text));
            scalar.RawValue.Should().Equal(JsonStringCodec.Encode(text));
            localized.GetText().Should().Be(text);
            scalar.GetString().Should().Be(text);
        }

        /// <summary>Saving an unchanged value must retain native, UTF-8 and escaped source tokens.</summary>
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void TextSettersPreserveUnchangedSourceTokens(bool utf8, bool escaped)
        {
            var raw = escaped ? Encoding.ASCII.GetBytes("\"It\\u2019s\"") : JsonStringCodec.Encode("It’s", utf8);
            var localized = new LocStringEntry("0", raw);
            var scalar = JsonGffField.CreateScalar(GffFieldType.CExoString, raw);
            using var session = new DocumentSession("encoding.utc.json", new JsonGffDocument("UTC ", new()));

            using (session.Begin("Save unchanged text"))
            {
                localized.SetText(localized.GetText());
                scalar.SetString(scalar.GetString());
            }

            localized.RawText.Should().Equal(raw);
            scalar.RawValue.Should().Equal(raw);
            session.UndoStack.Entries.Should().BeEmpty();
            session.UndoStack.IsDirty.Should().BeFalse();
        }

        /// <summary>Editing imported text retains UTF-8, while native text retains Windows-1252.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void TextSettersRetainNonAsciiSourceEncoding(bool utf8)
        {
            var raw = JsonStringCodec.Encode("It’s", utf8);
            var localized = new LocStringEntry("0", raw);
            var scalar = JsonGffField.CreateScalar(GffFieldType.CExoString, raw);
            using var session = new DocumentSession("encoding.utc.json", new JsonGffDocument("UTC ", new()));

            using (session.Begin("Edit text"))
            {
                localized.SetText("It’s edited");
                scalar.SetString("It’s edited");
            }

            var expected = JsonStringCodec.Encode("It’s edited", utf8);
            localized.RawText.Should().Equal(expected);
            scalar.RawValue.Should().Equal(expected);
            session.UndoStack.Entries.Should().HaveCount(1);
            session.UndoStack.Undo();
            localized.RawText.Should().Equal(raw);
            scalar.RawValue.Should().Equal(raw);
            session.UndoStack.Redo();
            localized.RawText.Should().Equal(expected);
            scalar.RawValue.Should().Equal(expected);
        }

        [Test]
        public void RealCreatureDescription_WithWindows1252Apostrophe_SurvivesAnEditCycle()
        {
            var path = Path.Combine(
                CorpusLocator.ModuleDirectory, "utc", "nar_slavercaptn.utc.json");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);
            var entry = document.Root.Get("Description").LocStringEntries!.Single();

            var text = entry.GetText();
            text.Should().Contain("Nar Shaddaa’s shadow ports");
            entry.SetText(text);

            document.ToBytes().Should().Equal(
                original, "reading and writing an editable field must preserve its native 0x92 byte");
        }
    }
}
