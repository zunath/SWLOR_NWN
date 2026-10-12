using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class SittablePlaceableWalkmeshTests
{
    [Test]
    public void ChancellorChair_SeatsSittersFacingTheSeatFront()
    {
        var useNode = ReadEngineUseNodes(StructureWalkmesh(FindRoot(), StructureType.ChairChancellor)).Should().ContainSingle(
            "a sitter faces from the chair toward its use point and faces east without one").Subject;

        useNode.X.Should().BeApproximately(0f, 0.01f);
        useNode.Y.Should().BeLessThan(-0.3f, "the seat opens toward model -Y, past its front edge");
    }

    [Test]
    public void Bench_SeatsSittersFacingTheSeatFront()
    {
        var useNode = ReadEngineUseNodes(StructureWalkmesh(FindRoot(), StructureType.Bench)).Should().ContainSingle().Subject;

        useNode.Y.Should().BeGreaterThan(0.3f, "the seat opens toward model +Y, past its front edge");
        // A use point exactly on model +Y matches the bench's own facing, and the engine then leaves the sitter as they arrived.
        useNode.X.Should().NotBe(0f);
        Math.Abs(useNode.X).Should().BeLessThan(0.05f);
    }

    [Test]
    public void SmallWoodBench2_SeatsSittersAcrossTheBench()
    {
        var useNode = ReadEngineUseNodes(StructureWalkmesh(FindRoot(), StructureType.BenchWoodSmall2)).Should().ContainSingle().Subject;

        useNode.X.Should().BeApproximately(0f, 0.01f);
        useNode.Y.Should().BeLessThan(-0.25f, "the bench runs along model X, so sitters face across it rather than along it");
    }

    private static string StructureWalkmesh(string root, StructureType type)
    {
        var resref = type.GetAttribute<StructureType, StructureAttribute>().Resref;
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "utp", resref + ".utp.json")));
        var appearance = blueprint.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32();
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var model = appearances.GetValue(appearance, "ModelName");

        return Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), "*.pwk", SearchOption.AllDirectories)
            .Should().ContainSingle(path => string.Equals(Path.GetFileNameWithoutExtension(path), model, StringComparison.OrdinalIgnoreCase),
                $"{type} uses model {model}, whose walkmesh must ship in the HAKs").Subject;
    }

    // The engine only reads a use node whose "node dummy" line has "pwk_use01"/"pwk_use02" at character 15 (a four-character prefix).
    private static List<(float X, float Y)> ReadEngineUseNodes(string path)
    {
        var nodes = new List<(float X, float Y)>();
        var inUseNode = false;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.TrimStart(' ', '\t');
            if (line.StartsWith("node", StringComparison.Ordinal))
                inUseNode = line.Length >= 24 && string.CompareOrdinal(line, 4, " dummy ", 0, 7) == 0
                    && string.CompareOrdinal(line, 15, "pwk_use0", 0, 8) == 0 && line[23] is '1' or '2';
            else if (inUseNode && line.StartsWith("position", StringComparison.Ordinal))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                nodes.Add((float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture)));
                inUseNode = false;
            }
        }

        return nodes;
    }

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
