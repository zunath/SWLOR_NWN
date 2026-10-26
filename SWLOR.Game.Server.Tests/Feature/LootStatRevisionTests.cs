using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class LootStatRevisionTests
{
    // Blueprint values without the retired elemental Defense, which never counted. The Bible rows had folded it into
    // Physical Defense; the Carapace values match the crafted Armor-40 pieces (Eclipse Bracer, Transcendent Gloves,
    // Supreme Tunic).
    //        resref               rank  phys force  hp  fp stm fpRegen stmRegen
    [TestCase("cara_bracer",        40, 18, 10, 80, 0, 0, 0, 0)]
    [TestCase("cara_wraps",         40,  9, 20, 20, 8, 0, 2, 0)]
    [TestCase("cara_tunic",         40, 14, 14, 45, 0, 10, 0, 4)]
    [TestCase("cara_armor",         40, 24, 19, 90, 0, 0, 0, 0)]
    [TestCase("cara_robes",         40, 13, 24, 45, 0, 0, 4, 0)]
    [TestCase("cara_boots",         40, 10, 10, 25, 0, 8, 0, 2)]
    [TestCase("cara_leggings",      40, 18, 10, 80, 0, 0, 0, 0)]
    [TestCase("cara_shoes",         40,  9, 20, 25, 8, 0, 2, 0)]
    [TestCase("cara_gloves",        40, 10, 10, 20, 0, 8, 0, 2)]
    [TestCase("cara_cap",           40, 10, 10, 40, 0, 8, 0, 2)]
    [TestCase("cara_headdress",     40,  9, 20, 40, 8, 0, 2, 0)]
    [TestCase("cara_helmet",        40, 18, 10, 80, 0, 0, 0, 0)]
    [TestCase("apprentice_dark_",   20,  0, 10, 25, 0, 0, 8, 0)]
    // Raised to the Kwi Heavy Armor, Kwi Light Cloak and Kwi Hyper Gloves loot standard.
    [TestCase("dan_heavy_armor",    40, 25, 20, 95, 0, 0, 0, 0)]
    [TestCase("dan_h_cloak",        40, 11,  8,  5, 0, 2, 0, 0)]
    [TestCase("dan_hyper_gloves",   40, 10, 21, 21, 9, 0, 2, 0)]
    public void RetiredDefenseItem_UsesBibleDefenseWithoutRetiredElementalDefense(string resref, int rank, int physical, int force,
        int hp, int fp, int stamina, int fpRegen, int staminaRegen)
    {
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", "uti", resref + ".uti.json")));
        var properties = ReadProperties(blueprint.RootElement);
        var expected = new Dictionary<(int Type, int Subtype), int> { [((int)ItemPropertyType.RequiresSkill, 0)] = rank };
        void Expect(ItemPropertyType type, int subtype, int value)
        {
            if (value > 0) expected[((int)type, subtype)] = value;
        }
        Expect(ItemPropertyType.Defense, 1, physical);
        Expect(ItemPropertyType.Defense, 2, force);
        Expect(ItemPropertyType.HPBonus, 0, hp);
        Expect(ItemPropertyType.FP, 0, fp);
        Expect(ItemPropertyType.Stamina, 0, stamina);
        Expect(ItemPropertyType.FPRegen, 0, fpRegen);
        Expect(ItemPropertyType.STMRegen, 0, staminaRegen);
        properties.Should().BeEquivalentTo(expected);
        ReadLocalInt(blueprint.RootElement, RetiredDefenseStatRevisionCatalog.RevisionVariable)
            .Should().Be(RetiredDefenseStatRevisionCatalog.CurrentRevision);
        RetiredDefenseStatRevisionCatalog.Changes.Should().ContainKey(resref);
        foreach (var change in RetiredDefenseStatRevisionCatalog.Changes[resref])
            properties.GetValueOrDefault(((int)change.Type, change.Type == ItemPropertyType.Defense ? change.Subtype : 0))
                .Should().Be(change.Current);

        using var archive = ZipFile.OpenRead(Path.Combine(FindRepositoryRoot().FullName, "design", "bible", "SWLOR Design Bible.xlsx"));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XDocument Load(string name) { using var stream = archive.GetEntry(name)!.Open(); return XDocument.Load(stream); }
        var shared = Load("xl/sharedStrings.xml").Descendants(ns + "si")
            .Select(x => string.Concat(x.Descendants(ns + "t").Select(t => t.Value))).ToArray();
        var sheet = Load("xl/workbook.xml").Descendants(ns + "sheet").Single(x => (string?)x.Attribute("name") == "Equipment - Armor");
        var id = (string)sheet.Attribute(rel + "id")!;
        var target = (string)Load("xl/_rels/workbook.xml.rels").Root!.Elements()
            .Single(x => (string?)x.Attribute("Id") == id).Attribute("Target")!;
        string Text(XElement cell) => cell.Attribute("t")?.Value switch
        {
            "inlineStr" => string.Concat(cell.Descendants(ns + "t").Select(x => x.Value)),
            "s" => shared[int.Parse(cell.Element(ns + "v")!.Value)],
            _ => cell.Element(ns + "v")?.Value ?? "",
        };
        var row = Load(target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target)
            .Descendants(ns + "row").Single(x => x.Elements(ns + "c")
                .Any(cell => cell.Attribute("r")!.Value.StartsWith('B') && Text(cell) == resref));
        var cells = row.Elements(ns + "c").ToDictionary(cell =>
            new string(cell.Attribute("r")!.Value.TakeWhile(char.IsLetter).ToArray()), Text);
        cells["E"].Should().Be(rank.ToString());
        cells["F"].Should().Be(physical.ToString());
        cells["G"].Should().Be(force.ToString());
        cells["H"].Should().Be(hp.ToString());
        cells["I"].Should().Be(fp.ToString());
        cells["J"].Should().Be(stamina.ToString());
        cells["K"].Should().Be(fpRegen.ToString());
        cells["L"].Should().Be(staminaRegen.ToString());
        cells["T"].Should().Be("-");
    }

    [Test]
    public void RetiredDefenseCatalogBlueprints_CarryTheRevisionMarker()
    {
        // New drops are already correct, so the revision must never run on them.
        foreach (var resref in RetiredDefenseStatRevisionCatalog.Changes.Keys)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                FindRepositoryRoot().FullName, "Module", "uti", resref + ".uti.json"), System.Text.Encoding.Latin1));
            ReadLocalInt(blueprint.RootElement, RetiredDefenseStatRevisionCatalog.RevisionVariable)
                .Should().Be(RetiredDefenseStatRevisionCatalog.CurrentRevision, resref);
        }
    }

    [Test]
    public void ItemBlueprints_CarryOnlyPhysicalAndForceDefense()
    {
        // Only Physical and Force Defense count in combat; any other subtype shows on the item and does nothing.
        var offenders = new List<string>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot().FullName, "Module", "uti"), "*.uti.json"))
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(path, System.Text.Encoding.Latin1));
            if (!blueprint.RootElement.TryGetProperty("PropertiesList", out var list))
                continue;
            foreach (var property in list.GetProperty("value").EnumerateArray())
            {
                var subtype = property.GetProperty("Subtype").GetProperty("value").GetInt32();
                if (property.GetProperty("PropertyName").GetProperty("value").GetInt32() == Defense &&
                    subtype is not ((int)CombatDamageType.Physical or (int)CombatDamageType.Force))
                    offenders.Add($"{Path.GetFileName(path)}: Defense subtype {subtype}");
            }
        }

        offenders.Should().BeEmpty();
    }

    private const int RequiresSkill = (int)ItemPropertyType.RequiresSkill;
    private const int Defense = (int)ItemPropertyType.Defense;

    // Budget units: one level-45 enhancement slot buys 5 Defense, 40 HP or 6 FP/STM. Attack, Force Attack
    // and Evasion cost double because each point moves damage dealt or taken more than a point of Defense.
    private static readonly Dictionary<(int Type, int Subtype), double> UnitWeights = new()
    {
        [(Defense, 1)] = 1, [(Defense, 2)] = 1, [((int)ItemPropertyType.HPBonus, 0)] = 1 / 8.0,
        [((int)ItemPropertyType.FP, 0)] = 5 / 6.0, [((int)ItemPropertyType.Stamina, 0)] = 5 / 6.0,
        [((int)ItemPropertyType.CombatReadiness, 0)] = 1, [((int)ItemPropertyType.Attack, 0)] = 2,
        [((int)ItemPropertyType.ForceAttack, 0)] = 2, [((int)ItemPropertyType.Evasion, 0)] = 2,
        [((int)ItemPropertyType.FPRegen, 0)] = 2.5, [((int)ItemPropertyType.STMRegen, 0)] = 2.5,
    };

    // Level-50 budget per slot, matching crafted Chaos/Immortal/Magus gear.
    private static readonly Dictionary<BaseItem, double> SlotBudgets = new()
    {
        [BaseItem.Armor] = 68, [BaseItem.Helmet] = 52, [BaseItem.Gloves] = 50, [BaseItem.Bracer] = 50,
        [BaseItem.Belt] = 30, [BaseItem.Cloak] = 31, [BaseItem.Ring] = 30, [BaseItem.Amulet] = 31,
        [BaseItem.Boots] = 49, [BaseItem.LargeShield] = 32,
    };

    // Weapon DMG by base item and required level: the highest crafted, store or loot value of the
    // established gear at each level, before the level-45 loot tier existed.
    private static readonly Dictionary<int, SortedDictionary<int, int>> WeaponDamageCurves = new()
    {
        [(int)BaseItem.Longsword] = Curve(6, 7, 11, 11, 15, 15, 18, 19, 23, 24),
        [(int)BaseItem.Rifle] = Curve(8, 11, 22, 22, 29, 29, 35, 35, 39, 40),
        [(int)BaseItem.TwoBladedSword] = Curve(6, 7, 11, 11, 15, 15, 18, 19, 23, 24),
        [(int)BaseItem.GreatSword] = Curve(9, 12, 22, 22, 35, 35, 38, 38, 42, 44),
        [(int)BaseItem.Dagger] = Curve(7, 7, 10, 10, 15, 15, 18, 18, 21, 22),
        [(int)BaseItem.QuarterStaff] = Curve(6, 7, 11, 11, 15, 15, 18, 19, 23, 24),
        [(int)BaseItem.ShortSpear] = Curve(8, 11, 21, 21, 27, 33, 33, 40, 41, 42),
        [(int)BaseItem.Shuriken] = Curve(5, 5, 8, 8, 11, 11, 13, 14, 19, 19),
        // Pistol blueprints use base item 61.
        [(int)BaseItem.Sling] = Curve(6, 7, 11, 11, 15, 15, 17, 22, 22, 23),
        [(int)BaseItem.Katar] = Curve(8, 8, 10, 10, 13, 13, 14, 15, 18, 19),
        // Power Gloves keep each Katar tier's weapon budget.
        [(int)BaseItem.PowerGlove] = Curve(8, 8, 10, 10, 13, 13, 14, 15, 18, 19),
    };

    [Test]
    public void Catalog_MatchesRevisedBlueprints()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();
        foreach (var (resref, changes) in LootStatRevisionCatalog.Changes)
        {
            var path = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            if (!File.Exists(path))
            {
                failures.Add($"{resref}: blueprint is missing");
                continue;
            }

            using var uti = JsonDocument.Parse(File.ReadAllText(path));
            var properties = ReadProperties(uti.RootElement);
            foreach (var change in changes)
            {
                var subtype = change.Type == ItemPropertyType.Defense ? change.Subtype : 0;
                properties.TryGetValue(((int)change.Type, subtype), out var value);
                var expected = RareLootStatRevisionCatalog.Changes.TryGetValue(resref, out var corrections)
                    ? corrections.FirstOrDefault(x => x.Type == change.Type && x.Subtype == change.Subtype, change).Current
                    : change.Current;
                if (FoundryLootStatRevisionCatalog.Changes.TryGetValue(resref, out var foundryCorrections))
                    expected = foundryCorrections.Where(x => x.Type == change.Type && x.Subtype == change.Subtype)
                        .Select(x => x.Current).DefaultIfEmpty(expected).Single();
                if (value != expected)
                    failures.Add($"{resref}: {change.Type} is {value}, catalog expects {expected}");
                if (change.Previous == change.Current)
                    failures.Add($"{resref}: {change.Type} has no change");
            }

            if (ReadLocalInt(uti.RootElement, LootStatRevisionMigration.RevisionVariable) != LootStatRevisionMigration.CurrentRevision)
                failures.Add($"{resref}: blueprint must carry {LootStatRevisionMigration.RevisionVariable} = {LootStatRevisionMigration.CurrentRevision}");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void FoundryCorrections_MatchBlueprintsAndFollowTheExistingLootRevisions()
    {
        var root = FindRepositoryRoot();
        foreach (var (resref, changes) in FoundryLootStatRevisionCatalog.Changes)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json")));
            var properties = ReadProperties(blueprint.RootElement);
            foreach (var change in changes)
            {
                var preceding = LootStatRevisionCatalog.Changes[resref].Single(x => x.Type == change.Type && x.Subtype == change.Subtype);
                if (RareLootStatRevisionCatalog.Changes.TryGetValue(resref, out var rareChanges))
                    preceding = rareChanges.FirstOrDefault(x => x.Type == change.Type && x.Subtype == change.Subtype, preceding);
                change.Previous.Should().Be(preceding.Current, resref);
                properties[((int)change.Type, change.Subtype)].Should().Be(change.Current, resref);
            }
            ReadLocalInt(blueprint.RootElement, FoundryLootStatRevisionCatalog.RevisionVariable)
                .Should().Be(FoundryLootStatRevisionCatalog.CurrentRevision, resref);
        }
    }

    [Test]
    public void ArkanianArmorRevision_EndsOnTheBlueprintAndMarksOnlyIt()
    {
        var root = FindRepositoryRoot();
        foreach (var (resref, changes) in ArkanianArmorStatRevisionCatalog.Changes)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json")));
            var properties = new Dictionary<(int Type, int Subtype), int>();
            foreach (var property in blueprint.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray())
            {
                var key = (property.GetProperty("PropertyName").GetProperty("value").GetInt32(),
                    property.GetProperty("Subtype").GetProperty("value").GetInt32());
                properties[key] = properties.GetValueOrDefault(key) + property.GetProperty("CostValue").GetProperty("value").GetInt32();
            }

            // Ice Resistance replaces the shipped blueprint's retired Ice Defense and is revised from each copy's own Ice values.
            var iceResistance = ((int)ItemPropertyType.Resistance, (int)ResistanceType.Ice);
            properties.GetValueOrDefault(iceResistance).Should().Be(ArkanianArmorStatRevisionCatalog.IceResistance, resref);
            properties.Keys.Should().BeSubsetOf(changes.Select(change => ((int)change.Type, change.Subtype)).Append(iceResistance),
                $"every {resref} property moves with the revision");
            foreach (var change in changes)
            {
                change.Previous.Should().NotBe(change.Current, $"{resref} {change.Type} changes");
                properties.GetValueOrDefault(((int)change.Type, change.Subtype)).Should().Be(change.Current, $"{resref} {change.Type} {change.Subtype}");
            }
            ReadLocalInt(blueprint.RootElement, ArkanianArmorStatRevisionCatalog.RevisionVariable)
                .Should().Be(ArkanianArmorStatRevisionCatalog.CurrentRevision, resref);
        }

        Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json")
            .Where(path => File.ReadAllText(path).Contains(ArkanianArmorStatRevisionCatalog.RevisionVariable))
            .Select(path => Path.GetFileName(path)[..^".uti.json".Length])
            .Should().BeEquivalentTo(ArkanianArmorStatRevisionCatalog.Changes.Keys,
                "an item built from a stamped blueprint is never revised, so only cataloged blueprints carry the marker");
    }

    [Test]
    public void RevisionMarker_OnlyAppearsOnCatalogedBlueprints()
    {
        var root = FindRepositoryRoot();
        var stamped = Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "uti"), "*.uti.json")
            .Where(path => File.ReadAllText(path).Contains(LootStatRevisionMigration.RevisionVariable))
            .Select(path => Path.GetFileName(path)[..^".uti.json".Length]);

        stamped.Should().OnlyContain(resref => LootStatRevisionCatalog.Changes.ContainsKey(resref),
            "an item built from a stamped blueprint is never migrated, so the catalog must describe it");
    }

    [Test]
    public void LevelFortyFiveLoot_StaysWithinItsSlotBudget()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();
        foreach (var resref in GetLootAndSalvageResrefs())
        {
            var path = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            if (!File.Exists(path))
                continue;

            using var uti = JsonDocument.Parse(File.ReadAllText(path));
            var properties = ReadProperties(uti.RootElement);
            if (RequiredLevel(properties) != 45)
                continue;

            var baseItem = uti.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32();
            if (properties.TryGetValue(((int)ItemPropertyType.DMG, 0), out var damage))
            {
                if (!WeaponDamageCurves.TryGetValue(baseItem, out var curve))
                {
                    failures.Add($"{resref}: base item {baseItem} has no weapon damage reference");
                    continue;
                }

                if (damage > curve[50])
                    failures.Add($"{resref}: DMG {damage} exceeds the level-50 reference {curve[50]}");
                foreach (var type in new[] { ItemPropertyType.Attack, ItemPropertyType.ForceAttack })
                    if (Value(properties, type) > (RareLootStatRevisionCatalog.Changes.ContainsKey(resref) ? 20 : 5))
                        failures.Add($"{resref}: weapon {type} exceeds its enhanced-crafted allowance");
                continue;
            }

            // Corrected rare wearables are compared with actual enhanced recipes in RareLootBalanceTests.
            if (RareLootStatRevisionCatalog.Changes.ContainsKey(resref) ||
                !SlotBudgets.TryGetValue((BaseItem)baseItem, out var budget))
                continue;
            foreach (var type in new[] { ItemPropertyType.Attack, ItemPropertyType.ForceAttack, ItemPropertyType.Evasion })
                if (Value(properties, type) > 8)
                    failures.Add($"{resref}: {type} {Value(properties, type)} exceeds 8");
            var units = properties.Sum(x => UnitWeights.TryGetValue(x.Key, out var weight) ? weight * x.Value : 0);
            if (units > budget + 0.5)
                failures.Add($"{resref}: {units:0.0} budget units exceed the {(BaseItem)baseItem} budget of {budget}");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void SlicingRewardWeapons_FollowTheLevelDamageCurve()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();
        foreach (var resref in SlicingRewardCatalog.Entries.Select(x => x.Resref).Distinct())
        {
            var path = Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json");
            if (!File.Exists(path))
                continue;

            using var uti = JsonDocument.Parse(File.ReadAllText(path));
            var properties = ReadProperties(uti.RootElement);
            if (!properties.TryGetValue(((int)ItemPropertyType.DMG, 0), out var damage))
                continue;

            var baseItem = uti.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32();
            var level = RequiredLevel(properties) ?? 0;
            WeaponDamageCurves.Should().ContainKey(baseItem, $"{resref} needs a weapon damage reference");
            var reference = WeaponDamageCurves[baseItem].Last(x => x.Key <= level).Value;
            if (damage > reference)
                failures.Add($"{resref}: DMG {damage} exceeds the level-{level} reference {reference}");
            var attackBudget = level switch { < 10 => 1, < 20 => 4, < 30 => 7, < 40 => 10, _ => 13 };
            if (Value(properties, ItemPropertyType.Attack) > attackBudget)
                failures.Add($"{resref}: Attack exceeds its tier's enhanced-crafted allowance {attackBudget}");
            if (Value(properties, ItemPropertyType.CombatReadiness) > 4)
                failures.Add($"{resref}: Combat Readiness exceeds 4");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static SortedDictionary<int, int> Curve(params int[] damageByLevel)
    {
        var levels = new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 50 };
        return new SortedDictionary<int, int>(levels.Zip(damageByLevel).ToDictionary(x => x.First, x => x.Second));
    }

    private static IEnumerable<string> GetLootAndSalvageResrefs()
    {
        var resrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in typeof(ILootTableDefinition).Assembly.GetTypes()
                     .Where(type => typeof(ILootTableDefinition).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface))
        {
            foreach (var (_, table) in ((ILootTableDefinition)Activator.CreateInstance(type)!).BuildLootTables())
                foreach (var item in table)
                    resrefs.Add(item.Resref);
        }

        foreach (var recipe in new SalvagedFieldGearRecipes().BuildRecipes().Values)
            resrefs.Add(recipe.Resref);
        return resrefs;
    }

    private static Dictionary<(int Type, int Subtype), int> ReadProperties(JsonElement uti)
    {
        var properties = new Dictionary<(int Type, int Subtype), int>();
        foreach (var property in uti.GetProperty("PropertiesList").GetProperty("value").EnumerateArray())
        {
            var type = property.GetProperty("PropertyName").GetProperty("value").GetInt32();
            var subtype = type == Defense ? property.GetProperty("Subtype").GetProperty("value").GetInt32() : 0;
            var value = property.GetProperty("CostValue").GetProperty("value").GetInt32();
            if (type == RequiresSkill && properties.ContainsKey((type, 0)))
                continue;
            properties[(type, subtype)] = properties.GetValueOrDefault((type, subtype)) + value;
        }

        return properties;
    }

    private static int? RequiredLevel(Dictionary<(int Type, int Subtype), int> properties)
        => properties.TryGetValue((RequiresSkill, 0), out var level) ? level : null;

    private static int Value(Dictionary<(int Type, int Subtype), int> properties, ItemPropertyType type)
        => properties.GetValueOrDefault(((int)type, 0));

    private static int ReadLocalInt(JsonElement uti, string name)
    {
        if (!uti.TryGetProperty("VarTable", out var table))
            return 0;
        foreach (var variable in table.GetProperty("value").EnumerateArray())
            if (variable.GetProperty("Name").GetProperty("value").GetString() == name)
                return variable.GetProperty("Value").GetProperty("value").GetInt32();
        return 0;
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
