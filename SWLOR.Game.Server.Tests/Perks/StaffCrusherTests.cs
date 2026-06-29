using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Staff;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class StaffCrusherTests
{
    [Test]
    public void StaffCrusherAbilities_MatchCombatBible()
    {
        var slam = new SlamAbilityDefinition().BuildAbilities();
        AssertAbility(slam[FeatType.Slam1], "Slam I", 1, RecastGroup.Slam, 30f, 0f, 2, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(slam[FeatType.Slam2], "Slam II", 2, RecastGroup.Slam, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(slam[FeatType.Slam3], "Slam III", 3, RecastGroup.Slam, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var crusherStance = new CrusherStanceAbilityDefinition().BuildAbilities()[FeatType.CrusherStance1];
        AssertAbility(crusherStance, "Crusher Stance", 1, RecastGroup.CrusherStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var ribBreaker = new RibBreakerAbilityDefinition().BuildAbilities();
        AssertAbility(ribBreaker[FeatType.RibBreaker1], "Rib Breaker I", 1, RecastGroup.RibBreaker, 45f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(ribBreaker[FeatType.RibBreaker2], "Rib Breaker II", 2, RecastGroup.RibBreaker, 45f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(ribBreaker[FeatType.RibBreaker3], "Rib Breaker III", 3, RecastGroup.RibBreaker, 45f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var groundQuake = new GroundQuakeAbilityDefinition().BuildAbilities();
        AssertAbility(groundQuake[FeatType.GroundQuake1], "Ground Quake I", 1, RecastGroup.GroundQuake, 60f, 0f, 5, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(groundQuake[FeatType.GroundQuake2], "Ground Quake II", 2, RecastGroup.GroundQuake, 60f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var bonecrusher = new BonecrusherAbilityDefinition().BuildAbilities()[FeatType.Bonecrusher1];
        AssertAbility(bonecrusher, "Bonecrusher", 1, RecastGroup.Bonecrusher, 120f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var worldbreaker = new WorldbreakerAbilityDefinition().BuildAbilities()[FeatType.Worldbreaker1];
        AssertAbility(worldbreaker, "Worldbreaker", 1, RecastGroup.Capstone, 345f, 2f, 15, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void Worldbreaker_DeclaresSelfCenteredTargetingPreview()
    {
        var worldbreaker = new WorldbreakerAbilityDefinition().BuildAbilities()[FeatType.Worldbreaker1];

        worldbreaker.Targeting.Should().NotBeNull();
        worldbreaker.Targeting!.Spell.Should().Be(Spell.Worldbreaker1);
        worldbreaker.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
        worldbreaker.Targeting.SizeX.Should().Be(10f);
        worldbreaker.Targeting.SizeY.Should().Be(0f);
        worldbreaker.Targeting.Flags.Should().Be(
            AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);
    }

    [Test]
    public void StaffCrusherStatusEffects_MatchCombatBible()
    {
        var crusherStance = new CrusherStanceStatusEffect();
        crusherStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(20);
        crusherStance.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(15);
        crusherStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-20);
        crusherStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-20);

        new WeakenedStatusEffect(10).StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
        new WeakenedStatusEffect().StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        new WeakenedStatusEffect(20).StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);

        new ExposedStatusEffect(-10).StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-10);

        Stat.GetStatTypeCategory(StatType.CriticalTargetDefensePercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenNegative);
        Stat.GetStatTypeCategory(StatType.CriticalTargetDefenseDurationSeconds).Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.CriticalDamagePercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.CriticalRatePercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.WeaponMightModifierDamageMultiplier).Should().Be(StatTypeCategory.BeneficialWhenPositive);

        var worldbreaker = new WorldbreakerStatusEffect();
        worldbreaker.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(-10);
    }

    [Test]
    public void BreakPosture_IsUniversalCriticalExposure()
    {
        var perks = BuildStaffCrusherPerksWithout2daLookup();
        var breakPosture = perks[PerkType.BreakPosture];

        AssertPerkLevel(
            breakPosture,
            "Break Posture",
            1,
            2,
            40,
            FeatType.BreakPostureTrait,
            "Critical hits with any weapon inflict Exposed, reducing Defense by 10% for 10 seconds.",
            StatType.CriticalTargetDefensePercentAdjustment,
            StatType.CriticalTargetDefenseDurationSeconds);
        AssertStatBonus(breakPosture.PerkLevels[1], StatType.CriticalTargetDefensePercentAdjustment, -10);
        AssertStatBonus(breakPosture.PerkLevels[1], StatType.CriticalTargetDefenseDurationSeconds, 10);
        breakPosture.PerkLevels[1].StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.StaffCriticalTargetDefensePercentAdjustment);
        breakPosture.PerkLevels[1].StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.StaffCriticalTargetDefenseDurationSeconds);
    }

    [Test]
    public void CrushingStyle_IsUniversalWeaponStyle()
    {
        var perks = BuildStaffCrusherPerksWithout2daLookup();
        var crushingStyle = perks[PerkType.CrushingStyle];

        AssertPerkLevel(
            crushingStyle,
            "Crushing Style",
            1,
            3,
            25,
            FeatType.CrushingStyleTrait,
            "Gains +1 weapon DMG for every 2 Might over 10 with all weapons and +10% critical chance.",
            StatType.WeaponMightModifierDamageMultiplier,
            StatType.CriticalRatePercentAdjustment);
        AssertStatBonus(crushingStyle.PerkLevels[1], StatType.WeaponMightModifierDamageMultiplier, 1);
        AssertStatBonus(crushingStyle.PerkLevels[1], StatType.CriticalRatePercentAdjustment, 10);
    }

    [Test]
    public void CrushingMastery_IsUniversalWeaponMastery()
    {
        var perks = BuildStaffCrusherPerksWithout2daLookup();
        var crushingMastery = perks[PerkType.CrushingMastery];

        AssertPerkLevel(
            crushingMastery,
            "Crushing Mastery",
            1,
            3,
            12,
            FeatType.CrushingMasteryTrait,
            "Critical hits with any weapon deal +10% damage and restore 2 STM. This can only trigger once every 6 seconds.",
            StatType.CriticalDamagePercentAdjustment,
            StatType.CriticalStaminaRestore,
            StatType.CriticalStaminaRestoreCooldownSeconds);
        AssertStatBonus(crushingMastery.PerkLevels[1], StatType.CriticalDamagePercentAdjustment, 10);
        AssertStatBonus(crushingMastery.PerkLevels[1], StatType.CriticalStaminaRestore, 2);
        AssertStatBonus(crushingMastery.PerkLevels[1], StatType.CriticalStaminaRestoreCooldownSeconds, 6);

        AssertPerkLevel(
            crushingMastery,
            "Crushing Mastery",
            2,
            2,
            32,
            null,
            "Bonus damage with all weapons increases to 2x your MGT modifier and critical chance increases by an additional 10%. Critical hits with any weapon still deal +10% damage and restore 2 STM once every 6 seconds.",
            StatType.CriticalDamagePercentAdjustment,
            StatType.CriticalStaminaRestore,
            StatType.CriticalStaminaRestoreCooldownSeconds,
            StatType.WeaponMightModifierDamageMultiplier,
            StatType.CriticalRatePercentAdjustment);
        AssertStatBonus(crushingMastery.PerkLevels[2], StatType.CriticalDamagePercentAdjustment, 10);
        AssertStatBonus(crushingMastery.PerkLevels[2], StatType.CriticalStaminaRestore, 2);
        AssertStatBonus(crushingMastery.PerkLevels[2], StatType.CriticalStaminaRestoreCooldownSeconds, 6);
        AssertStatBonus(crushingMastery.PerkLevels[2], StatType.WeaponMightModifierDamageMultiplier, 1);
        AssertStatBonus(crushingMastery.PerkLevels[2], StatType.CriticalRatePercentAdjustment, 10);

        AssertPerkLevel(
            crushingMastery,
            "Crushing Mastery",
            3,
            4,
            48,
            null,
            "Critical hits with any weapon deal +20% damage and restore 4 STM once every 6 seconds. Bonus damage with all weapons remains 2x your MGT modifier and the additional +10% critical chance remains.",
            StatType.CriticalDamagePercentAdjustment,
            StatType.CriticalStaminaRestore,
            StatType.CriticalStaminaRestoreCooldownSeconds,
            StatType.WeaponMightModifierDamageMultiplier,
            StatType.CriticalRatePercentAdjustment);
        AssertStatBonus(crushingMastery.PerkLevels[3], StatType.CriticalDamagePercentAdjustment, 20);
        AssertStatBonus(crushingMastery.PerkLevels[3], StatType.CriticalStaminaRestore, 4);
        AssertStatBonus(crushingMastery.PerkLevels[3], StatType.CriticalStaminaRestoreCooldownSeconds, 6);
        AssertStatBonus(crushingMastery.PerkLevels[3], StatType.WeaponMightModifierDamageMultiplier, 1);
        AssertStatBonus(crushingMastery.PerkLevels[3], StatType.CriticalRatePercentAdjustment, 10);

        foreach (var perkLevel in crushingMastery.PerkLevels.Values)
        {
            perkLevel.StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.StaffCriticalDamagePercentAdjustment);
            perkLevel.StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.StaffMightModifierDamageMultiplier);
            perkLevel.StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.StaffCriticalRatePercentAdjustment);
            perkLevel.StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.CriticalStaminaRestoreSkillType);
        }
    }

    [Test]
    public void EarlyCrusherActives_MatchLowLevelTuning()
    {
        var perks = BuildStaffCrusherPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.Slam], "Slam", 1, 3, 2, FeatType.Slam1, "Deals weapon DMG + 4 and inflicts Blind for 6 seconds.");
        AssertPerkLevel(perks[PerkType.GroundQuake], "Ground Quake", 1, 3, 8, FeatType.GroundQuake1, "Deals weapon DMG + 8 to nearby enemies. Inflicts Knockdown for 1 second.");
    }

    [Test]
    public void StaffCrusherFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.Slam1, "ife_slm1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Slam2, "ife_slm2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Slam3, "ife_slm3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CrusherStance1, "ife_crushstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.RibBreaker1, "ife_ribbrkr1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.RibBreaker2, "ife_ribbrkr2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.RibBreaker3, "ife_ribbrkr3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.GroundQuake1, "ife_grndquake1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.GroundQuake2, "ife_grndquake2", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.Bonecrusher1, "ife_bone1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.Worldbreaker1, "ife_worldbrk1", "P", "0x01", "1", "sphere", "10", "****", "17")
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
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

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
    public void StaffCrusherImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();
        var perkSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "StaffPerkDefinition.cs").FullName);

        perkSource.Should().Contain("StatType.CriticalTargetDefensePercentAdjustment, -10");
        perkSource.Should().Contain("StatType.CriticalTargetDefenseDurationSeconds, 10");
        perkSource.Should().Contain("StatType.CriticalDamagePercentAdjustment, 20");
        perkSource.Should().Contain("StatType.CriticalRatePercentAdjustment, 10");
        perkSource.Should().Contain("StatType.WeaponMightModifierDamageMultiplier, 1");
        perkSource.Should().NotContain("StatType.StaffCriticalTargetDefensePercentAdjustment");
        perkSource.Should().NotContain("StatType.StaffCriticalTargetDefenseDurationSeconds");
        perkSource.Should().NotContain("StatType.StaffCriticalDamagePercentAdjustment");
        perkSource.Should().NotContain("StatType.StaffMightModifierDamageMultiplier");
        perkSource.Should().NotContain("StatType.StaffCriticalRatePercentAdjustment");
        perkSource.Should().NotContain("StatType.CriticalStaminaRestoreSkillType");

        var slam = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Staff" / "SlamAbilityDefinition.cs").FullName).Replace("\r\n", "\n");
        slam.Should().Contain("SkillType.Staff,\n                4,\n                6,");
        slam.Should().Contain("SkillType.Staff,\n                20,\n                10,");
        slam.Should().Contain("SkillType.Staff,\n                32,\n                12,");

        var ribBreaker = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Staff" / "RibBreakerAbilityDefinition.cs").FullName);
        ribBreaker.Should().Contain("SkillType.Staff, 18, 15, typeof(WeakenedStatusEffect)");
        ribBreaker.Should().Contain("new WeakenedStatusEffect(10)");
        ribBreaker.Should().Contain("SkillType.Staff, 30, 15, typeof(WeakenedStatusEffect)");
        ribBreaker.Should().Contain("SkillType.Staff, 42, 15, typeof(WeakenedStatusEffect)");
        ribBreaker.Should().Contain("new WeakenedStatusEffect(20)");

        var groundQuake = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Staff" / "GroundQuakeAbilityDefinition.cs").FullName).Replace("\r\n", "\n");
        groundQuake.Should().Contain("SkillType.Staff,\n                8,\n                1,\n                typeof(KnockdownStatusEffect)");
        groundQuake.Should().Contain("SkillType.Staff,\n                28,\n                3,\n                typeof(KnockdownStatusEffect)");
        groundQuake.Should().Contain("centerOnActivator: true");
        groundQuake.Should().Contain("areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Shake");

        var bonecrusher = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Staff" / "BonecrusherAbilityDefinition.cs").FullName);
        bonecrusher.Should().Contain("StatusEffect.HasStatusEffect(target, typeof(KnockdownStatusEffect))");
        bonecrusher.Should().Contain("? typeof(StunnedStatusEffect)");
        bonecrusher.Should().Contain("var duration = statusEffect == null ? 0 : 3;");
        bonecrusher.Should().Contain("SkillType.Staff, 50, duration, statusEffect");

        var worldbreaker = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Staff" / "WorldbreakerAbilityDefinition.cs").FullName);
        worldbreaker.Should().Contain("HasActivationDelay(2f)");
        worldbreaker.Should().Contain("WorldbreakerRadius = 10f");
        worldbreaker.Should().Contain("SkillType.Staff");
        worldbreaker.Should().Contain("25");
        worldbreaker.Should().Contain("45");
        worldbreaker.Should().Contain("typeof(WorldbreakerStatusEffect)");
        worldbreaker.Should().Contain("afterSuccessfulHit: affectedEnemy");
        worldbreaker.Should().Contain("typeof(KnockdownStatusEffect)");
        worldbreaker.Should().Contain("3f");
        worldbreaker.Should().Contain("centerOnActivator: true");
        worldbreaker.Should().Contain("areaVisualEffect: VisualEffect.Vfx_Fnf_Screen_Shake");
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
        perk.Category.Should().Be(PerkCategoryType.StaffCrusher);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Staff, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Staff);
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

    private static Dictionary<PerkType, PerkDetail> BuildStaffCrusherPerksWithout2daLookup()
    {
        var definition = new StaffPerkDefinition();
        var methodNames = new[]
        {
            "Bonecrusher",
            "BreakPosture",
            "CrusherStance",
            "CrushingStyle",
            "CrushingMastery",
            "GroundQuake",
            "HeavyHands",
            "RibBreaker",
            "Slam",
            "SkullRattle",
            "Worldbreaker"
        };

        foreach (var methodName in methodNames)
        {
            typeof(StaffPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(StaffPerkDefinition)
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
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
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
