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
}
