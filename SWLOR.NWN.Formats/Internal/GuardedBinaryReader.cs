// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Text;

namespace SWLOR.NWN.Formats.Internal;

internal sealed class GuardedBinaryReader
{
    private readonly byte[] _data;

    public GuardedBinaryReader(byte[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public int Length => _data.Length;

    public byte ReadByte(long offset)
    {
        ValidateRange(offset, 1, "byte");
        return _data[(int)offset];
    }

    public sbyte ReadSByte(long offset) => unchecked((sbyte)ReadByte(offset));

    public ushort ReadUInt16(long offset)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(Slice(offset, 2, "UInt16"));
    }

    public short ReadInt16(long offset)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(Slice(offset, 2, "Int16"));
    }

    public uint ReadUInt32(long offset)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(Slice(offset, 4, "UInt32"));
    }

    public int ReadInt32(long offset)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(Slice(offset, 4, "Int32"));
    }

    public ulong ReadUInt64(long offset)
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(Slice(offset, 8, "UInt64"));
    }

    public long ReadInt64(long offset)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(Slice(offset, 8, "Int64"));
    }

    public float ReadSingle(long offset)
    {
        return BitConverter.Int32BitsToSingle(ReadInt32(offset));
    }

    public double ReadDouble(long offset)
    {
        return BitConverter.Int64BitsToDouble(ReadInt64(offset));
    }

    public byte[] ReadBytes(long offset, long count, string context)
    {
        return Slice(offset, count, context).ToArray();
    }

    public string ReadAscii(long offset, int count, string context, bool trimNull = false)
    {
        var value = Encoding.ASCII.GetString(Slice(offset, count, context));
        return trimNull ? value.TrimEnd('\0') : value;
    }

    public ReadOnlySpan<byte> Slice(long offset, long count, string context)
    {
        ValidateRange(offset, count, context);
        return _data.AsSpan((int)offset, (int)count);
    }

    public void ValidateRange(long offset, long count, string context)
    {
        if (offset < 0 || count < 0 || offset > _data.LongLength || count > _data.LongLength - offset)
        {
            throw new NwnFormatException(
                $"{context} range [{offset}, {offset + Math.Max(count, 0)}) exceeds the {_data.LongLength}-byte input.");
        }

        if (offset > int.MaxValue || count > int.MaxValue)
            throw new NwnFormatException($"{context} exceeds the supported in-memory resource size.");
    }

    public static int CheckedCount(uint count, int elementSize, int maximum, string context)
    {
        if (count > maximum)
            throw new NwnFormatException($"{context} count {count} exceeds the limit {maximum}.");

        try
        {
            return checked((int)count * elementSize);
        }
        catch (OverflowException ex)
        {
            throw new NwnFormatException($"{context} byte count overflows.", ex);
        }
    }
}
