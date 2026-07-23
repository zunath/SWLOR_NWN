using System.Text;

namespace SWLOR.Toolset.Domain.Gff
{
    /// <summary>
    /// Decodes and encodes JSON string tokens in the style emitted by nwn_gff (Nim's json
    /// module). NWN text is Windows-1252 rather than UTF-8; the byte-level methods remain available
    /// for void fields, which can contain arbitrary binary.
    /// </summary>
    public static class JsonStringCodec
    {
        private static readonly Encoding NwnEncoding;

        static JsonStringCodec()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            NwnEncoding = Encoding.GetEncoding(
                1252,
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ReplacementFallback);
        }

        /// <summary>
        /// Decodes a raw NWN string token (including surrounding quotes) as Windows-1252. Because
        /// that is a single-byte encoding, native high bytes survive an edit round trip unchanged.
        /// </summary>
        public static string Decode(ReadOnlySpan<byte> rawToken)
        {
            if (rawToken.Length < 2 || rawToken[0] != (byte)'"' || rawToken[^1] != (byte)'"')
                throw new FormatException("String token must be enclosed in double quotes.");

            var inner = rawToken[1..^1];
            var text = new StringBuilder(inner.Length);
            var segmentStart = 0;
            for (var i = 0; i < inner.Length; i++)
            {
                if (inner[i] != (byte)'\\')
                    continue;

                if (i > segmentStart)
                    text.Append(NwnEncoding.GetString(inner[segmentStart..i]));

                i++;
                if (i >= inner.Length)
                    throw new FormatException("Dangling escape at end of string token.");

                switch ((char)inner[i])
                {
                    case '"': text.Append('"'); break;
                    case '\\': text.Append('\\'); break;
                    case '/': text.Append('/'); break;
                    case 'b': text.Append('\b'); break;
                    case 'f': text.Append('\f'); break;
                    case 'n': text.Append('\n'); break;
                    case 'r': text.Append('\r'); break;
                    case 't': text.Append('\t'); break;
                    case 'u':
                        if (i + 4 >= inner.Length)
                            throw new FormatException("Truncated \\u escape in string token.");
                        text.Append((char)ParseHex4(inner.Slice(i + 1, 4)));
                        i += 4;
                        break;
                    default:
                        throw new FormatException($"Unknown escape '\\{(char)inner[i]}' in string token.");
                }

                segmentStart = i + 1;
            }

            if (segmentStart < inner.Length)
                text.Append(NwnEncoding.GetString(inner[segmentStart..]));
            return text.ToString();
        }

        /// <summary>
        /// Encodes a .NET string as a raw JSON string token (including quotes) using
        /// nwn_gff-compatible escaping: only the JSON-mandated escapes plus \u00XX for other
        /// control characters; non-ASCII passes through as raw Windows-1252. Characters that
        /// Windows-1252 cannot represent are rejected instead of being silently replaced by '?'.
        /// </summary>
        public static byte[] Encode(string value)
        {
            return EncodeBytes(NwnEncoding.GetBytes(value));
        }

        /// <summary>
        /// Decodes a raw string token (including surrounding quotes) to its literal content
        /// bytes, unescaping JSON escape sequences without ever validating or re-encoding as
        /// UTF-8. Unlike <see cref="Decode"/>, this is lossless for void payloads that embed
        /// raw binary — including invalid UTF-8 — directly in the token.
        /// </summary>
        public static byte[] DecodeToBytes(ReadOnlySpan<byte> rawToken)
        {
            if (rawToken.Length < 2 || rawToken[0] != (byte)'"' || rawToken[^1] != (byte)'"')
                throw new FormatException("String token must be enclosed in double quotes.");

            var inner = rawToken[1..^1];
            if (inner.IndexOf((byte)'\\') < 0)
                return inner.ToArray();

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

            return bytes.ToArray();
        }

        /// <summary>
        /// Encodes raw content bytes as a JSON string token (including quotes), the inverse of
        /// <see cref="DecodeToBytes"/>. Bytes are emitted independently — no UTF-8 grouping or
        /// validation — so arbitrary binary (e.g. void payloads) round-trips byte-identically.
        /// </summary>
        public static byte[] EncodeBytes(ReadOnlySpan<byte> rawBytes)
        {
            var builder = new List<byte>(rawBytes.Length + 2) { (byte)'"' };
            foreach (var b in rawBytes)
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
