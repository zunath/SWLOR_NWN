using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Plt;

namespace SWLOR.NWN.Formats.Tests;

public class PltReaderTests
{
    [Test]
    public void Reader_PreservesIntensityAndLayer()
    {
        var bytes = new byte[28];
        "PLT "u8.CopyTo(bytes);
        "V1  "u8.CopyTo(bytes.AsSpan(4));
        BitConverter.GetBytes(2u).CopyTo(bytes, 16);
        BitConverter.GetBytes(1u).CopyTo(bytes, 20);
        bytes[24] = 17;
        bytes[25] = PltLayers.Skin;
        bytes[26] = 250;
        bytes[27] = PltLayers.Tattoo2;

        var file = PltReader.Read(bytes);

        file.Width.Should().Be(2);
        file.Height.Should().Be(1);
        file.Pixels.Should().Equal(new PltPixel(17, 0), new PltPixel(250, 9));
    }

    [Test]
    public void BadLengthLayerAndVersion_AreRejected()
    {
        var bytes = new byte[26];
        "PLT "u8.CopyTo(bytes);
        "V1  "u8.CopyTo(bytes.AsSpan(4));
        BitConverter.GetBytes(1u).CopyTo(bytes, 16);
        BitConverter.GetBytes(1u).CopyTo(bytes, 20);
        bytes[24] = 1;
        bytes[25] = 10;

        Action badLayer = () => PltReader.Read(bytes);
        badLayer.Should().Throw<NwnFormatException>();

        bytes[25] = 0;
        bytes[4] = (byte)'X';
        Action badVersion = () => PltReader.Read(bytes);
        badVersion.Should().Throw<NwnFormatException>();

        Action truncated = () => PltReader.Read(bytes[..^1]);
        truncated.Should().Throw<NwnFormatException>();
    }
}
