using System.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.Tests.Service;

public class UbjsonTextRepairTests
{
    private static byte[] Bytes(params object[] parts)
    {
        return parts.SelectMany(part => part switch
        {
            char marker => new[] { (byte)marker },
            int value => new[] { (byte)value },
            string text => text.Select(character => (byte)character).ToArray(),
            byte[] bytes => bytes,
            _ => throw new ArgumentException($"Unsupported part {part}")
        }).ToArray();
    }

    // "i" + length + bytes; strings add the "S" marker, object keys do not.
    private static byte[] Key(params int[] bytes) => Bytes('i', bytes.Length, bytes.Select(value => (byte)value).ToArray());
    private static byte[] Str(params int[] bytes) => Bytes('S', Key(bytes));
    private static byte[] Repeat(char marker, int count) => Enumerable.Repeat((byte)marker, count).ToArray();

    private static (int Replaced, bool Complete) Repair(byte[] data)
    {
        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);
        return (replaced, complete);
    }

    [Test]
    public void UnsizedObjectKeysAndValuesAreRepaired()
    {
        var data = Bytes('{', Key('k', 0xE9, 'y'), Str('S', 'l', 0xE1, 'i', 'n', 'e'), Key('n'), 'i', 5, '}');

        Assert.That(Repair(data), Is.EqualTo((2, true)));
        Assert.That(data, Is.EqualTo(Bytes('{', Key('k', '?', 'y'), Str('S', 'l', '?', 'i', 'n', 'e'), Key('n'), 'i', 5, '}')));
    }

    [Test]
    public void NumberBytesAreNeverChanged()
    {
        var data = Bytes('[',
            'U', 0xE9, 'i', 0x80, 'I', 0xC3, 0x28, 'l', 0x80, 0xE9, 0x00, 0x01,
            'L', 0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA, 0xF9, 0xF8,
            'd', 0xC0, 0xAF, 0x80, 0x81, 'D', 0xE0, 0x80, 0xAF, 0xED, 0xA0, 0x80, 0xF5, 0xFF,
            'C', 'x', 'H', 'i', 4, "-1.5", 'T', 'F', 'Z', ']');
        var original = (byte[])data.Clone();

        Assert.That(Repair(data), Is.EqualTo((0, true)));
        Assert.That(data, Is.EqualTo(original));
    }

    [Test]
    public void SizedAndTypedArraysOfStringsAreRepaired()
    {
        // Typed values carry no marker; the sized array reads one marker per value and skips no-ops before it.
        var data = Bytes('[',
            '[', '$', 'S', '#', 'i', 2, Key('a', 0xE9), Key(0x80),
            '[', '#', 'i', 2, 'N', Str(0x92), 'N', Str('o', 'k'),
            ']');

        Assert.That(Repair(data), Is.EqualTo((3, true)));
        Assert.That(data, Is.EqualTo(Bytes('[',
            '[', '$', 'S', '#', 'i', 2, Key('a', '?'), Key('?'),
            '[', '#', 'i', 2, 'N', Str('?'), 'N', Str('o', 'k'),
            ']')));
    }

    [Test]
    public void SizedAndTypedObjectKeysAndValuesAreRepaired()
    {
        var data = Bytes('{', '#', 'i', 2,
            Key(0x80), '{', '$', 'S', '#', 'i', 2, Key('k', 0xE9), Key('x'), Key(0x81), Key(0x82),
            Key('n'), 'L', 0, 0, 0, 0, 0, 0, 0xE9, 0x80);

        Assert.That(Repair(data), Is.EqualTo((4, true)));
        Assert.That(data, Is.EqualTo(Bytes('{', '#', 'i', 2,
            Key('?'), '{', '$', 'S', '#', 'i', 2, Key('k', '?'), Key('x'), Key('?'), Key('?'),
            Key('n'), 'L', 0, 0, 0, 0, 0, 0, 0xE9, 0x80)));
    }

    [Test]
    public void NoOpsAreSkippedOnlyWhereTheReaderSkipsThem()
    {
        // Skipped: before the value, after the type, after '#', between an unsized key and its value, and before '}'.
        var skipped = Bytes('N', '[', '$', 'S', 'N', '#', 'N', 'i', 1, Key(0xE9));
        var unsizedObject = Bytes('{', Key(0x80), 'N', 'N', 'Z', 'N', '}');

        Assert.That(Repair(skipped), Is.EqualTo((1, true)));
        Assert.That(Repair(unsizedObject), Is.EqualTo((1, true)));

        // Not skipped: before a sized object's key or a typed string's length.
        Assert.That(Repair(Bytes('{', '#', 'i', 1, 'N', Key('k'), 'Z')).Complete, Is.False);
        Assert.That(Repair(Bytes('[', '$', 'S', '#', 'i', 1, 'N', Key('k'))).Complete, Is.False);
    }

    [Test]
    public void TypedArraysOfNumbersAndContainersAreWalked()
    {
        var numbers = Bytes('[', '$', 'i', '#', 'i', 3, 0xE9, 0x80, 0xFF);
        var original = (byte[])numbers.Clone();
        // Two typed array elements: an empty unsized array, then a sized one holding a string.
        var containers = Bytes('[', '$', '[', '#', 'i', 2, ']', '#', 'i', 1, Str(0x80));

        Assert.That(Repair(numbers), Is.EqualTo((0, true)));
        Assert.That(numbers, Is.EqualTo(original));
        Assert.That(Repair(containers), Is.EqualTo((1, true)));
    }

    [Test]
    public void EmptyAndNoOpTypedContainersAreComplete()
    {
        Assert.That(Repair(Bytes('[', ']')), Is.EqualTo((0, true)));
        Assert.That(Repair(Bytes('{', '}')), Is.EqualTo((0, true)));
        Assert.That(Repair(Bytes('[', '#', 'i', 0)), Is.EqualTo((0, true)));
        // The reader never stores the elements of an 'N'-typed array, whatever the count.
        Assert.That(Repair(Bytes('[', '$', 'N', '#', 'L', 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF)), Is.EqualTo((0, true)));
    }

    [Test]
    public void BigEndianLengthsAreHonoured()
    {
        var text = Enumerable.Repeat((byte)'a', 299).Append((byte)0xE9).ToArray();
        var data = Bytes('S', 'I', 0x01, 0x2C, text, 'S', 'U', 1, 0x80);

        Assert.That(Repair(data), Is.EqualTo((1, true)), "only the first value is read");
        Assert.That(data[4 + 299], Is.EqualTo((byte)'?'));
        Assert.That(data[^1], Is.EqualTo(0x80), "bytes after the value are not part of it");
    }

    [Test]
    public void NegativeLengthsAndCountsMatchTheReader()
    {
        // A negative string length reads as empty; a negative count is a huge size that runs out of data.
        Assert.That(Repair(Bytes('[', 'S', 'i', 0xFF, Str(0xE9), ']')), Is.EqualTo((1, true)));
        Assert.That(Repair(Bytes('[', '#', 'i', 0xFF, Str('a'))).Complete, Is.False);
    }

    [Test]
    public void EveryTruncationIsIncompleteAndNeverThrows()
    {
        var full = Bytes('{', Key(0x80), '[', '$', 'S', '#', 'i', 2, Key('a'), Key(0xE9),
            Key('n'), '{', '#', 'i', 1, Key('x'), 'H', 'i', 2, "12", Key('s'), Str('o', 'k'), '}');
        Assert.That(Repair((byte[])full.Clone()), Is.EqualTo((2, true)));

        for (var length = 0; length < full.Length; length++)
        {
            var prefix = full.Take(length).ToArray();
            Assert.That(Repair(prefix).Complete, Is.False, $"prefix of {length} bytes");
        }
    }

    [Test]
    public void TypedArraysOfEmptyValuesAreLimited()
    {
        static byte[] NullArray(int count) => Bytes('[', '$', 'Z', '#', 'l',
            (count >> 24) & 0xFF, (count >> 16) & 0xFF, (count >> 8) & 0xFF, count & 0xFF);

        Assert.That(Repair(NullArray(UbjsonTextRepair.MaxEmptyElements)), Is.EqualTo((0, true)));
        Assert.That(Repair(NullArray(UbjsonTextRepair.MaxEmptyElements + 1)).Complete, Is.False);
        Assert.That(Repair(Bytes('[', '$', 'T', '#', 'L', 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF)).Complete, Is.False);
    }

    [Test]
    public void NestingIsLimited()
    {
        const int depth = UbjsonTextRepair.MaxDepth;
        var deepest = Bytes(Repeat('[', depth - 1), '{', Key(0x80), 'Z', '}', Repeat(']', depth - 1));
        var tooDeep = Bytes(Repeat('[', depth + 1), Repeat(']', depth + 1));

        Assert.That(Repair(deepest), Is.EqualTo((1, true)));
        Assert.That(deepest[depth + 2], Is.EqualTo((byte)'?'), "the key byte after '{', 'i' and the length");
        Assert.That(Repair(tooDeep).Complete, Is.False);
        Assert.That(Repair(Repeat('[', 200_000)).Complete, Is.False);
    }

    [TestCase("1.5e-3", true)]
    [TestCase("", true)]
    [TestCase("%n", false)]
    [TestCase("1x", false)]
    public void HighPrecisionNumbersMustBeNumberText(string text, bool complete)
    {
        Assert.That(Repair(Bytes('H', 'i', text.Length, text)), Is.EqualTo((0, complete)));
        Assert.That(Repair(Bytes('H', 'i', 1, 0xE9)).Complete, Is.False);
    }
}
