using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Spear;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class SpearDamageTests
{
    [Test]
    public void SpearDamageAbilities_MatchCombatBible()
    {
        var flankingStance = new FlankingStanceAbilityDefinition().BuildAbilities()[FeatType.FlankingStance1];
        AssertAbility(flankingStance, "Flanking Stance", 1, RecastGroup.FlankingStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var sideAssault = new SideAssaultAbilityDefinition().BuildAbilities();
        AssertAbility(sideAssault[FeatType.SideAssault1], "Side Assault I", 1, RecastGroup.SideAssault, 12f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(sideAssault[FeatType.SideAssault2], "Side Assault II", 2, RecastGroup.SideAssault, 12f, 0f, 12, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(sideAssault[FeatType.SideAssault3], "Side Assault III", 3, RecastGroup.SideAssault, 12f, 0f, 18, true, false, true, false, AbilityActivationType.Weapon);

        var flankingBarrage = new FlankingBarrageAbilityDefinition().BuildAbilities()[FeatType.FlankingBarrage1];
        AssertAbility(flankingBarrage, "Flanking Barrage", 1, RecastGroup.FlankingBarrage, 120f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);
        flankingBarrage.CustomValidation.Should().BeNull();

        var sweepingFlank = new SweepingFlankAbilityDefinition().BuildAbilities()[FeatType.SweepingFlank1];
        AssertAbility(sweepingFlank, "Sweeping Flank", 1, RecastGroup.SweepingFlank, 60f, 2f, 10, true, false, false, true, AbilityActivationType.Casted);

        var hamperingBarrage = new HamperingBarrageAbilityDefinition().BuildAbilities()[FeatType.HamperingBarrage1];
        AssertAbility(hamperingBarrage, "Hampering Barrage", 1, RecastGroup.HamperingBarrage, 60f, 2f, 14, true, false, false, true, AbilityActivationType.Casted);

        var calmingStance = new CalmingStanceAbilityDefinition().BuildAbilities()[FeatType.CalmingStance1];
        AssertAbility(calmingStance, "Calming Stance", 1, RecastGroup.CalmingStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

    }

    [Test]
    public void SpearDamageStatusEffects_MatchCombatBible()
    {
        var flankingStance = new FlankingStanceStatusEffect();
        flankingStance.StatGroup.Stats[StatType.SideAttackDamagePercentAdjustment].Should().Be(20);
        flankingStance.StatGroup.Stats[StatType.SideAttackHitChancePercentAdjustment].Should().Be(15);
        flankingStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-25);
        flankingStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-25);
        flankingStance.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);

        var breach = new BreachStatusEffect();
        breach.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);
        breach.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-20);

        var flankingBarrage = new FlankingBarrageStatusEffect();
        flankingBarrage.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-12);

        var exposed = new ExposedStatusEffect();
        exposed.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);

        var improvedAttentiveness = new ImprovedAttentivenessStatusEffect();
        improvedAttentiveness.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(15);
        improvedAttentiveness.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);

        var calming = new CalmingStanceStatusEffect();
        calming.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-40);
        calming.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-40);
        calming.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-40);
        calming.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-40);

        var cripplingDefense = new CrippledDefenseStatusEffect();
        cripplingDefense.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-15);
        cripplingDefense.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-15);
    }

    [Test]
    public void SpearDamagePositionals_AreBaselineWithSideBonuses()
    {
        var perks = BuildSpearDamagePerksWithout2daLookup();

        AssertPerkLevel(
            perks[PerkType.FlankingBarrage],
            "Flanking Barrage",
            1,
            3,
            20,
            FeatType.FlankingBarrage1,
            "Deal weapon DMG + 16 to your target. From the side, deal +20 DMG and reduce their Attack by 12% for 8 seconds.");

        AssertPerkLevel(
            perks[PerkType.LateralStrike],
            "Lateral Strike",
            1,
            3,
            8,
            FeatType.LateralStrikeTrait,
            "Spear attacks restore 2 STM. Side attacks restore an additional 2 STM. Each restore can only trigger once every 4 seconds.",
            StatType.DamageDealtStaminaRestoreSkillType,
            StatType.DamageDealtStaminaRestore,
            StatType.DamageDealtStaminaRestoreCooldownSeconds,
            StatType.SideAttackStaminaRestore,
            StatType.SideAttackStaminaRestoreCooldownSeconds);
        AssertStatBonus(perks[PerkType.LateralStrike].PerkLevels[1], StatType.DamageDealtStaminaRestoreSkillType, (int)SkillType.Spear);
        AssertStatBonus(perks[PerkType.LateralStrike].PerkLevels[1], StatType.DamageDealtStaminaRestore, 2);
        AssertStatBonus(perks[PerkType.LateralStrike].PerkLevels[1], StatType.SideAttackStaminaRestore, 2);

        AssertPerkLevel(
            perks[PerkType.LateralStrike],
            "Lateral Strike",
            2,
            2,
            22,
            null,
            "Spear attacks restore 3 STM. Side attacks restore an additional 3 STM. Each restore can only trigger once every 4 seconds.",
            StatType.DamageDealtStaminaRestoreSkillType,
            StatType.DamageDealtStaminaRestore,
            StatType.DamageDealtStaminaRestoreCooldownSeconds,
            StatType.SideAttackStaminaRestore,
            StatType.SideAttackStaminaRestoreCooldownSeconds);
        AssertStatBonus(perks[PerkType.LateralStrike].PerkLevels[2], StatType.DamageDealtStaminaRestore, 3);
        AssertStatBonus(perks[PerkType.LateralStrike].PerkLevels[2], StatType.SideAttackStaminaRestore, 3);

        AssertPerkLevel(
            perks[PerkType.OpportunistsFlow],
            "Opportunist's Flow",
            1,
            4,
            35,
            FeatType.OpportunistsFlowTrait,
            "After dealing Spear damage, your next attack's delay is 10% quicker for 18 seconds. Side attacks grant an additional 10%.",
            StatType.DamageDealtAttackDelayReductionSkillType,
            StatType.DamageDealtAttackDelayReductionPercent,
            StatType.DamageDealtAttackDelayReductionDurationSeconds,
            StatType.SideAttackDelayReductionPercent,
            StatType.SideAttackDelayReductionDurationSeconds);
        AssertStatBonus(perks[PerkType.OpportunistsFlow].PerkLevels[1], StatType.DamageDealtAttackDelayReductionSkillType, (int)SkillType.Spear);
        AssertStatBonus(perks[PerkType.OpportunistsFlow].PerkLevels[1], StatType.DamageDealtAttackDelayReductionPercent, 10);
        AssertStatBonus(perks[PerkType.OpportunistsFlow].PerkLevels[1], StatType.DamageDealtAttackDelayReductionDurationSeconds, 18);
        AssertStatBonus(perks[PerkType.OpportunistsFlow].PerkLevels[1], StatType.SideAttackDelayReductionPercent, 10);
        AssertStatBonus(perks[PerkType.OpportunistsFlow].PerkLevels[1], StatType.SideAttackDelayReductionDurationSeconds, 18);

        AssertPerkLevel(
            perks[PerkType.RestorationStrike],
            "Restoration Strike",
            1,
            3,
            38,
            FeatType.RestorationStrikeTrait,
            "Critical hit chance increases by 10%. Critical hits restore 4 STM once every 6 seconds. If you were at the side of your target, critical hits have a 35% chance to restore an additional 8 STM.",
            StatType.CriticalRatePercentAdjustment,
            StatType.CriticalStaminaRestoreSkillType,
            StatType.CriticalStaminaRestore,
            StatType.CriticalStaminaRestoreCooldownSeconds,
            StatType.CriticalSideAttackStaminaRestoreChance,
            StatType.CriticalSideAttackStaminaRestore);
        AssertStatBonus(perks[PerkType.RestorationStrike].PerkLevels[1], StatType.CriticalStaminaRestoreSkillType, (int)SkillType.Spear);
        AssertStatBonus(perks[PerkType.RestorationStrike].PerkLevels[1], StatType.CriticalStaminaRestore, 4);
        AssertStatBonus(perks[PerkType.RestorationStrike].PerkLevels[1], StatType.CriticalSideAttackStaminaRestore, 8);

        var combat = File.ReadAllText((FindRepositoryRoot() / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var flankingBarrage = File.ReadAllText((FindRepositoryRoot() / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Spear" / "FlankingBarrageAbilityDefinition.cs").FullName);
        combat.Should().Contain("ApplyDamageDealtStaminaRestore(attacker, skillType);");
        combat.Should().Contain("ApplyDamageDealtAttackDelayReduction(attacker, skillType);");
        combat.Should().Contain("StatType.DamageDealtStaminaRestoreSkillType");
        combat.Should().Contain("StatType.DamageDealtAttackDelayReductionSkillType");
        flankingBarrage.Should().Contain("Combat.IsAttackerBesideTarget(activator, target)");
        flankingBarrage.Should().NotContain("You must be beside your target.");
    }

    [Test]
    public void SpearDamageFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.FlankingStance1, "ife_flankstnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SideAssault1, "ife_sideaslt1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SideAssault2, "ife_sideaslt2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SideAssault3, "ife_sideaslt3", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.FlankingBarrage1, "ife_flankbarr1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SweepingFlank1, "ife_swpngflnk1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.HamperingBarrage1, "ife_hampbarr1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.CalmingStance1, "ife_calmstnc1", "0x01", "0", "****", "****", "****", "****"),
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

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
        perk.Category.Should().Be(PerkCategoryType.SpearDamage);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Spear, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Spear);
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
            .Single(x => x.Stat == statType)
            .Calculate(0)
            .Should()
            .Be(value);
    }

    private static Dictionary<PerkType, PerkDetail> BuildSpearDamagePerksWithout2daLookup()
    {
        var definition = new SpearPerkDefinition();
        var methodNames = new[]
        {
            "AdaptivePrecisionStrike",
            "BreachStrike",
            "CalmingStance",
            "CripplingDefense",
            "FlankingBarrage",
            "Flanking",
            "FlankingStance",
            "HamperingBarrage",
            "ImprovedAttentiveness",
            "LateralStrike",
            "OpportunistFlow",
            "RestorationStrike",
            "SideAssault",
            "SweepingFlank"
        };

        foreach (var methodName in methodNames)
        {
            typeof(SpearPerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(SpearPerkDefinition)
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
