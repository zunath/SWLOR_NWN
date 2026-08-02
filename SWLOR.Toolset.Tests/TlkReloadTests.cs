using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Tlk;

namespace SWLOR.Toolset.Tests
{
    public class TlkReloadTests
    {
        [Test]
        public void ReloadCustomTlk_UsesThePackedFileAndCanRestoreTheRepositoryFallback()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var tlkPath = Path.Combine(tempRoot, "selected.tlk");

            try
            {
                WriteSingleEntryTlk(tlkPath, "selected from nwn.ini TLK folder");
                var service = new TlkService(TlkJsonFile.Parse(
                    "{ \"language\": 0, \"entries\": [ { \"id\": 0, \"text\": \"repository fallback\" } ] }"));
                var reloads = 0;
                service.CustomTlkReloaded += () => reloads++;

                service.ReloadCustomTlk(tlkPath);
                service.GetString(TlkService.CustomTlkBase).Should().Be("selected from nwn.ini TLK folder");

                service.ReloadCustomTlk(null);
                service.GetString(TlkService.CustomTlkBase).Should().Be("repository fallback");
                reloads.Should().Be(2);
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        private static void WriteSingleEntryTlk(string path, string text)
        {
            var payload = Encoding.UTF8.GetBytes(text);
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
            writer.Write(Encoding.ASCII.GetBytes("TLK "));
            writer.Write(Encoding.ASCII.GetBytes("V3.0"));
            writer.Write(0u); // language
            writer.Write(1u); // entry count
            writer.Write(60u); // strings offset (20-byte header + one 40-byte entry)
            writer.Write(1u); // text present
            writer.Write(new byte[16]); // sound ResRef
            writer.Write(0u); // volume variance
            writer.Write(0u); // pitch variance
            writer.Write(0u); // relative text offset
            writer.Write((uint)payload.Length);
            writer.Write(0f); // sound length
            writer.Write(payload);
            File.WriteAllBytes(path, stream.ToArray());
        }
    }
}
