using System.Text;

namespace SWLOR.FacadeRemoval;

/// <summary>
/// A source file's bytes as text that writes back byte for byte: read as Latin-1 (one character per byte, so every
/// position is a byte offset and the tool's ASCII edits never touch the other bytes), with a UTF-8 byte order mark kept
/// aside and the file's own line ending noted.
/// </summary>
internal sealed record SourceFile(string Bom, string Text)
{
    private const string Utf8Bom = "ï»¿";

    /// <summary>The line ending the tool writes into this file: CRLF when the file has one, else LF.</summary>
    public string LineEnding => Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    public static SourceFile Decode(byte[] bytes)
    {
        var text = Encoding.Latin1.GetString(bytes);
        return text.StartsWith(Utf8Bom, StringComparison.Ordinal)
            ? new SourceFile(Utf8Bom, text[Utf8Bom.Length..])
            : new SourceFile(string.Empty, text);
    }

    public static SourceFile Read(string path) => Decode(File.ReadAllBytes(path));

    public byte[] Encode(string text) => Encoding.Latin1.GetBytes(Bom + text);
}
