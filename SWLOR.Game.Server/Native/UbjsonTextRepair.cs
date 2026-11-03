using System.Collections.Generic;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Repairs invalid UTF-8 in every string value and object key of encoded UBJSON, in place and without
    /// changing any length, before the engine decodes it. The walk follows the engine's nlohmann UBJSON reader
    /// marker for marker, so it visits exactly the bytes that reader turns into strings. Number bytes are never
    /// touched. The walk keeps its own stack, so nesting depth does not limit it.
    /// </summary>
    public static class UbjsonTextRepair
    {
        private const int EndOfData = -1;

        // Container state kept on the walk's stack.
        private const int IsObject = 1;
        private const int IsUnsized = 2;
        private const int ChildStarted = 4;
        private const int TypeShift = 8;

        /// <summary>
        /// Repairs the strings of one encoded UBJSON value.
        /// </summary>
        /// <param name="ubjson">The encoded value, repaired in place.</param>
        /// <param name="complete">
        /// Whether the whole value was read. False means the data is malformed; the engine's reader rejects it too.
        /// </param>
        /// <returns>The number of bytes replaced.</returns>
        public static int RepairStrings(Span<byte> ubjson, out bool complete)
        {
            var position = 0;
            var current = EndOfData;
            var replaced = 0;
            var remaining = new List<long>();
            var states = new List<int>();

            complete = StartValue(ubjson, ref position, ref current, GetIgnoringNoOp(ubjson, ref position, ref current),
                remaining, states, ref replaced);
            while (complete && remaining.Count > 0)
            {
                var top = remaining.Count - 1;
                var state = states[top];
                var isObject = (state & IsObject) != 0;
                var type = state >> TypeShift;

                // An unsized container reads past the value it just finished before checking for its end.
                if ((state & ChildStarted) != 0)
                {
                    state &= ~ChildStarted;
                    states[top] = state;
                    if ((state & IsUnsized) != 0)
                        GetIgnoringNoOp(ubjson, ref position, ref current);
                }

                int marker;
                if ((state & IsUnsized) != 0)
                {
                    if (current == (isObject ? '}' : ']'))
                    {
                        Pop(remaining, states);
                        continue;
                    }

                    // An unsized object's key reuses the marker already read.
                    if (isObject && !ReadString(ubjson, ref position, ref current, false, ref replaced))
                    {
                        complete = false;
                        break;
                    }

                    marker = isObject ? GetIgnoringNoOp(ubjson, ref position, ref current) : current;
                }
                else
                {
                    // Typed arrays of values that hold no bytes (including the 'N' type) need no walk.
                    if (remaining[top] == 0 || (!isObject && type != 0 && !HoldsBytes(type)))
                    {
                        Pop(remaining, states);
                        continue;
                    }

                    remaining[top]--;
                    if (isObject && !ReadString(ubjson, ref position, ref current, true, ref replaced))
                    {
                        complete = false;
                        break;
                    }

                    marker = type != 0 ? type : GetIgnoringNoOp(ubjson, ref position, ref current);
                }

                states[top] = state | ChildStarted;
                complete = StartValue(ubjson, ref position, ref current, marker, remaining, states, ref replaced);
            }

            return replaced;
        }

        /// <summary>
        /// Reads a scalar value whole, or opens a container by pushing it onto the walk's stack.
        /// </summary>
        private static bool StartValue(Span<byte> data, ref int position, ref int current, int marker,
            List<long> remaining, List<int> states, ref int replaced)
        {
            switch (marker)
            {
                case 'T':
                case 'F':
                case 'Z':
                    return true;
                case 'U':
                case 'i':
                case 'C':
                    return Skip(data, ref position, ref current, 1);
                case 'I':
                    return Skip(data, ref position, ref current, 2);
                case 'l':
                case 'd':
                    return Skip(data, ref position, ref current, 4);
                case 'L':
                case 'D':
                    return Skip(data, ref position, ref current, 8);
                case 'H':
                    // High-precision numbers are read as number text and never stored as strings.
                    return ReadSize(data, ref position, ref current, out var digits) &&
                           Skip(data, ref position, ref current, digits);
                case 'S':
                    return ReadString(data, ref position, ref current, true, ref replaced);
                case '[':
                case '{':
                    if (!ReadSizeAndType(data, ref position, ref current, out var count, out var type))
                        return false;
                    remaining.Add(count);
                    states.Add((marker == '{' ? IsObject : 0) | (count < 0 ? IsUnsized : 0) | (type << TypeShift));
                    return true;
                default:
                    return false;
            }
        }

        private static void Pop(List<long> remaining, List<int> states)
        {
            remaining.RemoveAt(remaining.Count - 1);
            states.RemoveAt(states.Count - 1);
        }

        // An optional "$type" (which requires "#") and an optional "#count". A count of -1 means unsized; the
        // following marker is then left in current.
        private static bool ReadSizeAndType(Span<byte> data, ref int position, ref int current, out long count, out int type)
        {
            count = -1;
            type = 0;
            GetIgnoringNoOp(data, ref position, ref current);
            if (current == '$')
            {
                type = Get(data, ref position, ref current);
                if (type == EndOfData || GetIgnoringNoOp(data, ref position, ref current) != '#')
                    return false;
                return ReadSize(data, ref position, ref current, out count);
            }

            if (current == '#')
                return ReadSize(data, ref position, ref current, out count);

            return true;
        }

        private static bool ReadSize(Span<byte> data, ref int position, ref int current, out long size)
        {
            // A negative size counts as a huge unsigned size in the reader; it runs out of data either way.
            if (!ReadLength(data, ref position, ref current, GetIgnoringNoOp(data, ref position, ref current), out size))
                return false;
            if (size < 0)
                size = long.MaxValue;
            return true;
        }

        // A string is a length (a 'U', 'i', 'I', 'l' or 'L' number) followed by that many bytes. Object keys omit
        // the 'S' marker; an unsized object's key reuses the marker already read.
        private static bool ReadString(Span<byte> data, ref int position, ref int current, bool readMarker, ref int replaced)
        {
            if (readMarker)
                Get(data, ref position, ref current);

            if (!ReadLength(data, ref position, ref current, current, out var length))
                return false;
            if (length <= 0)
                return true;
            if (length > data.Length - position)
                return false;

            replaced += Utf8TextRepair.ReplaceInvalidBytes(data.Slice(position, (int)length));
            position += (int)length;
            current = data[position - 1];
            return true;
        }

        private static bool ReadLength(Span<byte> data, ref int position, ref int current, int marker, out long length)
        {
            length = 0;
            int bytes;
            bool signed;
            switch (marker)
            {
                case 'U':
                    bytes = 1;
                    signed = false;
                    break;
                case 'i':
                    bytes = 1;
                    signed = true;
                    break;
                case 'I':
                    bytes = 2;
                    signed = true;
                    break;
                case 'l':
                    bytes = 4;
                    signed = true;
                    break;
                case 'L':
                    bytes = 8;
                    signed = true;
                    break;
                default:
                    return false;
            }

            if (bytes > data.Length - position)
                return false;

            // UBJSON numbers are big-endian.
            ulong value = 0;
            for (var index = 0; index < bytes; index++)
                value = (value << 8) | (byte)Get(data, ref position, ref current);

            if (signed && bytes < 8)
            {
                var shift = 64 - bytes * 8;
                length = (long)(value << shift) >> shift;
            }
            else
            {
                length = (long)value;
            }

            return true;
        }

        private static bool HoldsBytes(int type)
        {
            return type != 'T' && type != 'F' && type != 'Z' && type != 'N';
        }

        private static bool Skip(Span<byte> data, ref int position, ref int current, long count)
        {
            if (count < 0 || count > data.Length - position)
                return false;
            if (count == 0)
                return true;

            position += (int)count;
            current = data[position - 1];
            return true;
        }

        private static int Get(Span<byte> data, ref int position, ref int current)
        {
            current = position < data.Length ? data[position++] : EndOfData;
            return current;
        }

        private static int GetIgnoringNoOp(Span<byte> data, ref int position, ref int current)
        {
            do
            {
                Get(data, ref position, ref current);
            }
            while (current == 'N');

            return current;
        }
    }
}
