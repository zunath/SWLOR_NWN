// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.Unicode;

namespace SWLOR.NWN.Formats.Internal;

internal static class NwnTextEncoding
{
    static NwnTextEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static string DecodeGeneral(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Encoding.UTF8.Preamble))
            return Encoding.UTF8.GetString(bytes[Encoding.UTF8.Preamble.Length..]);

        return Utf8.IsValid(bytes)
            ? Encoding.UTF8.GetString(bytes)
            : Encoding.GetEncoding(1252).GetString(bytes);
    }

    public static Encoding ForLanguage(uint languageId)
    {
        var codePage = languageId switch
        {
            5 => 1250,
            128 => 949,
            129 => 950,
            130 => 936,
            131 => 932,
            _ => 1252
        };

        return Encoding.GetEncoding(codePage);
    }
}
