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
        var root = FindRoot();
        var resref = StructureType.ChairChancellor.GetAttribute<StructureType, StructureAttribute>().Resref;
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "utp", resref + ".utp.json")));
        var appearance = blueprint.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32();
        var appearances = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var model = appearances.GetValue(appearance, "ModelName");

        var useNode = ReadEngineUseNode(Path.Combine(root, "SWLOR_Haks", "sw_plc", model + ".pwk"));

        useNode.Should().NotBeNull("a sitter faces from the chair toward its use point and faces east without one");
        useNode!.Value.X.Should().BeApproximately(0f, 0.01f);
        useNode.Value.Y.Should().BeLessThan(-0.45f, "the seat opens toward model -Y, past its front edge");
    }

    // The engine only reads a use node whose "node dummy" line has "pwk_use01" at character 15 (a four-character prefix).
    private static (float X, float Y)? ReadEngineUseNode(string path)
    {
        var inUseNode = false;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.TrimStart(' ', '\t');
            if (line.StartsWith("node", StringComparison.Ordinal))
                inUseNode = line.Length >= 24 && string.CompareOrdinal(line, 4, " dummy ", 0, 7) == 0
                    && string.CompareOrdinal(line, 15, "pwk_use01", 0, 9) == 0;
            else if (inUseNode && line.StartsWith("position", StringComparison.Ordinal))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return (float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture));
            }
        }

        return null;
    }

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
