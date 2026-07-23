using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;
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
