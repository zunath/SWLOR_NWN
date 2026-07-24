using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Self-enforcing guard for the economy exclusion of non-obtainable items: every item blueprint that
/// players cannot obtain through any source must be excluded from player-facing economy surfaces.
/// Items with a creature base type or an [NPC]/(NPC name are excluded automatically by the runtime
/// classifier; every other non-obtainable blueprint must carry the NO_ECONOMY flag. If a new item is
/// added without being obtainable or flagged, this test fails and names it — flag it with NO_ECONOMY
/// (see tools/FlagNpcEconomyItems.py) or make it obtainable through a real source.
/// </summary>
public class EconomyObtainabilityCoverageTests
{
    private static readonly HashSet<int> CreatureBaseItems = new() { 69, 70, 71, 72, 73 };

    [Test]
    public void NonObtainableItems_AreFlaggedNoEconomy()
    {
        var root = FindRepositoryRoot().FullName;
        var moduleUti = Path.Combine(root, "Module", "uti");

        var obtainable = ReadObtainableResrefs(root);

        var offenders = new List<string>();

        foreach (var utiPath in Directory.EnumerateFiles(moduleUti, "*.uti.json"))
        {
            var resref = Path.GetFileName(utiPath);
            resref = resref.Substring(0, resref.Length - ".uti.json".Length).ToLowerInvariant();

            if (obtainable.Contains(resref))
                continue;

            using var uti = JsonDocument.Parse(File.ReadAllText(utiPath));
            var rootElement = uti.RootElement;

            var baseItem = GetInt(rootElement, "BaseItem");
            var name = GetName(rootElement);

            // Already excluded by the runtime classifier - no flag needed.
            if (CreatureBaseItems.Contains(baseItem) || IsRestrictedName(name))
                continue;

            if (!HasNoEconomyFlag(rootElement))
                offenders.Add($"{resref} (base {baseItem}) '{name}'");
        }

        offenders.Should().BeEmpty(
            "every non-obtainable item must carry NO_ECONOMY (or be made obtainable). " +
            "Offenders:\n" + string.Join("\n", offenders.OrderBy(x => x)));
    }

    private static bool IsRestrictedName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var t = name.TrimStart();
        return t.StartsWith("[NPC]") || t.StartsWith("(NPC");
    }

    private static bool HasNoEconomyFlag(JsonElement uti)
    {
        if (!uti.TryGetProperty("VarTable", out var vt) ||
            !vt.TryGetProperty("value", out var list) ||
            list.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in list.EnumerateArray())
        {
            if (GetString(entry, "Name") == "NO_ECONOMY" &&
                GetInt(entry, "Type") == 1 &&
                GetInt(entry, "Value") == 1)
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> ReadObtainableResrefs(string root)
    {
        var obtainable = new HashSet<string>();
        void Add(string r)
        {
            if (!string.IsNullOrWhiteSpace(r)) obtainable.Add(r.ToLowerInvariant());
        }

        // NPC carried droppable items
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "Module", "utc"), "*.utc.json"))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("InventoryRes")) continue;
            using var doc = JsonDocument.Parse(text);
            if (!TryGetArray(doc.RootElement, "ItemList", out var items)) continue;
            foreach (var e in items.EnumerateArray())
                if (GetInt(e, "Dropable") == 1) Add(GetString(e, "InventoryRes"));
        }

        // Vendor stores (utm) + placed treasure containers (utp)
        foreach (var (dir, ext) in new[] { ("utm", "*.utm.json"), ("utp", "*.utp.json") })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "Module", dir), ext))
            {
                var text = File.ReadAllText(file);
                if (!text.Contains("InventoryRes")) continue;
                using var doc = JsonDocument.Parse(text);
                if (TryGetArray(doc.RootElement, "StoreList", out var stores))
                {
                    foreach (var st in stores.EnumerateArray())
                        if (TryGetArray(st, "ItemList", out var sItems))
                            foreach (var e in sItems.EnumerateArray()) Add(GetString(e, "InventoryRes"));
                }
                if (TryGetArray(doc.RootElement, "ItemList", out var items))
                    foreach (var e in items.EnumerateArray()) Add(GetString(e, "InventoryRes"));
            }
        }

        // C# literal item sources across the server.
        var literalPatterns = new[]
        {
            @"\.AddItem\(\s*""([^""]+)""",
            @"\.Resref\(\s*""([^""]+)""",
            @"\.Component\(\s*""([^""]+)""",
            @"CreateItemOnObject\(\s*""([^""]+)""",
            @"CopyItemAndModify\(\s*""([^""]+)""",
            @"new\s+ItemReward\(\s*""([^""]+)""",
            @"\.RewardItem\(\s*""([^""]+)""",
            @"\.AddItemReward\(\s*""([^""]+)""",
            @"RefinedItemResref\s*=\s*""([^""]+)""",
            @"new\s+TerminalItem\([^,]+,\s*""([^""]+)""",
            @"(?:Named|Schematic|Note|Tool)\(\s*SlicingSourceType\.[^,]+,\s*\d+,\s*""([^""]+)""",
        };
        var compiled = literalPatterns.Select(p => new Regex(p)).ToArray();

        // Files whose bare string literals are all item resrefs (attribute-decorated registries).
        var literalRegistries = new[]
        {
            "FishType.cs", "FishingRodType.cs", "FishingBaitType.cs",
            "SlicingCacheSmitheryRecipes.cs", "SlicingCacheCookingRecipes.cs", "TraceFuseRecipes.cs",
            "SlicingTerminalFurnitureRecipes.cs", "ConcentratedVenomRecipes.cs",
        };
        var serverDir = Path.Combine(root, "SWLOR.Game.Server");
        foreach (var file in Directory.EnumerateFiles(serverDir, "*.cs", SearchOption.AllDirectories))
        {
            // Skip nested worktree copies, but compare relative to the repository root so this
            // guard does not skip every file when the test itself runs from inside a worktree
            // (whose own path contains ".claude/worktrees").
            if (Path.GetRelativePath(root, file).Contains(Path.Combine(".claude", "worktrees"))) continue;
            var text = File.ReadAllText(file);
            foreach (var rx in compiled)
                foreach (Match m in rx.Matches(text))
                    Add(m.Groups[1].Value);

            if (literalRegistries.Any(reg => file.EndsWith(reg)))
                foreach (Match m in Regex.Matches(text, @"""([a-z0-9_]{2,16})"""))
                    Add(m.Groups[1].Value);
        }

        // Fixed resref constants that are granted directly.
        foreach (var r in new[] { "beast_dna", "beast_egg", "blueprint", "survival_knife",
                                  "fresh_bread", "dlarproto", "travelers_clothes",
                                  "ls_custom", "ss_custom" })
            Add(r);

        return obtainable;
    }

    private static bool TryGetArray(JsonElement element, string property, out JsonElement array)
    {
        array = default;
        return element.TryGetProperty(property, out var wrapper) &&
               wrapper.TryGetProperty("value", out array) &&
               array.ValueKind == JsonValueKind.Array;
    }

    private static string GetString(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var wrapper) &&
            wrapper.TryGetProperty("value", out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }
        return null;
    }

    private static int GetInt(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var wrapper) &&
            wrapper.TryGetProperty("value", out var value) &&
            value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }
        return -1;
    }

    private static string GetName(JsonElement uti)
    {
        if (uti.TryGetProperty("LocalizedName", out var ln) &&
            ln.TryGetProperty("value", out var value) &&
            value.TryGetProperty("0", out var first) &&
            first.ValueKind == JsonValueKind.String)
        {
            return first.GetString();
        }
        return null;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
