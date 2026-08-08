using System.Collections.Concurrent;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The permanent fidelity gate: every GFF JSON file under Module/ must round-trip through
    /// the document model byte-identically. Zero writes to disk. This suite must stay green in
    /// every later work package.
    /// </summary>
    public class RoundTripCorpusTests
    {
        [Test]
        public void EveryModuleGffJsonFile_RoundTripsByteIdentical()
        {
            var files = CorpusLocator.EnumerateGffJsonFiles().ToList();
            files.Count.Should().BeGreaterThan(15000, "the module corpus should be present");

            var failures = new ConcurrentBag<string>();
            var processed = 0;

            Parallel.ForEach(files, file =>
            {
                try
                {
                    var original = File.ReadAllBytes(file);
                    var written = JsonGffDocument.Parse(original).ToBytes();

                    if (!written.AsSpan().SequenceEqual(original))
                        failures.Add(DescribeMismatch(file, original, written));
                }
                catch (Exception ex)
                {
                    failures.Add($"{file}: {ex.GetType().Name}: {ex.Message}");
                }

                Interlocked.Increment(ref processed);
            });

            processed.Should().Be(files.Count);
            failures.Should().BeEmpty(
                $"all {files.Count} module files must round-trip byte-identically. " +
                $"{failures.Count} failed. First failures:\n{string.Join("\n", failures.Take(10))}");
        }

        internal static string DescribeMismatch(string file, byte[] original, byte[] written)
        {
            var length = Math.Min(original.Length, written.Length);
            var offset = 0;
            while (offset < length && original[offset] == written[offset])
                offset++;

            var contextStart = Math.Max(0, offset - 60);
            return $"{file}: first difference at offset {offset} " +
                   $"(original length {original.Length}, written length {written.Length})\n" +
                   $"  original: ...{Context(original, contextStart, offset)}\n" +
                   $"  written:  ...{Context(written, contextStart, offset)}";
        }

        private static string Context(byte[] content, int start, int offset)
        {
            var end = Math.Min(content.Length, offset + 40);
            return Encoding.UTF8.GetString(content, start, end - start)
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}
