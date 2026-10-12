using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlaceableWalkmeshUseNodeTests
{
    private static readonly Regex UseNodeName = new(@"use_?\d", RegexOptions.IgnoreCase);

    [Test]
    public void EveryWalkmeshUseNode_IsNamedSoTheEngineReadsIt()
    {
        var walkmeshes = PlaceableWalkmeshes(FindRoot()).ToList();
        walkmeshes.Should().HaveCountGreaterThan(1000, "the SWLOR_Haks submodule must be checked out");

        var ignored = new List<string>();
        foreach (var path in walkmeshes)
        {
            var number = 0;
            foreach (var raw in File.ReadLines(path))
            {
                number++;
                var line = raw.TrimStart(' ', '\t');
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && parts[0] == "node" && parts[1] == "dummy" && UseNodeName.IsMatch(parts[2]) && !IsEngineUseNode(line))
                    ignored.Add($"{Path.GetFileName(Path.GetDirectoryName(path))}/{Path.GetFileName(path)}:{number} {parts[2]}");
            }
        }

        ignored.Should().BeEmpty("the engine silently ignores a use node unless its name is a four-character prefix plus pwk_use01/pwk_use02: {0}",
            string.Join(", ", ignored));
    }

    [TestCase(StructureType.ChairLargeGreyRed)]
    [TestCase(StructureType.ChaiseLoungeRed)]
    public void SeatStructures_SeatSittersFacingTheSeatFront(StructureType type)
    {
        var useNode = ReadEngineUseNodes(StructureWalkmesh(FindRoot(), type)).Should().ContainSingle().Subject;

        useNode.X.Should().BeApproximately(0f, 0.01f);
        useNode.Y.Should().BeLessThan(-0.6f, "a sitter faces from the seat toward its use node, and the seat opens toward model -Y");
    }

    [Test]
    public void DroidAssemblyTerminal_IsApproachedFromItsPanelSide()
    {
        var useNode = ReadEngineUseNodes(StructureWalkmesh(FindRoot(), StructureType.DroidAssemblyTerminal)).Should().ContainSingle().Subject;

        useNode.X.Should().BeApproximately(0f, 0.01f);
        useNode.Y.Should().BeGreaterThan(0.25f, "the control panel faces model +Y, past the terminal's footprint");
    }

    // The engine matches these tokens at fixed offsets, so the name needs exactly a four-character prefix,
    // and it only turns slots 01 and 02 into action points.
    private static bool IsEngineUseNode(string line)
    {
        return line.StartsWith("node dummy ", StringComparison.Ordinal)
            && (HasToken(line, 15, "pwk_use", out var slot) || HasToken(line, 15, "pwk_dp_use_", out slot))
            && slot is "01" or "02";
    }

    private static bool HasToken(string line, int offset, string token, out string slot)
    {
        var end = offset + token.Length;
        slot = line.Length >= end + 2 ? line.Substring(end, 2) : string.Empty;
        return slot.Length == 2 && string.CompareOrdinal(line, offset, token, 0, token.Length) == 0;
    }

    // The engine skips a node left at the model origin.
    private static List<(float X, float Y)> ReadEngineUseNodes(string path)
    {
        var nodes = new List<(float X, float Y)>();
        var inUseNode = false;
        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.TrimStart(' ', '\t');
            if (line.StartsWith("node", StringComparison.Ordinal))
                inUseNode = IsEngineUseNode(line);
            else if (inUseNode && line.StartsWith("position", StringComparison.Ordinal))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var position = parts.Skip(1).Take(3).Select(p => float.Parse(p, CultureInfo.InvariantCulture)).ToArray();
                if (position.Any(p => p != 0f))
                    nodes.Add((position[0], position[1]));
                inUseNode = false;
            }
        }

        return nodes;
    }

    private static string StructureWalkmesh(string root, StructureType type)
    {
        var resref = type.GetAttribute<StructureType, StructureAttribute>().Resref;
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "utp", resref + ".utp.json")));
        var appearance = blueprint.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32();
        var model = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da")).GetValue(appearance, "ModelName");
        return PlaceableWalkmeshes(root).Single(path => Path.GetFileNameWithoutExtension(path).Equals(model, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> PlaceableWalkmeshes(string root) =>
        Directory.EnumerateDirectories(Path.Combine(root, "SWLOR_Haks")).SelectMany(dir => Directory.EnumerateFiles(dir, "*.pwk"));

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
