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

    [TestCase(StructureType.Bench)]
    [TestCase(StructureType.ChairDiningGrey)]
    [TestCase(StructureType.AshmarkWorkStool)]
    [TestCase(StructureType.CommandlineWorkStool)]
    public void SeatsOpeningTowardModelPlusY_SeatSittersFacingTheSeatFront(StructureType type)
    {
        AssertFacesModelPlusY(ReadEngineUseNodes(StructureWalkmesh(FindRoot(), type)).Should().ContainSingle().Subject);
    }

    [Test]
    public void Bench_IsApproachedFromItsFront()
    {
        var useNode = ReadEngineUseNodes(StructureWalkmesh(FindRoot(), StructureType.Bench)).Should().ContainSingle().Subject;

        useNode.Y.Should().BeGreaterThan(0.3f, "the seat opens toward model +Y, past its front edge");
    }

    [Test]
    public void DiningChair_KeepsItsUsePointInsideTheSeat()
    {
        var useNode = ReadEngineUseNodes(StructureWalkmesh(FindRoot(), StructureType.ChairDiningGrey)).Should().ContainSingle().Subject;

        // Mon Cala's dining chairs are tucked under their tables; a use point past the 0.25 m seat edge lands under the table,
        // and players walking up from the side or across the table then stall instead of sitting.
        useNode.Y.Should().BeLessThan(0.25f);
    }

    [TestCase("plc_chair2", 1)]
    [TestCase("udp-p-164", -1)]
    [TestCase("udp2-p-01", -1)]
    [TestCase("udp2-p-02", -1)]
    public void PlacedSeats_SeatSittersFacingTheSeatFront(string model, int seatFront)
    {
        var useNode = ReadEngineUseNodes(ModelWalkmesh(FindRoot(), model)).Should().ContainSingle(
            "a sitter faces from the chair toward its use point and faces east without one").Subject;

        if (seatFront > 0)
            AssertFacesModelPlusY(useNode);
        else
        {
            useNode.X.Should().BeApproximately(0f, 0.01f);
            useNode.Y.Should().BeLessThan(-0.3f, "the seat opens toward model -Y, past its front edge");
        }
    }

    private static void AssertFacesModelPlusY((float X, float Y) useNode)
    {
        useNode.Y.Should().BePositive("the seat opens toward model +Y");
        // A use point exactly on model +Y matches the seat's own facing, and the engine then leaves the sitter as they arrived.
        useNode.X.Should().NotBe(0f);
        Math.Abs(useNode.X).Should().BeLessThan(useNode.Y * 0.035f, "the sitter should face within 2 degrees of straight ahead");
    }

    [Test]
    public void SmallWoodBench2_SeatsSittersAcrossTheBench()
    {
        var useNode = ReadEngineUseNodes(StructureWalkmesh(FindRoot(), StructureType.BenchWoodSmall2)).Should().ContainSingle().Subject;

        useNode.X.Should().BeApproximately(0f, 0.01f);
        useNode.Y.Should().BeLessThan(-0.25f, "the bench runs along model X, so sitters face across it rather than along it");
    }

    [TestCase(StructureType.WardrobeCurvedWhite)]
    [TestCase(StructureType.WardrobeGreyLow)]
    public void Wardrobes_AreNotSeats(StructureType type)
    {
        var resref = type.GetAttribute<StructureType, StructureAttribute>().Resref;
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRoot(), "Module", "utp", resref + ".utp.json")));

        var onUsed = blueprint.RootElement.GetProperty("OnUsed").GetProperty("value").GetString();

        new[] { "x0_o2_use_chair", "x2_plc_used_sit", "zep_use_chair", "sit" }.Should().NotContain(onUsed);
    }

    [Test]
    public void CurvedWardrobe_BlocksWalkingThroughIt()
    {
        var path = StructureWalkmesh(FindRoot(), StructureType.WardrobeCurvedWhite);
        var verts = new List<(float X, float Y)>();
        var materials = new List<string>();
        var section = string.Empty;
        foreach (var raw in File.ReadLines(path))
        {
            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;
            if (parts[0] is "verts" or "faces" or "tverts" or "node" or "endnode") { section = parts[0]; continue; }
            if (section == "verts") verts.Add((float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture)));
            else if (section == "faces") materials.Add(parts[^1]);
        }

        materials.Should().NotBeEmpty().And.OnlyContain(material => material == "7", "every face of the cabinet's footprint is non-walkable");
        (verts.Max(v => v.X) - verts.Min(v => v.X)).Should().BeGreaterThan(2.0f, "the cabinet is 2.1 m wide");
        (verts.Max(v => v.Y) - verts.Min(v => v.Y)).Should().BeGreaterThan(0.75f, "the cabinet is 0.8 m deep");
    }

    private static string StructureWalkmesh(string root, StructureType type)
    {
        var resref = type.GetAttribute<StructureType, StructureAttribute>().Resref;
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "utp", resref + ".utp.json")));
        var appearance = blueprint.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32();
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var model = appearances.GetValue(appearance, "ModelName");

        return ModelWalkmesh(root, model!);
    }

    private static string ModelWalkmesh(string root, string model)
    {
        return Directory.EnumerateFiles(Path.Combine(root, "SWLOR_Haks"), "*.pwk", SearchOption.AllDirectories)
            .Should().ContainSingle(path => string.Equals(Path.GetFileNameWithoutExtension(path), model, StringComparison.OrdinalIgnoreCase),
                $"model {model}'s walkmesh must ship in the HAKs").Subject;
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
