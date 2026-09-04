using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class HakReorganizationReviewTests
{
    [Test]
    public void EveryMappedTileHakIsRequired()
    {
        var source = ReadSource("tools", "reorganize_hak_sources.py");

        source.Should().Contain("*TILE_HAKS.values(),");
        source.Should().Contain("missing_tiles = sorted(set(TILE_HAKS) - available_tiles)");
        source.Should().NotContain(
            "if (HAK_ROOT / hak).is_dir()",
            "missing mapped tile HAK directories must not be silently omitted");
    }

    private static string ReadSource(params string[] pathParts)
    {
        var repositoryRoot = FindRepositoryRoot();
        var fullPath = Path.Combine(new[] { repositoryRoot.FullName }.Concat(pathParts).ToArray());
        return File.ReadAllText(fullPath);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
