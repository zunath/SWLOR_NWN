using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Covers the Gathering styles: the stat adjustments each carries, the words that describe them, the perk and status effect each maps
/// to, and that the status effect carries exactly the style's stats and is a crafting style that does not outlast a logout.
/// </summary>
public class GatheringStyleTests
{
    private const string StripMiningProspecting =
        "A Strike takes 1.5 seconds less and costs 10 more Stability, but a Survey and an Extract each take 1 second more.";

    private const string CarefulHandsSearch =
        "A Search finds 1 point less often, and a Dig Deeper find favours the site's rare entries 3 more.";

    private static readonly Dictionary<StatType, int> StripMiningStats = new()
    {
        [StatType.ProspectingStrikeTimeReduction] = ProspectingPerkRules.StripMiningStrikeTimeTenths,
        [StatType.ProspectingSeamWorkTimeReduction] = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths,
        [StatType.ProspectingStrikeStabilityReduction] = -ProspectingPerkRules.StripMiningStrikeStability
    };

    private static readonly Dictionary<StatType, int> CarefulHandsStats = new()
    {
        [StatType.SearchChancePoints] = SearchPerkRules.CarefulHandsSearchChancePoints,
        [StatType.SearchDigRareWeight] = SearchPerkRules.CarefulHandsDigRareWeight
    };

    /// <summary>The adjustments an effect sets, without the zero entries every stat group starts with.</summary>
    private static Dictionary<StatType, int> Granted(StatusEffectBase effect) =>
        effect.StatGroup.Stats.Where(pair => pair.Value != 0).ToDictionary(pair => pair.Key, pair => pair.Value);

    private static Dictionary<StatType, int> StatsOf(GatheringStyle style) =>
        GatheringStyles.StatsOf(style).ToDictionary(pair => pair.Key, pair => pair.Value);

    private static StatusEffectBase NewStatusEffect(GatheringStyle style) =>
        (StatusEffectBase)Activator.CreateInstance(GatheringStyles.StatusEffectOf(style));

    // The styles and their stats.

    [Test]
    public void BothStylesAreListedInTheOrderTheWindowsShowThem_AndNoneIsNotAStyle()
    {
        GatheringStyles.All.Should().Equal(GatheringStyle.StripMining, GatheringStyle.CarefulHands);
        GatheringStyles.All.Should().NotContain(GatheringStyle.None);
    }

    [Test]
    public void StripMiningCarriesAQuickerStrikeThatCostsMoreStability_AndSlowerSeamWork()
    {
        StatsOf(GatheringStyle.StripMining).Should().BeEquivalentTo(StripMiningStats);
        StripMiningStats[StatType.ProspectingStrikeTimeReduction].Should().BePositive();
        StripMiningStats[StatType.ProspectingSeamWorkTimeReduction].Should().BeNegative();
        StripMiningStats[StatType.ProspectingStrikeStabilityReduction].Should().BeNegative();
    }

    [Test]
    public void CarefulHandsCarriesFewerSearchFindsAndRarerDigs()
    {
        StatsOf(GatheringStyle.CarefulHands).Should().BeEquivalentTo(CarefulHandsStats);
        CarefulHandsStats[StatType.SearchChancePoints].Should().BeNegative();
        CarefulHandsStats[StatType.SearchDigRareWeight].Should().BePositive();
    }

    [TestCase(GatheringStyle.None)]
    [TestCase((GatheringStyle)99)]
    public void ANonStyleCarriesNoStats(GatheringStyle style)
    {
        StatsOf(style).Should().BeEmpty();
    }

    [Test]
    public void StripMiningReadsBackAsTheProspectingEffectsItDescribes()
    {
        var stats = GatheringStyles.StatsOf(GatheringStyle.StripMining);

        var effects = ProspectingEffects.FromStats(stat => stats.GetValueOrDefault(stat));

        effects.Should().Be(ProspectingEffects.None with
        {
            StrikeStabilityReduction = -ProspectingPerkRules.StripMiningStrikeStability,
            StrikeTimeReductionTenths = ProspectingPerkRules.StripMiningStrikeTimeTenths,
            SeamWorkTimeReductionTenths = -ProspectingPerkRules.StripMiningSeamWorkTimeTenths
        });
    }

    [Test]
    public void CarefulHandsChangesNothingInProspecting()
    {
        var stats = GatheringStyles.StatsOf(GatheringStyle.CarefulHands);

        ProspectingEffects.FromStats(stat => stats.GetValueOrDefault(stat)).Should().Be(ProspectingEffects.None);
    }

    // The words.

    [TestCase(GatheringStyle.StripMining, "Strip Mining")]
    [TestCase(GatheringStyle.CarefulHands, "Careful Hands")]
    [TestCase(GatheringStyle.None, "None")]
    [TestCase((GatheringStyle)99, "None")]
    public void AStyleIsNamedAsThePlayerKnowsIt(GatheringStyle style, string name)
    {
        GatheringStyles.Name(style).Should().Be(name);
    }

    [Test]
    public void StripMiningSaysWhatItDoesInProspecting_AndNothingInSearch()
    {
        GatheringStyles.ProspectingDescription(GatheringStyle.StripMining).Should().Be(StripMiningProspecting);
        GatheringStyles.SearchDescription(GatheringStyle.StripMining).Should().BeEmpty();
    }

