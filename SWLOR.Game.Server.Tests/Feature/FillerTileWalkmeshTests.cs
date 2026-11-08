using System.Globalization;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

// A tile without a .wok counts as walkable floor in the engine (GetSurfaceMaterial returns 3 there), so a creature
// pushed out of furniture or a wall lands in the black void around the rooms and can walk around in it.
public class FillerTileWalkmeshTests
{
    [TestCase("sw_t_bunker", "fifi")]
    [TestCase("sw_t_duct", "nac01")]
    public void EveryTileModelShipsAWalkmesh(string hakFolder, string tileset)
    {
        var folder = Path.Combine(FindRoot(), "SWLOR_Haks", hakFolder);
        Directory.Exists(folder).Should().BeTrue($"the SWLOR_Haks submodule must be checked out with {hakFolder}");
        var set = File.ReadAllText(Path.Combine(folder, tileset + ".set"));
        var tileModels = Regex.Matches(set, @"(?m)^\s*Model=(\S+)").Select(m => m.Groups[1].Value).ToArray();
        tileModels.Should().HaveCount(Regex.Matches(set, @"(?m)^\s*\[TILE\d+\]").Count, "every [TILEn] section names one model");
        var walkmeshes = Directory.EnumerateFiles(folder, "*.wok").Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);

        tileModels.Distinct(StringComparer.OrdinalIgnoreCase).Where(m => !walkmeshes.Contains(m))
            .Should().BeEmpty($"every {tileset} tile model needs a .wok next to it in {hakFolder}");
    }

    [TestCase("sw_t_bunker", "fifi1_x01_01")]
    [TestCase("sw_t_bunker", "fifi2_x01_01")]
    [TestCase("sw_t_bunker", "fifi3_x01_01")]
    [TestCase("sw_t_duct", "nac01_x01_01")]
    public void BlackFillerTile_IsSolidNonWalkableFloor(string hakFolder, string model)
    {
        var root = FindRoot();
        var surfaces = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "surfacemat.2da"));
        var lines = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks", hakFolder, model + ".wok"))
            .Select(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Where(l => l.Length > 0).ToArray();
        var vi = Array.FindIndex(lines, l => l[0] == "verts");
        var fi = Array.FindIndex(lines, l => l[0] == "faces");
        vi.Should().BeGreaterThan(0);
        fi.Should().BeGreaterThan(vi);
        var vertices = lines.Skip(vi + 1).Take(int.Parse(lines[vi][1])).Select(l => (X: Parse(l[0]), Y: Parse(l[1]), Z: Parse(l[2]))).ToArray();
        var faces = lines.Skip(fi + 1).Take(int.Parse(lines[fi][1])).ToArray();
        vertices.Should().HaveCount(int.Parse(lines[vi][1]), "the vertex list must match its declared count");
        faces.Should().HaveCount(int.Parse(lines[fi][1]), "the face list must match its declared count");

        faces.Should().NotBeEmpty();
        faces.Select(f => surfaces.GetValue(int.Parse(f[^1]), "Walk")).Should().OnlyContain(walk => walk == "0",
            "nothing may stand on the black filler around the rooms");
        vertices.Should().OnlyContain(v => v.Z == 0f, "the filler blocks movement at floor level");
        // The faces must cover the whole 10 x 10 m tile, or the uncovered part acts as open floor again.
        var area = faces.Sum(f =>
        {
            var (a, b, c) = (vertices[int.Parse(f[0])], vertices[int.Parse(f[1])], vertices[int.Parse(f[2])]);
            return Math.Abs((b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y)) / 2;
        });
        area.Should().BeApproximately(100f, .001f);
        vertices.Min(v => v.X).Should().Be(-5f);
        vertices.Max(v => v.X).Should().Be(5f);
        vertices.Min(v => v.Y).Should().Be(-5f);
        vertices.Max(v => v.Y).Should().Be(5f);
    }

    private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
