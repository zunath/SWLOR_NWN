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
    /// <summary>
    /// These phenotype-22 robe models are internally inconsistent (pointers/counts that don't
    /// agree with the rest of the file) independent of the reader. They are pinned here the same
    /// way <c>FormatsCorpusTests</c> pins <c>ipf_shol197.plt</c>/<c>ipf_shor197.plt</c> as
    /// expected-invalid, so a genuine reader regression on any other file still fails the sweep.
    /// </summary>
    private static readonly HashSet<string> KnownInvalidHakMdls = new(StringComparer.OrdinalIgnoreCase)
    {
        "pfe22_robe027.mdl",
        "pfe22_robe172.mdl",
        "pfe22_robe174.mdl",
        "pfe22_robe200.mdl",
        "pfh22_robe172.mdl",
        "pfh22_robe174.mdl",
        "pfo22_robe027.mdl",
        "pfo22_robe172.mdl",
        "pfo22_robe174.mdl",
        "pfo22_robe200.mdl",
    };

    [Test]
    public void EveryHakMdlParsesOrIsPinnedAsKnownCorrupt()
    {
        var paths = LicensedCorpus.HakSourceDirectories()
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.mdl", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        paths.Should().NotBeEmpty();

        var requested = paths.Length;
        var executed = 0;
        var expectedInvalid = new List<string>();
        var failures = new List<string>();
        var seenKnownInvalid = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            var fileName = Path.GetFileName(path);
            var identity = "hak:" + Path.GetRelativePath(LicensedCorpus.HaksRoot, path)
                .Replace('\\', '/');
            var isKnownInvalid = KnownInvalidHakMdls.Contains(fileName);

            // Read and parse one file at a time; the bytes and model both go out of scope at the
            // end of the loop body so memory stays bounded across the ~54k-file sweep.
            var bytes = File.ReadAllBytes(path);
            try
            {
                _ = new MdlReader().Parse(bytes);
                if (isKnownInvalid)
                {
                    failures.Add(
                        $"{identity}: expected NwnFormatException (pinned known-corrupt) but parsing succeeded.");
                    continue;
                }
                executed++;
            }
            catch (NwnFormatException ex) when (isKnownInvalid)
            {
                seenKnownInvalid.Add(fileName);
                expectedInvalid.Add($"{identity}: {ex.Message}");
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add($"{identity}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(
            $"Hak MDL parse sweep requested={requested} executed={executed} failed={failures.Count} " +
            $"skipped=0 expected-invalid={expectedInvalid.Count}");
        if (expectedInvalid.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, expectedInvalid));
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(50)));

        failures.Should().BeEmpty();
        seenKnownInvalid.Should().BeEquivalentTo(
            KnownInvalidHakMdls,
            "every pinned known-corrupt robe model must actually exist in the corpus and throw " +
            "NwnFormatException, otherwise the pin is stale");
        executed.Should().Be(requested);
    }
}
