using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class EquipmentTypeTests
{
    private static readonly Dictionary<BaseItem, string> WearableNames = new()
    {
        [BaseItem.Helmet] = "helmets?|helms?|cowls?|hoods?|headwraps?|headbands?|circlets?|visors?|masks?|goggles?|caps?",
        [BaseItem.Cloak] = "cloaks?|shawls?|mantles?|capes?|shrouds?|ponchos?",
        [BaseItem.Gloves] = "gloves?|gauntlets?|cestus|handwraps?",
        [BaseItem.Bracer] = "bracers?|vambraces?|wristbands?|armbands?|wristguards?|cuffs?",
        [BaseItem.Boots] = "boots?|greaves|sabatons|shoes|leggings",
        [BaseItem.Belt] = "belts?|sashes?|girdles?",
        [BaseItem.Ring] = "rings?",
        [BaseItem.Amulet] = "amulets?|necklaces?|pendants?|talismans?|chokers?",
        [BaseItem.Armor] = "robes?|vests?|jackets?|cuirass|breastplates?|chestguards?|bodysuits?|tunics?",
    };

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module", "uti")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    [Test]
    public void WearableTypesMatchTheGarmentsNamedByTheirBlueprints()
    {
        var failures = new List<string>();
        var checkedItems = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root, "Module", "uti"), "*.uti.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var item = document.RootElement;
            var actual = (BaseItem)Value(item, "BaseItem").GetInt32();
            if (!WearableNames.ContainsKey(actual)) continue;
            var name = Value(item, "LocalizedName").TryGetProperty("0", out var text) ? text.GetString() ?? "" : "";
            // A barrel shroud is a weapon housing, rather than a cloth garment.
            if (Path.GetFileName(path) == "barrel_shroud.uti.json") continue;
            foreach (var (expected, nouns) in WearableNames)
            {
                if (!Regex.IsMatch(name, $@"\b({nouns})(?:\s+\d+)?$", RegexOptions.IgnoreCase)) continue;
                checkedItems++;
                if (actual != expected) failures.Add($"{Path.GetFileName(path)}: {name} is {actual}, expected {expected}");
            }
        }
        checkedItems.Should().BeGreaterThan(1000, "the audit must cover the entire named wearable catalog");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [TestCase("hexweave", BaseItem.Cloak)]
    [TestCase("brawl_wraps", BaseItem.Gloves)]
    [TestCase("deton_cestus", BaseItem.Gloves)]
    [TestCase("pil_nulaaglove", BaseItem.Gloves)]
    [TestCase("reclaim_gaunt", BaseItem.Gloves)]
    [TestCase("vitrupt_l3", BaseItem.Helmet)]
    [TestCase("scraplock_l3", BaseItem.Helmet)]
    public void ReportedAndDescriptionConfirmedEquipmentUsesItsActualType(string resref, BaseItem expected)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")));
        Value(document.RootElement, "BaseItem").GetInt32().Should().Be((int)expected);
    }

    [Test]
    public void LegacyConversionTargetsExactlyTheCorrectedBlueprintTypesAndModels()
    {
        EquipmentTypeMigration.Corrections.Should().HaveCount(54);
        foreach (var (resref, correction) in EquipmentTypeMigration.Corrections)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")));
            var item = document.RootElement;
            Value(item, "BaseItem").GetInt32().Should().Be((int)correction.Type, resref);
            Value(item, "ModelPart1").GetInt32().Should().Be(correction.Model, resref);
            Value(item, "xModelPart1").GetInt32().Should().Be(correction.Model, resref);
            EquipmentTypeMigration.IsLegacyEquipment(resref, correction.LegacyType).Should().BeTrue(resref);
            EquipmentTypeMigration.IsLegacyEquipment(resref, correction.Type).Should().BeFalse("conversion must be idempotent");
            if (correction.LegacyType != BaseItem.Armor)
                EquipmentTypeMigration.IsLegacyEquipment(resref, BaseItem.Armor).Should().BeFalse("unrelated customized types must remain untouched");
        }
        EquipmentTypeMigration.IsLegacyEquipment("ordinary_bracer", BaseItem.Bracer).Should().BeFalse();
        EquipmentTypeMigration.IsLegacyEquipment(null!, BaseItem.Bracer).Should().BeFalse();
        new _23_CorrectEquipmentTypes().Version.Should().BeGreaterThan(new _22_ReviseLootStats().Version);
    }

    [Test]
    public void EmbeddedCopiesOfCorrectedEquipmentMatchTheirBlueprintTypes()
    {
        var failures = new List<string>();
        foreach (var folder in new[] { "uti", "utc", "git", "utm" })
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root, "Module", folder), "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var node in Objects(document.RootElement))
            {
                if (!node.TryGetProperty("BaseItem", out var type) || !node.TryGetProperty("TemplateResRef", out var template)) continue;
                var resref = template.GetProperty("value").GetString() ?? "";
                if (EquipmentTypeMigration.Corrections.TryGetValue(resref, out var expected) && type.GetProperty("value").GetInt32() != (int)expected.Type)
                    failures.Add($"{Path.GetRelativePath(Root, path)}: {resref}");
            }
        }
        failures.Should().BeEmpty("embedded item data must not recreate the old types");
    }

    [Test]
    public void EveryWeaponWorkbookTypeMatchesItsBlueprintAndNativeCompatibility()
    {
        var nativeTypes = new Dictionary<string, BaseItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(Path.Combine(Root, "SWLOR_Haks", "sw_2da", "baseitems.2da")))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (cells.Length > 2 && int.TryParse(cells[0], out var row)) nativeTypes.TryAdd(cells[2], (BaseItem)row);
        }
        using var archive = ZipFile.OpenRead(Path.Combine(Root, "design", "bible", "SWLOR Design Bible.xlsx"));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XDocument Load(string name) { using var stream = archive.GetEntry(name)!.Open(); return XDocument.Load(stream); }
        var shared = Load("xl/sharedStrings.xml").Descendants(ns + "si").Select(x => string.Concat(x.Descendants(ns + "t").Select(t => t.Value))).ToArray();
        var sheet = Load("xl/workbook.xml").Descendants(ns + "sheet").Single(x => (string)x.Attribute("name")! == "Equipment - Weapons");
        var id = (string)sheet.Attribute(rel + "id")!;
        var target = (string)Load("xl/_rels/workbook.xml.rels").Root!.Elements().Single(x => (string)x.Attribute("Id")! == id).Attribute("Target")!;
        var worksheet = Load(target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target);
        string Text(XElement c) => c.Attribute("t")?.Value switch
        {
            "inlineStr" => string.Concat(c.Descendants(ns + "t").Select(x => x.Value)),
            "s" => shared[int.Parse(c.Element(ns + "v")!.Value)],
            _ => c.Element(ns + "v")?.Value ?? "",
        };
        var checkedItems = 0;
        foreach (var row in worksheet.Descendants(ns + "row").Skip(1))
        {
            var cells = row.Elements(ns + "c").ToDictionary(c => new string(c.Attribute("r")!.Value.TakeWhile(char.IsLetter).ToArray()), Text);
            if (!cells.TryGetValue("B", out var resref) || string.IsNullOrWhiteSpace(resref)) continue;
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")));
            var actual = (BaseItem)Value(document.RootElement, "BaseItem").GetInt32();
            nativeTypes.Should().ContainKey(cells["D"], resref);
            var declared = nativeTypes[cells["D"]];
            var expected = declared is BaseItem.Pistol or BaseItem.Sling or BaseItem.LegacyPistol
                ? PistolBaseItemCompatibility.GetCanonicalBaseItem(declared, resref) : declared;
            actual.Should().Be(expected, resref + " must match the declared base type after native pistol compatibility");
            checkedItems++;
        }
        checkedItems.Should().BeGreaterThan(500, "the audit must include the whole weapon sheet");
    }

    [Test]
    public void EveryArmorWorkbookSlotMatchesItsBlueprintType()
    {
        using var archive = ZipFile.OpenRead(Path.Combine(Root, "design", "bible", "SWLOR Design Bible.xlsx"));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XDocument Load(string name) { using var stream = archive.GetEntry(name)!.Open(); return XDocument.Load(stream); }
        var shared = Load("xl/sharedStrings.xml").Descendants(ns + "si").Select(x => string.Concat(x.Descendants(ns + "t").Select(t => t.Value))).ToArray();
        var sheet = Load("xl/workbook.xml").Descendants(ns + "sheet").Single(x => (string)x.Attribute("name")! == "Equipment - Armor");
        var id = (string)sheet.Attribute(rel + "id")!;
        var target = (string)Load("xl/_rels/workbook.xml.rels").Root!.Elements().Single(x => (string)x.Attribute("Id")! == id).Attribute("Target")!;
        var worksheet = Load(target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target);
        string Text(XElement c) => c.Attribute("t")?.Value switch
        {
            "inlineStr" => string.Concat(c.Descendants(ns + "t").Select(x => x.Value)),
            "s" => shared[int.Parse(c.Element(ns + "v")!.Value)],
            _ => c.Element(ns + "v")?.Value ?? "",
        };
        var slots = new Dictionary<string, BaseItem>
        {
            ["Body Armor"] = BaseItem.Armor, ["Helmet"] = BaseItem.Helmet, ["Gloves"] = BaseItem.Gloves,
            ["Bracer"] = BaseItem.Bracer, ["Boots"] = BaseItem.Boots, ["Belt"] = BaseItem.Belt,
            ["Cloak"] = BaseItem.Cloak, ["Ring"] = BaseItem.Ring, ["Amulet"] = BaseItem.Amulet, ["Necklace"] = BaseItem.Amulet,
        };
        var failures = new List<string>();
        var count = 0;
        foreach (var row in worksheet.Descendants(ns + "row").Skip(1))
        {
            var cells = row.Elements(ns + "c").ToDictionary(c => new string(c.Attribute("r")!.Value.TakeWhile(char.IsLetter).ToArray()), Text);
            if (!cells.TryGetValue("B", out var resref) || string.IsNullOrWhiteSpace(resref)) continue;
            cells.Should().ContainKey("C", resref);
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "uti", resref + ".uti.json")));
            var actual = (BaseItem)Value(document.RootElement, "BaseItem").GetInt32();
            var slot = cells["C"];
            var valid = slots.TryGetValue(slot, out var expected) ? actual == expected
                : slot == "Shield/Holdable" ? (int)actual == 210
                : slot == "Shield" && actual is BaseItem.SmallShield or BaseItem.LargeShield or BaseItem.TowerShield;
            if (!valid) failures.Add($"Row {row.Attribute("r")}: {resref} says {slot}, blueprint is {actual}");
            count++;
        }
        count.Should().BeGreaterThan(1700);
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static JsonElement Value(JsonElement item, string field) => item.GetProperty(field).GetProperty("value");
    private static IEnumerable<JsonElement> Objects(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            yield return node;
            foreach (var property in node.EnumerateObject())
            foreach (var child in Objects(property.Value)) yield return child;
        }
        else if (node.ValueKind == JsonValueKind.Array)
            foreach (var element in node.EnumerateArray())
            foreach (var child in Objects(element)) yield return child;
    }
}
