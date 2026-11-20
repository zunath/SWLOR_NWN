using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.LootTableDefinition;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Astromech forms of found gear: every armor-slot piece, staff and rifle that asks for a rank and is found in a loot
/// table, a store or the slicing rewards has an astromech form with the same stats (tools/GenerateAstromechRewards.py),
/// and the form drops wherever a looted piece does (AstromechLoot). AstromechStoreContentTests covers how stores and
/// slicing hand the forms out. These tests fail when a source gains a piece, or a piece changes, until the generator
/// is rerun.
/// </summary>
public class AstromechLootContentTests
{
    private const int AstromechRace = 170, DroidRace = 167;
    private const int RacialProperty = 64, RequiresSkillProperty = 131;
    private const int FabricationSkill = 10, AgricultureSkill = 31, EngineeringSkill = 32;
    private const int ArmorSkill = 6, SmitherySkill = 9, StaffSkill = 44, RifleSkill = 46, ChassisSkill = 61, ArmamentsSkill = 63;
    private const int ArmorBaseItem = 16, StaffBaseItem = 50, RifleBaseItem = 7, ShockProdBaseItem = 545, BlasterMountBaseItem = 546;
    private const int PlainToolPart = 11;

    // The base items that have an astromech form: the eight module slots, and the two weapons with an astromech tool.
    private static readonly Dictionary<int, string> ModuleNames = new()
    {
        [17] = "Sensor Package", [ArmorBaseItem] = "Chassis Plating", [36] = "Manipulator Servo", [78] = "Manipulator Servo",
        [26] = "Drive Unit", [21] = "Power Cell", [19] = "Logic Module", [52] = "Expansion Socket", [80] = "Deflector Emitter",
        [StaffBaseItem] = "Shock Prod", [RifleBaseItem] = "Blaster Mount"
    };

    private static readonly string Root = FindRepositoryRoot();
    private static readonly Lazy<Dictionary<string, JsonElement>> LootedGear = new(ReadLootedGear);
    private static readonly Lazy<Dictionary<string, JsonElement>> FoundGear = new(ReadFoundGear);

    // Forms of crafted gear and forms listed by hand: AstromechModuleContentTests and AstromechRewardContentTests cover them.
    private static readonly string[] OtherForms = { "am_h_", "am_t_", "am_w_", "am_q_", "am_plat_" };

    [Test]
    public void EveryFoundPieceAnAstromechCouldWear_HasAnAstromechForm()
    {
        LootedGear.Value.Count.Should().BeGreaterThan(700);
        FoundGear.Value.Count.Should().BeGreaterThan(LootedGear.Value.Count + 250, "stores and slicing rewards hold gear of their own");
        var missing = FoundGear.Value.Keys.Where(resref => !AstromechItemTwins.HasTwin(resref)).OrderBy(resref => resref).ToList();

        missing.Should().BeEmpty("rerun tools/GenerateAstromechRewards.py");
    }

