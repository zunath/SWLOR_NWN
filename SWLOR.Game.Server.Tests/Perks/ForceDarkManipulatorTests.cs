using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ForceDarkManipulatorTests
{
    [Test]
    public void ForceDarkManipulatorPerkLevels_MatchCombatBible()
    {
        var perks = BuildForceDarkManipulatorPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.CreepingTerror], "Creeping Terror", 1, 2, null, FeatType.CreepingTerror1,
            "Hobble one target for 6 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 18 seconds.");
        AssertPerkLevel(perks[PerkType.ForceGrip], "Force Grip", 1, 2, 5, FeatType.ForceGrip1,
            "Immobilize one target for 3 seconds and interrupt activation.");
        AssertPerkLevel(perks[PerkType.WeakenResolve], "Weaken Resolve", 1, 3, 8, FeatType.WeakenResolve1,
            "Increase force damage taken by 5% for 24 seconds.");
        AssertPerkLevel(perks[PerkType.FractureFocus], "Fracture Focus", 1, 3, 12, FeatType.FractureFocus1,
            "Increase one target's FP and STM ability costs by 20% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.MindShroud], "Mind Shroud", 1, 3, 15, FeatType.MindShroud1,
            "Reduces force damage taken by 5% and grants +10% resistance to confusion, daze, and fear for 30 seconds.");
        AssertPerkLevel(perks[PerkType.CreepingTerror], "Creeping Terror", 2, 3, 18, FeatType.CreepingTerror2,
            "Hobble up to 2 targets for 6 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 18 seconds.");
        AssertPerkLevel(perks[PerkType.ForceGrip], "Force Grip", 2, 2, 22, FeatType.ForceGrip2,
            "Immobilize one target for 4 seconds and interrupt activation.");
        AssertPerkLevel(perks[PerkType.NightmareField], "Nightmare Field", 1, 4, 25, FeatType.NightmareField1,
            "Nearby enemies suffer -10 Accuracy and -10 Evasion for 18 seconds.");
        AssertPerkLevel(perks[PerkType.WeakenResolve], "Weaken Resolve", 2, 3, 28, FeatType.WeakenResolve2,
            "Increase force damage taken by 10% for 24 seconds.");
        AssertPerkLevel(perks[PerkType.ForceChoke], "Force Choke", 1, 4, 30, FeatType.ForceChoke1,
            "Daze one target for 3 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 12 seconds.");
        AssertPerkLevel(perks[PerkType.MindShroud], "Mind Shroud", 2, 3, 35, FeatType.MindShroud2,
            "Reduces force damage taken by 10% and grants +15% resistance to confusion, daze, and fear for 30 seconds.");
        AssertPerkLevel(perks[PerkType.FractureFocus], "Fracture Focus", 2, 3, 38, FeatType.FractureFocus2,
            "Increase nearby enemies' FP and STM ability costs by 25% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.DominateWeakMind], "Dominate Weak Mind", 1, 4, 40, FeatType.DominateWeakMind1,
            "Inflicts Foggy Mind on one non-mechanical target for 8 seconds. Mind resistance shortens the duration. Mind-immune targets suffer -15 Accuracy instead.");
        AssertPerkLevel(perks[PerkType.CreepingTerror], "Creeping Terror", 3, 4, 42, FeatType.CreepingTerror3,
            "Hobble nearby enemies for 6 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 18 seconds.");
        AssertPerkLevel(perks[PerkType.CollapseWill], "Collapse Will", 1, 4, 45, FeatType.CollapseWill1,
            "Apply Exposed and Force Erosion for 18 seconds.");
        AssertPerkLevel(perks[PerkType.ForceGrip], "Force Grip", 3, 3, 48, FeatType.ForceGrip3,
            "Immobilize up to 2 targets for 4 seconds and interrupt activation.");
        AssertPerkLevel(perks[PerkType.EclipseOfResolve], "Eclipse of Resolve", 1, 5, 50, FeatType.EclipseOfResolve1,
            "Nearby enemies suffer -20% hit chance, -20% evasion chance, and +35% FP and STM costs for 20 seconds.");

        AssertUniversalForcePower(perks[PerkType.MindShroud]);
    }

    [Test]
    public void ForceDarkManipulatorAbilities_MatchCombatBible()
    {
        var creepingTerror = new CreepingTerrorAbilityDefinition().BuildAbilities();
        AssertAbility(creepingTerror[FeatType.CreepingTerror1], "Creeping Terror I", 1, RecastGroup.CreepingTerror, 30f, 1f, 4, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(creepingTerror[FeatType.CreepingTerror2], "Creeping Terror II", 2, RecastGroup.CreepingTerror, 30f, 1f, 6, true, true, false, true, AbilityActivationType.Casted, 15f, true);
        AssertAbility(creepingTerror[FeatType.CreepingTerror3], "Creeping Terror III", 3, RecastGroup.CreepingTerror, 30f, 1.5f, 8, true, false, false, true, AbilityActivationType.Casted, 5f, true);

        var forceGrip = new ForceGripAbilityDefinition().BuildAbilities();
        AssertAbility(forceGrip[FeatType.ForceGrip1], "Force Grip I", 1, RecastGroup.ForceGrip, 36f, 1f, 4, true, true, true, false, AbilityActivationType.Casted, 15f, false);
        AssertAbility(forceGrip[FeatType.ForceGrip2], "Force Grip II", 2, RecastGroup.ForceGrip, 36f, 1f, 5, true, true, true, false, AbilityActivationType.Casted, 15f, false);
        AssertAbility(forceGrip[FeatType.ForceGrip3], "Force Grip III", 3, RecastGroup.ForceGrip, 36f, 1f, 7, true, true, false, true, AbilityActivationType.Casted, 15f, false);
        forceGrip[FeatType.ForceGrip1].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
        forceGrip[FeatType.ForceGrip2].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
        forceGrip[FeatType.ForceGrip3].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);

        var weakenResolve = new WeakenResolveAbilityDefinition().BuildAbilities();
        AssertAbility(weakenResolve[FeatType.WeakenResolve1], "Weaken Resolve I", 1, RecastGroup.WeakenResolve, 18f, 1f, 3, true, true, true, false, AbilityActivationType.Casted, 15f, false);
        AssertAbility(weakenResolve[FeatType.WeakenResolve2], "Weaken Resolve II", 2, RecastGroup.WeakenResolve, 18f, 1f, 5, true, true, true, false, AbilityActivationType.Casted, 15f, false);

        var fractureFocus = new FractureFocusAbilityDefinition().BuildAbilities();
        AssertAbility(fractureFocus[FeatType.FractureFocus1], "Fracture Focus I", 1, RecastGroup.FractureFocus, 45f, 1f, 5, true, true, true, false, AbilityActivationType.Casted, 15f, false);
        AssertAbility(fractureFocus[FeatType.FractureFocus2], "Fracture Focus II", 2, RecastGroup.FractureFocus, 60f, 1f, 8, true, false, false, true, AbilityActivationType.Casted, 5f, false);

        var mindShroud = new MindShroudAbilityDefinition().BuildAbilities();
        AssertAbility(mindShroud[FeatType.MindShroud1], "Mind Shroud I", 1, RecastGroup.MindShroud, 60f, 0f, 3, false, false, true, false, AbilityActivationType.Casted, 5f, false);
        AssertAbility(mindShroud[FeatType.MindShroud2], "Mind Shroud II", 2, RecastGroup.MindShroud, 60f, 0f, 4, false, false, true, false, AbilityActivationType.Casted, 5f, false);

        var nightmareField = new NightmareFieldAbilityDefinition().BuildAbilities()[FeatType.NightmareField1];
        AssertAbility(nightmareField, "Nightmare Field", 1, RecastGroup.NightmareField, 75f, 1.5f, 7, true, false, false, true, AbilityActivationType.Casted, 5f, false);

        var forceChoke = new ForceChokeAbilityDefinition().BuildAbilities()[FeatType.ForceChoke1];
        AssertAbility(forceChoke, "Force Choke", 1, RecastGroup.ForceChoke, 60f, 1f, 7, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        forceChoke.ImpactAnimationType.Should().Be(Animation.CastOutAnimation);

        var dominateWeakMind = new DominateWeakMindAbilityDefinition().BuildAbilities()[FeatType.DominateWeakMind1];
        AssertAbility(dominateWeakMind, "Dominate Weak Mind", 1, RecastGroup.DominateWeakMind, 90f, 1f, 8, true, true, true, false, AbilityActivationType.Casted, 15f, false);

        var collapseWill = new CollapseWillAbilityDefinition().BuildAbilities()[FeatType.CollapseWill1];
        AssertAbility(collapseWill, "Collapse Will", 1, RecastGroup.CollapseWill, 75f, 1f, 9, true, true, true, false, AbilityActivationType.Casted, 15f, false);

        var eclipse = new EclipseOfResolveAbilityDefinition().BuildAbilities()[FeatType.EclipseOfResolve1];
        AssertAbility(eclipse, "Eclipse of Resolve", 1, RecastGroup.Capstone, 300f, 1.5f, 11, true, false, false, true, AbilityActivationType.Casted, 5f, false);
    }

    [Test]
    public void ForceDarkManipulatorStatusEffects_MatchCombatBible()
    {
        new WeakenResolve1StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(5);
        new WeakenResolve2StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(10);

        var fractureFocus1 = new FractureFocus1StatusEffect();
        fractureFocus1.StatGroup.Stats[StatType.FPCostPercentAdjustment].Should().Be(20);
        fractureFocus1.StatGroup.Stats[StatType.AbilityStaminaCostPercentAdjustment].Should().Be(20);

        var fractureFocus2 = new FractureFocus2StatusEffect();
        fractureFocus2.StatGroup.Stats[StatType.FPCostPercentAdjustment].Should().Be(25);
        fractureFocus2.StatGroup.Stats[StatType.AbilityStaminaCostPercentAdjustment].Should().Be(25);

        var mindShroud1 = new MindShroud1StatusEffect();
        mindShroud1.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-5);
        mindShroud1.StatGroup.Stats[StatType.MindResistance].Should().Be(10);

        var mindShroud2 = new MindShroud2StatusEffect();
        mindShroud2.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-10);
        mindShroud2.StatGroup.Stats[StatType.MindResistance].Should().Be(15);

        var nightmare = new NightmareField1StatusEffect();
        nightmare.StatGroup.Stats[StatType.Accuracy].Should().Be(-10);
        nightmare.StatGroup.Stats[StatType.Evasion].Should().Be(-10);

        var dominateWeakMindFallback = new DominateWeakMind1StatusEffect();
        dominateWeakMindFallback.StatGroup.Stats[StatType.Accuracy].Should().Be(-15);
        dominateWeakMindFallback.ResistanceType.Should().Be(SWLOR.Game.Server.Service.CombatService.ResistanceType.Invalid);

        var eclipse = new EclipseOfResolve1StatusEffect();
        eclipse.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(-20);
        eclipse.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);
        eclipse.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);
        eclipse.StatGroup.Stats[StatType.FPCostPercentAdjustment].Should().Be(35);
        eclipse.StatGroup.Stats[StatType.AbilityStaminaCostPercentAdjustment].Should().Be(35);

        var creepingTerrorDot = new CreepingTerrorDamageStatusEffect();
        creepingTerrorDot.Name.Should().Be("Creeping Terror");
        creepingTerrorDot.Frequency.Should().Be(3f);
        creepingTerrorDot.ResistanceType.Should().Be(SWLOR.Game.Server.Service.CombatService.ResistanceType.Disruption);

        var forceChokeDot = new ForceChokeDamageStatusEffect();
        forceChokeDot.Name.Should().Be("Force Choke");
        forceChokeDot.Frequency.Should().Be(3f);
        forceChokeDot.ResistanceType.Should().Be(SWLOR.Game.Server.Service.CombatService.ResistanceType.Disruption);
    }

    [Test]
    public void ForceDarkManipulatorSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();

        var creepingTerror = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "CreepingTerrorAbilityDefinition.cs").FullName);
        creepingTerror.Should().Contain("ApplyForceDamageOverTime");
        creepingTerror.Should().Contain("typeof(CreepingTerrorDamageStatusEffect)");
        creepingTerror.Should().Contain("18f");

        var forceChoke = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceChokeAbilityDefinition.cs").FullName);
        forceChoke.Should().Contain("typeof(ForceChokeDamageStatusEffect)");
        forceChoke.Should().Contain("12f");
        forceChoke.Should().Contain("AssignCommand(target, () => ActionPlayAnimation(Animation.ForceChoke))");
        forceChoke.Should().NotContain(".UsesImpactAnimation(Animation.ForceChoke)");

        var forceGrip = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceGripAbilityDefinition.cs").FullName);
        forceGrip.Should().Contain("afterSuccessfulHit: InterruptActivation");
        forceGrip.Should().Contain("ClearAllActions");
        forceGrip.Should().NotContain(".UsesImpactAnimation(Animation.ForceChoke)");

        var immobilized = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "ImmobilizedStatusEffect.cs").FullName);
        immobilized.Should().Contain("Enmity.AttackHighestEnmityTarget(creature)");

        var dominateWeakMind = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "DominateWeakMindAbilityDefinition.cs").FullName);
        dominateWeakMind.Should().Contain("HasCustomValidation");
        dominateWeakMind.Should().NotContain("WillSave");
        dominateWeakMind.Should().NotContain("SavingThrowType");
        dominateWeakMind.Should().Contain("StatType.MindStatusImmunity");
        dominateWeakMind.Should().Contain("ResistanceType.Mind");
        dominateWeakMind.Should().Contain("typeof(DominateWeakMind1StatusEffect)");
        dominateWeakMind.Should().Contain("typeof(FoggyMindStatusEffect)");

        var staminaRequirement = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "AbilityService" / "AbilityRequirementStamina.cs").FullName);
        staminaRequirement.Should().Contain("AbilityStaminaCostPercentAdjustment");

        var forceDamageOverTime = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "ForceDamageOverTimeStatusEffectBase.cs").FullName);
        forceDamageOverTime.Should().Contain("Ability.ApplyDarkForceDamageRestoration(Source, damage)");
    }

    [Test]
    public void ForceDarkManipulatorFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.CreepingTerror1, "ife_crpngtrrr1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceGrip1, "ife_forcegrp1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.WeakenResolve1, "ife_wknres1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FractureFocus1, "ife_fractfoc1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.MindShroud1, "ife_mndshrd1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CreepingTerror2, "ife_crpngtrrr2", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ForceGrip2, "ife_forcegrp2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.NightmareField1, "ife_nghtmrfld1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.WeakenResolve2, "ife_wknres2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceChoke1, "ife_forcechk1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.MindShroud2, "ife_mndshrd2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.FractureFocus2, "ife_fractfoc2", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.DominateWeakMind1, "ife_dmntweakmnd1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CreepingTerror3, "ife_crpngtrrr3", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.CollapseWill1, "ife_cllpswll1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceGrip3, "ife_forcegrp3", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.EclipseOfResolve1, "ife_eclres1", "P", "0x01", "1", "sphere", "5", "****", "17")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be(targetShape);
            abilityRow["TargetSizeX"].Should().Be(targetSizeX);
            abilityRow["TargetSizeY"].Should().Be(targetSizeY);
            abilityRow["TargetFlags"].Should().Be(targetFlags);
        }
    }

    private static void AssertPerkLevel(
        PerkDetail perk,
        string name,
        int level,
        int price,
        int? skillRank,
        FeatType? grantedFeat,
        string description)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.ForceDark);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertCharacterRequirement(perkLevel, CharacterType.ForceSensitive);

        if (skillRank.HasValue)
            AssertSkillRequirement(perkLevel, SkillType.Force, skillRank.Value);
        else
            perkLevel.Requirements.OfType<PerkRequirementSkill>().Should().BeEmpty();

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int fpCost,
        bool isHostile,
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType,
        float maxRange,
        bool triggersDarkForceConversion)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Force);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(activationType);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.MaxRange.Should().Be(maxRange);
        ability.TriggersDarkForceConversion.Should().Be(triggersDarkForceConversion);
        ability.BreaksStealth.Should().BeTrue();

        ability.Requirements
            .OfType<AbilityRequirementFP>()
            .Should()
            .ContainSingle()
            .Which
            .RequiredFP
            .Should()
            .Be(fpCost);
        ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
    }

    private static void AssertSkillRequirement(PerkLevel level, SkillType skill, int rank)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementSkill>()
            .Should()
            .ContainSingle()
            .Which;

        requirement.Type.Should().Be(skill);
        requirement.RequiredRank.Should().Be(rank);
    }

    private static void AssertCharacterRequirement(PerkLevel level, CharacterType characterType)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementCharacterType>()
            .Should()
            .ContainSingle()
            .Which;

        typeof(PerkRequirementCharacterType)
            .GetField("_requiredCharacterType", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(characterType);
    }

    private static void AssertUniversalForcePower(PerkDetail perk)
    {
        perk.ForceAffinityType.Should().BeNull();
        perk.StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.ForceAffinity);
    }

    private static Dictionary<PerkType, PerkDetail> BuildForceDarkManipulatorPerksWithout2daLookup()
    {
        var definition = new ForceDarkManipulatorPerkDefinition();
        var methodNames = new[]
        {
            "CollapseWill",
            "CreepingTerror",
            "DominateWeakMind",
            "EclipseOfResolve",
            "ForceChoke",
            "ForceGrip",
            "FractureFocus",
            "MindShroud",
            "NightmareField",
            "WeakenResolve"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ForceDarkManipulatorPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ForceDarkManipulatorPerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            result[row] = values;
        }

        return result;
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
