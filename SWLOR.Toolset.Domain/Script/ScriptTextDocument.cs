using System.Text;
using System.Text.Unicode;

namespace SWLOR.Toolset.Domain.Script
{
    /// <summary>How a script file's bytes map to text.</summary>
    public enum ScriptEncoding
    {
        /// <summary>Valid UTF-8, which covers every plain-ASCII script. The default for a new file.</summary>
        Utf8,

        /// <summary>
        /// Byte-for-byte Latin-1, used for legacy files that embed raw colour-code bytes and so are
        /// not valid UTF-8. Chosen over windows-1252 because it round-trips all 256 byte values.
        /// </summary>
        Latin1
    }

    /// <summary>The line ending a script file uses on disk.</summary>
    public enum ScriptEolStyle
    {
        /// <summary>Windows CRLF. The default for this module's corpus and for a new file.</summary>
        Crlf,

        /// <summary>Unix LF.</summary>
        Lf
    }

    /// <summary>
    /// A NWScript source file as text, plus everything needed to write it back without
    /// disturbing bytes the editor never touched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The GFF editors get zero-spurious-diff from lexical preservation of a parsed tree
    /// (see PLAN.md). A script is plain text, so the same guarantee reduces to four facts that
    /// must survive a load/save round trip: the EOL style, whether the file ended with a newline,
    /// whether it carried a UTF-8 BOM, and its encoding. Text is normalised to '\n' in memory -
    /// AvaloniaEdit works in '\n' and mixing styles inside the buffer would leak into every edit -
    /// and the original style is reapplied on the way out.
    /// </para>
    /// <para>
    /// <b>Encoding is detected, not assumed.</b> The module's colors_inc.nss is not valid UTF-8:
    /// it embeds raw high bytes directly inside string literals as NWScript colour codes. Those
    /// bytes are data, not text. This is the same trap PLAN.md records for GFF <c>void</c> fields
    /// ("all handling must be byte-level, never through .NET strings").
    /// </para>
    /// <para>
    /// So: a BOM means UTF-8; otherwise the bytes are validated without throwing and
    /// <b>Latin-1 is the fallback</b>, because it maps bytes 0x00-0xFF onto U+0000-U+00FF
    /// bijectively and therefore cannot lose a byte. Windows-1252 - the game's own encoding - was
    /// rejected despite displaying legacy accented text better: it leaves five byte values
    /// undefined, so it cannot promise a round trip, and the high bytes actually present here are
    /// colour codes rather than letters. On save the encoder throws rather than substituting, so a
    /// character that cannot be represented fails the save loudly instead of silently corrupting
    /// the file.
    /// </para>
    /// </remarks>
    public sealed class ScriptTextDocument
    {
        private static readonly UTF8Encoding StrictNoBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        private static readonly UTF8Encoding BomEncoding = new(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);

