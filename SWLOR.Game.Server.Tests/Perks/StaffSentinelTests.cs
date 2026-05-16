using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Staff;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class StaffSentinelTests
{
    [Test]
    public void StaffSentinelPerkLevels_MatchCombatBible()
    {
        var perks = BuildStaffSentinelPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.FlurryStyle], "Flurry Style", 1, 2, 5, null,
            "Staff attack delay is reduced by 10%.",
            StatType.AttackDelayReductionPercent);
        AssertPerkLevel(perks[PerkType.StaffParry], "Staff Parry", 1, 2, 8, null,
            "Gain +10 Attack Deflection while wielding a staff.",
            StatType.AttackDeflection);
        AssertPerkLevel(perks[PerkType.LegSweep], "Leg Sweep", 1, 3, 12, FeatType.LegSweep1,
            "Deals weapon DMG + 6 and inflicts Knockdown for 3 seconds.");
        AssertPerkLevel(perks[PerkType.SentinelStance], "Sentinel Stance", 1, 2, 15, FeatType.SentinelStance1,
            "While active, grants +15% Evasion and +15 Attack Deflection, but reduces Attack by 15%.");
        AssertPerkLevel(perks[PerkType.StaffParry], "Staff Parry", 2, 4, 18, null,
            "Gain +20 Attack Deflection total while wielding a staff.",
            StatType.AttackDeflection);
        AssertPerkLevel(perks[PerkType.GuardingStep], "Guarding Step", 1, 3, 20, FeatType.GuardingStep1,
            "Gain +25% Evasion and +20% Defense for 8 seconds.");
        AssertPerkLevel(perks[PerkType.LegSweep], "Leg Sweep", 2, 2, 22, FeatType.LegSweep2,
            "Deals weapon DMG + 16 and inflicts Knockdown for 3 seconds.");
        AssertPerkLevel(perks[PerkType.LineBreaker], "Line Breaker", 1, 3, 25, FeatType.LineBreaker1,
            "Deals weapon DMG + 18 to enemies in a line. Inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.StaffParry], "Staff Parry", 3, 4, 28, null,
            "Gain +30 Attack Deflection total while wielding a staff. Deflecting attacks restores 2 STM.",
            StatType.AttackDeflection,
            StatType.DeflectionStaminaRestore);
        AssertPerkLevel(perks[PerkType.SentinelGuard], "Sentinel Guard", 1, 3, 30, FeatType.SentinelGuard1,
            "For 12 seconds, allies within 5 meters gain +10 Attack Deflection and you generate extra enmity.");
        AssertPerkLevel(perks[PerkType.FlowingDefense], "Flowing Defense", 1, 2, 32, null,
            "After dodging or deflecting an attack, your next Staff ability costs 2 less STM.",
            StatType.AvoidedAttackNextSkillAbilitySkillType,
            StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment,
            StatType.AvoidedAttackNextSkillAbilityWindowSeconds);
        AssertPerkLevel(perks[PerkType.LegSweep], "Leg Sweep", 3, 4, 35, FeatType.LegSweep3,
            "Deals weapon DMG + 26 and inflicts Knockdown for 4 seconds.");
        AssertPerkLevel(perks[PerkType.SweepingGuard], "Sweeping Guard", 1, 3, 38, FeatType.SweepingGuard1,
            "Deals weapon DMG + 18 to all nearby enemies. Inflicts Knockdown for 2 seconds. You gain +20% Defense for 10 seconds.");
        AssertPerkLevel(perks[PerkType.PatientSentinel], "Patient Sentinel", 1, 3, 40, null,
            "If you have not used a combat ability for 6 seconds, your next Staff ability gains +15% accuracy and deals +15 DMG.",
            StatType.IdleSkillAbilitySkillType,
            StatType.IdleSkillAbilityRequiredIdleSeconds,
            StatType.IdleSkillAbilityHitChancePercentAdjustment,
            StatType.IdleSkillAbilityDamageBonus);
        AssertPerkLevel(perks[PerkType.StaffParry], "Staff Parry", 4, 4, 42, null,
            "Gain +40 Attack Deflection total while wielding a staff. Deflecting attacks restores 4 STM.",
            StatType.AttackDeflection,
            StatType.DeflectionStaminaRestore);
        AssertPerkLevel(perks[PerkType.ShelterCircle], "Shelter Circle", 1, 3, 45, FeatType.ShelterCircle1,
            "Allies in an area of effect (sphere) gain +20% Defense and +20% Evasion for 15 seconds.");
        AssertPerkLevel(perks[PerkType.PerfectFootwork], "Perfect Footwork", 1, 4, 48, null,
            "When reduced below 40% HP, gain +30% Evasion for 10 seconds. This can only trigger once every 3 minutes.",
            StatType.LowHPEvasionThresholdPercent,
            StatType.LowHPEvasionPercentAdjustment,
            StatType.LowHPEvasionDurationSeconds,
            StatType.LowHPEvasionCooldownSeconds);
        AssertPerkLevel(perks[PerkType.UnmovingCenter], "Unmoving Center", 1, 4, 50, FeatType.UnmovingCenter1,
            "For 20 seconds, you cannot be Knocked down or Dazed, gain +50 Attack Deflection, and staff attacks generate extra enmity.");
    }

    [Test]
    public void StaffSentinelAbilities_MatchCombatBible()
    {
        var legSweep = new LegSweepAbilityDefinition().BuildAbilities();
        AssertAbility(legSweep[FeatType.LegSweep1], "Leg Sweep I", 1, RecastGroup.LegSweep, 45f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(legSweep[FeatType.LegSweep2], "Leg Sweep II", 2, RecastGroup.LegSweep, 45f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(legSweep[FeatType.LegSweep3], "Leg Sweep III", 3, RecastGroup.LegSweep, 45f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var sentinelStance = new SentinelStanceAbilityDefinition().BuildAbilities()[FeatType.SentinelStance1];
        AssertAbility(sentinelStance, "Sentinel Stance", 1, RecastGroup.SentinelStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var guardingStep = new GuardingStepAbilityDefinition().BuildAbilities()[FeatType.GuardingStep1];
        AssertAbility(guardingStep, "Guarding Step", 1, RecastGroup.GuardingStep, 60f, 0f, 6, false, false, false, false, AbilityActivationType.Casted);

        var lineBreaker = new LineBreakerAbilityDefinition().BuildAbilities()[FeatType.LineBreaker1];
        AssertAbility(lineBreaker, "Line Breaker", 1, RecastGroup.LineBreaker, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var sentinelGuard = new SentinelGuardAbilityDefinition().BuildAbilities()[FeatType.SentinelGuard1];
        AssertAbility(sentinelGuard, "Sentinel Guard", 1, RecastGroup.SentinelGuard, 120f, 0f, 10, false, false, false, true, AbilityActivationType.Casted);

        var sweepingGuard = new SweepingGuardAbilityDefinition().BuildAbilities()[FeatType.SweepingGuard1];
        AssertAbility(sweepingGuard, "Sweeping Guard", 1, RecastGroup.SweepingGuard, 90f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var shelterCircle = new ShelterCircleAbilityDefinition().BuildAbilities()[FeatType.ShelterCircle1];
        AssertAbility(shelterCircle, "Shelter Circle", 1, RecastGroup.ShelterCircle, 180f, 0f, 12, false, false, false, true, AbilityActivationType.Casted);

        var unmovingCenter = new UnmovingCenterAbilityDefinition().BuildAbilities()[FeatType.UnmovingCenter1];
        AssertAbility(unmovingCenter, "Unmoving Center", 1, RecastGroup.Capstone, 1800f, 1f, 25, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void StaffSentinelStatusEffects_MatchCombatBible()
    {
        var sentinelStance = new SentinelStanceStatusEffect();
        sentinelStance.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(15);
        sentinelStance.StatGroup.Stats[StatType.AttackDeflection].Should().Be(15);
        sentinelStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);

        var guardingStep = new GuardingStepStatusEffect();
        guardingStep.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(25);
        guardingStep.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(20);
        guardingStep.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(20);

        var sentinelGuardAlly = new SentinelGuardStatusEffect();
        sentinelGuardAlly.StatGroup.Stats[StatType.AttackDeflection].Should().Be(10);

        var sentinelGuardSelf = new SentinelGuardStatusEffect();
        sentinelGuardSelf.ApplyEffect(1, 1, 12);
        sentinelGuardSelf.StatGroup.Stats.Should().NotContainKey(StatType.AttackDeflection);
        sentinelGuardSelf.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(20);

        var sweepingGuard = new SweepingGuardStatusEffect();
        sweepingGuard.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(20);
        sweepingGuard.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(20);

        var shelterCircle = new ShelterCircleStatusEffect();
        shelterCircle.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(20);
        shelterCircle.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(20);
        shelterCircle.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(20);

        var unmovingCenter = new UnmovingCenterStatusEffect();
        unmovingCenter.StatGroup.Stats[StatType.AttackDeflection].Should().Be(50);
        unmovingCenter.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(30);
        unmovingCenter.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(0);
        unmovingCenter.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);
    }

    [Test]
    public void StaffSentinelFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.LegSweep1, "ife_legswp1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.LegSweep2, "ife_legswp2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.LegSweep3, "ife_legswp3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SentinelStance1, "ife_sentstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardingStep1, "ife_grdstep1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.LineBreaker1, "ife_linebrkr1", "M", "0x3E", "1", "rectangle", "2.5", "8", "17"),
            (FeatType.SentinelGuard1, "ife_sentgrd1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ShelterCircle1, "ife_shelcirc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SweepingGuard1, "ife_swpnggrd1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.UnmovingCenter1, "ife_unmovcntr1", "P", "0x01", "0", "****", "****", "****", "****")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            spellRow["Range"].Should().Be(range);
            spellRow["TargetType"].Should().Be(targetType);
            spellRow["HostileSetting"].Should().Be(hostileSetting);
            spellRow["TargetShape"].Should().Be(targetShape);
            spellRow["TargetSizeX"].Should().Be(targetSizeX);
            spellRow["TargetSizeY"].Should().Be(targetSizeY);
            spellRow["TargetFlags"].Should().Be(targetFlags);
        }
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
        perk.Category.Should().Be(PerkCategoryType.StaffSentinel);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Staff, skillRank);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
            perkLevel.StatBonuses.Select(x => x.Stat).Should().Contain(statTypes);
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

    private static Dictionary<PerkType, PerkDetail> BuildStaffSentinelPerksWithout2daLookup()
    {
        var definition = new StaffPerkDefinition();
        var methodNames = new[]
        {
            "FlurryStyle",
            "FlowingDefense",
            "GuardingStep",
            "LegSweep",
            "LineBreaker",
            "PatientSentinel",
            "PerfectFootwork",
            "SentinelGuard",
            "SentinelStance",
            "ShelterCircle",
            "StaffParry",
            "SweepingGuard",
            "UnmovingCenter"
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
