using System.Collections.Concurrent;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Byte-identical round-trip over a per-folder sample of the module corpus. The full-corpus
    /// gate lives in RoundTripCorpusTests; this sample keeps the inner development loop fast.
    /// </summary>
    public class RoundTripSampleTests
    {
        private const int FilesPerFolder = 40;

        [Test]
        public void SampledModuleFiles_RoundTripByteIdentical()
        {
            var files = CorpusLocator.GffFolders
                .SelectMany(folder =>
                {
                    var path = Path.Combine(CorpusLocator.ModuleDirectory, folder);
                    return Directory.Exists(path)
                        ? Directory.EnumerateFiles(path, "*.json").Take(FilesPerFolder)
                        : Enumerable.Empty<string>();
                })
                .ToList();

            files.Should().NotBeEmpty();

            var failures = new ConcurrentBag<string>();
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
            });

            failures.Should().BeEmpty(
                $"all sampled files must round-trip byte-identically. Failures:\n{string.Join("\n", failures.Take(20))}");
        }

        private static string DescribeMismatch(string file, byte[] original, byte[] written)
        {
            var length = Math.Min(original.Length, written.Length);
            var offset = 0;
            while (offset < length && original[offset] == written[offset])
                offset++;

            var contextStart = Math.Max(0, offset - 60);
            var originalContext = Context(original, contextStart, offset);
            var writtenContext = Context(written, contextStart, offset);

            return $"{file}: first difference at offset {offset} " +
                   $"(original length {original.Length}, written length {written.Length})\n" +
                   $"  original: ...{originalContext}\n" +
                   $"  written:  ...{writtenContext}";
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
