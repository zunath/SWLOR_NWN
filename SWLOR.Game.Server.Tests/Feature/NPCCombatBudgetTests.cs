using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class NPCCombatBudgetTests
{
    private const int SkinSlot = 131072;
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    [TestCase(0, 138)]
    [TestCase(4, 138)]
    [TestCase(20, 138)]
    [TestCase(-4, 138)]
    public void NpcEvasionBudget_ExcludesNativeArmor(int nativeArmor, int expected)
    {
        Stat.GetEvasion(46, 30, 8, nativeArmor, true).Should().Be(expected);
        Stat.GetEvasion(46, 30, 18, nativeArmor, true).Should().Be(expected + 10,
            "stat-driven Evasion effects still apply to NPCs");
    }

    [TestCase(4, 158)]
    [TestCase(-4, 118)]
    public void PlayerAndUnskinnedEvasion_PreservesNativeArmor(int nativeArmor, int expected)
    {
        Stat.GetEvasion(46, 30, 8, nativeArmor, false).Should().Be(expected);
    }

    [Test]
    public void WorldNpcCombatSources_MatchEveryBibleBudgetAndExcludeLegacyArmor()
    {
        var root = FindRoot();
        var failures = new List<string>();
        var rows = ReadRows(root, "World NPCs");
        var delaySources = ReadRows(root, "World NPC Weapon Delays").ToDictionary(r => r["A"], r => Number(r["D"]));
        var scoreColumns = new[] { ("I", "Str"), ("J", "Dex"), ("K", "Wis"), ("L", "Con"), ("M", "Int") };
        var skinColumns = new[]
        {
            ("D", 99, -1), ("N", 96, -1), ("O", 92, -1), ("P", 91, -1),
            ("R", 111, -1), ("S", 112, -1), ("T", 117, -1), ("U", 94, 1), ("V", 94, 2),
            ("W", 133, 1), ("X", 133, 2), ("Y", 133, 3), ("Z", 133, 4),
            ("AA", 133, 100), ("AB", 133, 101), ("AC", 133, 102), ("AD", 133, 103),
        };

        foreach (var row in rows)
        {
            var resref = row["C"];
            using var utc = ReadAsset(root, "utc", resref);
            var creature = utc.RootElement;
            var equipped = Equipment(creature);
            using var skin = ReadAsset(root, "uti", equipped[SkinSlot]);
            Compare("NaturalAC", 0, Int(creature, "NaturalAC"));
            if (row["G"] == "Droid")
                Compare("Droid Trauma immunity", 100, Property(skin.RootElement, 133, 102));
            foreach (var (column, field) in scoreColumns)
                Compare(field, Number(row[column]), Int(creature, field));
            foreach (var (column, property, subtype) in skinColumns)
            {
                var actual = Property(skin.RootElement, property, subtype);
                if (property == 133 && actual > 100)
                    actual = -(actual - 100);
                Compare(column, Number(row[column]), actual);
            }

            var weaponDamage = 0;
            var naturalWeapons = new HashSet<string>();
            var delays = new List<int>();
            foreach (var (slot, itemResref) in equipped)
            {
                var path = Path.Combine(root, "Module", "uti", $"{itemResref}.uti.json");
                if (!File.Exists(path))
                    continue; // Retired cosmetic clothing on ambient/event templates.
                using var item = ReadAsset(root, "uti", itemResref);
                Compare($"{itemResref} native AC Bonus", 0, Property(item.RootElement, 1));
                if (Int(item.RootElement, "BaseItem") == 16 && IsNpcOnly(item.RootElement))
                    foreach (var kind in new[] { 20, 22, 23 })
                        Compare($"{itemResref} native mitigation {kind}", 0, Property(item.RootElement, kind));
                if (slot is not (16 or 32 or 16384 or 32768 or 65536) ||
                    Int(item.RootElement, "BaseItem") is 14 or 56 or 57)
                    continue;
                if (slot >= 16384 && !naturalWeapons.Add(itemResref))
                    continue;
                weaponDamage += Property(item.RootElement, 93);
                delays.Add(Property(item.RootElement, 98) * 10);
            }
            // An odd budget split between two identical hand weapons rounds up each half.
            var rounding = equipped.TryGetValue(16, out var right) &&
                           equipped.TryGetValue(32, out var left) && right == left &&
                           Number(row["Q"]) % 2 == 1 ? 1 : 0;
            Compare("weapon DMG", Number(row["Q"]) + rounding, weaponDamage);
            if (delays.Count > 0)
            {
                Compare("weapon Delay", Number(row["AN"]), delays.Max());
                if (!delaySources.TryGetValue(resref, out var sourceDelay))
                    failures.Add($"{resref}: missing equipped weapon delay source; builder would fall back to role cadence");
                else
                    Compare("weapon Delay lookup", delays.Max(), sourceDelay);
            }

            void Compare(string label, int expected, int actual)
            {
                if (expected != actual)
                    failures.Add($"{resref} {label}: expected {expected}, actual {actual}");
            }
        }
        rows.Count.Should().BeGreaterThan(450);
        failures.Should().BeEmpty("every runtime source must respect its authored combat budget");
    }

    [Test]
    public void LootBossModifier_IsAvailableInBuilderDropdownsAndFilter()
    {
        using var archive = ZipFile.OpenRead(Path.Combine(FindRoot(), "design", "bible", "SWLOR Design Bible - Combat Upgrade.xlsx"));
        var dropdowns = 0;
        foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/") && e.FullName.EndsWith(".xml")))
        {
            using var stream = entry.Open();
            var doc = XDocument.Load(stream);
            foreach (var validation in doc.Descendants(Ns + "dataValidation"))
            {
                var choices = validation.Element(Ns + "formula1")?.Value;
                if (choices?.Contains("Glass Cannon") != true)
                    continue;
                choices.Should().Contain("Loot Boss", "builders must be able to select the reviewed group encounter budget");
                dropdowns++;
            }
        }
        dropdowns.Should().Be(2);
        using var bookStream = archive.GetEntry("xl/workbook.xml")!.Open();
        XDocument.Load(bookStream).Descendants(Ns + "definedName")
            .Should().Contain(n => n.Value == "'Enemy Modifiers'!$A$1:$M$9");
    }

    [Test]
    public void PremiumLootBosses_UseGroupBudgetsAndReliableAtLevelHitRates()
    {
        var root = FindRoot();
        var rewardTables = new HashSet<string>
        {
            "DATHOMIR_CHIRODACTYL_RECIPES", "DANTOOINE_KINRATH_QUEEN_RECIPES",
            "DANTOOINE_BOL_BOSS_RECIPES", "QIONHIVE_BROODMOTHER_RECIPE",
            "TATOOINE_ANCIENT_WORM_STOLEN_GOODS", "FROG_BOSS_RECIPE",
            "KORRIBAN_MASTER_RECIPE", "KORRIBAN_MASTER_RESOURCE",
        };
        var presets = ReadRows(root, "Enemy Stat Presets").ToDictionary(r => r["A"]);
        var count = 0;
        foreach (var row in ReadRows(root, "World NPCs").Where(r => r["E"] == "Boss"))
        {
            using var utc = ReadAsset(root, "utc", row["C"]);
            var hasPremiumRewards = Variables(utc.RootElement)
                .Where(v => v.GetProperty("Name").GetProperty("value").GetString()!.StartsWith("LOOT"))
                .Any(v => v.GetProperty("Value").GetProperty("value").ValueKind == JsonValueKind.String &&
                          rewardTables.Contains(v.GetProperty("Value").GetProperty("value").GetString()!.Split(',')[0]));
            if (!hasPremiumRewards)
                continue;
            row["H"].Should().Be("Loot Boss", row["C"] + " carries premium rewards");
            var level = Number(row["D"]);
            var preset = presets[$"{level}|Boss|{row["F"]}"];
            Number(row["N"]).Should().Be(Number(preset["K"]) * 3);
            Number(row["Q"]).Should().Be((int)Math.Round(Number(preset["N"]) * 1.5m, MidpointRounding.AwayFromZero));
            Number(row["R"]).Should().Be(Number(preset["O"]) + 15);
            Number(row["S"]).Should().Be(Number(preset["P"]) + 15);
            Number(row["U"]).Should().Be(Number(preset["R"]) + 20);
            Number(row["V"]).Should().Be(Number(preset["S"]) + 20);
            var elite = presets[$"{level}|Elite|{row["F"]}"];
            Number(row["N"]).Should().BeGreaterThan(Number(elite["K"]) * 6);
            if (level <= 50)
            {
                var accuracy = Stat.GetAccuracy(level, 20 + level * 2 / 5, 0);
                var evasion = Stat.GetEvasion(level, Number(row["M"]), Number(row["T"]));
                Combat.CalculateHitRate(accuracy, evasion, 0).Should().BeInRange(75, 85);
            }
            count++;
        }
        count.Should().Be(13, "all existing premium loot families must receive the group encounter budget");
    }

    [Test]
    public void OrdinaryEnemiesAndPresets_MaintainAtLeast75PercentUnbuffedReferenceHitRate()
    {
        var root = FindRoot();
        var failures = new List<string>();
        var count = 0;
        foreach (var row in ReadRows(root, "Enemy Stat Presets").Where(r => r["C"] == "Normal"))
        {
            var level = Number(row["B"]);
            if (level > 50)
                continue; // Rank-50 players are not at-level against ranks 51-100.
            Check(row["A"], level, Number(row["J"]), Number(row["Q"]));
        }
        foreach (var row in ReadRows(root, "World NPCs").Where(r => r["E"] == "Normal"))
        {
            var level = Number(row["D"]);
            if (level > 50)
                continue;
            using var utc = ReadAsset(root, "utc", row["C"]);
            using var skin = ReadAsset(root, "uti", Equipment(utc.RootElement)[SkinSlot]);
            Check(row["C"], level, Int(utc.RootElement, "Int"),
                Property(skin.RootElement, 117) + Int(utc.RootElement, "NaturalAC") * 5);
        }

        void Check(string label, int level, int agility, int bonus)
        {
            var accuracy = Stat.GetAccuracy(level, 20 + level * 2 / 5, 0);
            var evasion = Stat.GetEvasion(level, agility, bonus);
            var hitRate = Combat.CalculateHitRate(accuracy, evasion, 0);
            if (hitRate < 75 || hitRate > 85)
                failures.Add($"{label}: {hitRate}% ({accuracy} Accuracy vs {evasion} Evasion)");
            count++;
        }
        count.Should().BeGreaterThan(450);
        failures.Should().BeEmpty("ordinary combat should be reliable without temporary buffs or rare gear");
    }

    [Test]
    public void PlacedWorldNpcs_UseReviewedScoresAndNativeAdjustedHp()
    {
        var root = FindRoot();
        var worldRefs = ReadRows(root, "World NPCs").Select(r => r["C"]).ToHashSet();
        var failures = new List<string>();
        var count = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "Module", "git"), "*.git.json"))
        {
            using var area = JsonDocument.Parse(File.ReadAllText(path));
            if (!area.RootElement.TryGetProperty("Creature List", out var list))
                continue;
            foreach (var creature in list.GetProperty("value").EnumerateArray())
            {
                var resref = creature.GetProperty("TemplateResRef").GetProperty("value").GetString()!;
                if (!worldRefs.Contains(resref))
                    continue;
                using var template = ReadAsset(root, "utc", resref);
                var equipment = Equipment(creature);
                var expectedEquipment = Equipment(template.RootElement);
                if (!equipment.TryGetValue(SkinSlot, out var skinResref))
                {
                    failures.Add($"{Path.GetFileName(path)} {resref}: missing placed stat skin");
                    continue;
                }
                if (skinResref != expectedEquipment[SkinSlot])
                    failures.Add($"{Path.GetFileName(path)} {resref}: placed stat skin {skinResref} differs from {expectedEquipment[SkinSlot]}");
                using var skin = ReadAsset(root, "uti", skinResref);
                var hp = Property(skin.RootElement, 96);
                if (hp <= 0)
                    continue;
                foreach (var field in new[] { "Str", "Dex", "Wis", "Con", "Int" })
                    Check(field, Int(template.RootElement, field));
                Check("NaturalAC", 0);
                foreach (var equippedItem in creature.GetProperty("Equip_ItemList").GetProperty("value").EnumerateArray())
                {
                    if (!equippedItem.TryGetProperty("PropertiesList", out var actualProperties))
                        continue;
                    var itemResref = equippedItem.GetProperty("TemplateResRef").GetProperty("value").GetString()!;
                    var itemPath = Path.Combine(root, "Module", "uti", $"{itemResref}.uti.json");
                    if (!File.Exists(itemPath))
                        continue;
                    using var blueprint = ReadAsset(root, "uti", itemResref);
                    var expectedProperties = blueprint.RootElement.GetProperty("PropertiesList");
                    if (!JsonElement.DeepEquals(actualProperties, expectedProperties))
                        failures.Add($"{Path.GetFileName(path)} {resref}: embedded {itemResref} combat properties differ from its blueprint");
                }
                var actualFeats = creature.GetProperty("FeatList");
                if (!JsonElement.DeepEquals(actualFeats, template.RootElement.GetProperty("FeatList")))
                    failures.Add($"{Path.GetFileName(path)} {resref}: placed ability kit differs from its blueprint");
                Check("CurrentHitPoints", hp);
                Check("MaxHitPoints", hp);
                var levels = creature.GetProperty("ClassList").GetProperty("value").EnumerateArray()
                    .Sum(c => Int(c, "ClassLevel"));
                var feats = creature.GetProperty("FeatList").GetProperty("value").EnumerateArray()
                    .Select(f => Int(f, "Feat")).ToHashSet();
                var adjustment = (int)Math.Floor((Int(creature, "Con") - 10) / 2m) * levels;
                adjustment += feats.Contains(40) ? levels : 0;
                adjustment += feats.Count(f => f is >= 754 and <= 763) * 20;
                Check("HitPoints", hp - adjustment);
                count++;

                void Check(string field, int expected)
                {
                    if (Int(creature, field) != expected)
                        failures.Add($"{Path.GetFileName(path)} {resref} {field}: expected {expected}, actual {Int(creature, field)}");
                }
            }
        }
        count.Should().BeGreaterThan(60);
        failures.Should().BeEmpty("placed copies must not bypass reviewed blueprint budgets");
    }

    [Test]
    public void DathomirSprantal_HasReliableHitRateAndLowerBudgetsThanChirodactyl()
    {
        var root = FindRoot();
        using var sprantal = ReadAsset(root, "utc", "vdathsprantal");
        using var sprantalSkin = ReadAsset(root, "uti", "sprantal_sk");
        using var chiro = ReadAsset(root, "utc", "vdathchirodac");
        using var chiroSkin = ReadAsset(root, "uti", "chirodactyl_sk");
        var evasion = Stat.GetEvasion(Property(sprantalSkin.RootElement, 99),
            Int(sprantal.RootElement, "Int"), Property(sprantalSkin.RootElement, 117) + Int(sprantal.RootElement, "NaturalAC") * 5);
        evasion.Should().Be(138);
        Combat.CalculateHitRate(148, evasion, 0).Should().Be(80);
        Combat.CalculateHitRate(162, evasion, 0).Should().Be(87);
        Property(chiroSkin.RootElement, 96).Should().BeGreaterThan(Property(sprantalSkin.RootElement, 96) * 10);
        Property(chiroSkin.RootElement, 94, 1).Should().BeGreaterThan(Property(sprantalSkin.RootElement, 94, 1));
        Property(chiroSkin.RootElement, 111).Should().BeGreaterThan(Property(sprantalSkin.RootElement, 111));
    }

    [Test]
    public void CapstoneGenerator_UsesRuntimeScoreMappingAndClearsInheritedArmor()
    {
        var source = File.ReadAllText(Path.Combine(FindRoot(), "tools", "UpdateCapstoneEnemyIdentity.ps1"));
        source.Should().Contain("Set-JsonTypedValue $utc \"Dex\" $stats.PER");
        source.Should().Contain("Set-JsonTypedValue $utc \"Int\" $stats.AGI");
        source.Should().Contain("Set-JsonTypedValue $utc \"NaturalAC\" 0");
    }

    private static int Number(string text) => decimal.ToInt32(decimal.Parse(text, CultureInfo.InvariantCulture));
    private static int Int(JsonElement obj, string field) => obj.GetProperty(field).GetProperty("value").GetInt32();
    private static IEnumerable<JsonElement> Variables(JsonElement obj) =>
        obj.TryGetProperty("VarTable", out var variables)
            ? variables.GetProperty("value").EnumerateArray() : Enumerable.Empty<JsonElement>();
    private static bool IsNpcOnly(JsonElement obj) =>
        Variables(obj)
            .Any(v => v.GetProperty("Name").GetProperty("value").GetString() == "NO_ECONOMY" &&
                      Int(v, "Value") == 1);
    private static int Property(JsonElement obj, int kind, int subtype = -1) =>
        obj.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
            .Where(p => Int(p, "PropertyName") == kind && (subtype < 0 || Int(p, "Subtype") == subtype))
            .Sum(p => Int(p, "CostValue"));
    private static Dictionary<int, string> Equipment(JsonElement obj) =>
        obj.GetProperty("Equip_ItemList").GetProperty("value").EnumerateArray()
            .ToDictionary(e => e.GetProperty("__struct_id").GetInt32(),
                e => (e.TryGetProperty("EquippedRes", out var resref) ? resref : e.GetProperty("TemplateResRef"))
                    .GetProperty("value").GetString()!);
    private static JsonDocument ReadAsset(string root, string type, string resref) =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", type, $"{resref}.{type}.json")));

    private static List<Dictionary<string, string>> ReadRows(string root, string name)
    {
        using var archive = ZipFile.OpenRead(Path.Combine(root, "design", "bible", "SWLOR Design Bible - Combat Upgrade.xlsx"));
        XDocument Read(string path)
        {
            using var stream = archive.GetEntry(path)!.Open();
            return XDocument.Load(stream);
        }
        XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var sheet = Read("xl/workbook.xml").Descendants(Ns + "sheet").Single(s => s.Attribute("name")!.Value == name);
        var id = sheet.Attribute(relNs + "id")!.Value;
        var target = Read("xl/_rels/workbook.xml.rels").Root!.Elements()
            .Single(r => r.Attribute("Id")!.Value == id).Attribute("Target")!.Value.TrimStart('/');
        if (!target.StartsWith("xl/")) target = "xl/" + target;
        var shared = archive.GetEntry("xl/sharedStrings.xml") == null ? Array.Empty<string>() :
            Read("xl/sharedStrings.xml").Descendants(Ns + "si")
                .Select(s => string.Concat(s.Descendants(Ns + "t").Select(t => t.Value))).ToArray();
        return Read(target).Descendants(Ns + "row").Where(r => int.Parse(r.Attribute("r")!.Value) > 1)
            .Select(r => r.Elements(Ns + "c").ToDictionary(
                c => new string(c.Attribute("r")!.Value.TakeWhile(char.IsLetter).ToArray()), c =>
                c.Attribute("t")?.Value == "inlineStr" ? string.Concat(c.Descendants(Ns + "t").Select(t => t.Value)) :
                c.Attribute("t")?.Value == "s" ? shared[int.Parse(c.Element(Ns + "v")!.Value)] :
                c.Element(Ns + "v")?.Value ?? ""))
            .Where(r => r.TryGetValue("C", out var value) && !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory!.FullName;
    }
}
