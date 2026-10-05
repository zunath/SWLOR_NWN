using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;
using Nwn.Authoring.Documents.Native;
using Nwn.Formats.Erf;
using Nwn.Formats.Resources;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests;

[TestFixture]
public sealed class NativeArchiveKeyCompatibilityTests
{
    [Test]
    public void AssignedPackedHakStackPreservesEveryKeyAndTheLegacyWinningPayloads()
    {
        var root = Environment.GetEnvironmentVariable("SWLOR_TEST_PACKED_HAKS_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Ignore("Set SWLOR_TEST_PACKED_HAKS_ROOT to a directory of the module's packed HAKs to run this corpus check.");
        Directory.Exists(root).Should().BeTrue();
        var paths = Directory.EnumerateFiles(root!, "*.hak")
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase);
        var names = IfoDocument.Load(Path.Combine(CorpusLocator.RepositoryRoot, "Module", "ifo", "module.ifo.json")).HakNames;
        names.Should().HaveCount(119);
        var layers = new List<ResourceIndex.HakLayer>();
        var entries = 0;
        var legacy = 0;
        foreach (var name in names)
        {
            paths.Should().ContainKey(name, "every assigned native HAK must be present");
            var path = paths[name];
            using var archive = ErfArchive.Open(path);
            entries += archive.Entries.Count;
            legacy += archive.Entries.Count(entry => !Resref.TryParse(entry.ResRef.Value, out _, out _));
            layers.Add(new(name, path));
        }
        entries.Should().Be(177322);
        legacy.Should().Be(920, "native keys with punctuation must remain indexed rather than disappear");
        var index = new ResourceIndex(null, layers);
        index.EnsureInitialized();
        var samples = new (string Name, ushort Type, string Layer, int Size, string Sha256)[]
        {
            ("iprp_spells past", 2017, "sw_2da", 907, "77cbd8a613191d2142490b7b43be3c2ea4d380f6d7fa53e145fd25e1baca4ad1"),
            ("ife_actions_&_tr", 3, "sw_ability", 4114, "ccc8cbe8cba56b1e4aaf43740ae1778b231ba7f2eed5d40fbca3bab0bd485077"),
            ("ife_arm's_length", 3, "sw_ability", 12332, "e058f21451c1bbd2b4177d64d7cb5aa73628de5e892c218752225cbb143814fe"),
            ("udp2-p-45", 2002, "sw_t_office", 40824, "4680fa8dab9639d72570a8ab3582407c9a7a65d0b42402cafa137101edd229f2"),
        };
        foreach (var sample in samples)
        {
            index.TryLookup(new(sample.Name.ToUpperInvariant(), sample.Type), out var handle).Should().BeTrue();
            handle.Provenance.LayerName.Should().Be(sample.Layer);
            ((Action)(() => handle.GetBytes(sample.Size - 1))).Should().Throw<FormatException>();
            var payload = handle.GetBytes(sample.Size);
            payload.Should().HaveCount(sample.Size);
            Convert.ToHexStringLower(SHA256.HashData(payload)).Should().Be(sample.Sha256);
        }
        TestContext.Progress.WriteLine($"Reviewed native HAK corpus: {names.Count} archives, {entries} entries, {legacy} legacy keys; four winning payload hashes match.");
    }
}
