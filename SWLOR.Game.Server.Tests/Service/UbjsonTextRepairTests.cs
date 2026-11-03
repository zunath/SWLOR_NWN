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

    // "S" + "i" + length + bytes; object keys use the same form without "S".
    private static byte[] Key(params int[] bytes) => Bytes('i', bytes.Length, bytes.Select(value => (byte)value).ToArray());
    private static byte[] Str(params int[] bytes) => Bytes('S', Key(bytes));

    [Test]
    public void UnsizedObjectKeysAndValuesAreRepaired()
    {
        var data = Bytes('{', Key('k', 0xE9, 'y'), Str('S', 'l', 0xE1, 'i', 'n', 'e'), Key('n'), 'i', 5, '}');

        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);

        Assert.That(replaced, Is.EqualTo(2));
        Assert.That(complete, Is.True);
        Assert.That(data, Is.EqualTo(Bytes('{', Key('k', '?', 'y'), Str('S', 'l', '?', 'i', 'n', 'e'), Key('n'), 'i', 5, '}')));
    }

    [Test]
    public void NumberBytesAreNeverChanged()
    {
        var data = Bytes('[',
            'U', 0xE9, 'i', 0x80, 'I', 0xC3, 0x28, 'l', 0x80, 0xE9, 0x00, 0x01,
            'L', 0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA, 0xF9, 0xF8,
            'd', 0xC0, 0xAF, 0x80, 0x81, 'D', 0xE0, 0x80, 0xAF, 0xED, 0xA0, 0x80, 0xF5, 0xFF,
            'C', 'x', 'H', 'i', 3, '1', '.', '5', 'T', 'F', 'Z', ']');
        var original = (byte[])data.Clone();

        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);

        Assert.That(replaced, Is.Zero);
        Assert.That(data, Is.EqualTo(original));
        Assert.That(complete, Is.True);
    }

    [Test]
    public void SizedAndTypedArraysOfStringsAreRepaired()
    {
        // An unsized array holding a typed array (values carry no marker) and a sized one with no-ops.
        var data = Bytes('[',
            '[', '$', 'S', '#', 'i', 2, Key('a', 0xE9), Key(0x80),
            '[', '#', 'i', 2, 'N', Str(0x92), 'N', Str('o', 'k'),
            ']');

        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);

        Assert.That(replaced, Is.EqualTo(3));
        Assert.That(complete, Is.True);
        Assert.That(data, Is.EqualTo(Bytes('[',
            '[', '$', 'S', '#', 'i', 2, Key('a', '?'), Key('?'),
            '[', '#', 'i', 2, 'N', Str('?'), 'N', Str('o', 'k'),
            ']')));
    }

    [Test]
    public void SizedAndTypedObjectKeysAndValuesAreRepaired()
    {
        // Typed values carry no marker; untyped sized objects read one marker per value.
        var data = Bytes('{', '#', 'i', 2,
            Key(0x80), '{', '$', 'S', '#', 'i', 2, Key('k', 0xE9), Key('x'), Key(0x81), Key(0x82),
            Key('n'), 'L', 0, 0, 0, 0, 0, 0, 0xE9, 0x80);

        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);

        Assert.That(replaced, Is.EqualTo(4));
        Assert.That(complete, Is.True);
        Assert.That(data, Is.EqualTo(Bytes('{', '#', 'i', 2,
            Key('?'), '{', '$', 'S', '#', 'i', 2, Key('k', '?'), Key('x'), Key('?'), Key('?'),
            Key('n'), 'L', 0, 0, 0, 0, 0, 0, 0xE9, 0x80)));
    }

    [Test]
    public void BigEndianLengthsAreHonoured()
    {
        var text = Enumerable.Repeat((byte)'a', 299).Append((byte)0xE9).ToArray();
        var data = Bytes('S', 'I', 0x01, 0x2C, text, 'S', 'U', 1, 0x80);

        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);

        Assert.That(replaced, Is.EqualTo(1), "only the first value is read");
        Assert.That(complete, Is.True);
        Assert.That(data[4 + 299], Is.EqualTo((byte)'?'));
        Assert.That(data[^1], Is.EqualTo(0x80), "bytes after the value are not part of it");
    }

    [Test]
    public void NegativeStringLengthReadsAsEmpty()
    {
        var data = Bytes('[', 'S', 'i', 0xFF, Str(0xE9), ']');

        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);

        Assert.That(replaced, Is.EqualTo(1));
        Assert.That(complete, Is.True);
        Assert.That(data, Is.EqualTo(Bytes('[', 'S', 'i', 0xFF, Str('?'), ']')));
    }

    [Test]
    public void TruncatedDataStopsWithoutThrowing()
    {
        var data = Bytes('[', Str(0xE9), 'S', 'i', 20, 0x80);

        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);

        Assert.That(replaced, Is.EqualTo(1));
        Assert.That(complete, Is.False);
        Assert.That(data[^1], Is.EqualTo(0x80), "a string that runs past the end is not read");
    }

    [Test]
    public void HugeCountOfEmptyValuesReturnsAtOnce()
    {
        var data = Bytes('[', '$', 'Z', '#', 'L', 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);

        Assert.That(UbjsonTextRepair.RepairStrings(data, out var complete), Is.Zero);
        Assert.That(complete, Is.True);
    }

    [Test]
    public void DeeplyNestedKeysAreRepaired()
    {
        const int depth = 200_000;
        var data = Bytes(Enumerable.Repeat((byte)'[', depth).ToArray(), '{', Key(0x80), 'Z', '}',
            Enumerable.Repeat((byte)']', depth).ToArray());

        var replaced = UbjsonTextRepair.RepairStrings(data, out var complete);

        Assert.That(replaced, Is.EqualTo(1));
        Assert.That(complete, Is.True);
        Assert.That(data[depth + 3], Is.EqualTo((byte)'?'));
    }

    [Test]
    public void UnclosedNestingIsReportedIncomplete()
    {
        var data = Enumerable.Repeat((byte)'[', 200_000).ToArray();

        Assert.That(UbjsonTextRepair.RepairStrings(data, out var complete), Is.Zero);
        Assert.That(complete, Is.False);
    }
}