    [Test]
    public void EveryFoundFormCopiesItsSource()
    {
        var problems = new List<string>();
        var names = new Dictionary<string, string>();
        foreach (var (source, from) in FoundGear.Value)
        {
            var form = AstromechItemTwins.Resolve(source, BodyFrame.Astromech);
            if (form == source)
                continue; // reported by EveryFoundPieceAnAstromechCouldWear_HasAnAstromechForm
            if (OtherForms.Any(form.StartsWith))
                continue;
            AstromechItemTwins.Resolve(source, BodyFrame.Humanoid).Should().Be(source);
            if (form.Length > 16)
                problems.Add($"{form}: a resource name holds 16 characters");
            var path = Path.Combine(Root, "Module", "uti", form + ".uti.json");
            if (!File.Exists(path))
            {
                problems.Add($"{form}: no blueprint");
                continue;
            }

            var to = ReadBlueprint(path);
            var sourceBase = Number(from, "BaseItem");
            var isTool = sourceBase is StaffBaseItem or RifleBaseItem;
            var expectedBase = sourceBase switch
            {
                StaffBaseItem => ShockProdBaseItem,
                RifleBaseItem => BlasterMountBaseItem,
                _ => sourceBase
            };
            if (Number(to, "BaseItem") != expectedBase)
                problems.Add($"{form}: base item {Number(to, "BaseItem")}");
            if (Text(to, "Tag") != form || Text(to, "TemplateResRef") != form)
                problems.Add($"{form}: tag or resref is not the file name");

            var name = to.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!;
            if (Encoding.UTF8.GetByteCount(name) != name.Length || !name.EndsWith(" " + ModuleNames[sourceBase]))
                problems.Add($"{form}: the name '{name}' is not ASCII ending in {ModuleNames[sourceBase]}");
            if (!names.TryAdd(name, form))
                problems.Add($"{form}: shares the name '{name}' with {names[name]}");

            // The stats: every property is the source's, with the astromech skill and the astromech race limit.
            var expected = Properties(from).Select(property => property switch
            {
                { Number: RequiresSkillProperty, Subtype: ArmorSkill } => property with { Subtype = ChassisSkill },
                { Number: RequiresSkillProperty, Subtype: StaffSkill or RifleSkill } => property with { Subtype = ArmamentsSkill },
                { Number: RacialProperty, Subtype: DroidRace } => property with { Subtype = AstromechRace },
                _ => property
            }).ToList();
            if (expected.All(property => property.Number != RacialProperty))
                expected.Add(new Property(RacialProperty, AstromechRace, 0, 0, 255, 0));
            var actual = Properties(to);
            if (!actual.SequenceEqual(expected))
                problems.Add($"{form}: its properties are not the source's");
            if (actual.Count(property => property.Number == RacialProperty && property.Subtype == AstromechRace) != 1)
                problems.Add($"{form}: is not limited to astromechs exactly once");
            var skill = actual.Single(property => property.Number == RequiresSkillProperty).Subtype;
            if (skill is not (ChassisSkill or ArmamentsSkill or SmitherySkill or FabricationSkill or AgricultureSkill or EngineeringSkill))
                problems.Add($"{form}: asks for skill {skill}, which an astromech cannot train");

            // Everything else is the source's, but the model numbers a chassis has no use for.
            foreach (var field in from.EnumerateObject())
            {
                if (field.Name is "LocalizedName" or "Tag" or "TemplateResRef" or "BaseItem" or "PropertiesList" or "Description" or "DescIdentified")
                    continue;
                var value = to.GetProperty(field.Name);
                if (field.Name == "VarTable")
                {
                    // The stat migrations find items by resref; a form carries none of their revision markers.
                    var kept = LocalVariables(from).Where(name => !name.EndsWith("_STATS_REVISION"));
                    if (!LocalVariables(to).SequenceEqual(kept))
                        problems.Add($"{form}: its local variables are not the source's without the revision markers");
                }
                else if (IsArmorPart(field.Name))
                {
                    var wanted = field.Name.EndsWith("_Torso") ? AstromechBody.PlatingTorso : 0;
                    if (value.GetProperty("value").GetInt32() != wanted)
                        problems.Add($"{form}: {field.Name} is not {wanted}");
                }
                else if (isTool && IsWeaponPart(field.Name))
                {
                    if (value.GetProperty("value").GetInt32() != PlainToolPart)
                        problems.Add($"{form}: {field.Name} is not {PlainToolPart}");
                }
                else if (value.GetRawText() != field.Value.GetRawText())
                {
                    problems.Add($"{form}: {field.Name} differs from {source}");
                }
            }
        }

        problems.Should().BeEmpty(string.Join(Environment.NewLine, problems.Take(40)));
    }

    [Test]
    public void AFormDropsOnlyWhenTheSecondRollPicksAPieceThatHasOne()
    {
        var gear = LootedGear.Value.Keys.First();
        var table = new LootTable { new LootTableItem("some_material", 1, 10, false), new LootTableItem(gear, 1, 10, true) };

        // Three attempts: the first fails its chance, the second picks the material, the third picks the gear.
        var chances = new Queue<bool>(new[] { false, true, true });
        var picks = new Queue<LootTableItem>(new[] { table[0], table[1] });
        var forms = AstromechLoot.Roll(table, 3, chances.Dequeue, _ => picks.Dequeue());

        forms.Should().ContainSingle();
        forms[0].Source.Should().BeSameAs(table[1]);
        forms[0].Resref.Should().Be(AstromechItemTwins.Resolve(gear, BodyFrame.Astromech));
        chances.Should().BeEmpty("every attempt rolls the table's chance");
        picks.Should().BeEmpty("only an attempt that passed picks an entry");
    }

    [Test]
    public void ATableWithoutGearIsNotRolledASecondTime()
    {
        var table = new LootTable { new LootTableItem("some_material", 1, 10, false), new LootTableItem("nw_it_gold001", 50, 10, false) };
        var rolls = 0;

        AstromechLoot.HasForms(table).Should().BeFalse();
        AstromechLoot.Roll(table, 5, () => { rolls++; return true; }, rolled => rolled[0]).Should().BeEmpty();
        AstromechLoot.Roll(new LootTable(), 5, () => { rolls++; return true; }, rolled => rolled[0]).Should().BeEmpty();
        AstromechLoot.Roll(null, 5, () => { rolls++; return true; }, rolled => rolled[0]).Should().BeEmpty();
        rolls.Should().Be(0);
    }

