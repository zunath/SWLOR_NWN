using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.CombatAnalyzer;
using SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Perks;

public class CapstoneAbilityTests
{
    [Test]
    public void OffensiveBuffsAndAnalyzer_ShareTheDefensiveCapstoneCooldown()
    {
        var abilities = new[]
        {
            new BloodFrenzyAbilityDefinition().BuildAbilities()[FeatType.BloodFrenzyBurst],
            new SoulAscensionAbilityDefinition().BuildAbilities()[FeatType.SoulAscensionBurst],
            new OverclockedAnalyzerAbilityDefinition().BuildAbilities()[FeatType.Overload],
            new InvincibleAbilityDefinition().BuildAbilities()[FeatType.Invincible1]
        };

        foreach (var ability in abilities)
        {
            ability.RecastGroup.Should().Be(RecastGroup.Capstone);
            ability.RecastDelay(0).Should().Be(CapstoneAbility.RecastDelaySeconds);
            ability.ActivationType.Should().Be(AbilityActivationType.Casted);
            ability.RequiresTarget.Should().BeFalse();
            ability.RequiresLocationTarget.Should().BeFalse();
            ability.IsHostileAbility.Should().BeFalse();
        }
    }

    [Test]
    public void BloodFrenzy_GrantsTemporaryWeaponIndependentHasteAndRecovery()
    {
        var status = new BloodFrenzyStatusEffect();
        status.StatGroup.Stats.Where(stat => stat.Value != 0).ToDictionary(stat => stat.Key, stat => stat.Value)
            .Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.AttackDelayReductionPercent] = 15,
            [StatType.AutoAttackHitStaminaRestore] = 1,
            [StatType.DefeatedEnemyStaminaRestore] = 8
        });
        status.Categories.Should().Be(StatusEffectCategory.Buff);
        status.PersistsOnLogout.Should().BeFalse();

        var perk = BuildPerk<VibrobladePerkDefinition>("BloodFrenzy", PerkType.BloodFrenzy);
        perk.GrantedFeats.Should().Equal(FeatType.BloodFrenzyBurst);
        perk.StatBonuses.Should().BeEmpty("purchasing the perk must not grant permanent kill procs");
        var ability = new BloodFrenzyAbilityDefinition().BuildAbilities()[FeatType.BloodFrenzyBurst];
        ability.Requirements.Should().ContainSingle().Which.Should().BeOfType<AbilityRequirementStamina>()
            .Which.RequiredSTM.Should().Be(CapstoneAbility.StaminaCost);
        ability.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Should().Contain(typeof(BloodFrenzyStatusEffect));
    }

    [Test]
    public void SoulAscension_GrantsTemporaryAttackAndHealingForAllDirectDamage()
    {
        var status = new SoulAscensionBurstStatusEffect();
        status.StatGroup.Stats.Where(stat => stat.Value != 0).ToDictionary(stat => stat.Key, stat => stat.Value)
            .Should().BeEquivalentTo(new Dictionary<StatType, int>
        {
            [StatType.AttackPercentAdjustment] = 20,
            [StatType.DamageDealtHPPercentRestore] = 8
        });
        status.Categories.Should().Be(StatusEffectCategory.Buff);
        status.PersistsOnLogout.Should().BeFalse();

        var perk = BuildPerk<HeavyVibrobladePerkDefinition>("SoulAscension", PerkType.SoulAscension);
        perk.GrantedFeats.Should().Equal(FeatType.SoulAscensionBurst);
        perk.StatBonuses.Should().BeEmpty("purchasing the perk must not grant the old HP-spend kill trigger");
        var ability = new SoulAscensionAbilityDefinition().BuildAbilities()[FeatType.SoulAscensionBurst];
        ability.AIHitPointCostPercent(0).Should().Be(10);
        ability.Requirements.Should().BeEmpty("Soul Ascension spends HP instead of STM");
        ability.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Should().Contain(typeof(SoulAscensionBurstStatusEffect));
    }

    [TestCase(11, 100, true)]
    [TestCase(10, 100, false)]
    [TestCase(1, 100, false)]
    [TestCase(12, 101, true)]
    [TestCase(11, 101, false)]
    [TestCase(1, 1, false)]
    [TestCase(100, 0, false)]
    public void SoulAscension_RequiresEnoughHealthToPayTheFullCostAndSurvive(int currentHp, int maxHp, bool expected)
    {
        SoulAscensionAbilityDefinition.CanPayHitPointCost(currentHp, maxHp).Should().Be(expected);
    }

    [Test]
    public void OffensiveCapstoneClientRows_ActivateOnSelf()
    {
        var root = FindRepositoryRoot();
        var feats = Test2daHelper.Read2da(new FileInfo(Path.Combine(root.FullName, "SWLOR_Haks/sw_2da/feat.2da")));
        var spells = Test2daHelper.Read2da(new FileInfo(Path.Combine(root.FullName, "SWLOR_Haks/sw_2da/spells.2da")));
        foreach (var feat in new[] { FeatType.BloodFrenzyBurst, FeatType.SoulAscensionBurst })
        {
            var row = feats[(int)feat];
            row["TARGETSELF"].Should().Be("1");
            row["HostileFeat"].Should().Be("****");
            var spell = spells[int.Parse(row["SPELLID"])];
            spell["FeatID"].Should().Be(((int)feat).ToString());
            spell["TargetType"].Should().Be("0x01");
            spell["HostileSetting"].Should().Be("0");
        }
    }

    private static PerkLevel BuildPerk<T>(string method, PerkType perkType) where T : new()
    {
        var definition = new T();
        typeof(T).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(definition, null);
        var builder = typeof(T).GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
        return perks[perkType].PerkLevels[1];
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
