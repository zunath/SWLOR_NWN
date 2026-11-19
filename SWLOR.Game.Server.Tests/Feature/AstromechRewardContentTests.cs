using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AstromechService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// An astromech equips only astromech modules and tools, so every quest reward and guild store weapon it could not use
/// has an astromech form with the same stats (tools/GenerateAstromechRewards.py). These tests fail when a humanoid item
/// changes, or a new humanoid reward is added, until the generator is rerun or extended.
/// </summary>
public class AstromechRewardContentTests
{
    private const int AstromechRace = 170;
    private const int RacialProperty = 64;
    private const int RequiresSkillProperty = 131;
    private const int ArmorSkill = 6, StaffSkill = 44, RifleSkill = 46, ChassisSkill = 61, ArmamentsSkill = 63;
    private const int StaffBaseItem = 50, RifleBaseItem = 7, ShockProdBaseItem = 545, BlasterMountBaseItem = 546;

    private static readonly string Root = FindRepositoryRoot();

    // Humanoid resref -> astromech resref for the reward and guild items (the crafted ladders have their own tests).
    private static readonly Dictionary<string, string> RewardTwins = new()
    {
        ["tidecall_pendant"] = "am_q_tidecall",
        ["reefwarden_band"] = "am_q_reefwarden",
        ["rancorhide_belt"] = "am_q_rancorhide",
        ["kwi_spirit_charm"] = "am_q_kwispirit",
        ["hunter_vambrace"] = "am_q_hunter",
        ["fdybreak_bracer"] = "am_q_fdybreak",
        ["chieftain_torc"] = "am_q_chieftain",
        ["caravan_sash"] = "am_q_caravan",
        ["canopy_band"] = "am_q_canopy",
        ["broodguard_ring"] = "am_q_broodguard",
        ["apprentice_dark_"] = "am_q_darkside",
        ["cap_staff"] = "am_w_prod_cap",
        ["cap_rifle"] = "am_w_blst_cap",
        ["h_staff_1"] = "am_w_prod_h1", ["h_rifle_1"] = "am_w_blst_h1",
        ["h_staff_2"] = "am_w_prod_h2", ["h_rifle_2"] = "am_w_blst_h2",
        ["h_staff_3"] = "am_w_prod_h3", ["h_rifle_3"] = "am_w_blst_h3",
        ["h_staff_4"] = "am_w_prod_h4", ["h_rifle_4"] = "am_w_blst_h4",
        ["h_staff_5"] = "am_w_prod_h5", ["h_rifle_5"] = "am_w_blst_h5",
    };

    public static IEnumerable<TestCaseData> AllTwins() =>
        RewardTwins.Select(pair => new TestCaseData(pair.Key, pair.Value).SetArgDisplayNames(pair.Value));

    [Test]
    public void AnAstromechReceivesTheAstromechForm_AndEveryoneElseTheItemItself()
    {
        foreach (var (source, twin) in RewardTwins)
        {
            AstromechItemTwins.Resolve(source, BodyFrame.Astromech).Should().Be(twin);
            AstromechItemTwins.Resolve(source, BodyFrame.Humanoid).Should().Be(source);
            AstromechItemTwins.HasTwin(source).Should().BeTrue();
        }

        // The crafted ladders are in the same table, so a quest that hands out a crafted piece is covered too.
        AstromechItemTwins.Resolve("b_staff", BodyFrame.Astromech).Should().Be("am_w_prod_01");
        AstromechItemTwins.Resolve("b_rifle", BodyFrame.Astromech).Should().Be("am_w_blst_01");
        AstromechItemTwins.Resolve("bm_necklace", BodyFrame.Astromech).Should().Be("am_h_logc_01");

        // An item with no astromech form is given as it is; an astromech can still carry, trade and sell it.
        AstromechItemTwins.Resolve("cap_knife", BodyFrame.Astromech).Should().Be("cap_knife");
        AstromechItemTwins.Resolve("stim_pack", BodyFrame.Astromech).Should().Be("stim_pack");
        AstromechItemTwins.Resolve(string.Empty, BodyFrame.Astromech).Should().BeEmpty();
        AstromechItemTwins.HasTwin("am_q_hunter").Should().BeFalse();
    }

