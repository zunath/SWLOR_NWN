using System.Text;
using System.Text.Unicode;

namespace SWLOR.Toolset.Domain.Workspace
{
    /// <summary>Normalizes the mixed UTF-8 and Windows-1252 JSON emitted by NWN tooling.</summary>
    internal static class NwnJsonEncoding
    {
        private static readonly Encoding NwnText = CreateNwnTextEncoding();

        public static byte[] ReadFileAsUtf8(string path)
        {
            var raw = File.ReadAllBytes(path);
            return Utf8.IsValid(raw)
                ? raw
                : Encoding.UTF8.GetBytes(NwnText.GetString(raw));
        }

        private static Encoding CreateNwnTextEncoding()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
    }
}
