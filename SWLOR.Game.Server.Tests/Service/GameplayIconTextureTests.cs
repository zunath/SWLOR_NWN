using System.Text;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class GameplayIconTextureTests
{
    [Test]
    public void Dxt1Decoder_PreservesColorSelectorsAndBottomUpStorage()
    {
        var block = new byte[] { 0, 248, 31, 0, 228, 228, 228, 228 };
        var image = Decode("DXT1", block);
        Assert.That(image[17], Is.EqualTo(8));
        Assert.That(image.Skip(18).Take(16), Is.EqualTo(new byte[] {
            0,0,255,255, 255,0,0,255, 85,0,170,255, 170,0,85,255 }));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Dxt5Decoder_PreservesBothAlphaInterpolationModes(bool descending)
    {
        var block = new byte[16];
        block[0] = descending ? (byte)255 : (byte)0;
        block[1] = descending ? (byte)0 : (byte)255;
        ulong selectors = 0;
        for (var i = 0; i < 16; i++) selectors |= (ulong)(i % 8) << (3 * i);
        for (var i = 0; i < 6; i++) block[2+i] = (byte)(selectors >> (8*i));
        block[8] = block[9] = 255;
        var image = Decode("DXT5", block);
        var expected = descending ? new byte[] {255,0,218,182,145,109,72,36}
            : new byte[] {0,255,51,102,153,204,0,255};
        Assert.That(Enumerable.Range(0,8).Select(i=>image[18+i*4+3]), Is.EqualTo(expected));
    }

    [Test]
    public void Decoder_RejectsTruncatedPayloadOrExtraMipLevels()
    {
        Assert.Throws<InvalidDataException>(() => Decode("DXT1", new byte[7]));
        Assert.Throws<InvalidDataException>(() => Decode("DXT1", new byte[8], 2));
    }

    private static byte[] Decode(string format, byte[] block, int levels = 1)
    {
        var header = new byte[128+block.Length];
        Encoding.ASCII.GetBytes("DDS ").CopyTo(header,0);
        foreach (var pair in new[] {(4,124),(8,0xA1007),(12,4),(16,4),(20,format=="DXT1"?8:16),
                     (28,levels),(76,32),(80,4),(108,0x1000)})
            BitConverter.GetBytes(pair.Item2).CopyTo(header,pair.Item1);
        Encoding.ASCII.GetBytes(format).CopyTo(header,84);
        block.CopyTo(header,128);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid()+".dds");
        try { File.WriteAllBytes(path,header); return GameplayIconTexture.ReadBottomLeftTga(path); }
        finally { File.Delete(path); }
    }
}
