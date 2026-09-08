using System.Security.Cryptography;
using System.Text;

namespace SWLOR.Toolset.Domain.Animation;

/// <summary>Editable bank text paired with a compiled HAK resource, outside packaged folders.</summary>
internal static class AnimationBankSource
{
    private const string Marker = "# SWLOR compiled animation source v1 ";
    internal static bool IsBinary(byte[] bytes) => bytes.Length >= 4 && BitConverter.ToUInt32(bytes) == 0;
    internal static string PathFor(string hakRoot, string modelPath) =>
        Path.Combine(hakRoot, "model_sources", Path.GetRelativePath(hakRoot, modelPath) + ".ascii");

    internal static byte[] Encode(byte[] source, byte[] compiled)
    {
        var text = Encoding.UTF8.GetString(AnimationSourceFile.Utf8Content(source).Span).Replace("\r\n", "\n");
        var content = Encoding.UTF8.GetBytes(text);
        return Encoding.UTF8.GetBytes(Marker + Hash(compiled) + " " + Hash(content) + "\n" + text);
    }

    internal static byte[] Decode(byte[] saved, byte[] compiled)
    {
        var text = Encoding.UTF8.GetString(AnimationSourceFile.Utf8Content(saved).Span).Replace("\r\n", "\n");
        var end = text.IndexOf('\n');
        if (end < 0 || !text.StartsWith(Marker, StringComparison.Ordinal))
            throw new InvalidDataException("Missing compiled animation source metadata. Recompile the bank from its verified source.");
        var hashes = text[Marker.Length..end].Split(' ');
        var content = Encoding.UTF8.GetBytes(text[(end + 1)..]);
        if (hashes.Length != 2 || hashes[0] != Hash(compiled) || hashes[1] != Hash(content))
            throw new InvalidDataException("Compiled animation bank and editable source do not match. Recompile the bank from its verified source.");
        return content;
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
