using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Rifle;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class RifleMarksmanTests
{
    [Test]
    public void RifleMarksmanPerkLevels_MatchCombatBible()
    {
        var perks = BuildRifleMarksmanPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.SteadyAim], "Steady Aim", 1, 3, 5, null,
            "Rifle combat abilities gain +5% accuracy and +5% critical chance.",
            StatType.AbilityHitChancePercentAdjustmentSkillType,
            StatType.AbilityHitChancePercentAdjustment,
            StatType.AbilityCriticalRatePercentAdjustmentSkillType,
            StatType.AbilityCriticalRatePercentAdjustment);
        AssertPerkLevel(perks[PerkType.AimedShot], "Aimed Shot", 1, 3, 8, FeatType.AimedShot1,
            "Deals weapon DMG + 18. If the target is farther than 8 meters away, deal an additional +10 DMG.");
        AssertPerkLevel(perks[PerkType.PiercingRound], "Piercing Round", 1, 3, 12, FeatType.PiercingRound1,
            "Deals weapon DMG + 14 and inflicts Sunder, reducing Defense by 10% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.SniperStance], "Sniper Stance", 1, 2, 15, FeatType.SniperStance1,
            "While active, grants +20% Attack and +15% critical damage, but reduces Evasion and Defense by 20%.");
        AssertPerkLevel(perks[PerkType.AimedShot], "Aimed Shot", 2, 4, 18, FeatType.AimedShot2,
            "Deals weapon DMG + 32. If the target is farther than 8 meters away, deal an additional +16 DMG.");
        AssertPerkLevel(perks[PerkType.ScopeCalibration], "Scope Calibration", 1, 3, 20, null,
            "Rifle critical hits deal +15% damage.",
            StatType.CriticalDamagePercentAdjustment);
        AssertPerkLevel(perks[PerkType.PiercingRound], "Piercing Round", 2, 2, 22, FeatType.PiercingRound2,
            "Deals weapon DMG + 26 and inflicts Sunder, reducing Defense by 15% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.SuppressiveLine], "Suppressive Line", 1, 3, 25, FeatType.SuppressiveLine1,
            "Deals weapon DMG + 22 to enemies in a line. Inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.AimedShot], "Aimed Shot", 3, 3, 28, FeatType.AimedShot3,
            "Deals weapon DMG + 46. If the target is farther than 8 meters away, deal an additional +24 DMG.");
        AssertPerkLevel(perks[PerkType.ExposeWeakPoint], "Expose Weak Point", 1, 3, 30, FeatType.ExposeWeakPoint1,
            "Deals weapon DMG + 20 and marks the target for 12 seconds. Physical attacks against the marked target deal +10% damage.");
        AssertPerkLevel(perks[PerkType.SteadyAim], "Steady Aim", 2, 2, 32, null,
            "Aimed Shot cooldowns are reduced by 5 seconds and gain an additional +10% accuracy.",
            StatType.AbilityHitChancePercentAdjustmentSkillType,
            StatType.AbilityHitChancePercentAdjustment,
            StatType.AbilityCriticalRatePercentAdjustmentSkillType,
            StatType.AbilityCriticalRatePercentAdjustment,
            StatType.AbilityHitChancePercentAdjustmentPerkType,
            StatType.TargetedAbilityHitChancePercentAdjustment,
            StatType.AbilityRecastDelayFlatAdjustmentPerkType,
            StatType.AbilityRecastDelayFlatAdjustment);
        AssertPerkLevel(perks[PerkType.BreachRound], "Breach Round", 1, 4, 35, FeatType.BreachRound1,
            "Deals weapon DMG + 35 and ignores 25% of the target's Defense.",
            StatType.AbilityDefenseIgnorePercentAdjustmentPerkType,
            StatType.AbilityDefenseIgnorePercentAdjustment);
        AssertPerkLevel(perks[PerkType.DeadCenter], "Dead Center", 1, 3, 38, null,
            "Rifle critical hits restore 4 STM and cause your next Aimed Shot within 8 seconds to deal +10 DMG. This can only trigger once every 6 seconds.",
            StatType.CriticalStaminaRestore,
            StatType.CriticalStaminaRestoreSkillType,
            StatType.CriticalStaminaRestoreCooldownSeconds,
            StatType.CriticalNextAbilityDamageBonusTriggerSkillType,
            StatType.CriticalNextAbilityDamageBonusPerkType,
            StatType.CriticalNextAbilityDamageBonus,
            StatType.CriticalNextAbilityDamageBonusDurationSeconds,
            StatType.CriticalNextAbilityDamageBonusCooldownSeconds);
        AssertPerkLevel(perks[PerkType.PiercingRound], "Piercing Round", 3, 2, 40, FeatType.PiercingRound3,
            "Deals weapon DMG + 38 and inflicts Sunder, reducing Defense by 20% for 15 seconds.");
        AssertPerkLevel(perks[PerkType.Headshot], "Headshot", 1, 4, 42, FeatType.Headshot1,
            "Deals weapon DMG + 60. Targets below 50% HP become Dazed for 3 seconds.");
        AssertPerkLevel(perks[PerkType.KillZone], "Kill Zone", 1, 3, 45, FeatType.KillZone1,
            "For 20 seconds, repeated attacks against the same target stack +4% rifle damage, up to +20%. Switching targets clears this bonus.");
        AssertPerkLevel(perks[PerkType.BallisticMastery], "Ballistic Mastery", 1, 4, 48, null,
            "Rifle abilities against Exposed or Sundered targets ignore an additional 15% Defense.",
            StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType,
            StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment);
        AssertPerkLevel(perks[PerkType.OneShot], "One Shot", 1, 4, 50, FeatType.OneShot1,
            "Deals weapon DMG + 100 to one target. If this defeats the target, restore 25 STM and gain +15% Attack for 15 seconds.");
    }

    [Test]
    public void RifleMarksmanAbilities_MatchCombatBible()
    {
        var aimedShot = new AimedShotAbilityDefinition().BuildAbilities();
        AssertAbility(aimedShot[FeatType.AimedShot1], "Aimed Shot I", 1, RecastGroup.AimedShot, 30f, 1f, 4, true, true, true, false, AbilityActivationType.Casted, 18f);
        AssertAbility(aimedShot[FeatType.AimedShot2], "Aimed Shot II", 2, RecastGroup.AimedShot, 30f, 1f, 6, true, true, true, false, AbilityActivationType.Casted, 18f);
        AssertAbility(aimedShot[FeatType.AimedShot3], "Aimed Shot III", 3, RecastGroup.AimedShot, 30f, 1f, 8, true, true, true, false, AbilityActivationType.Casted, 18f);

        var piercingRound = new PiercingRoundAbilityDefinition().BuildAbilities();
        AssertAbility(piercingRound[FeatType.PiercingRound1], "Piercing Round I", 1, RecastGroup.PiercingRound, 45f, 0f, 5, true, true, true, false, AbilityActivationType.Casted, 18f);
        AssertAbility(piercingRound[FeatType.PiercingRound2], "Piercing Round II", 2, RecastGroup.PiercingRound, 45f, 0f, 7, true, true, true, false, AbilityActivationType.Casted, 18f);
        AssertAbility(piercingRound[FeatType.PiercingRound3], "Piercing Round III", 3, RecastGroup.PiercingRound, 45f, 0f, 8, true, true, true, false, AbilityActivationType.Casted, 18f);

        var sniperStance = new SniperStanceAbilityDefinition().BuildAbilities()[FeatType.SniperStance1];
        AssertAbility(sniperStance, "Sniper Stance", 1, RecastGroup.SniperStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var suppressiveLine = new SuppressiveLineAbilityDefinition().BuildAbilities()[FeatType.SuppressiveLine1];
        AssertAbility(suppressiveLine, "Suppressive Line", 1, RecastGroup.SuppressiveLine, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var exposeWeakPoint = new ExposeWeakPointAbilityDefinition().BuildAbilities()[FeatType.ExposeWeakPoint1];
        AssertAbility(exposeWeakPoint, "Expose Weak Point", 1, RecastGroup.ExposeWeakPoint, 75f, 0f, 8, true, true, true, false, AbilityActivationType.Casted, 18f);

        var breachRound = new BreachRoundAbilityDefinition().BuildAbilities()[FeatType.BreachRound1];
        AssertAbility(breachRound, "Breach Round", 1, RecastGroup.BreachRound, 90f, 0f, 10, true, true, true, false, AbilityActivationType.Casted, 18f);

        var headshot = new HeadshotAbilityDefinition().BuildAbilities()[FeatType.Headshot1];
        AssertAbility(headshot, "Headshot", 1, RecastGroup.Headshot, 120f, 1.5f, 14, true, true, true, false, AbilityActivationType.Casted, 18f);

        var killZone = new KillZoneAbilityDefinition().BuildAbilities()[FeatType.KillZone1];
        AssertAbility(killZone, "Kill Zone", 1, RecastGroup.KillZone, 120f, 0f, 10, false, false, false, false, AbilityActivationType.Casted);

        var oneShot = new OneShotAbilityDefinition().BuildAbilities()[FeatType.OneShot1];
        AssertAbility(oneShot, "One Shot", 1, RecastGroup.Capstone, 1800f, 2f, 25, true, true, true, false, AbilityActivationType.Casted, 18f);
    }

    [Test]
    public void RifleMarksmanStatusEffects_MatchCombatBible()
    {
        var sniperStance = new SniperStanceStatusEffect();
        sniperStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(20);
        sniperStance.StatGroup.Stats[StatType.CriticalDamagePercentAdjustment].Should().Be(15);
        sniperStance.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);
        sniperStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-20);
        sniperStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-20);

        var exposeWeakPoint = new ExposeWeakPointStatusEffect();
        exposeWeakPoint.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(10);

        new SunderStatusEffect(10).StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-10);
        new SunderStatusEffect().StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-15);
        new SunderStatusEffect(20).StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-20);

        var killZone = new KillZoneStatusEffect();
        killZone.StatGroup.Stats[StatType.RepeatedTargetDamageSkillType].Should().Be((int)SkillType.Rifle);
        killZone.StatGroup.Stats[StatType.RepeatedTargetDamagePercentPerHit].Should().Be(4);
        killZone.StatGroup.Stats[StatType.RepeatedTargetDamagePercentMax].Should().Be(20);
        killZone.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);
    }

    [Test]
    public void RifleMarksmanSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();

        var combat = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        combat.Should().Contain("typeof(ExposeWeakPointStatusEffect)");
        combat.Should().Contain("ApplyCriticalDamageModifier");

        var ability = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Ability.cs").FullName);
        ability.Should().Contain("Combat.ApplyCriticalDamageModifier");
    }

    [Test]
    public void RifleMarksmanFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.AimedShot1, "ife_aimshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.PiercingRound1, "ife_piercrnd1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SniperStance1, "ife_snipstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.AimedShot2, "ife_aimshot2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.PiercingRound2, "ife_piercrnd2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SuppressiveLine1, "ife_spprssvline1", "M", "0x3E", "1", "rectangle", "2.5", "8", "17"),
            (FeatType.AimedShot3, "ife_aimshot3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ExposeWeakPoint1, "ife_xposeweakpt1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.BreachRound1, "ife_brchrnd1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.PiercingRound3, "ife_piercrnd3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.Headshot1, "ife_head1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.KillZone1, "ife_killzone1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.OneShot1, "ife_oneshot1", "M", "0x02", "1", "****", "****", "****", "****")
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
        int skillRank,
        FeatType? grantedFeat,
        string description,
        params StatType[] statTypes)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.RifleMarksman);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Rifle, skillRank);

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
        AbilityActivationType activationType,
        float expectedMaxRange = 5f)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Rifle);
        ability.MaxRange.Should().Be(expectedMaxRange);
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

    private static Dictionary<PerkType, PerkDetail> BuildRifleMarksmanPerksWithout2daLookup()
    {
        var definition = new RiflePerkDefinition();
        var methodNames = new[]
        {
            "AimedShot",
            "BallisticMastery",
            "BreachRound",
            "DeadCenter",
            "ExposeWeakPoint",
            "Headshot",
            "KillZone",
            "OneShot",
            "PiercingRound",
            "ScopeCalibration",
            "SniperStance",
            "SteadyAim",
            "SuppressiveLine"
        };

        foreach (var methodName in methodNames)
        {
            typeof(RiflePerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(RiflePerkDefinition)
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
