using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Throwing;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ThrowingDeadeyeTests
{
    [Test]
    public void ThrowingDeadeyePerkLevels_MatchCombatBible()
    {
        var perks = BuildThrowingDeadeyePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.PiercingToss], "Piercing Toss", 1, 2, 5, FeatType.PiercingToss1,
            "Your next attack deals weapon DMG + 12 and inflicts Bleed for 30 seconds.");
        AssertPerkLevel(perks[PerkType.PinningToss], "Pinning Toss", 1, 2, 8, FeatType.PinningToss1,
            "Your next attack deals weapon DMG + 8 and inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.ReturningGrip], "Returning Grip", 1, 3, 12, null,
            "After using a Throwing combat ability, your next auto-attack within 8 seconds deals +8 DMG.",
            StatType.ThrowingAbilityUsedNextAutoAttackDamageBonus,
            StatType.ThrowingAbilityUsedNextAutoAttackDamageDurationSeconds);
        AssertPerkLevel(perks[PerkType.DeadeyeStance], "Deadeye Stance", 1, 2, 15, FeatType.DeadeyeStance1,
            "While active, grants +15% accuracy and +15% critical chance, but reduces Evasion by 20%.");
        AssertPerkLevel(perks[PerkType.PiercingToss], "Piercing Toss", 2, 4, 18, FeatType.PiercingToss2,
            "Your next attack deals weapon DMG + 21 and inflicts Bleed for 60 seconds.");
        AssertPerkLevel(perks[PerkType.MarkingToss], "Marking Toss", 1, 3, 20, FeatType.MarkingToss1,
            "Deals weapon DMG + 18 and marks the target for 12 seconds. Throwing damage against the marked target is increased by 10%.");
        AssertPerkLevel(perks[PerkType.PinningToss], "Pinning Toss", 2, 2, 22, FeatType.PinningToss2,
            "Your next attack deals weapon DMG + 18 and inflicts Disoriented for 15 seconds.");
        AssertPerkLevel(perks[PerkType.RicochetToss], "Ricochet Toss", 1, 3, 25, FeatType.RicochetToss1,
            "Your thrown weapon hits the target and up to 2 additional enemies for weapon DMG + 15 each.");
        AssertPerkLevel(perks[PerkType.BleedersEye], "Bleeder's Eye", 1, 4, 28, null,
            "Deal +12% Throwing damage to bleeding targets.",
            StatType.DamageToBleedingTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.PiercingToss], "Piercing Toss", 3, 3, 30, FeatType.PiercingToss3,
            "Your next attack deals weapon DMG + 34 and inflicts Bleed for 60 seconds.");
        AssertPerkLevel(perks[PerkType.MarkedTempo], "Marked Tempo", 1, 2, 32, null,
            "Critical hits against your marked target restore 6 STM.",
            StatType.CriticalMarkedTargetStaminaRestore);
        AssertPerkLevel(perks[PerkType.PinningToss], "Pinning Toss", 3, 4, 35, FeatType.PinningToss3,
            "Your next attack deals weapon DMG + 30, inflicts Disoriented for 20 seconds, and reduces Evasion by an additional 15%.");
        AssertPerkLevel(perks[PerkType.SeveringToss], "Severing Toss", 1, 3, 38, FeatType.SeveringToss1,
            "Deals weapon DMG + 32 and inflicts Hamstring for 15 seconds.");
        AssertPerkLevel(perks[PerkType.RicochetToss], "Ricochet Toss", 2, 3, 40, FeatType.RicochetToss2,
            "Your thrown weapon hits the target and up to 4 additional enemies for weapon DMG + 24 each.");
        AssertPerkLevel(perks[PerkType.DeepWound], "Deep Wound", 1, 4, 42, null,
            "Bleed effects you apply deal +25% damage and last 10 seconds longer.",
            StatType.OutgoingBleedingDamagePercentAdjustment,
            StatType.OutgoingBleedingDurationBonusSeconds);
        AssertPerkLevel(perks[PerkType.FinishingToss], "Finishing Toss", 1, 3, 45, FeatType.FinishingToss1,
            "Deals weapon DMG + 40. Targets below 30% HP take an additional +30 DMG.");
        AssertPerkLevel(perks[PerkType.DeadeyeMastery], "Deadeye Mastery", 1, 4, 48, null,
            "Throwing abilities against bleeding or disoriented targets have +15% critical chance.",
            StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.PerfectThrow], "Perfect Throw", 1, 4, 50, FeatType.PerfectThrow1,
            "Deals weapon DMG + 80 to one target. If the target is bleeding, also inflict Hemorrhage, increasing damage taken by 10% for 15 seconds.");

        AssertStatBonus(perks[PerkType.ReturningGrip].PerkLevels[1], StatType.ThrowingAbilityUsedNextAutoAttackDamageBonus, 8);
        AssertStatBonus(perks[PerkType.ReturningGrip].PerkLevels[1], StatType.ThrowingAbilityUsedNextAutoAttackDamageDurationSeconds, 8);
    }

    [Test]
    public void ThrowingDeadeyeAbilities_MatchCombatBible()
    {
        var piercingToss = new PiercingTossAbilityDefinition().BuildAbilities();
        AssertAbility(piercingToss[FeatType.PiercingToss1], "Piercing Toss I", 1, RecastGroup.PiercingToss, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(piercingToss[FeatType.PiercingToss2], "Piercing Toss II", 2, RecastGroup.PiercingToss, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(piercingToss[FeatType.PiercingToss3], "Piercing Toss III", 3, RecastGroup.PiercingToss, 30f, 0f, 7, true, false, true, false, AbilityActivationType.Weapon);

        var pinningToss = new PinningTossAbilityDefinition().BuildAbilities();
        AssertAbility(pinningToss[FeatType.PinningToss1], "Pinning Toss I", 1, RecastGroup.PinningToss, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(pinningToss[FeatType.PinningToss2], "Pinning Toss II", 2, RecastGroup.PinningToss, 30f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(pinningToss[FeatType.PinningToss3], "Pinning Toss III", 3, RecastGroup.PinningToss, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var deadeyeStance = new DeadeyeStanceAbilityDefinition().BuildAbilities()[FeatType.DeadeyeStance1];
        AssertAbility(deadeyeStance, "Deadeye Stance", 1, RecastGroup.DeadeyeStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted, expectedMaxRange: 5f);

        var markingToss = new MarkingTossAbilityDefinition().BuildAbilities()[FeatType.MarkingToss1];
        AssertAbility(markingToss, "Marking Toss", 1, RecastGroup.MarkingToss, 60f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);

        var ricochetToss = new RicochetTossAbilityDefinition().BuildAbilities();
        AssertAbility(ricochetToss[FeatType.RicochetToss1], "Ricochet Toss I", 1, RecastGroup.RicochetToss, 60f, 0f, 8, true, true, false, true, AbilityActivationType.Casted);
        AssertAbility(ricochetToss[FeatType.RicochetToss2], "Ricochet Toss II", 2, RecastGroup.RicochetToss, 60f, 0f, 10, true, true, false, true, AbilityActivationType.Casted);

        var severingToss = new SeveringTossAbilityDefinition().BuildAbilities()[FeatType.SeveringToss1];
        AssertAbility(severingToss, "Severing Toss", 1, RecastGroup.SeveringToss, 60f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var finishingToss = new FinishingTossAbilityDefinition().BuildAbilities()[FeatType.FinishingToss1];
        AssertAbility(finishingToss, "Finishing Toss", 1, RecastGroup.FinishingToss, 90f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var perfectThrow = new PerfectThrowAbilityDefinition().BuildAbilities()[FeatType.PerfectThrow1];
        AssertAbility(perfectThrow, "Perfect Throw", 1, RecastGroup.Capstone, 1800f, 1f, 25, true, true, true, false, AbilityActivationType.Casted);
    }

    [Test]
    public void ThrowingDeadeyeStatusEffects_MatchCombatBible()
    {
        var deadeyeStance = new DeadeyeStanceStatusEffect();
        deadeyeStance.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(15);
        deadeyeStance.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(15);
        deadeyeStance.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);

        var markingToss = new MarkingTossStatusEffect();
        markingToss.StatGroup.Stats[StatType.ThrowingDamageTakenPercentAdjustment].Should().Be(10);
        markingToss.Categories.Should().HaveFlag(StatusEffectCategory.Debuff);
        markingToss.ResistanceType.Should().Be(ResistanceType.Trauma);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);

        var pinningToss3Disoriented = new DisorientedStatusEffect(15);
        pinningToss3Disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        pinningToss3Disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-30);
        pinningToss3Disoriented.Clone().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-30);

        var hemorrhage = new HemorrhageStatusEffect();
        hemorrhage.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(10);
        hemorrhage.Categories.Should().HaveFlag(StatusEffectCategory.Bleeding);
        hemorrhage.Categories.Should().HaveFlag(StatusEffectCategory.Debuff);
    }

    [Test]
    public void ThrowingDeadeyeSources_IncludeBibleStatValues()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "ThrowingPerkDefinition.cs").FullName);

        source.Should().Contain("StatType.DamageToBleedingTargetPercentAdjustment, creature => EquipmentPredicates.HasThrowing(creature) ? 12 : 0");
        source.Should().Contain("StatType.CriticalMarkedTargetStaminaRestore, creature => EquipmentPredicates.HasThrowing(creature) ? 6 : 0");
        source.Should().Contain("StatType.OutgoingBleedingDamagePercentAdjustment, creature => EquipmentPredicates.HasThrowing(creature) ? 25 : 0");
        source.Should().Contain("StatType.OutgoingBleedingDurationBonusSeconds, creature => EquipmentPredicates.HasThrowing(creature) ? 10 : 0");
        source.Should().Contain("StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment, creature => EquipmentPredicates.HasThrowing(creature) ? 15 : 0");
    }

    [Test]
    public void ThrowingDeadeyeFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.PiercingToss1, "ife_pierctoss1", "P", "0x01", "0"),
            (FeatType.PinningToss1, "ife_pintoss1", "P", "0x01", "0"),
            (FeatType.DeadeyeStance1, "ife_eyestnc1", "P", "0x01", "0"),
            (FeatType.PiercingToss2, "ife_pierctoss2", "P", "0x01", "0"),
            (FeatType.MarkingToss1, "ife_marktoss1", "M", "0x02", "1"),
            (FeatType.PinningToss2, "ife_pintoss2", "P", "0x01", "0"),
            (FeatType.RicochetToss1, "ife_ricotoss1", "M", "0x02", "1"),
            (FeatType.PiercingToss3, "ife_pierctoss3", "P", "0x01", "0"),
            (FeatType.PinningToss3, "ife_pintoss3", "P", "0x01", "0"),
            (FeatType.SeveringToss1, "ife_sevtoss1", "M", "0x02", "1"),
            (FeatType.RicochetToss2, "ife_ricotoss2", "M", "0x02", "1"),
            (FeatType.FinishingToss1, "ife_fintoss1", "M", "0x02", "1"),
            (FeatType.PerfectThrow1, "ife_perfthrow1", "M", "0x02", "1")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting) in feats)
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
            abilityRow["TargetShape"].Should().Be("****");
            abilityRow["TargetSizeX"].Should().Be("****");
            abilityRow["TargetSizeY"].Should().Be("****");
            abilityRow["TargetFlags"].Should().Be("****");
        }

        featRows[(int)FeatType.PiercingToss1]["CATEGORY"].Should().Be("10");
        featRows[(int)FeatType.PiercingToss2]["CATEGORY"].Should().Be("10");
        featRows[(int)FeatType.PiercingToss3]["CATEGORY"].Should().Be("10");
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
        perk.Category.Should().Be(PerkCategoryType.ThrowingDeadeye);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Throwing, skillRank);

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
        float expectedMaxRange = 20f)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Throwing);
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

    private static Dictionary<PerkType, PerkDetail> BuildThrowingDeadeyePerksWithout2daLookup()
    {
        var definition = new ThrowingPerkDefinition();
        var methodNames = new[]
        {
            "BleedersEye",
            "DeadeyeMastery",
            "DeadeyeStance",
            "DeepWound",
            "FinishingToss",
            "MarkedTempo",
            "MarkingToss",
            "PerfectThrow",
            "PiercingToss",
            "PinningToss",
            "ReturningGrip",
            "RicochetToss",
            "SeveringToss"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ThrowingPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ThrowingPerkDefinition)
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
