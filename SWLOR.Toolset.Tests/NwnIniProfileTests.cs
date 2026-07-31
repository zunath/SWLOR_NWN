using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests
{
    public class NwnIniProfileTests
    {
        [Test]
        public void Load_UsesAliasHakAndTlkDirectoriesAndEnumeratesPackedFiles()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            var iniDirectory = Path.Combine(tempRoot, "user");
            var hakDirectory = Path.Combine(tempRoot, "custom", "hak");
            var tlkDirectory = Path.Combine(tempRoot, "custom", "tlk");
            Directory.CreateDirectory(iniDirectory);
            Directory.CreateDirectory(hakDirectory);
            Directory.CreateDirectory(tlkDirectory);

            try
            {
                File.WriteAllText(Path.Combine(hakDirectory, "Second.HAK"), string.Empty);
                File.WriteAllText(Path.Combine(hakDirectory, "first.hak"), string.Empty);
                File.WriteAllText(Path.Combine(hakDirectory, "ignored.txt"), string.Empty);
                File.WriteAllText(Path.Combine(tlkDirectory, "sw_tlk.TLK"), string.Empty);

                var iniPath = Path.Combine(iniDirectory, "nwn.ini");
                File.WriteAllText(
                    iniPath,
                    $"[Display Options]\r\nHAK=ignored\r\n[Alias]\r\nHAK=\"{hakDirectory}\"\r\nTLK={tlkDirectory}\r\n");

                var profile = NwnIniProfile.Load(iniPath);

                profile.HakDirectory.Should().Be(Path.GetFullPath(hakDirectory));
                profile.TlkDirectory.Should().Be(Path.GetFullPath(tlkDirectory));
                profile.EnumerateHakNames().Should().Equal("first", "Second");
                profile.EnumerateTlkNames().Should().Equal("sw_tlk");
                profile.FindHakPath("SECOND").Should().Be(Path.Combine(hakDirectory, "Second.HAK"));
                profile.FindTlkPath("SW_TLK").Should().Be(Path.Combine(tlkDirectory, "sw_tlk.TLK"));
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        [Test]
        public void Load_ResolvesRelativeAliasesFromTheIniDirectory()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);

            try
            {
                var iniPath = Path.Combine(tempRoot, "nwn.ini");
                File.WriteAllText(iniPath, "[Alias]\r\nHAK=custom\\hak\r\nTLK=custom\\tlk\r\n");

                var profile = NwnIniProfile.Load(iniPath);

                profile.HakDirectory.Should().Be(Path.Combine(tempRoot, "custom", "hak"));
                profile.TlkDirectory.Should().Be(Path.Combine(tempRoot, "custom", "tlk"));
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