    [Test]
    public void CarefulHandsSaysWhatItDoesInSearch_AndNothingInProspecting()
    {
        GatheringStyles.ProspectingDescription(GatheringStyle.CarefulHands).Should().BeEmpty();
        GatheringStyles.SearchDescription(GatheringStyle.CarefulHands).Should().Be(CarefulHandsSearch);
    }

    [TestCase(GatheringStyle.None)]
    [TestCase((GatheringStyle)99)]
    public void ANonStyleHasNoWords(GatheringStyle style)
    {
        GatheringStyles.ProspectingDescription(style).Should().BeEmpty();
        GatheringStyles.SearchDescription(style).Should().BeEmpty();
        GatheringStyles.Description(style).Should().BeEmpty();
    }

    [Test]
    public void TheDescriptionNamesEachActivityTheStyleActsIn()
    {
        GatheringStyles.Description(GatheringStyle.StripMining).Should().Be("In Prospecting: " + StripMiningProspecting);
        GatheringStyles.Description(GatheringStyle.CarefulHands).Should().Be("In Search: " + CarefulHandsSearch);
    }

    [Test]
    public void EveryDescriptionIsAscii()
    {
        foreach (var style in GatheringStyles.All)
        {
            foreach (var text in new[]
                     {
                         GatheringStyles.Name(style), GatheringStyles.ProspectingDescription(style), GatheringStyles.SearchDescription(style),
                         GatheringStyles.Description(style)
                     })
            {
                text.All(character => character < 128).Should().BeTrue($"{style}: {text}");
            }
        }
    }

    // The perk and the status effect each style maps to.

    [Test]
    public void EachStyleMapsToItsPerkAndItsStatusEffect()
    {
        GatheringStyles.PerkOf(GatheringStyle.StripMining).Should().Be(PerkType.StripMiningStyle);
        GatheringStyles.PerkOf(GatheringStyle.CarefulHands).Should().Be(PerkType.CarefulHandsStyle);
        GatheringStyles.PerkOf(GatheringStyle.None).Should().Be(PerkType.Invalid);
        GatheringStyles.StatusEffectOf(GatheringStyle.StripMining).Should().Be(typeof(StripMiningStatusEffect));
        GatheringStyles.StatusEffectOf(GatheringStyle.CarefulHands).Should().Be(typeof(CarefulHandsStatusEffect));
        GatheringStyles.StatusEffectOf(GatheringStyle.None).Should().BeNull();
    }

    [Test]
    public void NoTwoStylesShareAPerkOrAStatusEffect()
    {
        GatheringStyles.All.Select(GatheringStyles.PerkOf).Should().OnlyHaveUniqueItems();
        GatheringStyles.All.Select(GatheringStyles.StatusEffectOf).Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void StripMiningStatusEffectCarriesExactlyTheStylesStats()
    {
        Granted(new StripMiningStatusEffect()).Should().BeEquivalentTo(StripMiningStats);
    }

    [Test]
    public void CarefulHandsStatusEffectCarriesExactlyTheStylesStats()
    {
        Granted(new CarefulHandsStatusEffect()).Should().BeEquivalentTo(CarefulHandsStats);
    }

    [TestCase(GatheringStyle.StripMining)]
    [TestCase(GatheringStyle.CarefulHands)]
    public void AStatusEffectCarriesNothingBesideTheStylesStats(GatheringStyle style)
    {
        var effect = NewStatusEffect(style);

        Granted(effect).Should().BeEquivalentTo(StatsOf(style).Where(pair => pair.Value != 0).ToDictionary(pair => pair.Key, pair => pair.Value));
        effect.StatGroup.Abilities.Values.Should().OnlyContain(value => value == 0);
        effect.StatGroup.Resists.Values.Should().OnlyContain(value => value == 0);
        effect.StatGroup.CraftSkillBonuses.Values.SelectMany(bonuses => bonuses.Values).Should().OnlyContain(value => value == 0);
    }

    [TestCase(GatheringStyle.StripMining)]
    [TestCase(GatheringStyle.CarefulHands)]
    public void AStyleStatusEffectIsAPassiveCraftingStyleBuffThatDoesNotOutlastALogout(GatheringStyle style)
    {
        var effect = NewStatusEffect(style);

        effect.Name.Should().Be(GatheringStyles.Name(style));
        effect.SourceType.Should().Be(StatusEffectSourceType.CraftingStyle);
        StatusEffectSelectors.IsCraftingStyle(effect).Should().BeTrue();
        effect.PersistsOnLogout.Should().BeFalse();
        effect.ActivationType.Should().Be(StatusEffectActivationType.Passive);
        (effect.Categories & StatusEffectCategory.Buff).Should().Be(StatusEffectCategory.Buff);
        effect.SendsApplicationMessage.Should().BeFalse();
        effect.SendsWornOffMessage.Should().BeFalse();
    }

    [Test]
    public void TheTwoStatusEffectsHaveTheirOwnIcons()
    {
        var stripMining = new StripMiningStatusEffect();
        var carefulHands = new CarefulHandsStatusEffect();

        stripMining.Icon.Should().NotBe(carefulHands.Icon);
        stripMining.Icon.Should().Be(EffectIconType.StripMiningStatusEffect);
        carefulHands.Icon.Should().Be(EffectIconType.CarefulHandsStatusEffect);
        stripMining.Name.Should().Be("Strip Mining");
        carefulHands.Name.Should().Be("Careful Hands");
    }
}
