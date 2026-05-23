using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class VibrobladeOffenseTests
{
    [Test]
    public void VibrobladeOffensePerkLevels_MatchCombatBible()
    {
        var perks = BuildVibrobladeOffensePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.SavageReflexes], "Savage Reflexes", 1, 2, 2, null,
            "Auto-attacks have 10% chance to deal +8 DMG.",
            StatType.AutoAttackDamageBonusChance,
            StatType.AutoAttackDamageBonus);
        AssertPerkLevel(perks[PerkType.HackingBlade], "Hacking Blade", 1, 2, 8, FeatType.HackingBlade1,
            "Your next attack deals an additional 8 DMG and inflicts Bleed for 30 seconds.");
        AssertPerkLevel(perks[PerkType.RiotBlade], "Riot Blade", 1, 2, 10, FeatType.RiotBlade1,
            "Instantly deals weapon DMG + 15 to your target.");
        AssertPerkLevel(perks[PerkType.WhirlwindAssault], "Whirlwind Assault", 1, 3, 12, FeatType.WhirlwindAssault1,
            "Deal weapon DMG + 12 to all nearby enemies.");
        AssertPerkLevel(perks[PerkType.BerserkerStance], "Berserker Stance", 1, 3, 15, FeatType.BerserkerStance1,
            "While active, grants +15% Attack, +10% Haste, -20% Defense, and -20% Force Defense.");
        AssertPerkLevel(perks[PerkType.RiotBlade], "Riot Blade", 2, 3, 18, FeatType.RiotBlade2,
            "Instantly deals weapon DMG + 30 to your target.");
        AssertPerkLevel(perks[PerkType.HackingBlade], "Hacking Blade", 2, 3, 20, FeatType.HackingBlade2,
            "Your next attack deals an additional 18 DMG and inflicts Bleed for 60 seconds.");
        AssertPerkLevel(perks[PerkType.RendingStrike], "Rending Strike", 1, 2, 22, FeatType.RendingStrike1,
            "Deals weapon DMG + 18. Inflicts Exposed which reduces Defense by 15% for 10s.");
        AssertPerkLevel(perks[PerkType.SavageCleave], "Savage Cleave", 1, 2, 25, FeatType.SavageCleave1,
            "Strike all enemies in front for weapon DMG + 25.");
        AssertPerkLevel(perks[PerkType.RiotBlade], "Riot Blade", 3, 3, 28, FeatType.RiotBlade3,
            "Instantly deals weapon DMG + 45 to your target.");
        AssertPerkLevel(perks[PerkType.WhirlwindAssault], "Whirlwind Assault", 2, 3, 30, FeatType.WhirlwindAssault2,
            "Deal weapon DMG + 20 to all nearby enemies.");
        AssertPerkLevel(perks[PerkType.Executioner], "Executioner", 1, 3, 32, null,
            "Deal +15% damage to targets below 30% HP.",
            StatType.TargetLowHPDamageThresholdPercent,
            StatType.TargetLowHPDamagePercentAdjustment);
        AssertPerkLevel(perks[PerkType.HackingBlade], "Hacking Blade", 3, 4, 35, FeatType.HackingBlade3,
            "Your next attack deals an additional 28 DMG and inflicts Bleed for 60 seconds.");
        AssertPerkLevel(perks[PerkType.RendingStrike], "Rending Strike", 2, 3, 38, FeatType.RendingStrike2,
            "Deals weapon DMG + 32. Inflicts Exposed which reduces Defense by 25% for 12s.");
        AssertPerkLevel(perks[PerkType.Bloodseeker], "Bloodseeker", 1, 3, 40, null,
            "Gain +10% Attack against bleeding targets.",
            StatType.AttackToBleedingTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.CrimsonFury], "Crimson Fury", 1, 3, 42, null,
            "Each bleeding enemy within 10m grants you +3% Attack (max +15%).",
            StatType.NearbyStatusTargetAttackPercentPerTarget,
            StatType.NearbyStatusTargetAttackRadiusMeters,
            StatType.NearbyStatusTargetAttackPercentMaximum,
            StatType.NearbyStatusTargetAttackStatusCategory);
        AssertPerkLevel(perks[PerkType.Carve], "Carve", 1, 3, 45, FeatType.Carve1,
            "Deals weapon DMG + 35, applies Hemorrhage which increases the damage your target takes by 10% for 12 seconds");
        AssertPerkLevel(perks[PerkType.BerserkerStance], "Berserker Stance", 2, 4, 48, FeatType.BerserkerStance2,
            "While active, grants +25% Attack, +15% Haste, -20% Defense, and -20% Force Defense.");
        AssertPerkLevel(perks[PerkType.BloodFrenzy], "Blood Frenzy", 1, 4, 50, null,
            "Defeating an enemy restores 15 STM and grants +10% Haste for 30 seconds.",
            StatType.DefeatedEnemyStaminaRestore,
            StatType.DefeatedEnemyAttackDelayReductionPercent,
            StatType.DefeatedEnemyAttackDelayReductionDurationSeconds);
    }

    [Test]
    public void VibrobladeOffenseStaticStatBonuses_MatchCombatBible()
    {
        var perks = BuildVibrobladeOffensePerksWithout2daLookup();

        AssertStatBonus(perks[PerkType.SavageReflexes].PerkLevels[1], StatType.AutoAttackDamageBonusChance, 10);
        AssertStatBonus(perks[PerkType.SavageReflexes].PerkLevels[1], StatType.AutoAttackDamageBonus, 8);

        AssertStatBonus(perks[PerkType.Executioner].PerkLevels[1], StatType.TargetLowHPDamageThresholdPercent, 30);
        AssertStatBonus(perks[PerkType.Executioner].PerkLevels[1], StatType.TargetLowHPDamagePercentAdjustment, 15);

        AssertStatBonus(perks[PerkType.BloodFrenzy].PerkLevels[1], StatType.DefeatedEnemyStaminaRestore, 15);
        AssertStatBonus(perks[PerkType.BloodFrenzy].PerkLevels[1], StatType.DefeatedEnemyAttackDelayReductionPercent, 10);
        AssertStatBonus(perks[PerkType.BloodFrenzy].PerkLevels[1], StatType.DefeatedEnemyAttackDelayReductionDurationSeconds, 30);
    }

    [Test]
    public void VibrobladeOffenseAbilities_MatchCombatBible()
    {
        var hackingBlade = new HackingBladeAbilityDefinition().BuildAbilities();
        AssertAbility(hackingBlade[FeatType.HackingBlade1], "Hacking Blade I", 1, RecastGroup.HackingBlade, 30f, 0f, 3, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(hackingBlade[FeatType.HackingBlade2], "Hacking Blade II", 2, RecastGroup.HackingBlade, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(hackingBlade[FeatType.HackingBlade3], "Hacking Blade III", 3, RecastGroup.HackingBlade, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);

        var riotBlade = new RiotBladeAbilityDefinition().BuildAbilities();
        AssertAbility(riotBlade[FeatType.RiotBlade1], "Riot Blade I", 1, RecastGroup.RiotBlade, 60f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(riotBlade[FeatType.RiotBlade2], "Riot Blade II", 2, RecastGroup.RiotBlade, 60f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(riotBlade[FeatType.RiotBlade3], "Riot Blade III", 3, RecastGroup.RiotBlade, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var whirlwind = new WhirlwindAssaultAbilityDefinition().BuildAbilities();
        AssertAbility(whirlwind[FeatType.WhirlwindAssault1], "Whirlwind Assault I", 1, RecastGroup.WhirlwindAssault, 120f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(whirlwind[FeatType.WhirlwindAssault2], "Whirlwind Assault II", 2, RecastGroup.WhirlwindAssault, 120f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);

        var berserkerStance = new BerserkerStanceAbilityDefinition().BuildAbilities();
        AssertAbility(berserkerStance[FeatType.BerserkerStance1], "Berserker Stance I", 1, RecastGroup.BerserkerStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);
        AssertAbility(berserkerStance[FeatType.BerserkerStance2], "Berserker Stance II", 2, RecastGroup.BerserkerStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var rendingStrike = new RendingStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(rendingStrike[FeatType.RendingStrike1], "Rending Strike I", 1, RecastGroup.RendingStrike, 60f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(rendingStrike[FeatType.RendingStrike2], "Rending Strike II", 2, RecastGroup.RendingStrike, 60f, 0f, 7, true, true, true, false, AbilityActivationType.Casted);

        var savageCleave = new SavageCleaveAbilityDefinition().BuildAbilities()[FeatType.SavageCleave1];
        AssertAbility(savageCleave, "Savage Cleave", 1, RecastGroup.SavageCleave, 45f, 0f, 7, true, false, false, true, AbilityActivationType.Casted);

        var carve = new CarveAbilityDefinition().BuildAbilities()[FeatType.Carve1];
        AssertAbility(carve, "Carve", 1, RecastGroup.Carve, 75f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);
    }

    [Test]
    public void VibrobladeOffenseStatusEffects_MatchCombatBible()
    {
        var berserker1 = BuildBerserkerStanceStats(1);
        berserker1.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(15);
        berserker1.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(10);
        berserker1.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-20);
        berserker1.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-20);

        var berserker2 = BuildBerserkerStanceStats(2);
        berserker2.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(25);
        berserker2.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(15);
        berserker2.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-20);
        berserker2.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-20);

        var exposed1 = new ExposedStatusEffect();
        exposed1.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);

        var exposed2 = new ExposedStatusEffect(-25);
        exposed2.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-25);

        var hemorrhage = new HemorrhageStatusEffect();
        hemorrhage.Categories.Should().HaveFlag(StatusEffectCategory.Bleeding);
        hemorrhage.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(10);
    }

    [Test]
    public void VibrobladeOffenseFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.BerserkerStance1, "ife_bersstnc1"),
            (FeatType.BerserkerStance2, "ife_bersstnc2"),
            (FeatType.Carve1, "ife_carv1"),
            (FeatType.RendingStrike1, "ife_rendstrk1"),
            (FeatType.RendingStrike2, "ife_rendstrk2"),
            (FeatType.SavageCleave1, "ife_savclv1"),
            (FeatType.WhirlwindAssault1, "ife_wwindaslt1"),
            (FeatType.WhirlwindAssault2, "ife_wwindaslt2"),
            (FeatType.HackingBlade1, "ife_hckngblade1"),
            (FeatType.RiotBlade1, "ife_rtblade1"),
            (FeatType.RiotBlade2, "ife_rtblade2"),
            (FeatType.HackingBlade2, "ife_hckngblade2"),
            (FeatType.RiotBlade3, "ife_rtblade3"),
            (FeatType.HackingBlade3, "ife_hckngblade3")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();
        }
    }

    private static BerserkerStanceStatusEffect BuildBerserkerStanceStats(int level)
    {
        var status = new BerserkerStanceStatusEffect();
        typeof(BerserkerStanceStatusEffect)
            .GetMethod("ApplyStatAdjustments", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(status, new object[] { level });

        return status;
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
        perk.Category.Should().Be(PerkCategoryType.VibrobladeOffense);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Vibroblade, skillRank);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
        {
            perkLevel.StatBonuses.Should().HaveCount(statTypes.Length);
            perkLevel.StatBonuses.Select(x => x.Stat).Should().Contain(statTypes);
        }
        else
        {
            perkLevel.StatBonuses.Should().BeEmpty();
        }
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

    private static Dictionary<PerkType, PerkDetail> BuildVibrobladeOffensePerksWithout2daLookup()
    {
        var definition = new VibrobladePerkDefinition();
        var methodNames = new[]
        {
            "SavageReflexes",
            "HackingBlade",
            "RiotBlade",
            "WhirlwindAssault",
            "BerserkerStance",
            "RendingStrike",
            "SavageCleave",
            "Executioner",
            "Bloodseeker",
            "CrimsonFury",
            "Carve",
            "BloodFrenzy"
        };

        foreach (var methodName in methodNames)
        {
            typeof(VibrobladePerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(VibrobladePerkDefinition)
            .GetField("_builder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
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
