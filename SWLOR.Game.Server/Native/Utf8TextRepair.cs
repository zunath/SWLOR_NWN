namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Replaces bytes that are not part of a well-formed UTF-8 sequence, in place, so the text keeps its length.
    /// Well-formed means RFC 3629: no overlong forms, no surrogates, nothing above U+10FFFF. This is the rule the
    /// engine's JSON serializer enforces when it turns JSON into text.
    /// </summary>
    public static class Utf8TextRepair
    {
        /// <summary>The byte written in place of each invalid byte.</summary>
        public const byte Replacement = (byte)'?';

        /// <summary>
        /// Replaces every byte that does not belong to a well-formed UTF-8 sequence with <see cref="Replacement"/>.
        /// Well-formed text is left unchanged.
        /// </summary>
        /// <param name="text">The text to repair in place.</param>
        /// <returns>The number of bytes replaced.</returns>
        public static int ReplaceInvalidBytes(Span<byte> text)
        {
            var replaced = 0;
            var index = 0;
            while (index < text.Length)
            {
                var length = GetSequenceLength(text, index);
                if (length > 0)
                {
                    index += length;
                    continue;
                }

                text[index] = Replacement;
                replaced++;
                index++;
            }

            return replaced;
        }

        /// <summary>
        /// Returns the length of the well-formed UTF-8 sequence starting at the index, or 0 when the byte there
        /// does not start one.
        /// </summary>
        private static int GetSequenceLength(ReadOnlySpan<byte> text, int index)
        {
            var lead = text[index];
            if (lead < 0x80)
                return 1;

            int length;
            byte secondMinimum = 0x80;
            byte secondMaximum = 0xBF;
            if (lead >= 0xC2 && lead <= 0xDF)
            {
                length = 2;
            }
            else if (lead == 0xE0)
            {
                length = 3;
                secondMinimum = 0xA0;
            }
            else if (lead == 0xED)
            {
                length = 3;
                secondMaximum = 0x9F;
            }
            else if (lead >= 0xE1 && lead <= 0xEF)
            {
                length = 3;
            }
            else if (lead == 0xF0)
            {
                length = 4;
                secondMinimum = 0x90;
            }
            else if (lead >= 0xF1 && lead <= 0xF3)
            {
                length = 4;
            }
            else if (lead == 0xF4)
            {
                length = 4;
                secondMaximum = 0x8F;
            }
            else
            {
                return 0;
            }

            if (index + length > text.Length)
                return 0;

            var second = text[index + 1];
            if (second < secondMinimum || second > secondMaximum)
                return 0;

            for (var offset = 2; offset < length; offset++)
            {
                var continuation = text[index + offset];
                if (continuation < 0x80 || continuation > 0xBF)
                    return 0;
            }

            return length;
        }
    }
}