    [Test]
    public void TheLootCodeRollsTheFormsWithTheTablesOwnChance()
    {
        var loot = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "Loot.cs"));

        loot.Should().Contain("AstromechLoot.Roll(table, attempts, () => Random.D100(1) <= chance,");
        loot.Should().Contain("rolled => rolled.GetRandomItemWithRareBonus(rareBonusChance));");
    }

    private static bool IsArmorPart(string field) => field.StartsWith("ArmorPart_") || field.StartsWith("xArmorPart_");

    private static bool IsWeaponPart(string field) => field.StartsWith("ModelPart") || field.StartsWith("xModelPart");

    /// <summary>
    /// Every item the loot tables name whose base item has an astromech form and that is not astromech gear itself.
    /// </summary>
    private static Dictionary<string, JsonElement> ReadLootedGear()
    {
        var gear = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var definitions = typeof(ViscaraLootTableDefinition).Assembly.GetTypes()
            .Where(type => typeof(ILootTableDefinition).IsAssignableFrom(type) && type is { IsClass: true, IsAbstract: false });
        foreach (var type in definitions)
        {
            var tables = ((ILootTableDefinition)Activator.CreateInstance(type)!).BuildLootTables();
            foreach (var item in tables.Values.SelectMany(table => table))
            {
                if (gear.ContainsKey(item.Resref) || item.Resref.StartsWith("am_", StringComparison.OrdinalIgnoreCase))
                    continue;
                var path = Path.Combine(Root, "Module", "uti", item.Resref.ToLowerInvariant() + ".uti.json");
                if (!File.Exists(path))
                    continue;
                var blueprint = ReadBlueprint(path);
                if (ModuleNames.ContainsKey(Number(blueprint, "BaseItem")))
                    gear[item.Resref] = blueprint;
            }
        }

        return gear;
    }

    /// <summary>
    /// The looted gear, and every piece of gear a store sells or the slicing rewards name. Gear asks for a skill rank.
    /// A droid-only piece counts only when looted: the droid gear of a store copies humanoid gear, whose form the store
    /// sells beside it (AstromechStoreContentTests).
    /// </summary>
    private static Dictionary<string, JsonElement> ReadFoundGear()
    {
        var gear = new Dictionary<string, JsonElement>(LootedGear.Value, StringComparer.OrdinalIgnoreCase);
        var named = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "utm"), "*.utm.json"))
        {
            using var store = JsonDocument.Parse(File.ReadAllText(file, Encoding.Latin1));
            named.AddRange(store.RootElement.GetProperty("StoreList").GetProperty("value").EnumerateArray()
                .Where(tab => tab.TryGetProperty("ItemList", out _))
                .SelectMany(tab => tab.GetProperty("ItemList").GetProperty("value").EnumerateArray())
                .Select(item => Text(item, "InventoryRes")));
        }

        foreach (var file in new[] { "SlicingRewardCatalog.cs", "SlicingReward.cs" })
        {
            var source = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "SlicingService", file));
            named.AddRange(Regex.Matches(source, "\"([a-z0-9_]{2,16})\"").Select(match => match.Groups[1].Value));
        }

        foreach (var resref in named.Select(resref => resref.ToLowerInvariant()).Distinct())
        {
            var path = Path.Combine(Root, "Module", "uti", resref + ".uti.json");
            if (gear.ContainsKey(resref) || resref.StartsWith("am_") || !File.Exists(path))
                continue;
            var blueprint = ReadBlueprint(path);
            var properties = Properties(blueprint);
            if (ModuleNames.ContainsKey(Number(blueprint, "BaseItem")) &&
                properties.Any(property => property.Number == RequiresSkillProperty) &&
                !properties.Any(property => property is { Number: RacialProperty, Subtype: DroidRace }))
                gear[resref] = blueprint;
        }

        return gear;
    }

    private static List<string> LocalVariables(JsonElement blueprint) =>
        blueprint.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Select(variable => variable.GetProperty("Name").GetProperty("value").GetString()!)
            .ToList();

    private record Property(int Number, int Subtype, int CostTable, int CostValue, int Param1, int Param1Value);

    private static List<Property> Properties(JsonElement blueprint)
    {
        int Read(JsonElement property, string name) => property.GetProperty(name).GetProperty("value").GetInt32();
        return blueprint.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
            .Where(property => property.TryGetProperty("PropertyName", out _))
            .Select(property => new Property(Read(property, "PropertyName"), Read(property, "Subtype"), Read(property, "CostTable"),
                Read(property, "CostValue"), Read(property, "Param1"), Read(property, "Param1Value")))
            .ToList();
    }

    private static int Number(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetInt32();

    private static string Text(JsonElement element, string name) => element.GetProperty(name).GetProperty("value").GetString()!;

    private static JsonElement ReadBlueprint(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path, Encoding.Latin1));
        return document.RootElement.Clone();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
