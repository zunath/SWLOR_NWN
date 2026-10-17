using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Distribution;

namespace SWLOR.Toolset.Tests;

public class AssetManifestTests
{
    [TestCase("hak/unlisted.hak", true)]
    [TestCase("hak/unlisted.hak", false)]
    [TestCase("tlk/unlisted.tlk", true)]
    [TestCase("tlk/unlisted.tlk", false)]
    [TestCase("notes.txt", true)]
    [TestCase("notes.txt", false)]
    public void AnExtraFileInvalidatesAnOtherwiseVerifiedCache(string extra, bool verifyFiles)
    {
        var root = Directory.CreateTempSubdirectory("swlor-assets-").FullName;
        try
        {
            const string revision = "2222222222222222222222222222222222222222";
            var manifest = new AssetManifest { AssetRevision = revision };
            foreach (var relative in new[] { "hak/sw_2da.hak", "tlk/sw_tlk.tlk", "tlk/sw_tlk.tlk.json" })
            {
                var path = Path.Combine(root, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, relative);
                manifest.Files[relative] = ContentPaths.HashFile(path);
            }
            File.WriteAllText(Path.Combine(root, AssetManifest.FileName), JsonSerializer.Serialize(manifest));
            AssetManifest.Load(root, revision, verifyFiles).Should().NotBeNull();
            File.WriteAllText(Path.Combine(root, extra), "undeclared");
            Action load = () => AssetManifest.Load(root, revision, verifyFiles);
            load.Should().Throw<InvalidDataException>().WithMessage("*Undeclared asset file*" + extra + "*");
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
