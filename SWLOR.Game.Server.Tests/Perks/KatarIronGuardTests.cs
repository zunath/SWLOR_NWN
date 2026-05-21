using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Katar;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class KatarIronGuardTests
{
    [Test]
    public void KatarIronGuardPerkLevels_MatchCombatBible()
    {
        var perks = BuildKatarIronGuardPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.GuardTraining], "Guard Training", 1, 2, 5, null,
            "Dual wielding katars grants a 15% chance to guard against physical attacks, reducing that hit's damage by 20% and generating extra enmity.",
            StatType.Guard);
        AssertPerkLevel(perks[PerkType.GuardCounter], "Guard Counter", 1, 2, 8, FeatType.GuardCounter1,
            "Your next attack deals weapon DMG + 8. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 16 instead.");
        AssertPerkLevel(perks[PerkType.TwinGuardStance], "Twin Guard Stance", 1, 3, 12, FeatType.TwinGuardStance1,
            "While active, grants +15% Defense and +20% Enmity generation, but reduces Attack by 15%.");
        AssertPerkLevel(perks[PerkType.GuardTraining], "Guard Training", 2, 2, 15, null,
            "Guard chance increases to 25% and guarded hits restore 2 STM.",
            StatType.Guard,
            StatType.GuardStaminaRestore);
        AssertPerkLevel(perks[PerkType.IronElbows], "Iron Elbows", 1, 4, 18, FeatType.IronElbows1,
            "Deals weapon DMG + 15 to all nearby enemies and generates extra enmity.");
        AssertPerkLevel(perks[PerkType.RedirectingGuard], "Redirecting Guard", 1, 3, 20, null,
            "When you guard an attack, your next katar attack within 10 seconds gains +10% critical chance and deals +10 DMG.",
            StatType.GuardedHitNextSkillAbilitySkillType,
            StatType.GuardedHitNextSkillAbilityCriticalRatePercentAdjustment,
            StatType.GuardedHitNextSkillAbilityDamageBonus,
            StatType.GuardedHitNextSkillAbilityWindowSeconds);
        AssertPerkLevel(perks[PerkType.GuardCounter], "Guard Counter", 2, 2, 22, FeatType.GuardCounter2,
            "Your next attack deals weapon DMG + 18. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 30 instead.");
        AssertPerkLevel(perks[PerkType.CoveringClaws], "Covering Claws", 1, 3, 25, FeatType.CoveringClaws1,
            "Strike enemies in a cone for weapon DMG + 20. Enemies hit generate +25% Enmity toward you for 12 seconds.");
        AssertPerkLevel(perks[PerkType.GuardTraining], "Guard Training", 3, 4, 28, null,
            "Guard chance increases to 35% and guarded hits reduce physical damage by 30%.",
            StatType.Guard,
            StatType.GuardStaminaRestore,
            StatType.GuardDamageReductionPercentAdjustment);
        AssertPerkLevel(perks[PerkType.TwinIntercept], "Twin Intercept", 1, 3, 30, FeatType.TwinIntercept1,
            "Target an ally within 6 meters. They gain a damage shield equal to 20% of your maximum HP and +15% Defense for 8 seconds. You gain extra enmity toward enemies near that ally.");
        AssertPerkLevel(perks[PerkType.RetaliatoryFlow], "Retaliatory Flow", 1, 2, 32, null,
            "After you guard a hit, your next Guard Counter within 8 seconds costs 2 less STM and deals +8 DMG.",
            StatType.GuardedHitNextMatchingAbilityPerkType,
            StatType.GuardedHitNextMatchingAbilityDamageBonus,
            StatType.GuardedHitNextMatchingAbilityStaminaCostAdjustment,
            StatType.GuardedHitNextMatchingAbilityWindowSeconds);
        AssertPerkLevel(perks[PerkType.WhirlingGuard], "Whirling Guard", 1, 4, 35, FeatType.WhirlingGuard1,
            "For 12 seconds, gain +20% guard chance and deal 8 DMG back to attackers whenever you guard a hit.");
        AssertPerkLevel(perks[PerkType.GuardCounter], "Guard Counter", 3, 3, 38, FeatType.GuardCounter3,
            "Your next attack deals weapon DMG + 28. If you guarded an attack within the last 8 seconds, this deals weapon DMG + 45 and inflicts Dazed for 3 seconds.");
        AssertPerkLevel(perks[PerkType.ImpenetrableGrip], "Impenetrable Grip", 1, 3, 40, null,
            "While dual wielding katars, gain +20% resistance to Knockdown and Dazed effects. Guarded hits restore 4 STM.",
            StatType.MobilityResistance,
            StatType.MindResistance,
            StatType.GuardStaminaRestore);
        AssertPerkLevel(perks[PerkType.IronWallStance], "Iron Wall Stance", 1, 4, 42, FeatType.IronWallStance1,
            "While active, grants +25% Defense, +20% Force Defense, and +30% Enmity generation, but reduces Attack by 25%.");
        AssertPerkLevel(perks[PerkType.BreakerReversal], "Breaker Reversal", 1, 3, 45, FeatType.BreakerReversal1,
            "After guarding an attack, your next katar attack deals weapon DMG + 35 and inflicts Exposed, reducing Defense by 15% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.GuardianReflexes], "Guardian Reflexes", 1, 4, 48, null,
            "When reduced below 35% HP, gain +25% guard chance for 12 seconds. This can only trigger once every 3 minutes.",
            StatType.LowHPGuardThresholdPercent,
            StatType.LowHPGuard,
            StatType.LowHPGuardDurationSeconds,
            StatType.LowHPGuardCooldownSeconds);
        AssertPerkLevel(perks[PerkType.AdamantineGuard], "Adamantine Guard", 1, 4, 50, FeatType.AdamantineGuard1,
            "For 45 seconds, gain +25 Guard. Guarded hits reduce damage by an additional 20% and generate 75% more enmity.");
    }

    [Test]
    public void KatarIronGuardAbilities_MatchCombatBible()
    {
        var guardCounter = new GuardCounterAbilityDefinition().BuildAbilities();
        AssertAbility(guardCounter[FeatType.GuardCounter1], "Guard Counter I", 1, RecastGroup.GuardCounter, 30f, 0f, 3, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(guardCounter[FeatType.GuardCounter2], "Guard Counter II", 2, RecastGroup.GuardCounter, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(guardCounter[FeatType.GuardCounter3], "Guard Counter III", 3, RecastGroup.GuardCounter, 45f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var twinGuardStance = new TwinGuardStanceAbilityDefinition().BuildAbilities()[FeatType.TwinGuardStance1];
        AssertAbility(twinGuardStance, "Twin Guard Stance", 1, RecastGroup.TwinGuardStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var ironElbows = new IronElbowsAbilityDefinition().BuildAbilities()[FeatType.IronElbows1];
        AssertAbility(ironElbows, "Iron Elbows", 1, RecastGroup.IronElbows, 60f, 0f, 7, true, false, false, true, AbilityActivationType.Casted);

        var coveringClaws = new CoveringClawsAbilityDefinition().BuildAbilities()[FeatType.CoveringClaws1];
        AssertAbility(coveringClaws, "Covering Claws", 1, RecastGroup.CoveringClaws, 45f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var twinIntercept = new TwinInterceptAbilityDefinition().BuildAbilities()[FeatType.TwinIntercept1];
        AssertAbility(twinIntercept, "Twin Intercept", 1, RecastGroup.TwinIntercept, 120f, 0f, 10, false, true, true, false, AbilityActivationType.Casted);
        twinIntercept.MaxRange.Should().Be(6f);
        twinIntercept.CustomValidation.Should().NotBeNull();

        var whirlingGuard = new WhirlingGuardAbilityDefinition().BuildAbilities()[FeatType.WhirlingGuard1];
        AssertAbility(whirlingGuard, "Whirling Guard", 1, RecastGroup.WhirlingGuard, 120f, 0f, 12, false, false, false, false, AbilityActivationType.Casted);

        var breakerReversal = new BreakerReversalAbilityDefinition().BuildAbilities()[FeatType.BreakerReversal1];
        AssertAbility(breakerReversal, "Breaker Reversal", 1, RecastGroup.BreakerReversal, 60f, 0f, 10, true, false, true, false, AbilityActivationType.Weapon);
        breakerReversal.CustomValidation.Should().NotBeNull();

        var ironWallStance = new IronWallStanceAbilityDefinition().BuildAbilities()[FeatType.IronWallStance1];
        AssertAbility(ironWallStance, "Iron Wall Stance", 1, RecastGroup.IronWallStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var adamantineGuard = new AdamantineGuardAbilityDefinition().BuildAbilities()[FeatType.AdamantineGuard1];
        AssertAbility(adamantineGuard, "Adamantine Guard", 1, RecastGroup.Capstone, 345f, 1f, 15, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void KatarIronGuardStatusEffects_MatchCombatBible()
    {
        var twinGuard = new TwinGuardStanceStatusEffect();
        twinGuard.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(15);
        twinGuard.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(20);
        twinGuard.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        twinGuard.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);

        var coveringClaws = new CoveringClawsStatusEffect();
        coveringClaws.StatGroup.Stats[StatType.EnmityToStatusSourcePercentAdjustment].Should().Be(25);

        var twinIntercept = new TwinInterceptStatusEffect();
        twinIntercept.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(15);
        twinIntercept.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);

        var whirlingGuard = new WhirlingGuardStatusEffect();
        whirlingGuard.StatGroup.Stats[StatType.Guard].Should().Be(20);
        whirlingGuard.StatGroup.Stats[StatType.GuardRetaliationDamage].Should().Be(8);

        var ironWall = new IronWallStanceStatusEffect();
        ironWall.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(25);
        ironWall.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(20);
        ironWall.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(30);
        ironWall.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-25);

        var adamantineGuard = new AdamantineGuardStatusEffect();
        adamantineGuard.StatGroup.Stats[StatType.Guard].Should().Be(25);
        adamantineGuard.StatGroup.Stats[StatType.GuardDamageReductionPercentAdjustment].Should().Be(20);
        adamantineGuard.StatGroup.Stats[StatType.GuardEnmityPercentAdjustment].Should().Be(75);
    }

    [Test]
    public void KatarIronGuardFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.GuardCounter1, "ife_grdcntr1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardCounter2, "ife_grdcntr2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardCounter3, "ife_grdcntr3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.TwinGuardStance1, "ife_twingrdstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.IronElbows1, "ife_ironelbw1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.CoveringClaws1, "ife_covclaw1", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.TwinIntercept1, "ife_twinintc1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.WhirlingGuard1, "ife_whirlgrd1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.BreakerReversal1, "ife_brkrrev1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.IronWallStance1, "ife_ironwallstn1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.AdamantineGuard1, "ife_adamgrd1", "P", "0x01", "0", "****", "****", "****", "****")
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

    [Test]
    public void KatarIronGuardImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();

        var guardCounter = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Katar" / "GuardCounterAbilityDefinition.cs").FullName);
        guardCounter.Should().Contain("private const float GuardedHitWindowSeconds = 8f;");
        guardCounter.Should().Contain("ApplyGuardCounter(activator, target, targetLocation, 8, 16, false);");
        guardCounter.Should().Contain("ApplyGuardCounter(activator, target, targetLocation, 18, 30, false);");
        guardCounter.Should().Contain("ApplyGuardCounter(activator, target, targetLocation, 28, 45, true);");
        guardCounter.Should().Contain("? typeof(DazedStatusEffect)");

        var ironElbows = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Katar" / "IronElbowsAbilityDefinition.cs").FullName);
        ironElbows.Should().Contain("Ability.ApplyCombatImpact(activator, activator, GetLocation(activator), SkillType.Katar, 15, 0, null, true, enmityBonus: 350);");

        var twinIntercept = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Katar" / "TwinInterceptAbilityDefinition.cs").FullName);
        twinIntercept.Should().Contain(".HasMaxRange(6f)");
        twinIntercept.Should().Contain("GetMaxHitPoints(activator) * 0.2f");
        twinIntercept.Should().Contain("ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(shield), target, 8f);");
        twinIntercept.Should().Contain("StatusEffect.ApplyStatusEffect(activator, target, typeof(TwinInterceptStatusEffect), 8f);");
        twinIntercept.Should().Contain("ModifyEnmityNearAlly(activator, target, 450);");

        var whirlingGuard = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "WhirlingGuardStatusEffect.cs").FullName);
        whirlingGuard.Should().Contain("StatGroup.Stats[StatType.Guard] = 20;");
        whirlingGuard.Should().Contain("StatGroup.Stats[StatType.GuardRetaliationDamage] = 8;");

        var breakerReversal = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Katar" / "BreakerReversalAbilityDefinition.cs").FullName);
        breakerReversal.Should().Contain("private const float GuardedHitWindowSeconds = 8f;");
        breakerReversal.Should().Contain("SkillType.Katar, 35, 12, typeof(ExposedStatusEffect), false");

        var adamantineGuard = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "AdamantineGuardStatusEffect.cs").FullName);
        adamantineGuard.Should().Contain("StatGroup.Stats[StatType.Guard] = 25;");
        adamantineGuard.Should().Contain("StatGroup.Stats[StatType.GuardDamageReductionPercentAdjustment] = 20;");
        adamantineGuard.Should().Contain("StatGroup.Stats[StatType.GuardEnmityPercentAdjustment] = 75;");
    }

    private static void AssertPerkLevel(
        PerkDetail perk,
        string name,
        int level,
        int price,
        int skillRank,
        FeatType? grantedFeat,
        string description,
        params StatType[] statTypes)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.KatarIronGuard);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Katar, skillRank);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
            perkLevel.StatBonuses.Select(x => x.Stat).Should().HaveCount(statTypes.Length).And.Contain(statTypes);
        else
            perkLevel.StatBonuses.Should().BeEmpty();
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int? staminaCost,
        bool isHostile,
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Katar);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(activationType);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.BreaksStealth.Should().BeTrue();

        if (staminaCost.HasValue)
        {
            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredSTM
                .Should()
                .Be(staminaCost.Value);
        }
        else
        {
            ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
        }

        ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();
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

    private static Dictionary<PerkType, PerkDetail> BuildKatarIronGuardPerksWithout2daLookup()
    {
        var definition = new KatarPerkDefinition();
        var methodNames = new[]
        {
            "AdamantineGuard",
            "BreakerReversal",
            "CoveringClaws",
            "GuardCounter",
            "GuardTraining",
            "GuardianReflexes",
            "ImpenetrableGrip",
            "IronElbows",
            "IronWallStance",
            "RedirectingGuard",
            "RetaliatoryFlow",
            "TwinGuardStance",
            "TwinIntercept",
            "WhirlingGuard"
        };

        foreach (var methodName in methodNames)
        {
            typeof(KatarPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(KatarPerkDefinition)
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
            for (var i = 0; i < header.Length && i + 1 < cells.Length; i++)
            {
                values[header[i]] = cells[i + 1];
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