    [TestCaseSource(nameof(AllTwins))]
    public void TheAstromechFormCopiesItsSource(string source, string twin)
    {
        twin.Length.Should().BeLessThanOrEqualTo(16);
        var from = ReadBlueprint(source);
        var to = ReadBlueprint(twin);

        to.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(twin);
        to.GetProperty("Tag").GetProperty("value").GetString().Should().Be(twin);
        var name = to.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!;
        Encoding.UTF8.GetByteCount(name).Should().Be(name.Length, "item names are ASCII");
        name.Should().NotBeNullOrWhiteSpace();

        var sourceBase = from.GetProperty("BaseItem").GetProperty("value").GetInt32();
        var expectedBase = sourceBase switch
        {
            StaffBaseItem => ShockProdBaseItem,
            RifleBaseItem => BlasterMountBaseItem,
            _ => sourceBase
        };
        to.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(expectedBase);

        // A plating twin keeps no part models of the humanoid piece: a leftover number can name a chassis part.
        to.EnumerateObject().Where(field => field.Name.StartsWith("ArmorPart_") || field.Name.StartsWith("xArmorPart_"))
            .Select(field => field.Value.GetProperty("value").GetInt32())
            .Should().OnlyContain(part => part == 0, twin);

        var expected = Properties(from).Select(property => property.Number == RequiresSkillProperty
            ? property with
            {
                Subtype = property.Subtype switch
                {
                    ArmorSkill => ChassisSkill,
                    StaffSkill => ArmamentsSkill,
                    RifleSkill => ArmamentsSkill,
                    _ => property.Subtype
                }
            }
            : property).ToList();
        expected.Add(new Property(RacialProperty, AstromechRace, 0, 0, 255, 0));
        Properties(to).Should().Equal(expected);
        Properties(to).Count(property => property.Number == RequiresSkillProperty &&
                                         (property.Subtype == ChassisSkill || property.Subtype == ArmamentsSkill))
            .Should().Be(1, "the astromech form asks for the astromech skill");

        // Everything but the name, the identifiers, the base item, the properties and the part models is the source's.
        foreach (var field in from.EnumerateObject())
        {
            if (field.Name is "LocalizedName" or "Tag" or "TemplateResRef" or "BaseItem" or "PropertiesList")
                continue;
            if (field.Name.StartsWith("ArmorPart_") || field.Name.StartsWith("xArmorPart_"))
                continue;
            to.GetProperty(field.Name).GetRawText().Should().Be(field.Value.GetRawText(), field.Name);
        }
    }

    [Test]
    public void EveryQuestRewardAnAstromechCouldNotUse_HasAnAstromechForm()
    {
        var quests = Path.Combine(Root, "SWLOR.Game.Server", "Feature", "QuestDefinition");
        var rewards = Directory.GetFiles(quests, "*.cs")
            .SelectMany(file => Regex.Matches(File.ReadAllText(file), "\\.AddItemReward\\(\"([^\"]+)\"").Select(m => m.Groups[1].Value))
            .Distinct()
            .ToList();
        rewards.Should().NotBeEmpty();

        var missing = new List<string>();
        foreach (var resref in rewards)
        {
            var path = Path.Combine(Root, "Module", "uti", resref + ".uti.json");
            if (!File.Exists(path))
                continue;

            // A piece that asks for Armor, Staff or Rifle is one an astromech trains the mirror of.
            var mirrored = Properties(ReadBlueprint(resref)).Any(property =>
                property.Number == RequiresSkillProperty && property.Subtype is ArmorSkill or StaffSkill or RifleSkill);
            if (mirrored && !AstromechItemTwins.HasTwin(resref))
                missing.Add(resref);
        }

        missing.Should().BeEmpty("add the item to tools/GenerateAstromechRewards.py");
    }

