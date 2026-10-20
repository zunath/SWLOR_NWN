using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests
{
    public class NwnIniProfileTests
    {
        [Test]
        public void Load_UsesCustomContentAliasesAndEnumeratesTheirFiles()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), "SWLOR.Toolset.Tests", Guid.NewGuid().ToString("N"));
            var iniDirectory = Path.Combine(tempRoot, "user");
            var hakDirectory = Path.Combine(tempRoot, "custom", "hak");
            var tlkDirectory = Path.Combine(tempRoot, "custom", "tlk");
            var movieDirectory = Path.Combine(tempRoot, "custom", "movies");
            var installDirectory = Path.Combine(tempRoot, "install");
            var builtInMovieDirectory = Path.Combine(installDirectory, "data", "mov");
            Directory.CreateDirectory(iniDirectory);
            Directory.CreateDirectory(hakDirectory);
            Directory.CreateDirectory(tlkDirectory);
            Directory.CreateDirectory(movieDirectory);
            Directory.CreateDirectory(builtInMovieDirectory);

            try
            {
                File.WriteAllText(Path.Combine(hakDirectory, "Second.HAK"), string.Empty);
                File.WriteAllText(Path.Combine(hakDirectory, "first.hak"), string.Empty);
                File.WriteAllText(Path.Combine(hakDirectory, "ignored.txt"), string.Empty);
                File.WriteAllText(Path.Combine(tlkDirectory, "sw_tlk.TLK"), string.Empty);
                File.WriteAllText(Path.Combine(movieDirectory, "custom_intro.BIK"), string.Empty);
                File.WriteAllText(Path.Combine(movieDirectory, "custom_outro.wbm"), string.Empty);
                File.WriteAllText(Path.Combine(movieDirectory, "ignored.mp4"), string.Empty);
                File.WriteAllText(Path.Combine(builtInMovieDirectory, "nwnintro.wbm"), string.Empty);

                var iniPath = Path.Combine(iniDirectory, "nwn.ini");
                File.WriteAllText(
                    iniPath,
                    $"[Display Options]\r\nHAK=ignored\r\n[Alias]\r\nHAK=\"{hakDirectory}\"\r\n" +
                    $"TLK={tlkDirectory}\r\nMOVIES={movieDirectory}\r\n");

                var profile = NwnIniProfile.Load(iniPath);

                profile.HakDirectory.Should().Be(Path.GetFullPath(hakDirectory));
                profile.TlkDirectory.Should().Be(Path.GetFullPath(tlkDirectory));
                profile.MovieDirectory.Should().Be(Path.GetFullPath(movieDirectory));
                profile.EnumerateHakNames().Should().Equal("first", "Second");
                profile.EnumerateTlkNames().Should().Equal("sw_tlk");
                profile.EnumerateMovieNames(installDirectory).Should()
                    .Equal("custom_intro", "custom_outro", "nwnintro");
                profile.FindHakPath("SECOND").Should().Be(Path.Combine(hakDirectory, "Second.HAK"));
                profile.FindTlkPath("SW_TLK").Should().Be(Path.Combine(tlkDirectory, "sw_tlk.TLK"));

                var resolution = profile.ResolveHakLayers(new[] { "SECOND", "missing", "first" });
                resolution.Layers.Select(layer => layer.Name).Should().Equal("SECOND", "first");
                resolution.Layers.Select(layer => layer.DirectoryPath).Should().Equal(
                    Path.Combine(hakDirectory, "Second.HAK"),
                    Path.Combine(hakDirectory, "first.hak"));
                resolution.MissingHakNames.Should().Equal("missing");
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
                File.WriteAllText(
                    iniPath,
                    "[Alias]\r\nHAK=custom\\hak\r\nTLK=custom\\tlk\r\nMOVIES=custom\\movies\r\n");

                var profile = NwnIniProfile.Load(iniPath);

                profile.HakDirectory.Should().Be(Path.Combine(tempRoot, "custom", "hak"));
                profile.TlkDirectory.Should().Be(Path.Combine(tempRoot, "custom", "tlk"));
                profile.MovieDirectory.Should().Be(Path.Combine(tempRoot, "custom", "movies"));
            }
            finally
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
