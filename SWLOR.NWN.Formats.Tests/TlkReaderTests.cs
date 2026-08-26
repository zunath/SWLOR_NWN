using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Tlk;

namespace SWLOR.NWN.Formats.Tests;

public class TlkReaderTests
{
    [Test]
    public void V30Reader_RespectsFlagsOffsetsAndSoundMetadata()
    {
        var bytes = new byte[20 + 80 + 5];
        "TLK "u8.CopyTo(bytes);
        "V3.0"u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 100);

        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), 0x0007);
        Encoding.ASCII.GetBytes("hello_sound").CopyTo(bytes, 24);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), 5);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(56), BitConverter.SingleToInt32Bits(1.25f));
        Encoding.ASCII.GetBytes("hello").CopyTo(bytes, 100);

        var tlk = TlkReader.Read(bytes);

        tlk.LanguageId.Should().Be(0);
        tlk.Entries.Should().HaveCount(2);
        tlk.GetString(0).Should().Be("hello");
        tlk.Entries[0].SoundResRef.Should().Be("hello_sound");
        tlk.Entries[0].SoundLength.Should().Be(1.25f);
        tlk.GetString(1).Should().BeNull();
        tlk.GetString(99).Should().BeNull();
    }

    [Test]
    public void PolishLanguageId_UsesWindows1250()
    {
        // "Łódź" encoded as Windows-1250. Windows-1252 would decode these bytes as "£ódŸ".
        byte[] encodedText = [0xA3, 0xF3, 0x64, 0x9F];
        var bytes = new byte[20 + 40 + encodedText.Length];
        "TLK "u8.CopyTo(bytes);
        "V3.0"u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), 5);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 60);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), (uint)encodedText.Length);
        encodedText.CopyTo(bytes, 60);

        var tlk = TlkReader.Read(bytes);

        tlk.LanguageId.Should().Be(5);
        tlk.GetString(0).Should().Be("Łódź");
    }

    [Test]
    public void OutOfBoundsStringAndWrongVersion_AreRejected()
    {
        var bytes = new byte[60];
        "TLK "u8.CopyTo(bytes);
        "V3.0"u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 60);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), 100);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), 10);

        Action outOfBounds = () => TlkReader.Read(bytes);
        outOfBounds.Should().Throw<NwnFormatException>();

        bytes[4] = (byte)'X';
        Action wrongVersion = () => TlkReader.Read(bytes);
        wrongVersion.Should().Throw<NwnFormatException>();
    }

    [Test]
    public void SingleByteTextCannotExceedTheDecodedUtf16AllocationBudget()
    {
        var encodedLength = checked((int)(
            (TlkFormatLimits.MaximumDecodedAllocationBytes -
             TlkFormatLimits.EstimatedManagedBytesPerEntry) / sizeof(char) + 1));
        var bytes = new byte[60 + encodedLength];
        "TLK "u8.CopyTo(bytes);
        "V3.0"u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), 60);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(48), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(52), (uint)encodedLength);

        Action read = () => TlkReader.Read(bytes);

        read.Should().Throw<NwnFormatException>().WithMessage("*allocation budget*");
    }

    [Test]
    public void SoundResRefsAreChargedAgainstTheDecodedAllocationBudget()
    {
        var count = TlkFormatLimits.MaximumEntryCount;
        var bytes = new byte[checked(20 + count * 40)];
        "TLK "u8.CopyTo(bytes);
        "V3.0"u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), (uint)count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)bytes.Length);

        for (var index = 0; index < 3; index++)
        {
            var entryOffset = 20 + index * 40;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset), 0x0002);
            Encoding.ASCII.GetBytes("sixteen_char_ref").CopyTo(bytes, entryOffset + 4);
        }

        Action read = () => TlkReader.Read(bytes);

        read.Should().Throw<NwnFormatException>()
            .WithMessage("*allocation budget*TLK sound ResRef*");
    }

    [Test]
    public void UniqueDecodedTextRangesIncludeStringAndDictionaryOverheadInTheBudget()
    {
        var count = TlkFormatLimits.MaximumEntryCount;
        var stringsOffset = checked(20 + count * 40);
        var bytes = new byte[stringsOffset + 2];
        "TLK "u8.CopyTo(bytes);
        "V3.0"u8.CopyTo(bytes.AsSpan(4));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), (uint)count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16), (uint)stringsOffset);

        for (var index = 0; index < 2; index++)
        {
            var entryOffset = 20 + index * 40;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset), 0x0001);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset + 28), (uint)index);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset + 32), 1);
            bytes[stringsOffset + index] = (byte)('a' + index);
        }

        Action read = () => TlkReader.Read(bytes);

        read.Should().Throw<NwnFormatException>()
            .WithMessage("*allocation budget*TLK string 1*");
    }
}
