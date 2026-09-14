// SPDX-License-Identifier: MIT

using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.NWN.Formats.Corpus.Tests;

/// <summary>
/// Closes the verification gap left by <see cref="MdlCorpusScopeTests"/>: that test only reads a
/// four-byte signature per file to classify binary vs ASCII, it never parses. This test actually
/// parses every binary and ASCII MDL under the SWLOR_Haks corpus with <see cref="MdlReader"/>, so
/// the "fully parsed" claim is backed by a real full sweep instead of a 600-file sample plus a
/// signature count.
/// </summary>
[Category(MdlCorpusScopeTests.CorpusCategory)]
public sealed class HakMdlParseSweepTests
{
    /// <summary>Requires every current HAK model to parse, including the repaired phenotype-22 robes.</summary>
    [Test]
    public void EveryHakMdlParses()
    {
        var paths = LicensedCorpus.HakSourceDirectories()
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.mdl", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        paths.Should().NotBeEmpty();

        var requested = paths.Length;
        var executed = 0;
        var failures = new List<string>();

        foreach (var path in paths)
        {
            var identity = "hak:" + Path.GetRelativePath(LicensedCorpus.HaksRoot, path)
                .Replace('\\', '/');

            // Read and parse one file at a time; the bytes and model both go out of scope at the
            // end of the loop body so memory stays bounded across the complete corpus.
            var bytes = File.ReadAllBytes(path);
            try
            {
                _ = new MdlReader().Parse(bytes);
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add($"{identity}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(
            $"Hak MDL parse sweep requested={requested} executed={executed} failed={failures.Count} " +
            "skipped=0");
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(50)));

        failures.Should().BeEmpty();
        executed.Should().Be(requested);
    }
}
