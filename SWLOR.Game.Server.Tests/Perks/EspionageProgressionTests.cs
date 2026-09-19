using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EspionageRecipeDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Perks;

public class EspionageProgressionTests
{
    [TestCase(PerkType.Stealth)]
    [TestCase(PerkType.Poisoncraft)]
    [TestCase(PerkType.Trapcraft)]
    [TestCase(PerkType.Slicing)]
    public void EveryStarterPerk_IsAvailableAtZeroWithoutAnotherProfession(PerkType profession)
    {
        var level = BuildPerk(profession).PerkLevels[1];
        Assert.That(level.Requirements.OfType<PerkRequirementSkill>(), Is.Empty);
        Assert.That(level.Requirements.OfType<PerkRequirementMustHavePerk>(), Is.Empty);
    }

    [TestCase(PerkType.Poisoncraft)]
    [TestCase(PerkType.Trapcraft)]
    [TestCase(PerkType.Slicing)]
    public void PracticeLimits_MatchActualNextPerkRequirements(PerkType profession)
    {
        var perk = BuildPerk(profession);
        foreach (var (tier, level) in perk.PerkLevels.Where(x => x.Key > 1))
        {
            var requirement = level.Requirements.OfType<PerkRequirementSkill>()
                .Single(x => x.Type == SkillType.Espionage);
            Assert.That(EspionageProgression.GetPracticeRankLimit(profession, tier - 1),
                Is.EqualTo(requirement.RequiredRank));
        }

        if (profession == PerkType.Trapcraft)
        {
            var masterRank = BuildPerk(PerkType.MasterSaboteur).PerkLevels[1].Requirements
                .OfType<PerkRequirementSkill>().Single().RequiredRank;
            Assert.That(EspionageProgression.GetPracticeRankLimit(profession, 4), Is.EqualTo(masterRank));
        }
    }

    [TestCase(PerkType.Poisoncraft)]
    [TestCase(PerkType.Trapcraft)]
    public void CraftingAlone_HasAnObtainableRecipeAtEveryRankToFifty(PerkType profession)
    {
        var recipes = profession == PerkType.Poisoncraft
            ? new VenomCoatingRecipes().BuildRecipes().Values.ToArray()
            : new TrapKitRecipes().BuildRecipes().Values.ToArray();

        for (var rank = 0; rank < 50; rank++)
        {
            var available = recipes.Where((recipe, index) =>
                EspionageProgression.GetRequiredRank(profession, index + 1) <= rank &&
                Craft.GetRequiredSkillRankForRecipe(recipe) <= rank &&
                Craft.GetBaseRecipeXP(recipe, rank) > 0).ToArray();

            Assert.That(available, Has.Length.EqualTo(1), $"{profession} rank {rank} needs a current training recipe.");
            Assert.That(available[0].Requirements, Has.Count.EqualTo(1), "No dropped schematic or unrelated perk may block the core path.");
            Assert.That(available[0].Requirements[0], Is.TypeOf<RecipePerkRequirement>());
        }

        Assert.That(recipes.All(recipe => Craft.GetBaseRecipeXP(recipe, 50) == 0), Is.True);
    }

    [TestCase(PerkType.Slicing)]
    [TestCase(PerkType.Trapcraft)]
    public void TierActivities_TrainContinuouslyAndStopAtTheirNextUnlock(PerkType profession)
    {
        for (var rank = 0; rank < 50; rank++)
        {
            var xp = Enumerable.Range(1, 5).Select(tier => EspionageProgression.CalculateXP(profession, tier, rank)).ToArray();
            Assert.That(xp.Count(value => value > 0), Is.EqualTo(1), $"{profession} rank {rank}");
        }

        for (var tier = 1; tier <= 5; tier++)
        {
            Assert.That(EspionageProgression.CalculateXP(profession, tier, 50), Is.Zero);
            Assert.That(EspionageProgression.CalculateXP(profession, tier,
                EspionageProgression.GetPracticeRankLimit(profession, tier)), Is.Zero);
        }
    }

    [TestCase(0, false, 1, false)]
    [TestCase(1, false, 1, true)]
    [TestCase(1, false, 2, false)]
    [TestCase(4, false, 4, true)]
    [TestCase(4, false, 5, false)]
    [TestCase(4, true, 5, true)]
    [TestCase(4, true, 6, false)]
    [TestCase(4, true, 0, false)]
    public void TrapAccess_UsesTheSameTierRulesForPlacementDetectionAndDisarm(int trapcraft, bool master, int tier, bool allowed)
    {
        Assert.That(EspionageProgression.CanUseTrapTier(trapcraft, master, tier), Is.EqualTo(allowed));
    }

    private static PerkDetail BuildPerk(PerkType profession)
    {
        var definition = new EspionagePerkDefinition();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(EspionagePerkDefinition).GetMethod(profession.ToString(), flags)!.Invoke(definition, null);
        var builder = typeof(EspionagePerkDefinition).GetField("_builder", flags)!.GetValue(definition);
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder).GetField("_perks", flags)!.GetValue(builder)!;
        return perks[profession];
    }
}
