using System.Text;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>
    /// Normalizes canonical Windows-1252 NWN JSON and explicitly BOM-marked UTF-8 JSON.
    /// </summary>
    internal static class NwnJsonEncoding
    {
        private static readonly Encoding NwnText = CreateNwnTextEncoding();

        public static byte[] ReadFileAsUtf8(string path)
        {
            var raw = File.ReadAllBytes(path);
            if (raw.AsSpan().StartsWith(Encoding.UTF8.Preamble))
                return raw.AsSpan(Encoding.UTF8.Preamble.Length).ToArray();

            // nwn_gff JSON is canonically Windows-1252. Validity cannot distinguish it from
            // UTF-8: the Windows-1252 bytes C2 A9, for example, are also valid UTF-8 but mean a
            // different string. A UTF-8 BOM is the explicit provenance that opts a file into
            // UTF-8; unmarked module JSON keeps the NWN encoding.
            return Encoding.UTF8.GetBytes(NwnText.GetString(raw));
        }

        private static Encoding CreateNwnTextEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
    }
}
