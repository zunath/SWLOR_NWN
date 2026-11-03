using System.Text;
using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Service;

public class Utf8TextRepairTests
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    [TestCase("plain ASCII text")]
    [TestCase("Sláine – café ✓")]
    [TestCase("emoji 😀 and U+10FFFF \U0010FFFF")]
    [TestCase("")]
    public void ValidUtf8IsUnchanged(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var original = (byte[])bytes.Clone();

        var replaced = Utf8TextRepair.ReplaceInvalidBytes(bytes);

        Assert.That(replaced, Is.Zero);
        Assert.That(bytes, Is.EqualTo(original));
    }

    [Test]
    public void WindowsAccentBeforeAsciiIsReplaced()
    {
        // The production crash: a Windows-1252 "é" (E9) followed by 'f' at index 6.
        var bytes = new byte[] { (byte)'R', (byte)'e', (byte)'n', (byte)'a', (byte)'m', 0xE9, (byte)'f' };

        var replaced = Utf8TextRepair.ReplaceInvalidBytes(bytes);

        Assert.That(replaced, Is.EqualTo(1));
        Assert.That(Encoding.ASCII.GetString(bytes), Is.EqualTo("Renam?f"));
    }

    [TestCase(new byte[] { 0x80 }, "?", TestName = "Lone continuation byte")]
    [TestCase(new byte[] { 0x92, 0x93 }, "??", TestName = "Windows-1252 curly quotes")]
    [TestCase(new byte[] { 0xC0, 0xAF }, "??", TestName = "Overlong two-byte form")]
    [TestCase(new byte[] { 0xE0, 0x80, 0xAF }, "???", TestName = "Overlong three-byte form")]
    [TestCase(new byte[] { 0xED, 0xA0, 0x80 }, "???", TestName = "Surrogate code point")]
    [TestCase(new byte[] { 0xF4, 0x90, 0x80, 0x80 }, "????", TestName = "Above U+10FFFF")]
    [TestCase(new byte[] { 0xF5, 0x80 }, "??", TestName = "Byte never used in UTF-8")]
    [TestCase(new byte[] { (byte)'a', 0xE2, 0x82 }, "a??", TestName = "Sequence cut off at the end")]
    [TestCase(new byte[] { 0xE2, (byte)'x', 0xAC }, "?x?", TestName = "Sequence interrupted by ASCII")]
    public void InvalidBytesAreReplaced(byte[] bytes, string expected)
    {
        var replaced = Utf8TextRepair.ReplaceInvalidBytes(bytes);

        Assert.That(Encoding.ASCII.GetString(bytes), Is.EqualTo(expected));
        Assert.That(replaced, Is.EqualTo(expected.Length - expected.Replace("?", "").Length));
    }

    [Test]
    public void ValidSequencesNextToInvalidBytesAreKept()
    {
        var bytes = new byte[] { 0xE9, 0xC3, 0xA9, 0x80, 0xE2, 0x82, 0xAC };

        var replaced = Utf8TextRepair.ReplaceInvalidBytes(bytes);

        Assert.That(replaced, Is.EqualTo(2));
        Assert.That(StrictUtf8.GetString(bytes), Is.EqualTo("?é?€"));
    }

    [Test]
    public void RepairedRandomBytesAlwaysDecodeAsStrictUtf8()
    {
        var random = new Random(3133);
        for (var sample = 0; sample < 2000; sample++)
        {
            var bytes = new byte[random.Next(0, 40)];
            random.NextBytes(bytes);

            Utf8TextRepair.ReplaceInvalidBytes(bytes);

            Assert.DoesNotThrow(() => StrictUtf8.GetString(bytes), $"sample {sample}: {Convert.ToHexString(bytes)}");
        }
    }
}
