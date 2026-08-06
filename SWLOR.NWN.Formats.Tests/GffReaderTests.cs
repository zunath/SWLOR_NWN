using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.NWN.Formats.Tests;

public class GffReaderTests
{
    [Test]
    public void V32Reader_ParsesEveryFieldTypeAndNestedStructure()
    {
        var file = GffReader.Read(BuildAllFieldsGff());
        var fields = file.RootStruct.Fields;

        file.FileType.Should().Be("TST ");
        file.FileVersion.Should().Be("V3.2");
        fields.Should().HaveCount(16);
        fields[0].Value.Should().Be((byte)200);
        fields[1].Value.Should().Be((sbyte)-7);
        fields[2].Value.Should().Be((ushort)60_000);
        fields[3].Value.Should().Be((short)-1234);
        fields[4].Value.Should().Be(4_000_000_000u);
        fields[5].Value.Should().Be(-70000);
        fields[6].Value.Should().Be(18_000_000_000_000_000_000UL);
        fields[7].Value.Should().Be(-9_000_000_000_000_000_000L);
        fields[8].Value.Should().Be(7.5f);
        fields[9].Value.Should().Be(12345.6789);
        fields[10].Value.Should().Be("Hello");
        fields[11].Value.Should().Be("abc");

        var loc = fields[12].Value.Should().BeOfType<CExoLocString>().Subject;
        loc.StrRef.Should().Be(uint.MaxValue);
        loc.LocalizedStrings.Should().ContainKey(1).WhoseValue.Should().Be("Salut");
        fields[13].Value.Should().BeEquivalentTo(new byte[] { 0, 1, 255 });
        fields[14].Value.Should().BeOfType<GffStruct>().Which.Type.Should().Be(42);
        fields[15].Value.Should().BeOfType<GffList>().Which.Elements.Single().Type.Should().Be(7);
    }

    [Test]
    public void CyclesBadOffsetsOversizedCountsAndWrongVersions_AreRejected()
    {
        var cyclic = BuildAllFieldsGff();
        const int fieldTableOffset = 56 + 3 * 12;
        BinaryPrimitives.WriteUInt32LittleEndian(cyclic.AsSpan(fieldTableOffset + 14 * 12 + 8), 0);
        Action cycleAction = () => GffReader.Read(cyclic);
        cycleAction.Should().Throw<NwnFormatException>().WithMessage("*cyclic*");

        var badOffset = BuildAllFieldsGff();
        BinaryPrimitives.WriteUInt32LittleEndian(badOffset.AsSpan(32), uint.MaxValue);
        Action offsetAction = () => GffReader.Read(badOffset);
        offsetAction.Should().Throw<NwnFormatException>();

        var oversized = BuildAllFieldsGff();
        BinaryPrimitives.WriteUInt32LittleEndian(oversized.AsSpan(12), 4_000_001);
        Action countAction = () => GffReader.Read(oversized);
        countAction.Should().Throw<NwnFormatException>();

        var wrongVersion = BuildAllFieldsGff();
        wrongVersion[4] = (byte)'X';
        Action versionAction = () => GffReader.Read(wrongVersion);
        versionAction.Should().Throw<NwnFormatException>();

        Action truncatedAction = () => GffReader.Read(BuildAllFieldsGff()[..^1]);
        truncatedAction.Should().Throw<NwnFormatException>();
    }

    [Test]
    public void RepeatedVoidFieldAliasesAreCumulativelyBudgetedWithoutRepeatedCopies()
    {
        var bytes = BuildAliasedVoidGff(fieldReferences: 64, payloadLength: 1024 * 1024);
        var before = GC.GetAllocatedBytesForCurrentThread();

        Action action = () => GffReader.Read(bytes);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*allocation budget*");
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(
            5_000_000,
            "the aliased VOID payload is cached while each logical expansion is still budgeted");
    }

    [Test]
    public void LocStringSubstringsDecodeInTheirDeclaredLanguage()
    {
        // String id 10 = language 5 (Polish) * 2 + male; the payload bytes are Windows-1250
        // "Łódź" (A3 F3 64 9F), which Windows-1252 would garble (0xA3 reads as £).
        var file = GffReader.Read(BuildLocStringGff(
            stringId: 10,
            payload: new byte[] { 0xA3, 0xF3, 0x64, 0x9F }));

        var loc = file.RootStruct.Fields.Single().Value.Should().BeOfType<CExoLocString>().Subject;
        loc.LocalizedStrings.Should().ContainKey(10).WhoseValue.Should().Be("Łódź");
    }

    [Test]
    public void RepeatedStructAliasesInAListAreCumulativelyBudgeted()
    {
        // 64 list entries all referencing one struct holding a 2 MiB VOID: the parse caches the
        // struct, but every logical expansion must still be charged, so the cumulative budget
        // trips long before the JSON bridge could expand the aliases into hundreds of megabytes.
        var bytes = BuildAliasedStructListGff(listReferences: 64, payloadLength: 2 * 1024 * 1024);
        var before = GC.GetAllocatedBytesForCurrentThread();

        Action action = () => GffReader.Read(bytes);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*allocation budget*");
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(
            10_000_000,
            "aliased structs are parsed once while each logical expansion is still budgeted");
    }

