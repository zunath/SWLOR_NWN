using System.Collections;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class ItemAppearanceCatalogTests
{
    private sealed record ArmorSlot(string Name, string Field, Func<BaseArmorAppearanceDefinition, int[]> Catalog, bool AllowsNone = false);

    private static readonly ArmorSlot[] ArmorSlots =
    {
        new("Neck", "ArmorPart_Neck", a => a.Neck, AllowsNone: true),
        new("Torso", "ArmorPart_Torso", a => a.Torso),
        new("Belt", "ArmorPart_Belt", a => a.Belt),
        new("Pelvis", "ArmorPart_Pelvis", a => a.Pelvis),
        new("LShoulder", "ArmorPart_LShoul", a => a.Shoulder),
        new("RShoulder", "ArmorPart_RShoul", a => a.Shoulder),
        new("LBicep", "ArmorPart_LBicep", a => a.Bicep),
        new("RBicep", "ArmorPart_RBicep", a => a.Bicep),
        new("LForearm", "ArmorPart_LFArm", a => a.Forearm),
        new("RForearm", "ArmorPart_RFArm", a => a.Forearm),
        new("LHand", "ArmorPart_LHand", a => a.Hand),
        new("RHand", "ArmorPart_RHand", a => a.Hand),
        new("LThigh", "ArmorPart_LThigh", a => a.Thigh),
        new("RThigh", "ArmorPart_RThigh", a => a.Thigh),
        new("LShin", "ArmorPart_LShin", a => a.Shin),
        new("RShin", "ArmorPart_RShin", a => a.Shin),
        new("LFoot", "ArmorPart_LFoot", a => a.Foot),
        new("RFoot", "ArmorPart_RFoot", a => a.Foot),
        new("Robe", "ArmorPart_Robe", a => a.Robe),
    };

    private sealed record Catalogs(
        IReadOnlyList<BaseArmorAppearanceDefinition> Armor,
        IReadOnlyDictionary<BaseItem, WeaponAppearanceBaseDefinition> Weapons);

    [Test]
    public void PlayerItemBlueprints_UseOnlyAppearancesFromTheAppearanceEditorCatalog()
    {
        var catalogs = LoadCatalogs();
        var checkedCounts = new SortedDictionary<BaseItem, int>();
        var violations = new SortedDictionary<BaseItem, List<string>>();

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(FindRepositoryRoot().FullName, "Module", "uti"), "*.uti.json"))
            Audit(JObject.Parse(File.ReadAllText(file)), Path.GetFileName(file).Replace(".uti.json", ""),
                catalogs, checkedCounts, violations);

        foreach (var required in new[] { BaseItem.Armor, BaseItem.Helmet, BaseItem.Cloak, BaseItem.ShortSpear, BaseItem.Longsword })
            checkedCounts.Should().ContainKey(required, "blueprints of every audited kind must be discovered");

        FailOnViolations("blueprints", checkedCounts, violations);
    }

    [Test]
    public void PlayerItemsPlacedInAreasAndStores_UseOnlyAppearancesFromTheAppearanceEditorCatalog()
    {
        var catalogs = LoadCatalogs();
        var moduleRoot = Path.Combine(FindRepositoryRoot().FullName, "Module");
        var checkedCounts = new SortedDictionary<BaseItem, int>();
        var violations = new SortedDictionary<BaseItem, List<string>>();

        foreach (var file in Directory.EnumerateFiles(moduleRoot, "*.json", SearchOption.AllDirectories))
        {
            var folder = Path.GetFileName(Path.GetDirectoryName(file));
            if (folder is "uti" or "itp" or "utc")
                continue;

            var text = File.ReadAllText(file);
            if (!text.Contains("\"TemplateResRef\"", StringComparison.Ordinal) ||
                !text.Contains("\"BaseItem\"", StringComparison.Ordinal) ||
                !text.Contains("ModelPart1", StringComparison.Ordinal) && !text.Contains("ArmorPart_Torso", StringComparison.Ordinal))
                continue;

            foreach (var item in FindEmbeddedItems(JToken.Parse(text)))
                Audit(item, $"{folder}/{Path.GetFileName(file)}: {item["TemplateResRef"]?["value"]}", catalogs, checkedCounts, violations);
        }

        checkedCounts.Should().NotBeEmpty("area and store item copies must be discovered");
        FailOnViolations("embedded item copies", checkedCounts, violations);
    }

    // Creature equipment and inventory are NPC gear, not items players are given.
    private static IEnumerable<JObject> FindEmbeddedItems(JToken token)
    {
        if (token is JObject container)
        {
            if (container.ContainsKey("TemplateResRef") && container.ContainsKey("BaseItem"))
                yield return container;

            foreach (var property in container.Properties().Where(x => x.Name != "Creature List"))
                foreach (var nested in FindEmbeddedItems(property.Value))
                    yield return nested;
        }
        else if (token is JArray array)
        {
            foreach (var nested in array.SelectMany(FindEmbeddedItems))
                yield return nested;
        }
    }

    private static void Audit(
        JObject item,
        string label,
        Catalogs catalogs,
        SortedDictionary<BaseItem, int> checkedCounts,
        SortedDictionary<BaseItem, List<string>> violations)
    {
        var baseItem = (BaseItem)(item["BaseItem"]?["value"]?.Value<int>() ?? -1);
        if (!IsCoveredByEditor(baseItem, catalogs.Weapons) || IsExcluded(item, baseItem, out var name))
            return;

        var problems = baseItem switch
        {
            BaseItem.Armor => FindArmorProblems(item, catalogs.Armor),
            BaseItem.Helmet => FindModelProblems(item, catalogs.Armor, a => a.Helmet),
            BaseItem.Cloak => FindModelProblems(item, catalogs.Armor, a => a.Cloak),
            _ => FindWeaponProblems(item, catalogs.Weapons[baseItem])
        };

        checkedCounts[baseItem] = checkedCounts.GetValueOrDefault(baseItem) + 1;
        if (problems.Count == 0)
            return;

        if (!violations.TryGetValue(baseItem, out var list))
            violations[baseItem] = list = new List<string>();
        list.Add($"{label} ({name}): {string.Join(", ", problems)}");
    }

    private static void FailOnViolations(
        string subject,
        SortedDictionary<BaseItem, int> checkedCounts,
        SortedDictionary<BaseItem, List<string>> violations)
    {
        if (violations.Count == 0)
            return;

        var report = string.Join("\n", violations.Select(group =>
            $"{group.Key}: {group.Value.Count} of {checkedCounts[group.Key]} {subject}\n  " +
            string.Join("\n  ", group.Value)));
        Assert.Fail("Player items may only use appearances selectable in the appearance editor:\n" + report);
    }

    private static bool IsCoveredByEditor(BaseItem baseItem, IReadOnlyDictionary<BaseItem, WeaponAppearanceBaseDefinition> weapons) =>
        baseItem is BaseItem.Armor or BaseItem.Helmet or BaseItem.Cloak || weapons.ContainsKey(baseItem);

    private static bool IsExcluded(JObject blueprint, BaseItem baseItem, out string name)
    {
        var noEconomy = (blueprint["VarTable"]?["value"] as JArray)?.Any(variable =>
            variable["Name"]?["value"]?.Value<string>() == Item.NoEconomyVariable &&
            variable["Type"]?["value"]?.Value<int>() == 1 &&
            variable["Value"]?["value"]?.Value<int>() == 1) == true;
        name = blueprint["LocalizedName"]?["value"]?["0"]?.Value<string>() ?? string.Empty;

        // The editor refuses plot and cursed items, so they are never customized by players.
        return Item.IsEconomyRestricted(baseItem, name, noEconomy, hasInventoryIcon: true) ||
               blueprint["Plot"]?["value"]?.Value<int>() == 1 ||
               blueprint["Cursed"]?["value"]?.Value<int>() == 1;
    }

    private static List<string> FindArmorProblems(JObject blueprint, IReadOnlyList<BaseArmorAppearanceDefinition> catalogs)
    {
        List<string> fewest = null;
        foreach (var catalog in catalogs)
        {
            var problems = new List<string>();
            foreach (var slot in ArmorSlots)
            {
                var part = ReadPart(blueprint, slot.Field);
                if (!slot.Catalog(catalog).Contains(part) && !(slot.AllowsNone && part == 0))
                    problems.Add($"{slot.Name} {part}");
            }

            if (problems.Count == 0)
                return problems;
            if (fewest == null || problems.Count < fewest.Count)
                fewest = problems;
        }

        return fewest;
    }

    private static List<string> FindModelProblems(
        JObject blueprint,
        IReadOnlyList<BaseArmorAppearanceDefinition> catalogs,
        Func<BaseArmorAppearanceDefinition, int[]> select)
    {
        var model = ReadPart(blueprint, "ModelPart1");
        return catalogs.Any(catalog => select(catalog).Contains(model))
            ? new List<string>()
            : new List<string> { $"Model {model}" };
    }

    private static List<string> FindWeaponProblems(JObject blueprint, WeaponAppearanceBaseDefinition appearance)
    {
        var problems = new List<string>();
        if (appearance.IsSimple)
        {
            var model = ReadPart(blueprint, "ModelPart1");
            if (!appearance.SimpleParts.Contains(model))
                problems.Add($"Model {model}");
            return problems;
        }

        // ModelPart1 is the bottom piece, ModelPart2 the middle, ModelPart3 the top.
        var parts = new[]
        {
            ("Bottom", "ModelPart1", appearance.BottomParts),
            ("Middle", "ModelPart2", appearance.MiddleParts),
            ("Top", "ModelPart3", appearance.TopParts)
        };
        foreach (var (partName, field, catalog) in parts)
        {
            // UTI weapon parts store model * 10 + color; the editor uses color * 100 + model.
            var stored = ReadPart(blueprint, field);
            var catalogId = stored / 10 + stored % 10 * 100;
            if (!catalog.Contains(catalogId))
                problems.Add($"{partName} {catalogId}");
        }

        return problems;
    }

    // The word-sized "x" twin is authoritative when present; GFF labels cap at 16 characters, so long names are clipped.
    private static int ReadPart(JObject blueprint, string field) =>
        blueprint["x" + (field.Length > 15 ? field[..15] : field)]?["value"]?.Value<int>() ??
        blueprint[field]?["value"]?.Value<int>() ?? 0;

    private static Catalogs LoadCatalogs() => new(LoadArmorCatalogs(), LoadWeaponCatalogs());

    private static IReadOnlyList<BaseArmorAppearanceDefinition> LoadArmorCatalogs()
    {
        var editor = typeof(AppearanceEditorViewModel);
        editor.GetMethod("LoadArmorAppearances", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
        var loaded = (IDictionary)editor.GetField("_armorAppearances", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        return loaded.Values.Cast<BaseArmorAppearanceDefinition>()
            .GroupBy(definition => definition.GetType())
            .Select(group => group.First())
            .ToList();
    }

    private static IReadOnlyDictionary<BaseItem, WeaponAppearanceBaseDefinition> LoadWeaponCatalogs()
    {
        var editor = typeof(AppearanceEditorViewModel);
        editor.GetMethod("LoadWeaponAppearances", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null);
        var loaded = (IDictionary)editor.GetField("_weaponAppearances", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        return loaded.Keys.Cast<BaseItem>().ToDictionary(key => key, key => (WeaponAppearanceBaseDefinition)loaded[key]!);
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
