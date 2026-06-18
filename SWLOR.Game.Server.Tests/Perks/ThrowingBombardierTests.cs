using System.Reflection;
using System.Text.Json;
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
    private const int CustomTlkOffset = 16777216;

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
        AssertAbility(bombardierStance, "Bombardier Stance", 1, RecastGroup.BombardierStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted, expectedMaxRange: 5f);

        var concussiveToss = new ConcussiveTossAbilityDefinition().BuildAbilities();
        AssertAbility(concussiveToss[FeatType.ConcussiveToss1], "Concussive Toss I", 1, RecastGroup.ConcussiveToss, 60f, 0f, 6, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(concussiveToss[FeatType.ConcussiveToss2], "Concussive Toss II", 2, RecastGroup.ConcussiveToss, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var fireburstToss = new FireburstTossAbilityDefinition().BuildAbilities()[FeatType.FireburstToss1];
        AssertAbility(fireburstToss, "Fireburst Toss", 1, RecastGroup.FireburstToss, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var rainOfSteel = new RainOfSteelAbilityDefinition().BuildAbilities()[FeatType.RainOfSteel1];
        AssertAbility(rainOfSteel, "Rain of Steel", 1, RecastGroup.Capstone, 345f, 2f, 15, true, false, false, true, AbilityActivationType.Casted);
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
    public void ThrowingBombardierPerks_DescribeFireDamage()
    {
        var perks = BuildThrowingBombardierPerksWithout2daLookup();

        AssertPerkLevel(
            perks[PerkType.ExplosiveToss],
            "Explosive Toss",
            1,
            3,
            2,
            FeatType.ExplosiveToss1,
            "Your next attack deals weapon DMG + 8 as fire damage to up to 3 creatures within 3 meters of your target.");
        AssertPerkLevel(
            perks[PerkType.ExplosiveToss],
            "Explosive Toss",
            2,
            4,
            18,
            FeatType.ExplosiveToss2,
            "Your next attack deals weapon DMG + 16 as fire damage to up to 3 creatures within 3 meters of your target.");
        AssertPerkLevel(
            perks[PerkType.ExplosiveToss],
            "Explosive Toss",
            3,
            3,
            28,
            FeatType.ExplosiveToss3,
            "Your next attack deals weapon DMG + 26 as fire damage to up to 3 creatures within 3 meters of your target.");
        AssertPerkLevel(
            perks[PerkType.ExplosiveToss],
            "Explosive Toss",
            4,
            4,
            42,
            FeatType.ExplosiveToss4,
            "Your next attack deals weapon DMG + 38 as fire damage to up to 3 creatures within 3 meters of your target and inflicts Exposed for 15 seconds.");
        AssertPerkLevel(
            perks[PerkType.FireburstToss],
            "Fireburst Toss",
            1,
            3,
            25,
            FeatType.FireburstToss1,
            "Deals weapon DMG + 20 as fire damage to enemies in the target area and inflicts Exposed for 12 seconds.");
    }

    [Test]
    public void ThrowingBombardierFeatAndAbilityDescriptions_MentionFireDamage()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "swlor2_tlk" / "swlor2_tlk.tlk.json");
        var descriptions = new[]
        {
            (FeatType.ExplosiveToss1, "Your next attack deals weapon DMG + 8 as fire damage to up to 3 creatures within 3 meters of your target."),
            (FeatType.ExplosiveToss2, "Your next attack deals weapon DMG + 16 as fire damage to up to 3 creatures within 3 meters of your target."),
            (FeatType.ExplosiveToss3, "Your next attack deals weapon DMG + 26 as fire damage to up to 3 creatures within 3 meters of your target."),
            (FeatType.ExplosiveToss4, "Your next attack deals weapon DMG + 38 as fire damage to up to 3 creatures within 3 meters of your target and inflicts Exposed for 15 seconds."),
            (FeatType.FireburstToss1, "Deals weapon DMG + 20 as fire damage to enemies in the target area and inflicts Exposed for 12 seconds.")
        };

        foreach (var (featType, expectedDescription) in descriptions)
        {
            var featRow = featRows[(int)featType];
            var featDescriptionId = int.Parse(featRow["DESCRIPTION"]) - CustomTlkOffset;
            tlkEntries[featDescriptionId].Should().Be(expectedDescription);

            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var abilityDescriptionId = int.Parse(abilityRow["SpellDesc"]) - CustomTlkOffset;
            tlkEntries[abilityDescriptionId].Should().Be(expectedDescription);
        }
    }

    [Test]
    public void ThrowingBombardierSources_IncludeBibleStatValues()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "ThrowingPerkDefinition.cs").FullName);

        source.Should().Contain("StatType.ExplosiveTossBleedDurationSeconds, 15");
        source.Should().Contain("StatType.ExplosiveTossBleedDurationSeconds, 30");
        source.Should().Contain("StatType.AbilityDefenseIgnorePercentAdjustmentPerkType, (int)PerkType.ExplosiveToss");
        source.Should().Contain("StatType.AbilityDefenseIgnorePercentAdjustment, 10");
        source.Should().Contain("StatType.CriticalAbilityKnockdownPerkType, (int)PerkType.ExplosiveToss");
        source.Should().Contain("StatType.CriticalAbilityKnockdownDurationSeconds, 2");
        source.Should().NotContain("EquipmentPredicates.HasThrowing");

        var explosiveToss = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Throwing" / "ExplosiveTossAbilityDefinition.cs").FullName);
        explosiveToss.Should().Contain("targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire");
        explosiveToss.Should().Contain("areaVisualEffect: VisualEffect.Vfx_Fnf_Gas_Explosion_Fire");

        var fireburstToss = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Throwing" / "FireburstTossAbilityDefinition.cs").FullName);
        fireburstToss.Should().Contain("targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire");
        fireburstToss.Should().Contain("areaVisualEffect: VisualEffect.Vfx_Fnf_Gas_Explosion_Fire");

        var flashToss = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Throwing" / "FlashTossAbilityDefinition.cs").FullName);
        flashToss.Should().Contain("targetVisualEffect: VisualEffect.Vfx_Imp_Sonic");
        flashToss.Should().Contain("areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst");

        var rainOfSteel = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Throwing" / "RainOfSteelAbilityDefinition.cs").FullName);
        rainOfSteel.Should().Contain("targetVisualEffect: VisualEffect.Vfx_Com_Blood_Spark_Medium");
        rainOfSteel.Should().Contain("areaVisualEffect: VisualEffect.Vfx_Fnf_Swinging_Blade");

        var combat = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        combat.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Fire)");
        combat.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S)");
    }
    [Test]
    public void ThrowingBombardierFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");
        var classFeatRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "CLS_FEAT_FIGHT.2da");

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
            (FeatType.FlashToss2, "ife_flashtoss2", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.ExplosiveToss4, "ife_xplsvtoss4", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.RainOfSteel1, "ife_rainsteel1", "M", "0x3E", "1", "sphere", "8", "****", "1")
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

        AssertClassFeatOnMenu(classFeatRows, FeatType.ExplosiveToss1, "ExplosiveToss1");
        AssertClassFeatOnMenu(classFeatRows, FeatType.ExplosiveToss2, "ExplosiveToss2");
        AssertClassFeatOnMenu(classFeatRows, FeatType.ExplosiveToss3, "ExplosiveToss3");
        AssertClassFeatOnMenu(classFeatRows, FeatType.ExplosiveToss4, "ExplosiveToss4");
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

    private static void AssertClassFeatOnMenu(
        Dictionary<int, Dictionary<string, string>> classFeatRows,
        FeatType featType,
        string label)
    {
        var row = classFeatRows.Values
            .Should()
            .ContainSingle(x => x["FeatIndex"] == ((int)featType).ToString())
            .Which;

        row["FeatLabel"].Should().Be(label);
        row["List"].Should().Be("1");
        row["GrantedOnLevel"].Should().Be("99");
        row["OnMenu"].Should().Be("1");
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

    private static Dictionary<int, string> ReadTlkEntries(PathInfo path)
    {
        using var tlk = JsonDocument.Parse(File.ReadAllText(path.FullName));
        return tlk.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("id").GetInt32(),
                entry => entry.GetProperty("text").GetString() ?? string.Empty);
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
