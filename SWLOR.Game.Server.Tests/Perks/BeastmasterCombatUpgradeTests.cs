using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster;
using SWLOR.Game.Server.Feature.PerkDefinition.Beast;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
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
        var damage = BuildPerksWithout2daLookup(new BeastDamagePerkDefinition(), "BloodFrenzy", "PredatorsMark");
        AssertStatBonus(damage[PerkType.BeastBloodFrenzy].PerkLevels[1], StatType.DamageDealtBleedingTargetStaminaRestoreChance, 20);
        AssertStatBonus(damage[PerkType.BeastBloodFrenzy].PerkLevels[1], StatType.DamageDealtBleedingTargetStaminaRestore, 1);
        AssertStatBonus(damage[PerkType.BeastBloodFrenzy].PerkLevels[2], StatType.DamageDealtBleedingTargetStaminaRestoreChance, 30);
        AssertStatBonus(damage[PerkType.BeastBloodFrenzy].PerkLevels[2], StatType.DamageDealtBleedingTargetStaminaRestore, 1);
        AssertStatBonus(damage[PerkType.PredatorsMark].PerkLevels[1], StatType.PredatorsMarkDamageTakenFromBeastPercent, 10);
        AssertStatBonus(damage[PerkType.PredatorsMark].PerkLevels[1], StatType.PredatorsMarkDurationSeconds, 30);
        AssertStatBonus(damage[PerkType.PredatorsMark].PerkLevels[2], StatType.PredatorsMarkDamageTakenFromBeastPercent, 10);
        AssertStatBonus(damage[PerkType.PredatorsMark].PerkLevels[2], StatType.PredatorsMarkDurationSeconds, 30);
        AssertStatBonus(damage[PerkType.PredatorsMark].PerkLevels[2], StatType.PredatorsMarkHastePercentPerStack, 5);
        AssertStatBonus(damage[PerkType.PredatorsMark].PerkLevels[2], StatType.PredatorsMarkAbilityHitChancePercentPerStack, 2);
        AssertStatBonus(damage[PerkType.PredatorsMark].PerkLevels[2], StatType.PredatorsMarkFollowUpDurationSeconds, 30);
        AssertStatBonus(damage[PerkType.PredatorsMark].PerkLevels[2], StatType.PredatorsMarkFollowUpMaximumStacks, 4);

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
        new PrimalOverrun1StatusEffect().StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(12);
        new AlphaRhythm1StatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(8);
        new AlphaRhythm1BeastStatusEffect().StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(10);
        new Assault1StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(6);
        new Assault3StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(14);
        new DistractingFeint1StatusEffect().StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-4);
        new DistractingFeint1StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-4);
        new EvasiveChallenge1SelfStatusEffect().StatGroup.Stats[StatType.AvoidedAttackSingleStaminaRestore].Should().Be(1);
        new Intercept2StatusEffect().StatGroup.Stats[StatType.DamageTakenRedirectToStatusSourcePercent].Should().Be(50);
        new PredatorsMark1StatusEffect(10).StatGroup.Stats[StatType.DamageTakenFromStatusSourcePercentAdjustment].Should().Be(10);
        new GuardingBondBeastStatusEffect().StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(20);
        new GuardingBondBeastStatusEffect().StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(20);
        new GuardingBondBeastStatusEffect().StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(-15);
        new GuardingBondBeastStatusEffect().StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(75);
        new PredatoryBondBeastStatusEffect().StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(25);
        new PredatoryBondBeastStatusEffect().StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(15);
        new PredatoryBondBeastStatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(10);
        new PredatoryBondBeastStatusEffect().StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);
        new PredatoryBondBeastStatusEffect().StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(-40);
    }

    [Test]
    public void BeastmasterAbilities_MatchTargetingAndResourceCosts()
    {
        var bite = new BiteAbilityDefinition().BuildAbilities();
        bite.Values.Should().OnlyContain(x => x.ActivationType == AbilityActivationType.Weapon);
        bite.Values.Should().OnlyContain(x => !x.RequiresTarget);

        var rendingClaw = new RendingClawAbilityDefinition().BuildAbilities();
        rendingClaw.Values.Should().OnlyContain(x => x.ActivationType == AbilityActivationType.Weapon);
        rendingClaw.Values.Should().OnlyContain(x => !x.RequiresTarget);

        var exposePrey = new ExposePreyAbilityDefinition().BuildAbilities()[FeatType.ExposePrey1];
        AssertQueuedBeastAbility(exposePrey, "Expose Prey", RecastGroup.ExposePrey, 24f, 7);

        var executePrey = new ExecutePreyAbilityDefinition().BuildAbilities()[FeatType.ExecutePrey1];
        AssertQueuedBeastAbility(executePrey, "Execute Prey", RecastGroup.ExecutePrey, 30f, 8);

        var coordinatedStrike = new CoordinatedStrikeAbilityDefinition().BuildAbilities();
        AssertQueuedBeastAbility(
            coordinatedStrike[FeatType.CoordinatedStrike1],
            "Coordinated Strike I",
            RecastGroup.CoordinatedStrike,
            15f,
            5);
        AssertQueuedBeastAbility(
            coordinatedStrike[FeatType.CoordinatedStrike2],
            "Coordinated Strike II",
            RecastGroup.CoordinatedStrike,
            15f,
            7);

        var apexBite = new ApexBiteAbilityDefinition().BuildAbilities()[FeatType.ApexBite1];
        AssertQueuedBeastAbility(apexBite, "Apex Bite", RecastGroup.ApexBite, 45f, 10);

        var pounce = new PounceAbilityDefinition().BuildAbilities();
        AssertPounceAbility(pounce[FeatType.Pounce1], "Pounce I", 5);
        AssertPounceAbility(pounce[FeatType.Pounce2], "Pounce II", 6);

        var evasiveChallenge = new EvasiveChallengeAbilityDefinition().BuildAbilities();
        AssertStaminaAbility(evasiveChallenge[FeatType.EvasiveChallenge1], "Evasive Challenge I", RecastGroup.EvasiveChallenge, 30f, 5, requiresTarget: true);
        AssertStaminaAbility(evasiveChallenge[FeatType.EvasiveChallenge2], "Evasive Challenge II", RecastGroup.EvasiveChallenge, 30f, 7, requiresTarget: false);

        var anger = new AngerAbilityDefinition().BuildAbilities()[FeatType.Anger1];
        AssertStaminaAbility(anger, "Anger I", RecastGroup.Anger, 12f, 3, requiresTarget: true);
        anger.AITargetSelector.Should().NotBeNull();
        anger.AIScore.Should().NotBeNull();

        var guardingRoar = new GuardingRoarAbilityDefinition().BuildAbilities()[FeatType.GuardingRoar1];
        AssertStaminaAbility(guardingRoar, "Guarding Roar I", RecastGroup.GuardingRoar, 24f, 6, requiresTarget: false);
        guardingRoar.IsHostileAbility.Should().BeTrue();
        guardingRoar.IsAreaAbility.Should().BeTrue();
        guardingRoar.MaxRange.Should().Be(5f);
        guardingRoar.AITargetSelector.Should().NotBeNull();
        guardingRoar.AIScore.Should().NotBeNull();

        var ironHide = new IronHideAbilityDefinition().BuildAbilities()[FeatType.IronHide1];
        AssertStaminaAbility(ironHide, "Iron Hide I", RecastGroup.IronHide, 18f, 3, requiresTarget: false);

        var forceTouch = new ForceTouchAbilityDefinition().BuildAbilities()[FeatType.ForceTouch3];
        forceTouch.Requirements.OfType<AbilityRequirementFP>().Should().ContainSingle().Which.RequiredFP.Should().Be(6);
        forceTouch.RecastDelay(0).Should().Be(8f);

        var guardingBond = new GuardingBondAbilityDefinition().BuildAbilities()[FeatType.GuardingBond];
        AssertBeastBondAbility(guardingBond, "Guarding Bond");

        var predatoryBond = new PredatoryBondAbilityDefinition().BuildAbilities()[FeatType.PredatoryBond];
        AssertBeastBondAbility(predatoryBond, "Predatory Bond");
    }

    [Test]
    public void TameChance_ScalesSocialByThreePercentAndCapsAtSeventyFive()
    {
        var baseline = TameAbilityDefinition.CalculateTameChance(
            beastMasterySkillRank: 20,
            npcLevel: 20,
            social: 0);

        baseline.Should().Be(40);
        TameAbilityDefinition.CalculateTameChance(20, 20, 10).Should().Be(baseline + 30);
        TameAbilityDefinition.CalculateTameChance(20, 20, 25).Should().Be(75);
    }

    [Test]
    public void BeastmasterSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();
        var pounce = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "PounceAbilityDefinition.cs").FullName);
        pounce.Should().Contain("ActionPlayAnimation(Animation.ForceLeap, LeapAnimationSpeed, LeapAnimationDurationSeconds)");
        pounce.Should().Contain("JumpToLocation(destination)");
        pounce.Should().Contain("SetFacingPoint(GetPosition(target))");
        pounce.Should().Contain("UsePerkFeat.InterruptAbilityActivation(target)");
        pounce.Should().Contain("PounceOpeningDistanceMeters");
        pounce.Should().Contain("private const float MaxRangeMeters = 15.0f;");
        pounce.Should().Contain("private const int Pounce1Damage = 14;");
        pounce.Should().Contain("private const int Pounce2Damage = 24;");
        pounce.Should().Contain("ClearAllActions()");
        pounce.Should().NotContain("ActionJumpToObject(target)");
        pounce.Should().NotContain("ActionDoCommand(");

        var pounce1Impact = pounce.Substring(
            pounce.IndexOf("private static void Pounce1ImpactAction", StringComparison.Ordinal),
            pounce.IndexOf("private static void Pounce2ImpactAction", StringComparison.Ordinal) -
            pounce.IndexOf("private static void Pounce1ImpactAction", StringComparison.Ordinal));
        pounce1Impact.IndexOf("CompleteLeap(activator, target)", StringComparison.Ordinal)
            .Should().BeLessThan(pounce1Impact.IndexOf("Ability.ApplyCombatImpact(", StringComparison.Ordinal));
        pounce1Impact.IndexOf("Ability.ApplyCombatImpact(", StringComparison.Ordinal)
            .Should().BeLessThan(pounce1Impact.IndexOf("InterruptTargetActivation(target)", StringComparison.Ordinal));

        var apexBite = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "ApexBiteAbilityDefinition.cs").FullName);
        apexBite.Should().Contain("45");
        apexBite.Should().Contain("criticalRatePercentAdjustment: 25");

        var iceBreath = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "IceBreathAbilityDefinition.cs").FullName);
        iceBreath.Should().Contain("typeof(HamstringStatusEffect)");
        iceBreath.Should().Contain("typeof(ImmobilizedStatusEffect)");

        var innervate = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "InnervateAbilityDefinition.cs").FullName);
        innervate.Should().NotContain("ScaleDirectEffect");

        var crushingSlam = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "CrushingSlamAbilityDefinition.cs").FullName);
        crushingSlam.Should().Contain("centerOnActivator: true");

        var rampage = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "RampageAbilityDefinition.cs").FullName);
        rampage.Should().Contain("centerOnActivator: true");

        var evasiveChallenge = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Beastmaster" / "EvasiveChallengeAbilityDefinition.cs").FullName);
        evasiveChallenge.Should().Contain("RemoveStatusEffectsWithNegativeStat");

        var combat = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        combat.Should().Contain("ApplyAbilityUsedMasterAbilityHitChance");
        combat.Should().Contain("ApplyDamageTakenRedirectToStatusSource");
        combat.Should().Contain("ApplyPredatorsMarkEffects");
        combat.Should().Contain("ApplyPredatorsMarkEffects(attacker, defender, isAbilityDamage);");
        combat.Should().NotContain("skillType != SkillType.BeastMastery");
        combat.Should().Contain("StatType.PredatorsMarkDurationSeconds");
        combat.Should().Contain("StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding)");
        combat.Should().Contain("StatType.DamageDealtBleedingTargetStaminaRestoreChance");
        combat.Should().Contain("StatType.DamageDealtBleedingTargetStaminaRestore");
        combat.Should().Contain("StatType.BeastBalancedAbilityStaminaRestoreCategoryId");
        combat.Should().Contain("InventorySlot.CreatureRight");
        combat.Should().Contain("bool usesQueuedNaturalWeapon = false");
        combat.Should().Contain("usesQueuedNaturalWeapon && skillType == SkillType.BeastMastery");
        combat.Should().Contain("return GetCreatureNaturalWeapon(activator);");
    }

    [Test]
    public void BeastmasterPlayerPerkRequirements_DoNotRequireBeastLevel()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "BeastMasteryPerkDefinition.cs").FullName);

        source.Should().NotContain(
            "RequirementBeastLevel",
            "player Beast Mastery perks must not gate purchase by active beast level");
    }

    [Test]
    public void BeastmasterBeastPerkRequirements_RequireBeastLevelNotPlayerSkill()
    {
        var root = FindRepositoryRoot();
        var definitionFiles = Directory.EnumerateFiles(
            (root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "Beast").FullName,
            "*.cs");

        foreach (var file in definitionFiles)
        {
            var source = File.ReadAllText(file);
            source.Should().Contain(
                "RequirementBeastLevel",
                $"{Path.GetFileName(file)} must gate beast-owned perks by beast level");
            source.Should().NotContain(
                "RequirementSkill(SkillType.BeastMastery",
                $"{Path.GetFileName(file)} must not treat beast level as a player Beast Mastery rank");
        }
    }

    [Test]
    public void BeastmasterActiveBeastRequirements_GuardMissingActiveBeastBeforeLookup()
    {
        var root = FindRepositoryRoot();
        var requirementFiles = new[]
        {
            root / "SWLOR.Game.Server" / "Service" / "PerkService" / "PerkRequirementBeastLevel.cs",
            root / "SWLOR.Game.Server" / "Service" / "PerkService" / "PerkRequirementBeastRole.cs"
        };

        foreach (var file in requirementFiles)
        {
            var source = File.ReadAllText(file.FullName);
            var guardIndex = source.IndexOf("string.IsNullOrWhiteSpace(dbPlayer.ActiveBeastId)", StringComparison.Ordinal);
            var lookupIndex = source.IndexOf("DB.Get<Beast>(dbPlayer.ActiveBeastId)", StringComparison.Ordinal);

            guardIndex.Should().BeGreaterThanOrEqualTo(0, $"{Path.GetFileName(file.FullName)} must handle players without an active beast");
            lookupIndex.Should().BeGreaterThan(guardIndex, $"{Path.GetFileName(file.FullName)} must not call DB.Get<Beast> with a null active beast id");
        }
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

    private static void AssertQueuedBeastAbility(
        AbilityDetail ability,
        string name,
        RecastGroup recastGroup,
        float recastSeconds,
        int staminaCost)
    {
        AssertStaminaAbility(ability, name, recastGroup, recastSeconds, staminaCost, requiresTarget: false);
        ability.ActivationType.Should().Be(AbilityActivationType.Weapon);
        ability.IsSingleTargetAbility.Should().BeTrue();
        ability.IsHostileAbility.Should().BeTrue();
    }

    private static void AssertPounceAbility(AbilityDetail ability, string name, int staminaCost)
    {
        AssertStaminaAbility(ability, name, RecastGroup.Pounce, 18f, staminaCost, requiresTarget: true);
        ability.ActivationType.Should().Be(AbilityActivationType.Casted);
        ability.IsSingleTargetAbility.Should().BeTrue();
        ability.IsHostileAbility.Should().BeTrue();
        ability.MaxRange.Should().Be(15f);
        ability.HasExplicitMaxRange.Should().BeTrue();
        ability.AIScore.Should().NotBeNull();
        ability.ActivationAction.Should().NotBeNull();
        ability.ImpactDelay.Should().Be(1f);
    }

    private static void AssertBeastBondAbility(AbilityDetail ability, string name)
    {
        ability.Name.Should().Be(name);
        ability.SkillType.Should().Be(SkillType.BeastMastery);
        ability.RecastGroup.Should().Be(RecastGroup.BeastBond);
        ability.RecastDelay(0).Should().Be(30f);
        ability.RequiresTarget.Should().BeFalse();
        ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
        ability.StatusEffectTypesRemovedOnPerkRefund.Should().ContainSingle();
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
                File.Exists(Path.Combine(candidate, "SWLOR.Game.Server", "Readmes", "CombatUpgradeBiblePerkManifest.csv")))
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
