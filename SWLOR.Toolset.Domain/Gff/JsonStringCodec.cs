using System.Text;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// Decodes and encodes JSON string tokens in the style emitted by nwn_gff (Nim's json
    /// module). Decoding is for display/inspection only — untouched values round-trip through
    /// their raw bytes and are never re-encoded, which matters because void fields embed raw
    /// binary (including invalid UTF-8) directly in string tokens.
    /// </summary>
    public static class JsonStringCodec
    {
        /// <summary>
        /// Decodes a raw string token (including surrounding quotes) to a .NET string.
        /// Invalid UTF-8 sequences decode to replacement characters; callers must not
        /// re-encode a decoded string over an untouched raw token.
        /// </summary>
        public static string Decode(ReadOnlySpan<byte> rawToken)
        {
            if (rawToken.Length < 2 || rawToken[0] != (byte)'"' || rawToken[^1] != (byte)'"')
                throw new FormatException("String token must be enclosed in double quotes.");

            var inner = rawToken[1..^1];
            if (inner.IndexOf((byte)'\\') < 0)
                return Encoding.UTF8.GetString(inner);

            var bytes = new List<byte>(inner.Length);
            for (var i = 0; i < inner.Length; i++)
            {
                var b = inner[i];
                if (b != (byte)'\\')
                {
                    bytes.Add(b);
                    continue;
                }

                i++;
                if (i >= inner.Length)
                    throw new FormatException("Dangling escape at end of string token.");

                switch ((char)inner[i])
                {
                    case '"': bytes.Add((byte)'"'); break;
                    case '\\': bytes.Add((byte)'\\'); break;
                    case '/': bytes.Add((byte)'/'); break;
                    case 'b': bytes.Add(8); break;
                    case 'f': bytes.Add(12); break;
                    case 'n': bytes.Add((byte)'\n'); break;
                    case 'r': bytes.Add((byte)'\r'); break;
                    case 't': bytes.Add((byte)'\t'); break;
                    case 'u':
                        if (i + 4 >= inner.Length)
                            throw new FormatException("Truncated \\u escape in string token.");
                        var code = ParseHex4(inner.Slice(i + 1, 4));
                        i += 4;
                        AppendUtf8(bytes, code);
                        break;
                    default:
                        throw new FormatException($"Unknown escape '\\{(char)inner[i]}' in string token.");
                }
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        /// <summary>
        /// Encodes a .NET string as a raw JSON string token (including quotes) using
        /// nwn_gff-compatible escaping: only the JSON-mandated escapes plus \u00XX for other
        /// control characters; non-ASCII passes through as raw UTF-8.
        /// </summary>
        public static byte[] Encode(string value)
        {
            var builder = new List<byte>(value.Length + 2) { (byte)'"' };
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                switch (b)
                {
                    case (byte)'"':
                        builder.Add((byte)'\\');
                        builder.Add((byte)'"');
                        break;
                    case (byte)'\\':
                        builder.Add((byte)'\\');
                        builder.Add((byte)'\\');
                        break;
                    case 8:
                        AddAscii(builder, "\\b");
                        break;
                    case 12:
                        AddAscii(builder, "\\f");
                        break;
                    case (byte)'\n':
                        AddAscii(builder, "\\n");
                        break;
                    case (byte)'\r':
                        AddAscii(builder, "\\r");
                        break;
                    case (byte)'\t':
                        AddAscii(builder, "\\t");
                        break;
                    default:
                        if (b < 0x20)
                            AddAscii(builder, $"\\u{b:x4}");
                        else
                            builder.Add(b);
                        break;
                }
            }

            builder.Add((byte)'"');
            return builder.ToArray();
        }

        private static void AddAscii(List<byte> builder, string text)
        {
            foreach (var c in text)
                builder.Add((byte)c);
        }

        private static int ParseHex4(ReadOnlySpan<byte> hex)
        {
            var value = 0;
            foreach (var b in hex)
            {
                value <<= 4;
                value |= (char)b switch
                {
                    >= '0' and <= '9' => b - '0',
                    >= 'a' and <= 'f' => b - 'a' + 10,
                    >= 'A' and <= 'F' => b - 'A' + 10,
                    _ => throw new FormatException("Invalid hex digit in \\u escape.")
                };
            }

            return value;
        }

        private static void AppendUtf8(List<byte> bytes, int codePoint)
        {
            if (codePoint < 0x80)
            {
                bytes.Add((byte)codePoint);
            }
            else if (codePoint < 0x800)
            {
                bytes.Add((byte)(0xC0 | (codePoint >> 6)));
                bytes.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
            else
            {
                bytes.Add((byte)(0xE0 | (codePoint >> 12)));
                bytes.Add((byte)(0x80 | ((codePoint >> 6) & 0x3F)));
                bytes.Add((byte)(0x80 | (codePoint & 0x3F)));
            }
        }
    }
}
