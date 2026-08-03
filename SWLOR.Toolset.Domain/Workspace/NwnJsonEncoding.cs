using System.Buffers;
using System.Text;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Normalizes NWN JSON that may be Windows-1252 or UTF-8.
    /// </summary>
    internal static class NwnJsonEncoding
    {
        public static byte[] ReadFileAsUtf8(string path)
        {
            var raw = File.ReadAllBytes(path);
            var contentOffset = raw.AsSpan().StartsWith(Encoding.UTF8.Preamble)
                ? Encoding.UTF8.Preamble.Length
                : 0;
            return NormalizeStringTokens(raw, contentOffset);
        }

        /// <summary>
        /// Converts each JSON string independently through the same encoding-aware token codec as
        /// the editable GFF reader. GIT files can contain legacy Windows-1252 and imported UTF-8
        /// strings together; choosing one encoding for the entire byte stream corrupts one side of
        /// that mix. Syntax, whitespace, and numeric values remain byte-for-byte unchanged.
        /// </summary>
        private static byte[] NormalizeStringTokens(byte[] raw, int contentOffset)
        {
            var content = raw.AsSpan(contentOffset);
            ArrayBufferWriter<byte>? output = null;
            var position = 0;
            var copyStart = 0;
            while (position < content.Length)
            {
                var relativeStart = content[position..].IndexOf((byte)'"');
                if (relativeStart < 0)
                    break;

                var tokenStart = position + relativeStart;
                var tokenEnd = FindStringTokenEnd(content, tokenStart);
                var token = content[tokenStart..tokenEnd];
                if (!JsonStringCodec.IsUtf8Content(token[1..^1]))
                {
                    output ??= new ArrayBufferWriter<byte>(content.Length);
                    output.Write(content[copyStart..tokenStart]);
                    output.Write(JsonStringCodec.Encode(JsonStringCodec.Decode(token), useUtf8: true));
                    copyStart = tokenEnd;
                }

                position = tokenEnd;
            }

            if (output == null)
                return contentOffset == 0 ? raw : content.ToArray();

            output.Write(content[copyStart..]);
            return output.WrittenSpan.ToArray();
        }

        private static int FindStringTokenEnd(ReadOnlySpan<byte> content, int tokenStart)
        {
            for (var position = tokenStart + 1; position < content.Length; position++)
            {
                if (content[position] == (byte)'\\')
                {
                    position++;
                    if (position >= content.Length)
                        break;
                    continue;
                }

                if (content[position] == (byte)'"')
                    return position + 1;
            }

            throw new FormatException($"Unterminated JSON string token at offset {tokenStart}.");
        }
    }
}