    [Test]
    public void QuestItemRewards_GiveAndNameTheAstromechForm()
    {
        var rewards = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "QuestService", "QuestRewards.cs"));
        rewards.Should().Contain("CreateItemOnObject(GetResref(player), player, _quantity);");
        rewards.Should().Contain("return AstromechItemTwins.Resolve(_resref, player);");
        rewards.Should().NotContain("CreateItemOnObject(_resref, player, _quantity);");

        var dialog = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "DialogDefinition", "QuestRewardSelectionDialog.cs"));
        dialog.Should().Contain("reward.GetMenuName(Player)");
    }

    [Test]
    public void TheHuntersGuildStoresSellTheHunterTools()
    {
        for (var rank = 1; rank <= 5; rank++)
        {
            var store = File.ReadAllText(Path.Combine(Root, "Module", "utm", $"gp_hunt_{rank}.utm.json"), Encoding.Latin1);
            foreach (var resref in new[] { $"h_staff_{rank}", $"h_rifle_{rank}", $"am_w_prod_h{rank}", $"am_w_blst_h{rank}" })
                Regex.Matches(store, $"\"value\": \"{resref}\"").Count.Should().Be(1, $"{resref} in gp_hunt_{rank}");
        }
    }

    [Test]
    public void EveryPlacedCopyOfAStore_SellsTheAstromechGearItsBlueprintLists()
    {
        // The game opens the copy of a store placed in an area, which holds its own item list. Gear added only to the
        // store blueprint never reaches players, so every placed copy must list what the blueprint lists.
        var blueprints = new Dictionary<string, List<string>>();
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "utm"), "*.utm.json"))
        {
            var text = File.ReadAllText(file, Encoding.Latin1);
            var gear = Regex.Matches(text, "\"value\": \"(am_[a-z0-9_]+)\"").Select(match => match.Groups[1].Value).Distinct().ToList();
            if (gear.Count > 0)
                blueprints[Path.GetFileName(file).Split('.')[0]] = gear;
        }

        blueprints.Keys.Should().Contain(new[] { "cz220_merchant", "gp_hunt_1", "gp_hunt_5" });

        var placed = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "git"), "*.git.json"))
        {
            var text = File.ReadAllText(file, Encoding.Latin1);
            if (!blueprints.Keys.Any(tag => text.Contains($"\"{tag}\"")))
                continue;

            using var area = JsonDocument.Parse(text);
            if (!area.RootElement.TryGetProperty("StoreList", out var stores))
                continue;

            foreach (var store in stores.GetProperty("value").EnumerateArray())
            {
                var tag = store.GetProperty("Tag").GetProperty("value").GetString()!;
                if (!blueprints.TryGetValue(tag, out var gear))
                    continue;

                placed++;
                var sold = store.GetProperty("StoreList").GetProperty("value").EnumerateArray()
                    .Where(tab => tab.TryGetProperty("ItemList", out _))
                    .SelectMany(tab => tab.GetProperty("ItemList").GetProperty("value").EnumerateArray())
                    .Select(item => item.GetProperty("TemplateResRef").GetProperty("value").GetString())
                    .ToList();
                foreach (var resref in gear)
                    sold.Count(item => item == resref).Should().Be(1, $"{resref} in the {tag} placed in {Path.GetFileName(file)}");
            }
        }

        placed.Should().BeGreaterThanOrEqualTo(26, "the CZ-220 merchant and five Hunters Guild offices are placed");
    }

    [Test]
    public void TheGeneratorIsUpToDate()
    {
        var start = new ProcessStartInfo("python", "-B tools/GenerateAstromechRewards.py --check")
        {
            WorkingDirectory = Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.Should().Be(0, output);
    }

    private record Property(int Number, int Subtype, int CostTable, int CostValue, int Param1, int Param1Value);

    private static List<Property> Properties(JsonElement blueprint)
    {
        int Read(JsonElement property, string name) => property.GetProperty(name).GetProperty("value").GetInt32();
        return blueprint.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
            .Select(property => new Property(Read(property, "PropertyName"), Read(property, "Subtype"), Read(property, "CostTable"),
                Read(property, "CostValue"), Read(property, "Param1"), Read(property, "Param1Value")))
            .ToList();
    }

    private static JsonElement ReadBlueprint(string resref)
    {
        var path = Path.Combine(Root, "Module", "uti", resref + ".uti.json");
        File.Exists(path).Should().BeTrue(path);
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
