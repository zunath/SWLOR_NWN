using System.Collections.Generic;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Repairs invalid UTF-8 in the strings and object keys of encoded UBJSON in place, following the engine's
    /// nlohmann UBJSON reader marker for marker, and flags data the engine could not decode safely.
    /// </summary>
    public static class UbjsonTextRepair
    {
        /// <summary>The most containers open at once. The engine's reader recurses once per level.</summary>
        public const int MaxDepth = 256;

        /// <summary>The largest count for a typed array of values that hold no bytes (true, false, null).</summary>
        public const int MaxEmptyElements = 4096;

        private const int EndOfData = -1;
        private const string NumberText = "0123456789+-.eE";

        private const int IsObject = 1;
        private const int IsUnsized = 2;
        private const int ChildStarted = 4;
        private const int TypeShift = 8;

        /// <summary>
        /// Repairs the strings of one encoded UBJSON value and reports whether the engine can decode it safely.
        /// </summary>
        /// <param name="ubjson">The encoded value, repaired in place.</param>
        /// <param name="complete">False when the data is malformed or exceeds a safety limit.</param>
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
                    return ReadSize(data, ref position, ref current, out var digits) &&
                           SkipNumberText(data, ref position, ref current, digits);
                case 'S':
                    return ReadString(data, ref position, ref current, true, ref replaced);
                case '[':
                case '{':
                    if (remaining.Count >= MaxDepth ||
                        !ReadSizeAndType(data, ref position, ref current, out var count, out var type))
                        return false;
                    // The engine stores every element of such an array even though the data holds none of them.
                    if (marker == '[' && type != 'N' && !HoldsBytes(type) && count > MaxEmptyElements)
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

        // An optional "$type" (which requires "#") and an optional "#count"; -1 means unsized.
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
            if (!ReadLength(data, ref position, ref current, GetIgnoringNoOp(data, ref position, ref current), out size))
                return false;
            // The reader treats a negative size as a huge unsigned one.
            if (size < 0)
                size = long.MaxValue;
            return true;
        }

        // A string is a length number then that many bytes; object keys omit the 'S' marker.
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

        // The engine quotes invalid high-precision text in an error message, so only number characters are safe.
        private static bool SkipNumberText(Span<byte> data, ref int position, ref int current, long count)
        {
            if (count < 0 || count > data.Length - position)
                return false;

            foreach (var character in data.Slice(position, (int)count))
            {
                if (NumberText.IndexOf((char)character) < 0)
                    return false;
            }

            return Skip(data, ref position, ref current, count);
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
