using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Throwing;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ThrowingBombardierTests
{
    [Test]
    public void ThrowingBombardierPerkLevels_MatchCombatBible()
    {
        var perks = BuildThrowingBombardierPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.ExplosiveToss], "Explosive Toss", 1, 3, 5, FeatType.ExplosiveToss1,
            "Your next attack damages up to 3 creatures within 3 meters of your target for weapon DMG + 8.");
        AssertPerkLevel(perks[PerkType.FlashToss], "Flash Toss", 1, 3, 8, FeatType.FlashToss1,
            "Deals weapon DMG + 6 to enemies in the target area. Inflicts Blind for 6 seconds.");
        AssertPerkLevel(perks[PerkType.ShrapnelCasing], "Shrapnel Casing", 1, 3, 12, null,
            "Explosive Toss abilities inflict Bleed for 15 seconds.",
            StatType.ExplosiveTossBleedDurationSeconds);
        AssertPerkLevel(perks[PerkType.BombardierStance], "Bombardier Stance", 1, 2, 15, FeatType.BombardierStance1,
            "While active, Throwing area abilities deal +15% damage, but Defense is reduced by 15%.");
        AssertPerkLevel(perks[PerkType.ExplosiveToss], "Explosive Toss", 2, 4, 18, FeatType.ExplosiveToss2,
            "Your next attack damages up to 3 creatures within 3 meters of your target for weapon DMG + 16.");
        AssertPerkLevel(perks[PerkType.ConcussiveToss], "Concussive Toss", 1, 3, 20, FeatType.ConcussiveToss1,
            "Deals weapon DMG + 14 to enemies in the target area. Inflicts Dazed for 2 seconds.");
        AssertPerkLevel(perks[PerkType.ClusterPouch], "Cluster Pouch", 1, 2, 22, null,
            "Throwing combat abilities that hit 3 or more targets restore 4 STM.",
            StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold,
            StatType.ThrowingAreaAbilityMinTargetsStaminaRestore);
        AssertPerkLevel(perks[PerkType.FireburstToss], "Fireburst Toss", 1, 3, 25, FeatType.FireburstToss1,
            "Deals weapon DMG + 20 to enemies in the target area and inflicts Exposed for 12 seconds.");
        AssertPerkLevel(perks[PerkType.ExplosiveToss], "Explosive Toss", 3, 3, 28, FeatType.ExplosiveToss3,
            "Your next attack damages up to 3 creatures within 3 meters of your target for weapon DMG + 26.");
        AssertPerkLevel(perks[PerkType.ConcussiveToss], "Concussive Toss", 2, 3, 30, FeatType.ConcussiveToss2,
            "Deals weapon DMG + 26 to enemies in the target area. Inflicts Dazed for 3 seconds.");
        AssertPerkLevel(perks[PerkType.ShrapnelCasing], "Shrapnel Casing", 2, 2, 32, null,
            "Bleed from Explosive Toss abilities lasts 30 seconds and Explosive Toss ignores 10% Defense.",
            StatType.ExplosiveTossBleedDurationSeconds,
            StatType.AbilityDefenseIgnorePercentAdjustmentPerkType,
            StatType.AbilityDefenseIgnorePercentAdjustment);
        AssertPerkLevel(perks[PerkType.ClusterStorm], "Cluster Storm", 1, 4, 35, FeatType.ClusterStorm1,
            "Throw three explosives at the target area. Each explosive deals weapon DMG + 12 to nearby enemies.");
        AssertPerkLevel(perks[PerkType.FlashToss], "Flash Toss", 2, 3, 38, FeatType.FlashToss2,
            "Deals weapon DMG + 22 to enemies in the target area. Inflicts Blind for 10 seconds.");
        AssertPerkLevel(perks[PerkType.BombardiersRhythm], "Bombardier's Rhythm", 1, 2, 40, null,
            "Each enemy hit by a Throwing area ability grants +2% Attack for 10 seconds, up to +20%.",
            StatType.ThrowingAreaAbilityAttackPercentPerTarget,
            StatType.ThrowingAreaAbilityAttackDurationSeconds,
            StatType.ThrowingAreaAbilityAttackPercentMax);
        AssertPerkLevel(perks[PerkType.ExplosiveToss], "Explosive Toss", 4, 4, 42, FeatType.ExplosiveToss4,
            "Your next attack damages up to 3 creatures within 3 meters of your target for weapon DMG + 38 and inflicts Exposed for 15 seconds.");
        AssertPerkLevel(perks[PerkType.SaturationToss], "Saturation Toss", 1, 3, 45, FeatType.SaturationToss1,
            "Creates a target area for 12 seconds. Enemies inside take weapon DMG + 10 every 4 seconds.");
        AssertPerkLevel(perks[PerkType.VolatilePayload], "Volatile Payload", 1, 4, 48, null,
            "Critical hits with Explosive Toss abilities inflict Knockdown for 2 seconds.",
            StatType.CriticalAbilityKnockdownPerkType,
            StatType.CriticalAbilityKnockdownDurationSeconds);
        AssertPerkLevel(perks[PerkType.RainOfSteel], "Rain of Steel", 1, 4, 50, FeatType.RainOfSteel1,
            "All enemies in a large area of effect (sphere) take weapon DMG + 35 and suffer Bleed for 60 seconds. Bonus damage applies fully.");

        AssertStatBonus(perks[PerkType.ClusterPouch].PerkLevels[1], StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold, 3);
        AssertStatBonus(perks[PerkType.ClusterPouch].PerkLevels[1], StatType.ThrowingAreaAbilityMinTargetsStaminaRestore, 4);
        AssertStatBonus(perks[PerkType.BombardiersRhythm].PerkLevels[1], StatType.ThrowingAreaAbilityAttackPercentPerTarget, 2);
        AssertStatBonus(perks[PerkType.BombardiersRhythm].PerkLevels[1], StatType.ThrowingAreaAbilityAttackDurationSeconds, 10);
        AssertStatBonus(perks[PerkType.BombardiersRhythm].PerkLevels[1], StatType.ThrowingAreaAbilityAttackPercentMax, 20);
    }

    [Test]
    public void ThrowingBombardierAbilities_MatchCombatBible()
    {
        var explosiveToss = new ExplosiveTossAbilityDefinition().BuildAbilities();
        AssertAbility(explosiveToss[FeatType.ExplosiveToss1], "Explosive Toss I", 1, RecastGroup.ExplosiveToss, 45f, 0f, 4, true, false, false, true, AbilityActivationType.Weapon);
        AssertAbility(explosiveToss[FeatType.ExplosiveToss2], "Explosive Toss II", 2, RecastGroup.ExplosiveToss, 45f, 0f, 5, true, false, false, true, AbilityActivationType.Weapon);
        AssertAbility(explosiveToss[FeatType.ExplosiveToss3], "Explosive Toss III", 3, RecastGroup.ExplosiveToss, 45f, 0f, 7, true, false, false, true, AbilityActivationType.Weapon);
        AssertAbility(explosiveToss[FeatType.ExplosiveToss4], "Explosive Toss IV", 4, RecastGroup.ExplosiveToss, 45f, 0f, 9, true, false, false, true, AbilityActivationType.Weapon);

        var flashToss = new FlashTossAbilityDefinition().BuildAbilities();
        AssertAbility(flashToss[FeatType.FlashToss1], "Flash Toss I", 1, RecastGroup.FlashToss, 45f, 0f, 4, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(flashToss[FeatType.FlashToss2], "Flash Toss II", 2, RecastGroup.FlashToss, 45f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var bombardierStance = new BombardierStanceAbilityDefinition().BuildAbilities()[FeatType.BombardierStance1];
        AssertAbility(bombardierStance, "Bombardier Stance", 1, RecastGroup.BombardierStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var concussiveToss = new ConcussiveTossAbilityDefinition().BuildAbilities();
        AssertAbility(concussiveToss[FeatType.ConcussiveToss1], "Concussive Toss I", 1, RecastGroup.ConcussiveToss, 60f, 0f, 6, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(concussiveToss[FeatType.ConcussiveToss2], "Concussive Toss II", 2, RecastGroup.ConcussiveToss, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var fireburstToss = new FireburstTossAbilityDefinition().BuildAbilities()[FeatType.FireburstToss1];
        AssertAbility(fireburstToss, "Fireburst Toss", 1, RecastGroup.FireburstToss, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var clusterStorm = new ClusterStormAbilityDefinition().BuildAbilities()[FeatType.ClusterStorm1];
        AssertAbility(clusterStorm, "Cluster Storm", 1, RecastGroup.ClusterStorm, 120f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);

        var saturationToss = new SaturationTossAbilityDefinition().BuildAbilities()[FeatType.SaturationToss1];
        AssertAbility(saturationToss, "Saturation Toss", 1, RecastGroup.SaturationToss, 120f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);

        var rainOfSteel = new RainOfSteelAbilityDefinition().BuildAbilities()[FeatType.RainOfSteel1];
        AssertAbility(rainOfSteel, "Rain of Steel", 1, RecastGroup.Capstone, 1800f, 2f, 25, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void ThrowingBombardierStatusEffects_MatchCombatBible()
    {
        var bombardierStance = new BombardierStanceStatusEffect();
        bombardierStance.StatGroup.Stats[StatType.ThrowingAreaAbilityDamagePercentAdjustment].Should().Be(15);
        bombardierStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-15);
        bombardierStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-15);
    }

    [Test]
    public void ThrowingBombardierFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.ExplosiveToss1, "ife_xplsvtoss1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.FlashToss1, "ife_flashtoss1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.BombardierStance1, "ife_bombstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ExplosiveToss2, "ife_xplsvtoss2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ConcussiveToss1, "ife_conctoss1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.FireburstToss1, "ife_firetoss1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.ExplosiveToss3, "ife_xplsvtoss3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ConcussiveToss2, "ife_conctoss2", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.ClusterStorm1, "ife_clstrstrm1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.FlashToss2, "ife_flashtoss2", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.ExplosiveToss4, "ife_xplsvtoss4", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SaturationToss1, "ife_sattoss1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.RainOfSteel1, "ife_rainsteel1", "M", "0x3E", "1", "sphere", "8", "****", "1")
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
        perk.Category.Should().Be(PerkCategoryType.ThrowingBombardier);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Throwing, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Throwing);
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

    private static Dictionary<PerkType, PerkDetail> BuildThrowingBombardierPerksWithout2daLookup()
    {
        var definition = new ThrowingPerkDefinition();
        var methodNames = new[]
        {
            "BombardiersRhythm",
            "BombardierStance",
            "ClusterPouch",
            "ClusterStorm",
            "ConcussiveToss",
            "ExplosiveToss",
            "FireburstToss",
            "FlashToss",
            "RainOfSteel",
            "SaturationToss",
            "ShrapnelCasing",
            "VolatilePayload"
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
