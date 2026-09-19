using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class DroidInstructionsTests
{
    private DroidPerkCacheScope _perks;

    [SetUp]
    public void LoadDefinitions() => _perks = new DroidPerkCacheScope();

    [TearDown]
    public void RestoreDefinitions() => _perks?.Dispose();

    [Test]
    public void MigrationPreservesLearnedRanksButEnforcesOneActiveRankAndSlotBudget()
    {
        var droid = new ConstructedDroid
        {
            LearnedPerks = null,
            ActivePerks = new List<DroidPerk>
            {
                null, new(PerkType.MedKit, 1), new(PerkType.MedKit, 2), new(PerkType.MedKit, 2),
                new(PerkType.Provoke, 2), new(PerkType.DualWield, 1), new((PerkType)999999, 1)
            }
        };

        DroidInstructions.Normalize(droid, 3, 3).Should().BeTrue();
        droid.ActivePerks.Should().ContainSingle().Which.Should().BeEquivalentTo(new DroidPerk(PerkType.MedKit, 2));
        droid.LearnedPerks.Should().BeEquivalentTo(new[]
        {
            new DroidPerk(PerkType.MedKit, 1), new DroidPerk(PerkType.MedKit, 2), new DroidPerk(PerkType.Provoke, 2)
        });
        DroidInstructions.Normalize(droid, 3, 3).Should().BeFalse("a retry must preserve the same learned and active configuration");
    }

    [Test]
    public void MigrationClampsRetainedInstructionsAndDeactivatesRanksAboveControllerTier()
    {
        var droid = new ConstructedDroid
        {
            ActivePerks = new List<DroidPerk> { new(PerkType.MedKit, 99), new(PerkType.Provoke, 1) }
        };
        DroidInstructions.Normalize(droid, 1, 20).Should().BeTrue();
        droid.LearnedPerks.Should().ContainEquivalentOf(new DroidPerk(PerkType.MedKit, 4));
        droid.ActivePerks.Should().ContainSingle().Which.Perk.Should().Be(PerkType.Provoke);
        DroidInstructions.Normalize(droid, 1, 20).Should().BeFalse();
    }

    [Test]
    public void RuntimeRejectsUnsupportedRanksAndHonorsExactCapacity()
    {
        var instructions = new[]
        {
            new DroidPerk(PerkType.MedKit, 99), new DroidPerk(PerkType.DualWield, 1),
            new DroidPerk(PerkType.Provoke, 2), new DroidPerk(PerkType.MedKit, 1)
        };
        var active = DroidInstructions.SelectActive(instructions, 2, 3);
        active.Should().HaveCount(2);
        DroidInstructions.GetSlots(active).Should().Be(3);
        DroidInstructions.SelectActive(instructions, 5, 0).Should().BeEmpty();
        DroidInstructions.TryGetLevel(new DroidPerk(PerkType.MedKit, 99), out _).Should().BeFalse();
        DroidInstructions.TryGetLevel(new DroidPerk(PerkType.DualWield, 1), out _).Should().BeFalse();
    }

    [Test]
    public void EveryInstructionRankGrantsARegisteredCompanionAbility()
    {
        Ability.CacheData();
        var abilities = Ability.GetAllAbilityDetails();
        var actions = new DefaultAIProfileDefinition().BuildProfiles()[AIProfileType.DroidCompanion]
            .Actions.Where(action => action.Type == AIActionType.Ability).Select(action => action.Feat).ToHashSet();

        foreach (var (perk, detail) in Perk.GetAllPerks())
        foreach (var (rank, level) in detail.PerkLevels.Where(entry => entry.Value.DroidAISlots > 0))
        {
            var context = $"{perk} rank {rank}";
            var feats = level.GrantedFeats.Where(abilities.ContainsKey).ToArray();
            feats.Should().NotBeEmpty(context);
            foreach (var feat in feats)
            {
                actions.Should().Contain(feat, context);
                abilities[feat].EffectiveLevelPerkType.Should().Be(perk, context);
                abilities[feat].AbilityLevel.Should().Be(rank, context);
                abilities[feat].Requirements.Should().NotContain(requirement => requirement is AbilityRequirementFP,
                    "droid instructions cannot spend Force points: " + context);
            }
        }
    }

    [Test]
    public void SuppressiveLineUsesRankCostsAndControllerTiersAndRetainsLowerLearnedRanks()
    {
        var instructions = new[] { new DroidPerk(PerkType.SuppressiveLine, 1), new DroidPerk(PerkType.SuppressiveLine, 2) };
        DroidInstructions.SelectActive(instructions, 1, 10).Should().BeEmpty();
        DroidInstructions.SelectActive(instructions, 2, 1).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(instructions[0]);
        DroidInstructions.SelectActive(instructions, 4, 2).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(instructions[1]);
        DroidInstructions.SelectActive(new[] { instructions[1] }, 4, 1).Should().BeEmpty();

        var droid = new ConstructedDroid { ActivePerks = instructions.ToList() };
        DroidInstructions.Normalize(droid, 4, 2);
        droid.LearnedPerks.Should().BeEquivalentTo(instructions);
        DroidInstructions.GetSlots(droid.ActivePerks).Should().Be(2);
        DroidInstructions.Normalize(droid, 4, 2).Should().BeFalse();
    }

    [Test]
    public void LeadershipForceAndPassiveWeaponTraitsRemainUnavailable()
    {
        Ability.CacheData();
        var abilities = Ability.GetAllAbilityDetails();
        foreach (var (perk, detail) in Perk.GetAllPerks())
        foreach (var (rank, level) in detail.PerkLevels)
        {
            var restrictedSkill = level.Requirements.OfType<PerkRequirementSkill>()
                .Any(requirement => requirement.Type is SkillType.Leadership or SkillType.Force or SkillType.Lightsaber or SkillType.Saberstaff);
            if (restrictedSkill || !level.GrantedFeats.Any(abilities.ContainsKey))
                DroidInstructions.TryGetLevel(new DroidPerk(perk, rank), out _).Should().BeFalse($"{perk} rank {rank}");
        }
    }
}