        private static readonly Encoding StrictLatin1 =
            Encoding.GetEncoding("ISO-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        private ScriptTextDocument(string text, ScriptEolStyle eol, bool trailingNewline, bool bom, ScriptEncoding encoding)
        {
            Text = text;
            EolStyle = eol;
            HasTrailingNewline = trailingNewline;
            HasByteOrderMark = bom;
            EncodingKind = encoding;
        }

        /// <summary>The encoding this file was read with, and will be written back with.</summary>
        public ScriptEncoding EncodingKind { get; }

        /// <summary>The source, with every line ending normalised to a single '\n'.</summary>
        public string Text { get; }

        /// <summary>The line ending this file used on disk, reapplied by <see cref="ToBytes"/>.</summary>
        public ScriptEolStyle EolStyle { get; }

        /// <summary>Whether the file ended with a line ending. Preserved exactly.</summary>
        public bool HasTrailingNewline { get; }

        /// <summary>Whether the file began with a UTF-8 BOM. Preserved exactly.</summary>
        public bool HasByteOrderMark { get; }

        /// <summary>Reads a script from disk, capturing its EOL/BOM/trailing-newline shape.</summary>
        public static ScriptTextDocument Load(string path) => FromBytes(File.ReadAllBytes(path));

        /// <summary>Reads a script from raw bytes. Split out from <see cref="Load"/> so it is testable without a file.</summary>
        public static ScriptTextDocument FromBytes(byte[] bytes)
        {
            var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            var offset = bom ? 3 : 0;
            var count = bytes.Length - offset;

            var isUtf8 = Utf8.IsValid(bytes.AsSpan(offset, count));
            var raw = isUtf8
                ? StrictNoBom.GetString(bytes, offset, count)
                : StrictLatin1.GetString(bytes, offset, count);
            var encoding = isUtf8 ? ScriptEncoding.Utf8 : ScriptEncoding.Latin1;

            // A file with no line ending at all is CRLF, matching the corpus and NewScript below;
            // otherwise the first ending seen wins. Mixed-ending files are normalised to that first
            // style, which is a real (rare) change - but leaving them mixed guarantees a diff on the
            // first edit anyway, and one consistent style is the lesser surprise.
            var eol = raw.Contains("\r\n") ? ScriptEolStyle.Crlf
                : raw.Contains('\n') ? ScriptEolStyle.Lf
                : ScriptEolStyle.Crlf;

            var normalised = raw.Replace("\r\n", "\n").Replace('\r', '\n');
            var trailing = normalised.EndsWith('\n');
            if (trailing)
                normalised = normalised[..^1];

            return new ScriptTextDocument(normalised, eol, trailing, bom, encoding);
        }

        /// <summary>The template for a brand-new script. Mirrors ModuleResourceTemplateFactory.</summary>
        public static ScriptTextDocument NewScript(string title) =>
            NewScript(title, "void main()\n{\n}");

        /// <summary>A brand-new script with caller-selected source, preserving the new-file shape.</summary>
        public static ScriptTextDocument NewScript(string title, string body) =>
            new($"// {title}\n{Normalise(body).TrimEnd('\n')}", ScriptEolStyle.Crlf,
                trailingNewline: true, bom: false, ScriptEncoding.Utf8);

        /// <summary>Returns this document with different text, keeping the on-disk shape.</summary>
        public ScriptTextDocument WithText(string text) =>
            new(Normalise(text), EolStyle, HasTrailingNewline, HasByteOrderMark, EncodingKind);

        /// <summary>
        /// Serialises <paramref name="text"/> using this document's captured shape. Passing back the
        /// text unchanged reproduces the original file byte for byte.
        /// </summary>
        public byte[] ToBytes(string text)
        {
            var body = Normalise(text);

            // The trailing newline is a property of the file, not of the buffer: an editor that
            // strips it on save would rewrite the last line of every file it touched.
            if (HasTrailingNewline)
                body += "\n";

            if (EolStyle == ScriptEolStyle.Crlf)
                body = body.Replace("\n", "\r\n");

            // Both encoders throw rather than substitute: a character the file's encoding cannot
            // represent must fail the save, not be written back as '?'.
            var encoded = EncodingKind == ScriptEncoding.Latin1
                ? StrictLatin1.GetBytes(body)
                : StrictNoBom.GetBytes(body);

            if (!HasByteOrderMark)
                return encoded;

            // Encoding.GetBytes never emits the preamble - encoderShouldEmitUTF8Identifier only
            // affects GetPreamble() - so a BOM has to be prepended by hand.
            var preamble = BomEncoding.GetPreamble();
            var withBom = new byte[preamble.Length + encoded.Length];
            preamble.CopyTo(withBom, 0);
            encoded.CopyTo(withBom, preamble.Length);
            return withBom;
        }

        /// <summary>Serialises this document's own text.</summary>
        public byte[] ToBytes() => ToBytes(Text);

        private static string Normalise(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
