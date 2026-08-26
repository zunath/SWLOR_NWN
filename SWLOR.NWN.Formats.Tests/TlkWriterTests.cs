using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Tlk;

namespace SWLOR.NWN.Formats.Tests;

public class TlkWriterTests
{
    [Test]
    public void Writer_RoundTripsSparseMultilineAndExplicitEmptyEntries()
    {
        var source = new Dictionary<int, string>
        {
            [2] = "First line\nSecond line",
            [5] = string.Empty
        };

        var bytes = TlkWriter.Write(0, source);
        var tlk = TlkReader.Read(bytes);

        tlk.LanguageId.Should().Be(0);
        tlk.Entries.Should().HaveCount(6);
        tlk.GetString(0).Should().BeNull();
        tlk.GetString(1).Should().BeNull();
        tlk.GetString(2).Should().Be("First line\nSecond line");
        tlk.GetString(3).Should().BeNull();
        tlk.GetString(4).Should().BeNull();
        tlk.GetString(5).Should().BeEmpty();
        tlk.Entries[5].Flags.Should().Be(1);
    }

    [Test]
    public void Writer_UsesTheLanguageCodePageWithoutLossyReplacement()
    {
        var bytes = TlkWriter.Write(5, new Dictionary<int, string> { [0] = "Łódź" });

        bytes.AsSpan(60).ToArray().Should().Equal(0xA3, 0xF3, 0x64, 0x9F);
        TlkReader.Read(bytes).GetString(0).Should().Be("Łódź");

        Action writeUnencodable = () =>
            TlkWriter.Write(0, new Dictionary<int, string> { [0] = "Not Windows-1252: 😀" });
        writeUnencodable.Should().Throw<ArgumentException>()
            .WithMessage("*entry 0*cannot be encoded*")
            .WithInnerException<EncoderFallbackException>();
    }

    [Test]
    public void Writer_RejectsInvalidIdsAndNullText()
    {
        Action negativeId = () =>
            TlkWriter.Write(0, new Dictionary<int, string> { [-1] = "invalid" });
        Action excessiveId = () =>
            TlkWriter.Write(0, new Dictionary<int, string>
            {
                [TlkFormatLimits.MaximumEntryId + 1] = "invalid"
            });
        var nullTextEntries = new Dictionary<int, string> { [0] = null! };
        Action nullText = () => TlkWriter.Write(0, nullTextEntries);

        negativeId.Should().Throw<ArgumentOutOfRangeException>();
        excessiveId.Should().Throw<ArgumentOutOfRangeException>();
        nullText.Should().Throw<ArgumentException>().WithMessage("*entry 0 has null text*");
    }

    [Test]
    public void FormatLimits_ExposeConsistentEntryCountAndIdBoundaries()
    {
        TlkFormatLimits.MaximumEntryCount.Should().Be(1_048_575);
        TlkFormatLimits.MaximumEntryId.Should().Be(TlkFormatLimits.MaximumEntryCount - 1);
    }

    [Test]
    public void WriterAndReader_AgreeAtTheEffectiveEntryAndAllocationBoundary()
    {
        var source = new Dictionary<int, string>
        {
            [TlkFormatLimits.MaximumEntryId] = "last"
        };

        var bytes = TlkWriter.Write(0, source);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12))
            .Should().Be((uint)TlkFormatLimits.MaximumEntryCount);
        var roundTripped = TlkReader.Read(bytes);
        roundTripped.Entries.Should().HaveCount(TlkFormatLimits.MaximumEntryCount);
        roundTripped.GetString((uint)TlkFormatLimits.MaximumEntryId).Should().Be("last");

        Action exceedDecodedBudget = () => TlkWriter.Write(0, new Dictionary<int, string>
        {
            [TlkFormatLimits.MaximumEntryId] = new string('x', 33)
        });
        exceedDecodedBudget.Should().Throw<ArgumentException>()
            .WithMessage("*decoded metadata and text*");
    }

    [Test]
    public void Writer_EmptyInputProducesAValidEmptyTlk()
    {
        var bytes = TlkWriter.Write(0, new Dictionary<int, string>());

        bytes.Should().HaveCount(20);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)).Should().Be(0);
        BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16)).Should().Be(20);
        TlkReader.Read(bytes).Entries.Should().BeEmpty();
    }

    [Test]
    public void Writer_ReproducesTheCheckedInSwlorCustomTlk()
    {
        var repoRoot = FindRepositoryRoot();
        var tlkDirectory = Path.Combine(repoRoot, "SWLOR_Haks", "sw_tlk");
        using var json = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(tlkDirectory, "sw_tlk.tlk.json")));
        var languageId = json.RootElement.GetProperty("language").GetUInt32();
        var entries = json.RootElement.GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("id").GetInt32(),
                entry => entry.GetProperty("text").GetString()!);

        var generated = TlkWriter.Write(languageId, entries);
        var checkedIn = File.ReadAllBytes(Path.Combine(tlkDirectory, "sw_tlk.tlk"));

        generated.Should().Equal(checkedIn);
        var roundTripped = TlkReader.Read(generated);
        roundTripped.Entries.Should().HaveCount(entries.Keys.Max() + 1);
        roundTripped.GetString(80_831).Should().BeNull();
        foreach (var (id, text) in entries)
            roundTripped.GetString((uint)id).Should().Be(text);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR repository root.");
    }
}
