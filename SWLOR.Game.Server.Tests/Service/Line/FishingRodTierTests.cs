using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.FishingLocationDefinition;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>Covers the rod tiers the Line reads and the rarity of the fish it fights.</summary>
public class FishingRodTierTests
{
    [Test]
    public void EveryRod_HasATierFromZeroToFour()
    {
        foreach (var rod in Enum.GetValues<FishingRodType>().Where(rod => rod != FishingRodType.Invalid))
        {
            FishingRodTier.All.Should().ContainKey(rod);
            FishingRodTier.Tier(rod).Should().BeInRange(0, FishingRodTier.HighestTier);
            FishingRodTier.RequiredRank(rod).Should().Be(FishingRodTier.Tier(rod) * FishingRodTier.RanksPerTier);
        }
    }

    [Test]
    public void ARodWithoutATier_IsRefused()
    {
        var act = () => FishingRodTier.Tier(FishingRodType.Invalid);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void EveryRodsTier_IsTheAgricultureRankItsItemRequires()
    {
        foreach (var (rod, rank) in FishingRodTier.All)
        {
            var resref = rod.GetAttribute<FishingRodType, FishingRodAttribute>().Resref;
            using var item = ReadItem(resref);

            var requirements = item.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
                .Where(property => property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)ItemPropertyType.RequiresSkill)
                .Where(property => property.GetProperty("Subtype").GetProperty("value").GetInt32() == (int)SkillType.Agriculture)
                .Select(property => property.GetProperty("CostValue").GetProperty("value").GetInt32())
                .ToList();

            requirements.Should().Equal(new[] { rank }, $"{rod} ({resref}) asks for Agriculture rank {rank}");
        }
    }

    [Test]
    public void TheSixSpecialFish_AreTheOnlyRareOnes_AndAreTheOnesTheSpecialLocationSpawns()
    {
        var rare = Enum.GetValues<FishType>()
            .Where(fish => fish != FishType.Invalid)
            .Where(fish => fish.GetAttribute<FishType, FishAttribute>().Rarity == FishRarity.Rare)
            .ToList();

        rare.Should().BeEquivalentTo(new[]
        {
            FishType.Mercanbaligi, FishType.Nashmau, FishType.Rhinochimera, FishType.Mhaura, FishType.Zitah, FishType.Alzabi
        });

        var spawned = new SpecialFishingLocationDefinition().Build().Values
            .SelectMany(location => location.FishMap.Values)
            .SelectMany(fish => fish)
            .Select(fish => fish.Type)
            .Distinct()
            .ToList();
        spawned.Should().BeEquivalentTo(rare);
    }

    private static JsonDocument ReadItem(string resref)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;

        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "Module", "uti", $"{resref}.uti.json")));
    }
}
