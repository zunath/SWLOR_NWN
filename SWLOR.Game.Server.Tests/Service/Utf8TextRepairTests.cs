using System.Buffers;
using System.Text;
using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Service;

public class Utf8TextRepairTests
{
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

    [TestCase(new byte[] { 0xC2, 0x80 }, TestName = "First two-byte code point")]
    [TestCase(new byte[] { 0xDF, 0xBF }, TestName = "Last two-byte code point")]
    [TestCase(new byte[] { 0xE0, 0xA0, 0x80 }, TestName = "First three-byte code point")]
    [TestCase(new byte[] { 0xED, 0x9F, 0xBF }, TestName = "Last code point before the surrogates")]
    [TestCase(new byte[] { 0xEE, 0x80, 0x80 }, TestName = "First code point after the surrogates")]
    [TestCase(new byte[] { 0xEF, 0xBF, 0xBF }, TestName = "Last three-byte code point")]
    [TestCase(new byte[] { 0xF0, 0x90, 0x80, 0x80 }, TestName = "First four-byte code point")]
    [TestCase(new byte[] { 0xF4, 0x8F, 0xBF, 0xBF }, TestName = "Last code point")]
    public void BoundaryCodePointsAreKept(byte[] bytes)
    {
        var original = (byte[])bytes.Clone();

        Assert.That(Utf8TextRepair.ReplaceInvalidBytes(bytes), Is.Zero);
        Assert.That(bytes, Is.EqualTo(original));
    }

    [Test]
    public void WindowsAccentBeforeAsciiIsReplaced()
    {
        var bytes = new byte[] { (byte)'R', (byte)'e', (byte)'n', (byte)'a', (byte)'m', 0xE9, (byte)'f' };

        var replaced = Utf8TextRepair.ReplaceInvalidBytes(bytes);

        Assert.That(replaced, Is.EqualTo(1));
        Assert.That(Encoding.ASCII.GetString(bytes), Is.EqualTo("Renam?f"));
    }

    [TestCase(new byte[] { 0x80 }, "?", TestName = "Lone continuation byte")]
    [TestCase(new byte[] { 0x92, 0x93 }, "??", TestName = "Windows-1252 curly quotes")]
    [TestCase(new byte[] { 0xC0, 0xAF }, "??", TestName = "Overlong two-byte form")]
    [TestCase(new byte[] { 0xC1, 0xBF }, "??", TestName = "Overlong two-byte form at its top")]
    [TestCase(new byte[] { 0xE0, 0x80, 0xAF }, "???", TestName = "Overlong three-byte form")]
    [TestCase(new byte[] { 0xE0, 0x9F, 0xBF }, "???", TestName = "Overlong three-byte form at its top")]
    [TestCase(new byte[] { 0xED, 0xA0, 0x80 }, "???", TestName = "Surrogate code point")]
    [TestCase(new byte[] { 0xF0, 0x8F, 0xBF, 0xBF }, "????", TestName = "Overlong four-byte form")]
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
    public void RandomBytesMatchTheStrictDecoder()
    {
        var random = new Random(3133);
        for (var sample = 0; sample < 20_000; sample++)
        {
            var bytes = new byte[random.Next(0, 24)];
            random.NextBytes(bytes);
            var (expected, expectedReplaced) = RepairWithRune(bytes);

            var replaced = Utf8TextRepair.ReplaceInvalidBytes(bytes);

            Assert.That(bytes, Is.EqualTo(expected), $"sample {sample}");
            Assert.That(replaced, Is.EqualTo(expectedReplaced), $"sample {sample}");
        }
    }

    // The reference: keep each well-formed scalar, replace every byte of each ill-formed subsequence.
    private static (byte[] Bytes, int Replaced) RepairWithRune(byte[] input)
    {
        var output = (byte[])input.Clone();
        var replaced = 0;
        var index = 0;
        while (index < output.Length)
        {
            var status = Rune.DecodeFromUtf8(output.AsSpan(index), out _, out var consumed);
            if (status != OperationStatus.Done)
            {
                output.AsSpan(index, consumed).Fill((byte)'?');
                replaced += consumed;
            }

            index += consumed;
        }

        return (output, replaced);
    }
}
