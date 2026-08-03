using System.Text;
using System.Text.Unicode;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Normalizes NWN JSON that may be Windows-1252 or UTF-8.
    /// </summary>
    internal static class NwnJsonEncoding
    {
        private static readonly Encoding NwnText = CreateNwnTextEncoding();

        public static byte[] ReadFileAsUtf8(string path)
        {
            var raw = File.ReadAllBytes(path);
            if (raw.AsSpan().StartsWith(Encoding.UTF8.Preamble))
                return raw.AsSpan(Encoding.UTF8.Preamble.Length).ToArray();

            // Modern tools also emit BOM-less UTF-8. Treat a valid unmarked byte stream as UTF-8;
            // otherwise fall back to the legacy NWN Windows-1252 encoding. An unmarked stream that
            // is valid in both encodings is inherently ambiguous, and choosing UTF-8 avoids
            // corrupting contemporary module exports.
            if (Utf8.IsValid(raw))
                return raw;

            return Encoding.UTF8.GetBytes(NwnText.GetString(raw));
        }

        private static Encoding CreateNwnTextEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
    }
}
