using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Astromech gear in stores and slicing rewards (tools/GenerateAstromechRewards.py). A store that sells a piece of
/// gear sells an astromech piece with the same stats beside it, in its blueprint and in every copy placed in an area;
/// a slicing cache hands an astromech the astromech form of the piece it rolled. These tests fail when a store or the
/// slicing rewards gain a piece until the generator is rerun.
/// </summary>
public class AstromechStoreContentTests
{
    private const int AstromechRace = 170;
    private const int RacialProperty = 64, RequiresSkillProperty = 131;
    private const int ArmorSkill = 6, StaffSkill = 44, RifleSkill = 46, ChassisSkill = 61, ArmamentsSkill = 63;
    private const int StaffBaseItem = 50, RifleBaseItem = 7, ShockProdBaseItem = 545, BlasterMountBaseItem = 546;

    // The base items that have an astromech form: the eight module slots, and the two weapons with an astromech tool.
    private static readonly HashSet<int> GearBaseItems = new() { 17, 16, 36, 78, 26, 21, 19, 52, 80, StaffBaseItem, RifleBaseItem };

    private static readonly string Root = FindRepositoryRoot();
    private static readonly Dictionary<string, Stats?> StatsByResref = new(StringComparer.OrdinalIgnoreCase);

    [Test]
    public void EveryStoreBlueprint_SellsAnAstromechEqualOfEachPieceOfGear()
    {
        var problems = new List<string>();
        var stocked = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "utm"), "*.utm.json"))
        {
            using var store = JsonDocument.Parse(File.ReadAllText(file, Encoding.Latin1));
            var sold = Sold(store.RootElement);
            var missing = MissingEquals(sold);
            if (sold.Any(IsAstromechGear))
                stocked++;
            problems.AddRange(missing.Select(problem => $"{Path.GetFileName(file)}: {problem}"));
        }

        stocked.Should().BeGreaterThanOrEqualTo(28, "the general merchants and the guild stores sell gear");
        problems.Should().BeEmpty("rerun tools/GenerateAstromechRewards.py" + Environment.NewLine +
                                  string.Join(Environment.NewLine, problems.Take(40)));
    }

    [Test]
    public void EveryStorePlacedInAnArea_SellsAnAstromechEqualOfEachPieceOfGear()
    {
        // The game opens the copy of a store placed in an area, which holds its own item list.
        var problems = new List<string>();
        var stocked = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "git"), "*.git.json"))
        {
            var text = File.ReadAllText(file, Encoding.Latin1);
            if (!text.Contains("\"InventoryRes\"") && !text.Contains("\"Repos_PosX\""))
                continue;

            using var area = JsonDocument.Parse(text);
            if (!area.RootElement.TryGetProperty("StoreList", out var stores))
                continue;

            foreach (var store in stores.GetProperty("value").EnumerateArray())
            {
                var sold = Sold(store);
                if (sold.Any(IsAstromechGear))
                    stocked++;
                var name = (store.TryGetProperty("ResRef", out var blueprint) ? blueprint : store.GetProperty("Tag"))
                    .GetProperty("value").GetString();
                problems.AddRange(MissingEquals(sold).Select(problem => $"{name} in {Path.GetFileName(file)}: {problem}"));
            }
        }

        stocked.Should().BeGreaterThanOrEqualTo(261, "every placed general merchant and guild store sells gear");
        problems.Should().BeEmpty("rerun tools/GenerateAstromechRewards.py" + Environment.NewLine +
                                  string.Join(Environment.NewLine, problems.Take(40)));
    }

    [Test]
    public void ASlicingCacheHandsAnAstromechTheFormOfEachPieceOfGear()
    {
        var gear = SlicingRewards().Where(reward => GearStats(reward.Resref) != null).ToList();
        gear.Select(reward => reward.Resref).Distinct().Count().Should().BeGreaterThanOrEqualTo(40);

        var problems = new List<string>();
        foreach (var reward in gear)
        {
            var (resref, quantity) = SlicingReward.ItemsFor(new[] { reward }, BodyFrame.Astromech).Single();
            if (quantity != reward.Quantity)
                problems.Add($"{reward.Resref}: the quantity changed");
            if (resref == reward.Resref)
                problems.Add($"{reward.Resref}: no astromech form");
            else if (!Equals(AstromechStats(resref), Wanted(GearStats(reward.Resref)!)))
                problems.Add($"{reward.Resref}: {resref} does not have its stats");
        }

        problems.Should().BeEmpty("rerun tools/GenerateAstromechRewards.py" + Environment.NewLine +
                                  string.Join(Environment.NewLine, problems.Distinct().Take(40)));
    }

    [Test]
    public void ASlicingCacheHandsEveryoneElseWhatItRolled()
    {
        var rewards = SlicingRewards();

        SlicingReward.ItemsFor(rewards, BodyFrame.Humanoid).Should().Equal(rewards.Select(reward => (reward.Resref, reward.Quantity)));

        // Supplies, tools, schematics and notes are no gear: an astromech receives them as they are.
        var plain = rewards.Where(reward => GearStats(reward.Resref) == null).ToList();
        plain.Should().NotBeEmpty();
        SlicingReward.ItemsFor(plain, BodyFrame.Astromech).Should().Equal(plain.Select(reward => (reward.Resref, reward.Quantity)));
    }

    [Test]
    public void TheSlicingCodeGrantsTheItemsOfTheSlicersBody()
    {
        var session = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "SlicingService", "SlicingSession.cs"));

        session.Should().Contain("SlicingReward.ItemsFor(rewards, Race.GetBodyFrame(session.Player))");
        session.Should().NotContain("CreateItemOnObject(reward.Resref");
    }

    /// <summary>
    /// Every reward a ground slicing success can hand out: the catalog, and the rolled supplies and older lockbox pieces.
    /// </summary>
    private static List<SlicingRewardEntry> SlicingRewards()
    {
        var rewards = new List<SlicingRewardEntry>(SlicingRewardCatalog.Entries);
        foreach (var source in new[] { SlicingSourceType.Lockbox, SlicingSourceType.Terminal })
        {
            for (var tier = 1; tier <= 5; tier++)
            {
                for (var categoryRoll = 0; categoryRoll < 10000; categoryRoll += 100)
                {
                    for (var itemRoll = 0; itemRoll < 1200 * 6; itemRoll += 50)
                        rewards.AddRange(SlicingReward.RollSuccess(source, tier, categoryRoll, itemRoll, itemRoll / 50));
                }
            }
        }

        return rewards.GroupBy(reward => (reward.Resref, reward.Quantity)).Select(group => group.First()).ToList();
    }

    private static bool IsAstromechGear(string resref) => resref.StartsWith("am_", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The pieces of gear among these sold items that have no astromech piece with their stats sold beside them.
    /// </summary>
    private static List<string> MissingEquals(List<string> sold)
    {
        var problems = new List<string>();
        var astromech = sold.Where(IsAstromechGear).ToList();
        foreach (var resref in astromech.GroupBy(resref => resref).Where(group => group.Count() > 1).Select(group => group.Key))
            problems.Add($"{resref} is sold more than once");

        var offered = astromech.Select(AstromechStats).Where(stats => stats != null).ToHashSet();
        foreach (var resref in sold.Where(resref => !IsAstromechGear(resref)).Distinct())
        {
            var stats = GearStats(resref);
            if (stats == null)
                continue;
            if (AstromechItemTwins.HasTwin(resref) && !astromech.Contains(AstromechItemTwins.Resolve(resref, BodyFrame.Astromech)))
                problems.Add($"{resref} is sold without its form {AstromechItemTwins.Resolve(resref, BodyFrame.Astromech)}");
            else if (!offered.Contains(Wanted(stats)))
                problems.Add($"{resref} is sold with no astromech piece of its stats");
        }

        return problems;
    }

    /// <summary>
    /// The resrefs a store sells. A blueprint names each item (InventoryRes); a placed store mostly holds it in full.
    /// </summary>
    private static List<string> Sold(JsonElement store) =>
        store.GetProperty("StoreList").GetProperty("value").EnumerateArray()
            .Where(tab => tab.TryGetProperty("ItemList", out _))
            .SelectMany(tab => tab.GetProperty("ItemList").GetProperty("value").EnumerateArray())
            .Select(item => item.TryGetProperty("TemplateResRef", out var resref) ? resref : item.GetProperty("InventoryRes"))
            .Select(resref => resref.GetProperty("value").GetString()!.ToLowerInvariant())
            .ToList();

    /// <summary>
    /// The stats of an astromech piece: its base item and every property but the race limit.
    /// </summary>
    private static Stats? AstromechStats(string resref)
    {
        var stats = ReadStats(resref);
        return stats is { Astromech: true } ? stats with { Astromech = false } : null;
    }

    /// <summary>
    /// The stats of a piece of gear an astromech cannot wear: an armor-slot piece, staff or rifle that asks for a rank.
    /// </summary>
    private static Stats? GearStats(string resref)
    {
        var stats = ReadStats(resref);
        return stats is { Astromech: false, AsksForRank: true } && GearBaseItems.Contains(stats.BaseItem) ? stats : null;
    }

    /// <summary>
    /// The stats of the astromech piece that equals this humanoid or droid piece.
    /// </summary>
    private static Stats Wanted(Stats gear)
    {
        var baseItem = gear.BaseItem switch
        {
            StaffBaseItem => ShockProdBaseItem,
            RifleBaseItem => BlasterMountBaseItem,
            _ => gear.BaseItem
        };
        var properties = gear.Properties.Split(';').Select(property =>
        {
            var numbers = property.Split(',');
            if (numbers[0] == RequiresSkillProperty.ToString())
            {
                numbers[1] = int.Parse(numbers[1]) switch
                {
                    ArmorSkill => ChassisSkill.ToString(),
                    StaffSkill or RifleSkill => ArmamentsSkill.ToString(),
                    _ => numbers[1]
                };
            }

            return string.Join(',', numbers);
        });
        return new Stats(baseItem, string.Join(';', properties.OrderBy(property => property, StringComparer.Ordinal)), true, false);
    }

    private record Stats(int BaseItem, string Properties, bool AsksForRank, bool Astromech);

    private static Stats? ReadStats(string resref)
    {
        if (StatsByResref.TryGetValue(resref, out var known))
            return known;

        var path = Path.Combine(Root, "Module", "uti", resref.ToLowerInvariant() + ".uti.json");
        Stats? stats = null;
        if (File.Exists(path))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path, Encoding.Latin1));
            var item = document.RootElement;
            int Read(JsonElement property, string name) => property.GetProperty(name).GetProperty("value").GetInt32();
            var properties = item.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
                .Where(property => property.TryGetProperty("PropertyName", out _))
                .Select(property => new[] { "PropertyName", "Subtype", "CostTable", "CostValue", "Param1", "Param1Value" }
                    .Select(name => Read(property, name)).ToArray())
                .ToList();
            var stat = properties.Where(property => property[0] != RacialProperty)
                .Select(property => string.Join(',', property))
                .OrderBy(property => property, StringComparer.Ordinal);
            stats = new Stats(
                item.GetProperty("BaseItem").GetProperty("value").GetInt32(),
                string.Join(';', stat),
                properties.Any(property => property[0] == RequiresSkillProperty),
                properties.Any(property => property[0] == RacialProperty && property[1] == AstromechRace));
        }

        StatsByResref[resref] = stats;
        return stats;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