    private static byte[] BuildLocStringGff(uint stringId, byte[] payload)
    {
        const int headerSize = 56;
        const int structSize = 12;
        const int fieldSize = 12;
        const int labelSize = 16;
        var structOffset = headerSize;
        var fieldOffset = structOffset + structSize;
        var labelOffset = fieldOffset + fieldSize;
        var fieldDataOffset = labelOffset + labelSize;
        var substringBytes = 8 + payload.Length;
        var totalSize = 8 + substringBytes;
        var fieldDataCount = 4 + totalSize;
        var totalLength = fieldDataOffset + fieldDataCount;
        var bytes = new byte[totalLength];

        "TST "u8.CopyTo(bytes);
        "V3.2"u8.CopyTo(bytes.AsSpan(4));
        WriteHeaderPair(bytes, 8, structOffset, 1);
        WriteHeaderPair(bytes, 16, fieldOffset, 1);
        WriteHeaderPair(bytes, 24, labelOffset, 1);
        WriteHeaderPair(bytes, 32, fieldDataOffset, fieldDataCount);
        WriteHeaderPair(bytes, 40, totalLength, 0);
        WriteHeaderPair(bytes, 48, totalLength, 0);

        WriteStruct(bytes, structOffset, uint.MaxValue, 0, 1);
        WriteField(bytes, fieldOffset, GffField.CExoLocString, 0, 0);
        "Name"u8.CopyTo(bytes.AsSpan(labelOffset));

        var cursor = fieldDataOffset;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cursor), checked((uint)totalSize));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cursor + 4), uint.MaxValue);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cursor + 8), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cursor + 12), stringId);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(cursor + 16), checked((uint)payload.Length));
        payload.CopyTo(bytes.AsSpan(cursor + 20));
        return bytes;
    }

    private static byte[] BuildAliasedStructListGff(int listReferences, int payloadLength)
    {
        const int headerSize = 56;
        const int structSize = 12;
        const int fieldSize = 12;
        const int labelSize = 16;
        var structOffset = headerSize;
        var fieldOffset = structOffset + 2 * structSize;
        var labelOffset = fieldOffset + 2 * fieldSize;
        var fieldDataOffset = labelOffset + 2 * labelSize;
        var fieldDataCount = 4 + payloadLength;
        var listIndicesOffset = checked(fieldDataOffset + fieldDataCount);
        var listIndicesCount = checked((1 + listReferences) * 4);
        var totalLength = checked(listIndicesOffset + listIndicesCount);
        var bytes = new byte[totalLength];

        "TST "u8.CopyTo(bytes);
        "V3.2"u8.CopyTo(bytes.AsSpan(4));
        WriteHeaderPair(bytes, 8, structOffset, 2);
        WriteHeaderPair(bytes, 16, fieldOffset, 2);
        WriteHeaderPair(bytes, 24, labelOffset, 2);
        WriteHeaderPair(bytes, 32, fieldDataOffset, fieldDataCount);
        WriteHeaderPair(bytes, 40, listIndicesOffset, 0);
        WriteHeaderPair(bytes, 48, listIndicesOffset, listIndicesCount);

        // Root struct: one List field. Struct 1: one VOID field holding the large payload.
        WriteStruct(bytes, structOffset, uint.MaxValue, 0, 1);
        WriteStruct(bytes, structOffset + structSize, 1, 1, 1);
        WriteField(bytes, fieldOffset, GffField.List, 0, 0);
        WriteField(bytes, fieldOffset + fieldSize, GffField.VOID, 1, 0);
        "Items"u8.CopyTo(bytes.AsSpan(labelOffset));
        "Payload"u8.CopyTo(bytes.AsSpan(labelOffset + labelSize));

        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(fieldDataOffset), checked((uint)payloadLength));

        BinaryPrimitives.WriteUInt32LittleEndian(
            bytes.AsSpan(listIndicesOffset), checked((uint)listReferences));
        for (var index = 0; index < listReferences; index++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(
                bytes.AsSpan(listIndicesOffset + 4 + index * 4), 1);
        }
        return bytes;
    }

    private static byte[] BuildAllFieldsGff()
    {
        const int structCount = 3;
        const int fieldCount = 16;
        const int labelCount = 16;
        const int headerSize = 56;
        var structOffset = headerSize;
        var fieldOffset = structOffset + structCount * 12;
        var labelOffset = fieldOffset + fieldCount * 12;
        var fieldData = BuildFieldData(out var dataOffsets);
        var fieldDataOffset = labelOffset + labelCount * 16;
        var fieldIndicesOffset = fieldDataOffset + fieldData.Length;
        var listIndicesOffset = fieldIndicesOffset + fieldCount * 4;
        var totalLength = listIndicesOffset + 8;

        var bytes = new byte[totalLength];
        "TST "u8.CopyTo(bytes);
        "V3.2"u8.CopyTo(bytes.AsSpan(4));
        WriteHeaderPair(bytes, 8, structOffset, structCount);
        WriteHeaderPair(bytes, 16, fieldOffset, fieldCount);
        WriteHeaderPair(bytes, 24, labelOffset, labelCount);
        WriteHeaderPair(bytes, 32, fieldDataOffset, fieldData.Length);
        WriteHeaderPair(bytes, 40, fieldIndicesOffset, fieldCount * 4);
        WriteHeaderPair(bytes, 48, listIndicesOffset, 8);

        WriteStruct(bytes, structOffset, uint.MaxValue, 0, fieldCount);
        WriteStruct(bytes, structOffset + 12, 42, 0, 0);
        WriteStruct(bytes, structOffset + 24, 7, 0, 0);

        var simpleData = new uint[]
        {
            200,
            unchecked((uint)(byte)(sbyte)-7),
            60_000,
            unchecked((uint)(ushort)(short)-1234),
            4_000_000_000,
            unchecked((uint)-70_000),
            dataOffsets[0],
            dataOffsets[1],
            unchecked((uint)BitConverter.SingleToInt32Bits(7.5f)),
            dataOffsets[2],
            dataOffsets[3],
            dataOffsets[4],
            dataOffsets[5],
            dataOffsets[6],
            1,
            0
        };
        for (var index = 0; index < fieldCount; index++)
            WriteField(bytes, fieldOffset + index * 12, (uint)index, (uint)index, simpleData[index]);

        var labels = new[]
        {
            "Byte", "Char", "Word", "Short", "Dword", "Int", "Dword64", "Int64",
            "Float", "Double", "String", "ResRef", "LocString", "Void", "Struct", "List"
        };
        for (var index = 0; index < labels.Length; index++)
            Encoding.ASCII.GetBytes(labels[index]).CopyTo(bytes, labelOffset + index * 16);

        fieldData.CopyTo(bytes, fieldDataOffset);
        for (var index = 0; index < fieldCount; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(fieldIndicesOffset + index * 4), (uint)index);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(listIndicesOffset), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(listIndicesOffset + 4), 2);
        return bytes;
    }

    private static byte[] BuildFieldData(out uint[] offsets)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        offsets = new uint[7];

        offsets[0] = checked((uint)stream.Position);
        writer.Write(18_000_000_000_000_000_000UL);
        offsets[1] = checked((uint)stream.Position);
        writer.Write(-9_000_000_000_000_000_000L);
        offsets[2] = checked((uint)stream.Position);
        writer.Write(12345.6789);
        offsets[3] = checked((uint)stream.Position);
        writer.Write(5u);
        writer.Write("Hello"u8);
        offsets[4] = checked((uint)stream.Position);
        writer.Write((byte)3);
        writer.Write("abc"u8);
        offsets[5] = checked((uint)stream.Position);
        writer.Write(21u);
        writer.Write(uint.MaxValue);
        writer.Write(1u);
        writer.Write(1u);
        writer.Write(5u);
        writer.Write("Salut"u8);
        offsets[6] = checked((uint)stream.Position);
        writer.Write(3u);
        writer.Write(new byte[] { 0, 1, 255 });
        return stream.ToArray();
    }

    private static byte[] BuildAliasedVoidGff(int fieldReferences, int payloadLength)
    {
        const int headerSize = 56;
        const int structSize = 12;
        const int fieldSize = 12;
        const int labelSize = 16;
        var structOffset = headerSize;
        var fieldOffset = structOffset + structSize;
        var labelOffset = fieldOffset + fieldSize;
        var fieldDataOffset = labelOffset + labelSize;
        var fieldIndicesOffset = checked(fieldDataOffset + 4 + payloadLength);
        var totalLength = checked(fieldIndicesOffset + fieldReferences * 4);
        var bytes = new byte[totalLength];

        "TST "u8.CopyTo(bytes);
        "V3.2"u8.CopyTo(bytes.AsSpan(4));
        WriteHeaderPair(bytes, 8, structOffset, 1);
        WriteHeaderPair(bytes, 16, fieldOffset, 1);
        WriteHeaderPair(bytes, 24, labelOffset, 1);
        WriteHeaderPair(bytes, 32, fieldDataOffset, 4 + payloadLength);
        WriteHeaderPair(bytes, 40, fieldIndicesOffset, fieldReferences * 4);
        WriteHeaderPair(bytes, 48, totalLength, 0);

        WriteStruct(bytes, structOffset, uint.MaxValue, 0, checked((uint)fieldReferences));
        WriteField(bytes, fieldOffset, GffField.VOID, 0, 0);
        "Payload"u8.CopyTo(bytes.AsSpan(labelOffset));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(fieldDataOffset), checked((uint)payloadLength));
        return bytes;
    }

    private static void WriteHeaderPair(byte[] bytes, int offset, int sectionOffset, int count)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), checked((uint)sectionOffset));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 4), checked((uint)count));
    }

    private static void WriteStruct(byte[] bytes, int offset, uint type, uint dataOrOffset, uint fieldCount)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), type);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 4), dataOrOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 8), fieldCount);
    }

    private static void WriteField(byte[] bytes, int offset, uint type, uint labelIndex, uint dataOrOffset)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), type);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 4), labelIndex);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 8), dataOrOffset);
    }
}
