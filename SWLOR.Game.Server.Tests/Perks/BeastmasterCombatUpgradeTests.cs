using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster;
using SWLOR.Game.Server.Feature.PerkDefinition.Beast;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class BeastmasterCombatUpgradeTests
{
    [Test]
    public void BeastmasterPassivePerks_ExposeBibleStats()
    {
        var damage = BuildPerksWithout2daLookup(new BeastDamagePerkDefinition(), "BloodFrenzy");
        AssertStatBonus(damage[PerkType.BeastBloodFrenzy].PerkLevels[1], StatType.DamageDealtBleedingTargetStaminaRestoreChance, 20);
        AssertStatBonus(damage[PerkType.BeastBloodFrenzy].PerkLevels[2], StatType.DamageDealtBleedingTargetStaminaRestoreChance, 30);

        var tank = BuildPerksWithout2daLookup(new BeastTankPerkDefinition(), "FocusAttention", "LastGuardian");
        AssertStatBonus(tank[PerkType.FocusAttention].PerkLevels[3], StatType.AbilityRecastDelayFlatAdjustmentPerkType, (int)PerkType.Anger);
        AssertStatBonus(tank[PerkType.FocusAttention].PerkLevels[3], StatType.AbilityRecastDelayFlatAdjustment, -3);
        AssertStatBonus(tank[PerkType.LastGuardian].PerkLevels[1], StatType.FatalDamageTemporaryHPPercent, 20);
        AssertStatBonus(tank[PerkType.LastGuardian].PerkLevels[1], StatType.FatalDamageTemporaryHPCooldownSeconds, 180);

        var balanced = BuildPerksWithout2daLookup(new BeastBalancedPerkDefinition(), "PackRhythm");
        AssertStatBonus(balanced[PerkType.PackRhythm].PerkLevels[1], StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, 3);
        AssertStatBonus(balanced[PerkType.PackRhythm].PerkLevels[2], StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment, 6);

        var bruiser = BuildPerksWithout2daLookup(new BeastBruiserPerkDefinition(), "EnduranceLink", "VenomousHide");
        AssertStatBonus(bruiser[PerkType.EnduranceLink].PerkLevels[3], StatType.AutoAttackMasterStaminaRestoreChance, 30);
        AssertStatBonus(bruiser[PerkType.VenomousHide].PerkLevels[2], StatType.MeleeDamageTakenPoisonDamageChance, 20);

        var evasion = BuildPerksWithout2daLookup(new BeastEvasionPerkDefinition(), "Sniff", "QuickRecovery");
        AssertStatBonus(evasion[PerkType.Sniff].PerkLevels[3], StatType.RareItemFindChance, 15);
        AssertStatBonus(evasion[PerkType.QuickRecovery].PerkLevels[2], StatType.AvoidedAttackStaminaRestoreChance, 25);

        var force = BuildPerksWithout2daLookup(new BeastForcePerkDefinition(), "ForceLink");
        AssertStatBonus(force[PerkType.ForceLink].PerkLevels[3], StatType.AutoAttackMasterFPRestoreChance, 30);
    }

    [Test]
    public void BeastmasterStatuses_MatchBibleEffects()
    {
        new BolsterAttack1StatusEffect().StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(5);
        new BolsterAttack3StatusEffect().StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(12);
        new Hasten1StatusEffect().StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(15);
        new Hasten2StatusEffect().StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(25);
        new PredatorRush1StatusEffect().StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(20);
        new PrimalOverrun1StatusEffect().StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(12);
        new AlphaRhythm1StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(8);
        new AlphaRhythm1BeastStatusEffect().StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(10);
        new Assault1StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(6);
        new Assault3StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(14);
        new EvasiveChallenge1SelfStatusEffect().StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore].Should().Be(1);
        new Intercept2StatusEffect().StatGroup.Stats[StatType.DamageTakenRedirectToStatusSourcePercent].Should().Be(50);
    }

    [Test]
    public void BeastmasterAbilities_MatchTargetingAndResourceCosts()
    {
        var predatorsMark = new PredatorsMarkAbilityDefinition().BuildAbilities()[FeatType.PredatorsMark1];
        AssertStaminaAbility(predatorsMark, "Predator's Mark I", RecastGroup.PredatorsMark, 45f, 5, requiresTarget: true);

        var apexBite = new ApexBiteAbilityDefinition().BuildAbilities()[FeatType.ApexBite1];
        AssertStaminaAbility(apexBite, "Apex Bite", RecastGroup.ApexBite, 120f, 10, requiresTarget: true);

        var evasiveChallenge = new EvasiveChallengeAbilityDefinition().BuildAbilities();
        AssertStaminaAbility(evasiveChallenge[FeatType.EvasiveChallenge1], "Evasive Challenge I", RecastGroup.EvasiveChallenge, 60f, 5, requiresTarget: true);
        AssertStaminaAbility(evasiveChallenge[FeatType.EvasiveChallenge2], "Evasive Challenge II", RecastGroup.EvasiveChallenge, 60f, 7, requiresTarget: false);

        var ironHide = new IronHideAbilityDefinition().BuildAbilities()[FeatType.IronHide1];
        AssertStaminaAbility(ironHide, "Iron Hide I", RecastGroup.IronHide, 30f, 3, requiresTarget: false);

        var forceTouch = new ForceTouchAbilityDefinition().BuildAbilities()[FeatType.ForceTouch3];
        forceTouch.Requirements.OfType<AbilityRequirementFP>().Should().ContainSingle().Which.RequiredFP.Should().Be(6);
        forceTouch.RecastDelay(0).Should().Be(12f);
    }

    [Test]
    public void BeastmasterSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();
        var pounce = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "PounceAbilityDefinition.cs").FullName);
        pounce.Should().Contain("ActionJumpToObject(target)");
        pounce.Should().Contain("ClearAllActions()");

        var apexBite = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "ApexBiteAbilityDefinition.cs").FullName);
        apexBite.Should().Contain("45");
        apexBite.Should().Contain("criticalRatePercentAdjustment: 25");

        var iceBreath = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "IceBreathAbilityDefinition.cs").FullName);
        iceBreath.Should().Contain("typeof(HamstringStatusEffect)");
        iceBreath.Should().Contain("typeof(ImmobilizedStatusEffect)");

        var combat = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        combat.Should().Contain("ApplyAbilityUsedMasterAbilityHitChance");
        combat.Should().Contain("ApplyDamageTakenRedirectToStatusSource");
    }

    private static void AssertStaminaAbility(
        AbilityDetail ability,
        string name,
        RecastGroup recastGroup,
        float recastSeconds,
        int staminaCost,
        bool requiresTarget)
    {
        ability.Name.Should().Be(name);
        ability.SkillType.Should().Be(SkillType.BeastMastery);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.Requirements.OfType<AbilityRequirementStamina>().Should().ContainSingle().Which.RequiredSTM.Should().Be(staminaCost);
    }

    private static void AssertStatBonus(PerkLevel level, StatType statType, int value)
    {
        level.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == statType)
            .Which
            .Calculate(0)
            .Should()
            .Be(value);
    }

    private static Dictionary<PerkType, PerkDetail> BuildPerksWithout2daLookup<T>(T definition, params string[] methodNames)
    {
        foreach (var methodName in methodNames)
        {
            typeof(T)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(T)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "swlor2_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
