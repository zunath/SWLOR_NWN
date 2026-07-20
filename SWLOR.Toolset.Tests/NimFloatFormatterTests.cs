using System.Collections.Concurrent;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Proves NimFloatFormatter reproduces every float/double literal in the module corpus,
    /// so values we format are indistinguishable from values nwn_gff would emit.
    /// </summary>
    public class NimFloatFormatterTests
    {
        [Test]
        public void EveryFloatLiteralInCorpus_ReformatsIdentically()
        {
            var distinctFailures = new ConcurrentDictionary<string, string>();
            var literalCount = 0L;

            Parallel.ForEach(CorpusLocator.EnumerateGffJsonFiles(), file =>
            {
                var document = JsonGffDocument.Parse(File.ReadAllBytes(file));
                foreach (var (type, raw) in EnumerateFloatTokens(document.Root))
                {
                    Interlocked.Increment(ref literalCount);
                    var text = Encoding.ASCII.GetString(raw);

                    // GFF floats are 32-bit: the packer parses this text to a float32 and a
                    // later unpack prints that float32 widened to double. Conformance must
                    // therefore go through the same funnel.
                    var value = NimFloatFormatter.Parse(text);
                    if (type == GffFieldType.Float)
                        value = (float)value;

                    var reformatted = NimFloatFormatter.Format(value);
                    if (reformatted != text)
                        distinctFailures.TryAdd(text, $"'{text}' reformatted as '{reformatted}' ({file})");
                }
            });

            literalCount.Should().BeGreaterThan(100000, "the corpus contains many float literals");
            distinctFailures.Should().BeEmpty(
                $"every float literal must reformat identically. Distinct failures:\n" +
                string.Join("\n", distinctFailures.Values.Take(25)));
        }

        [Test]
        public void Format_IntegralValues_KeepTrailingPointZero()
        {
            NimFloatFormatter.Format(45.0).Should().Be("45.0");
            NimFloatFormatter.Format(0.0).Should().Be("0.0");
            NimFloatFormatter.Format(-3.0).Should().Be("-3.0");
        }

        [Test]
        public void Format_Float32Widening_MatchesCorpusStyle()
        {
            // Verified against msvcrt %.16g: the 17-digit intermediate 1000000014901161|2
            // rounds down to 16 digits in fixed notation.
            NimFloatFormatter.Format(0.1f).Should().Be("0.1000000014901161");
        }

        private static IEnumerable<(GffFieldType Type, byte[] Raw)> EnumerateFloatTokens(JsonGffStruct target)
        {
            foreach (var (_, field) in target.Entries)
            {
                switch (field.Type)
                {
                    case GffFieldType.Float:
                    case GffFieldType.Double:
                        yield return (field.Type, field.RawValue!);
                        break;
                    case GffFieldType.Struct:
                        foreach (var token in EnumerateFloatTokens(field.Struct!))
                            yield return token;
                        break;
                    case GffFieldType.List:
                        foreach (var element in field.Elements!)
                        foreach (var token in EnumerateFloatTokens(element))
                            yield return token;
                        break;
                }
            }
        }
    }
}
