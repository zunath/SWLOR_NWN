using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Pistol;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class PistolSkirmisherTests
{
    [Test]
    public void PistolSkirmisherPerkLevels_MatchCombatBible()
    {
        var perks = BuildPistolSkirmisherPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.MobileFootwork], "Mobile Footwork", 1, 2, 5, null,
            "After using a pistol ability, gain +10% Evasion for 6 seconds.",
            StatType.PistolAbilityUsedEvasionPercentAdjustment,
            StatType.PistolAbilityUsedEvasionDurationSeconds);
        AssertPerkLevel(perks[PerkType.DisarmingShot], "Disarming Shot", 1, 2, 8, FeatType.DisarmingShot1,
            "Deals weapon DMG + 8 and inflicts Weakened, reducing Attack by 10% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.SnapRoll], "Snap Roll", 1, 3, 12, FeatType.SnapRoll1,
            "Gain +25% Evasion for 6 seconds and reduce your current enmity by 10%.");
        AssertPerkLevel(perks[PerkType.SkirmisherStance], "Skirmisher Stance", 1, 2, 15, FeatType.SkirmisherStance1,
            "While active, grants +15% Evasion and reduces Enmity generation by 20%, but reduces Attack by 10%.");
        AssertPerkLevel(perks[PerkType.InterruptingShot], "Interrupting Shot", 1, 4, 18, FeatType.InterruptingShot1,
            "Interrupts your target's ability activation and inflicts Foggy Mind for 12 seconds.");
        AssertPerkLevel(perks[PerkType.KitingInstinct], "Kiting Instinct", 1, 3, 20, null,
            "When attacked in melee, you have a 20% chance to restore 3 STM and gain +10% Evasion for 6 seconds.",
            StatType.MeleeDamageTakenStaminaRestoreChance,
            StatType.MeleeDamageTakenStaminaRestore,
            StatType.MeleeDamageTakenEvasionPercentAdjustment,
            StatType.MeleeDamageTakenEvasionDurationSeconds);
        AssertPerkLevel(perks[PerkType.DisarmingShot], "Disarming Shot", 2, 2, 22, FeatType.DisarmingShot2,
            "Deals weapon DMG + 18 and inflicts Weakened, reducing Attack by 15% for 15 seconds.");
        AssertPerkLevel(perks[PerkType.RicochetShot], "Ricochet Shot", 1, 3, 25, FeatType.RicochetShot1,
            "A shot bounces to up to 3 enemies for weapon DMG + 12 each. Each target is inflicted with Blind for 6 seconds.");
        AssertPerkLevel(perks[PerkType.SnapRoll], "Snap Roll", 2, 4, 28, FeatType.SnapRoll2,
            "Gain +35% Evasion for 8 seconds and your next pistol attack within 8 seconds deals +10 DMG.");
        AssertPerkLevel(perks[PerkType.LowShot], "Low Shot", 1, 3, 30, FeatType.LowShot1,
            "Deals weapon DMG + 20 and inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.EvasiveReload], "Evasive Reload", 1, 2, 32, null,
            "Using Snap Roll or Ricochet Shot reduces Disarming Shot cooldowns by 10 seconds.",
            StatType.AbilityUsedRecastReductionTriggerGroup,
            StatType.AbilityUsedRecastReductionSecondaryTriggerGroup,
            StatType.AbilityUsedRecastReductionTargetGroup,
            StatType.AbilityUsedRecastReductionSeconds);
        AssertPerkLevel(perks[PerkType.InterruptingShot], "Interrupting Shot", 2, 4, 35, FeatType.InterruptingShot2,
            "Deals weapon DMG + 20, interrupts your target's ability activation, and inflicts Foggy Mind for 20 seconds.");
        AssertPerkLevel(perks[PerkType.PointBlankBurst], "Point Blank Burst", 1, 3, 38, FeatType.PointBlankBurst1,
            "Deals weapon DMG + 18 to enemies in a cone. Inflicts Knockdown for 3 seconds.");
        AssertPerkLevel(perks[PerkType.DuelistsDistance], "Duelist's Distance", 1, 3, 40, null,
            "Deal +12% pistol damage to enemies within 8 meters that are not targeting you.",
            StatType.DamageToNearbyNonTargetingTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.DisarmingShot], "Disarming Shot", 3, 4, 42, FeatType.DisarmingShot3,
            "Deals weapon DMG + 32 and inflicts Weakened, reducing Attack by 20% for 15 seconds.");
        AssertPerkLevel(perks[PerkType.SmokeRound], "Smoke Round", 1, 3, 45, FeatType.SmokeRound1,
            "Enemies in the target area are inflicted with Blind for 12 seconds. You reduce enmity against affected enemies.");
        AssertPerkLevel(perks[PerkType.SkirmishersNerve], "Skirmisher's Nerve", 1, 4, 48, null,
            "When reduced below 40% HP, your next pistol ability costs 0 STM and grants +20% Evasion for 8 seconds. This can only trigger once every 2 minutes.",
            StatType.LowHPEvasionThresholdPercent,
            StatType.LowHPEvasionPercentAdjustment,
            StatType.LowHPEvasionDurationSeconds,
            StatType.LowHPEvasionCooldownSeconds,
            StatType.LowHPNextAbilityNoStaminaCostThresholdPercent,
            StatType.LowHPNextAbilityNoStaminaCostSkillType,
            StatType.LowHPNextAbilityNoStaminaCostDurationSeconds,
            StatType.LowHPNextAbilityNoStaminaCostCooldownSeconds);
        AssertPerkLevel(perks[PerkType.LastWord], "Last Word", 1, 4, 50, FeatType.LastWord1,
            "Interrupts all enemies in a cone, deals weapon DMG + 35, and inflicts Dazed for 3 seconds.");
    }

    [Test]
    public void PistolSkirmisherAbilities_MatchCombatBible()
    {
        var disarmingShot = new DisarmingShotAbilityDefinition().BuildAbilities();
        AssertAbility(disarmingShot[FeatType.DisarmingShot1], "Disarming Shot I", 1, RecastGroup.DisarmingShot, 30f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(disarmingShot[FeatType.DisarmingShot2], "Disarming Shot II", 2, RecastGroup.DisarmingShot, 30f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(disarmingShot[FeatType.DisarmingShot3], "Disarming Shot III", 3, RecastGroup.DisarmingShot, 30f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var snapRoll = new SnapRollAbilityDefinition().BuildAbilities();
        AssertAbility(snapRoll[FeatType.SnapRoll1], "Snap Roll I", 1, RecastGroup.SnapRoll, 60f, 0f, 6, false, false, false, false, AbilityActivationType.Casted);
        AssertAbility(snapRoll[FeatType.SnapRoll2], "Snap Roll II", 2, RecastGroup.SnapRoll, 60f, 0f, 8, false, false, false, false, AbilityActivationType.Casted);

        var skirmisherStance = new SkirmisherStanceAbilityDefinition().BuildAbilities()[FeatType.SkirmisherStance1];
        AssertAbility(skirmisherStance, "Skirmisher Stance", 1, RecastGroup.SkirmisherStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var interruptingShot = new InterruptingShotAbilityDefinition().BuildAbilities();
        AssertAbility(interruptingShot[FeatType.InterruptingShot1], "Interrupting Shot I", 1, RecastGroup.InterruptingShot, 45f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(interruptingShot[FeatType.InterruptingShot2], "Interrupting Shot II", 2, RecastGroup.InterruptingShot, 45f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var ricochetShot = new RicochetShotAbilityDefinition().BuildAbilities()[FeatType.RicochetShot1];
        AssertAbility(ricochetShot, "Ricochet Shot", 1, RecastGroup.RicochetShot, 60f, 0f, 8, true, true, false, true, AbilityActivationType.Casted);

        var lowShot = new LowShotAbilityDefinition().BuildAbilities()[FeatType.LowShot1];
        AssertAbility(lowShot, "Low Shot", 1, RecastGroup.LowShot, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var pointBlankBurst = new PointBlankBurstAbilityDefinition().BuildAbilities()[FeatType.PointBlankBurst1];
        AssertAbility(pointBlankBurst, "Point Blank Burst", 1, RecastGroup.PointBlankBurst, 90f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var smokeRound = new SmokeRoundAbilityDefinition().BuildAbilities()[FeatType.SmokeRound1];
        AssertAbility(smokeRound, "Smoke Round", 1, RecastGroup.SmokeRound, 120f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var lastWord = new LastWordAbilityDefinition().BuildAbilities()[FeatType.LastWord1];
        AssertAbility(lastWord, "Last Word", 1, RecastGroup.Capstone, 1800f, 1f, 25, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void PistolSkirmisherStatusEffects_MatchCombatBible()
    {
        var snapRoll1 = new SnapRollStatusEffect();
        snapRoll1.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(25);

        var snapRoll2 = new SnapRollStatusEffect(35);
        snapRoll2.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(35);

        var skirmisherStance = new SkirmisherStanceStatusEffect();
        skirmisherStance.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(15);
        skirmisherStance.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(-20);
        skirmisherStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);

        new WeakenedStatusEffect(10).StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
        new WeakenedStatusEffect().StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        new WeakenedStatusEffect(20).StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);

        var foggyMind = new FoggyMindStatusEffect(2);
        foggyMind.StatGroup.Stats[StatType.ActivationDelayFlatAdjustment].Should().Be(2);
    }

    [Test]
    public void SnapRoll2_GrantsNextPistolAutoAttackDamageBonus()
    {
        const uint Creature = 1234;
        TemporaryStatModifier.Clear(Creature);

        typeof(SnapRollAbilityDefinition)
            .GetMethod("GrantSnapRoll2DamageBonus", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { Creature });

        TemporaryStatModifier.GetStatAdjustment(
                Creature,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                StatType.NextSkillAutoAttackDamageBonusSkillType)
            .Should()
            .Be((int)SkillType.Pistol);
        TemporaryStatModifier.GetStatAdjustment(
                Creature,
                StatType.NextSkillAutoAttackDamageBonus,
                StatType.NextSkillAutoAttackDamageBonusSkillType)
            .Should()
            .Be(10);

        TemporaryStatModifier.Clear(Creature);
    }

    [Test]
    public void PistolSkirmisherFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.DisarmingShot1, "ife_disarmshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SnapRoll1, "ife_snaproll1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SkirmisherStance1, "ife_skirmstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.InterruptingShot1, "ife_intrshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DisarmingShot2, "ife_disarmshot2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.RicochetShot1, "ife_ricoshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SnapRoll2, "ife_snaproll2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.LowShot1, "ife_lowshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.InterruptingShot2, "ife_intrshot2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.PointBlankBurst1, "ife_ptblankburs1", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.DisarmingShot3, "ife_disarmshot3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SmokeRound1, "ife_smokrnd1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.LastWord1, "ife_lastword1", "M", "0x3E", "1", "cone", "5", "5", "17")
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
        perk.Category.Should().Be(PerkCategoryType.PistolSkirmisher);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Pistol, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Pistol);
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

    private static Dictionary<PerkType, PerkDetail> BuildPistolSkirmisherPerksWithout2daLookup()
    {
        var definition = new PistolPerkDefinition();
        var methodNames = new[]
        {
            "DisarmingShot",
            "DuelistsDistance",
            "EvasiveReload",
            "InterruptingShot",
            "KitingInstinct",
            "LastWord",
            "LowShot",
            "MobileFootwork",
            "PointBlankBurst",
            "RicochetShot",
            "SkirmishersNerve",
            "SkirmisherStance",
            "SmokeRound",
            "SnapRoll"
        };

        foreach (var methodName in methodNames)
        {
            typeof(PistolPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(PistolPerkDefinition)
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
