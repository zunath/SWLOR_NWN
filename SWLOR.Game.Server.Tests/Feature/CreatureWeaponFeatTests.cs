using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class CreatureWeaponFeatTests
{
    private const int RightHandSlotStructId = 16;
    private const int LeftHandSlotStructId = 32;

    /// <summary>
    /// Every creature blueprint that holds a weapon has at least one feat baseitems.2da requires for it.
    /// </summary>
    [Test]
    public void CreatureBlueprints_HaveAFeatEachHeldWeaponRequires()
    {
        // The engine moves a blueprint's held weapon into inventory at spawn when the creature has
        // none of the feats baseitems.2da requires for it, so the creature fights unarmed.
        var root = FindRepositoryRoot();
        var requiredFeats = ReadWeaponRequiredFeats(root);
        var unarmed = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utc"), "*.utc.json"))
        {
            using var utc = JsonDocument.Parse(File.ReadAllText(file));
            if (!utc.RootElement.TryGetProperty("Equip_ItemList", out var equipped))
                continue;

            var feats = GetCreatureFeats(utc.RootElement);
            foreach (var entry in equipped.GetProperty("value").EnumerateArray())
            {
                var slot = entry.GetProperty("__struct_id").GetInt32();
                if (slot != RightHandSlotStructId && slot != LeftHandSlotStructId)
                    continue;

                var resref = entry.GetProperty("EquippedRes").GetProperty("value").GetString();
                var itemPath = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
                if (!File.Exists(itemPath))
                    continue;

                using var uti = JsonDocument.Parse(File.ReadAllText(itemPath));
                var baseItem = uti.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32();
                if (requiredFeats.TryGetValue(baseItem, out var required) && !required.Overlaps(feats))
                    unarmed.Add($"{Path.GetFileName(file)} holds {resref} but has none of feats {string.Join(",", required)}");
            }
        }

        unarmed.Should().BeEmpty();
    }

    /// <summary>Feat IDs in a creature blueprint's FeatList, or an empty set when it has none.</summary>
    private static HashSet<int> GetCreatureFeats(JsonElement utc) =>
        utc.TryGetProperty("FeatList", out var feats)
            ? feats.GetProperty("value").EnumerateArray().Select(entry => entry.GetProperty("Feat").GetProperty("value").GetInt32()).ToHashSet()
            : new HashSet<int>();

    /// <summary>
    /// Maps each baseitems.2da row with at least one ReqFeat column set to those feat IDs. Throws when the header
    /// lacks a ReqFeat column, so a changed file cannot make the guard pass without checking anything.
    /// </summary>
    private static Dictionary<int, HashSet<int>> ReadWeaponRequiredFeats(DirectoryInfo root)
    {
        var lines = File.ReadAllLines(Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da", "baseitems.2da"));
        var header = lines.First(line => line.Contains("ReqFeat0")).Split((char[])null!, System.StringSplitOptions.RemoveEmptyEntries);
        var columns = Enumerable.Range(0, 5).Select(i => System.Array.IndexOf(header, $"ReqFeat{i}") + 1).ToArray();
        if (columns.Any(column => column == 0))
            throw new InvalidDataException("baseitems.2da is missing one or more ReqFeat columns.");
        var result = new Dictionary<int, HashSet<int>>();

        foreach (var line in lines)
        {
            var cells = System.Text.RegularExpressions.Regex.Matches(line, "\"[^\"]*\"|\\S+").Select(m => m.Value).ToArray();
            if (cells.Length <= columns.Max() || !int.TryParse(cells[0], out var row))
                continue;

            var feats = columns.Select(column => int.TryParse(cells[column], out var feat) ? feat : -1).Where(feat => feat >= 0).ToHashSet();
            if (feats.Count > 0)
                result[row] = feats;
        }

        if (result.Count == 0)
            throw new InvalidDataException("baseitems.2da lists no feat requirements.");

        return result;
    }

    /// <summary>The repository root, found by walking up from the test directory to the solution file.</summary>
    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
