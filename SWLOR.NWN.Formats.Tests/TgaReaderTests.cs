using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Tga;

namespace SWLOR.NWN.Formats.Tests;

public class TgaReaderTests
{
    [Test]
    public void RawTrueColor_ConvertsBgrAndBottomOriginToTopLeftRgba()
    {
        var bytes = Header(imageType: 2, width: 1, height: 2, depth: 24, descriptor: 0);
        bytes = bytes.Concat(new byte[]
        {
            0, 0, 255, // bottom red
            255, 0, 0  // top blue
        }).ToArray();

        var image = TgaReader.Read(bytes);

        image.Width.Should().Be(1);
        image.Height.Should().Be(2);
        image.Pixels.Should().Equal(
            0, 0, 255, 255,
            255, 0, 0, 255);
    }

    [Test]
    public void RleTrueColor_ValidatesPacketsAndRightOrigin()
    {
        var bytes = Header(imageType: 10, width: 2, height: 1, depth: 32, descriptor: 0x38)
            .Concat(new byte[] { 0x81, 3, 2, 1, 4 })
            .ToArray();

        var image = TgaReader.Read(bytes);
        image.Pixels.Should().Equal(1, 2, 3, 4, 1, 2, 3, 4);

        bytes[^5] = 0x82;
        Action action = () => TgaReader.Read(bytes);
        action.Should().Throw<NwnFormatException>();
    }

    [Test]
    public void TrueColorWithoutAttributeBitsStillCarriesAlpha()
    {
        // NWN-corpus 32-bit TGAs frequently declare 0 attribute bits in the image
        // descriptor while still carrying meaningful alpha in the 4th byte, and the
        // engine honors that byte regardless of the declared attribute bits.
        var bytes = Header(imageType: 2, width: 1, height: 1, depth: 32, descriptor: 0x20)
            .Concat(new byte[] { 3, 2, 1, 0 })
            .ToArray();

        var image = TgaReader.Read(bytes);

        image.Pixels.Should().Equal(1, 2, 3, 0);
    }

    [Test]
    public void RleTrueColor_MixesRawAndRepeatedPacketsInOneContiguousSurface()
    {
        var bytes = Header(imageType: 10, width: 4, height: 1, depth: 24, descriptor: 0x20)
            .Concat(new byte[]
            {
                0x01,             // two raw pixels
                0, 0, 255,        // red
                0, 255, 0,        // green
                0x81,             // two repeated pixels
                255, 0, 0         // blue
            })
            .ToArray();

        var image = TgaReader.Read(bytes);

        image.Pixels.Should().Equal(
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            0, 0, 255, 255);
    }

    [Test]
    public void AllowedSurfaceUsesOneBoundedBufferAndOversizedSurfaceIsRejectedBeforeAllocation()
    {
        const ushort width = 512;
        const ushort height = 512;
        var bytes = new byte[18 + width * height * 3];
        Header(imageType: 2, width: width, height: height, depth: 24, descriptor: 0x20).CopyTo(bytes, 0);

        _ = TgaReader.Read(
            Header(imageType: 2, width: 1, height: 1, depth: 24, descriptor: 0x20)
                .Concat(new byte[3])
                .ToArray());
        var before = GC.GetAllocatedBytesForCurrentThread();
        var image = TgaReader.Read(bytes);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        image.Pixels.Should().HaveCount(width * height * 4);
        allocated.Should().BeLessThan(
            2_000_000,
            "a 512x512 decode should allocate its RGBA surface, not one managed object per pixel");

        var oversized = Header(
            imageType: 2,
            width: 16_384,
            height: 16_384,
            depth: 24,
            descriptor: 0x20);
        Action decode = () => TgaReader.Read(oversized);
        decode.Should().Throw<NwnFormatException>()
            .WithMessage("*pixel count*");
    }

    [TestCase(0x40)]
    [TestCase(0x80)]
    [TestCase(0xC0)]
    public void InterleavedDescriptorsAreRejected(int interleavingBits)
    {
        var bytes = Header(
                imageType: 2,
                width: 1,
                height: 1,
                depth: 24,
                descriptor: checked((byte)(0x20 | interleavingBits)))
            .Concat(new byte[3])
            .ToArray();

        Action action = () => TgaReader.Read(bytes);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*interleaved*");
    }

    private static byte[] Header(byte imageType, ushort width, ushort height, byte depth, byte descriptor)
    {
        var bytes = new byte[18];
        bytes[2] = imageType;
        BitConverter.GetBytes(width).CopyTo(bytes, 12);
        BitConverter.GetBytes(height).CopyTo(bytes, 14);
        bytes[16] = depth;
        bytes[17] = descriptor;
        return bytes;
    }
}
