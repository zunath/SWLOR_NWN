using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Bif;
using SWLOR.NWN.Formats.Key;

namespace SWLOR.NWN.Formats.Tests;

public class KeyBifReaderTests
{
    [Test]
    public void KeyEntriesResolveVariableBifResources()
    {
        var key = KeyReader.Read(BuildKey());
        var resource = key.ResourceEntries.Single();

        resource.ResRef.Should().Be("sample");
        resource.ResourceType.Should().Be(2002);
        resource.BifIndex.Should().Be(0);
        resource.VariableTableIndex.Should().Be(0);
        key.GetBifForResource(resource)!.Filename.Should().Be(@"data\sample.bif");

        var bif = BifReader.Read(BuildBif());
        bif.ExtractVariableResource(0).Should().Equal(1, 2, 3, 4);
        bif.ExtractVariableResource(1).Should().BeNull();
    }

    [Test]
    public void BadArchiveIndicesAndResourceRanges_AreRejected()
    {
        var key = BuildKey();
        key[64 + 12 + 18 + 2] = 0x10; // BIF index 1 in resource id
        Action keyAction = () => KeyReader.Read(key);
        keyAction.Should().Throw<NwnFormatException>();

        var bif = BuildBif();
        BitConverter.GetBytes(10_000u).CopyTo(bif, 24);
        Action bifAction = () => BifReader.Read(bif);
        bifAction.Should().Throw<NwnFormatException>();
    }

    [Test]
    public void FixedResourceTable_IsRejectedExplicitly()
    {
        var bif = BuildBif();
        BitConverter.GetBytes(1u).CopyTo(bif, 12);

        Action action = () => BifReader.Read(bif);

        action.Should().Throw<NwnFormatException>().WithMessage("*fixed resources*");
    }

    [Test]
    public void OversizedBifResourcesAreRejectedBeforeExtraction()
    {
        var bif = BuildBif();
        BitConverter.GetBytes(256u * 1024 * 1024 + 1).CopyTo(bif, 28);

        Action action = () => BifReader.Read(bif);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*extraction limit*");
    }

    [Test]
    public void BifMetadataAllocationIsCumulativelyBounded()
    {
        var bif = new byte[20];
        "BIFF"u8.CopyTo(bif);
        "V1  "u8.CopyTo(bif.AsSpan(4));
        BitConverter.GetBytes(1_048_577u).CopyTo(bif, 8);
        BitConverter.GetBytes(20u).CopyTo(bif, 16);

        Action action = () => BifReader.Read(bif);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*allocation budget*");
    }

    [Test]
    public void KeyFilenameAliasesAndResourceObjectsAreCumulativelyBudgetedBeforeAllocation()
    {
        var key = BuildKeyWithAliasedMaximumLengthFilenames(513);
        var before = GC.GetAllocatedBytesForCurrentThread();

        Action action = () => KeyReader.Read(key);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*allocation budget*");
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(
            1_000_000,
            "all filename references are charged before any aliased string is decoded");

        const int resourceCount = 524_289;
        const int headerSize = 64;
        const int resourceEntrySize = 22;
        var resourceHeavyKey = new byte[checked(headerSize + resourceCount * resourceEntrySize)];
        "KEY "u8.CopyTo(resourceHeavyKey);
        "V1  "u8.CopyTo(resourceHeavyKey.AsSpan(4));
        BitConverter.GetBytes((uint)resourceCount).CopyTo(resourceHeavyKey, 12);
        BitConverter.GetBytes((uint)headerSize).CopyTo(resourceHeavyKey, 16);
        BitConverter.GetBytes((uint)headerSize).CopyTo(resourceHeavyKey, 20);
        before = GC.GetAllocatedBytesForCurrentThread();

        Action resourceAction = () => KeyReader.Read(resourceHeavyKey);

        resourceAction.Should().Throw<NwnFormatException>()
            .WithMessage("*allocation budget*KEY resource metadata*");
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(
            1_000_000,
            "the 128-byte per-entry charge must reject the table before entry objects or ResRef strings are created");
    }

    private static byte[] BuildKey()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("KEY "u8);
        writer.Write("V1  "u8);
        writer.Write(1u);
        writer.Write(1u);
        writer.Write(64u);
        writer.Write(76u);
        writer.Write(126u);
        writer.Write(1u);
        writer.Write(new byte[32]);

        const string filename = @"data\sample.bif";
        writer.Write(40u);
        writer.Write(98u);
        writer.Write((ushort)filename.Length);
        writer.Write((ushort)1);

        writer.Write(Encoding.ASCII.GetBytes("sample"));
        writer.Write(new byte[10]);
        writer.Write((ushort)2002);
        writer.Write(0u);
        writer.Write(Encoding.ASCII.GetBytes(filename));
        return stream.ToArray();
    }

    private static byte[] BuildBif()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("BIFF"u8);
        writer.Write("V1  "u8);
        writer.Write(1u);
        writer.Write(0u);
        writer.Write(20u);
        writer.Write(0u);
        writer.Write(36u);
        writer.Write(4u);
        writer.Write(2002u);
        writer.Write(new byte[] { 1, 2, 3, 4 });
        return stream.ToArray();
    }

    private static byte[] BuildKeyWithAliasedMaximumLengthFilenames(int bifCount)
    {
        const int headerSize = 64;
        const int entrySize = 12;
        const int filenameSize = ushort.MaxValue;
        var filenameOffset = checked(headerSize + bifCount * entrySize);
        var bytes = new byte[checked(filenameOffset + filenameSize)];
        "KEY "u8.CopyTo(bytes);
        "V1  "u8.CopyTo(bytes.AsSpan(4));
        BitConverter.GetBytes((uint)bifCount).CopyTo(bytes, 8);
        BitConverter.GetBytes((uint)headerSize).CopyTo(bytes, 16);
        BitConverter.GetBytes((uint)headerSize).CopyTo(bytes, 20);

        for (var index = 0; index < bifCount; index++)
        {
            var entry = headerSize + index * entrySize;
            BitConverter.GetBytes((uint)filenameOffset).CopyTo(bytes, entry + 4);
            BitConverter.GetBytes(ushort.MaxValue).CopyTo(bytes, entry + 8);
        }
        bytes[filenameOffset] = (byte)'a';
        return bytes;
    }
}
