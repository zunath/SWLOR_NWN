using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Rifle;
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

public class RiflePacificationTests
{
    [Test]
    public void RiflePacificationAbilities_MatchCombatBible()
    {
        var tranquilizerShot = new TranquilizerShotAbilityDefinition().BuildAbilities();
        AssertAbility(tranquilizerShot[FeatType.TranquilizerShot1], "Tranquilizer Shot I", 1, RecastGroup.TranquilizerShot, 60f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(tranquilizerShot[FeatType.TranquilizerShot2], "Tranquilizer Shot II", 2, RecastGroup.TranquilizerShot, 60f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);

        var cripplingShot = new CripplingShotAbilityDefinition().BuildAbilities();
        AssertAbility(cripplingShot[FeatType.CripplingShot1], "Crippling Shot I", 1, RecastGroup.CripplingShot, 30f, 0f, 4, true, true, true, false, AbilityActivationType.Casted, 30f);
        AssertAbility(cripplingShot[FeatType.CripplingShot2], "Crippling Shot II", 2, RecastGroup.CripplingShot, 30f, 0f, 6, true, true, true, false, AbilityActivationType.Casted, 30f);
        AssertAbility(cripplingShot[FeatType.CripplingShot3], "Crippling Shot III", 3, RecastGroup.CripplingShot, 30f, 0f, 8, true, true, true, false, AbilityActivationType.Casted, 30f);

        var spotterStance = new SpotterStanceAbilityDefinition().BuildAbilities()[FeatType.SpotterStance1];
        AssertAbility(spotterStance, "Spotter Stance", 1, RecastGroup.SpotterStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var tranqCone = new TranqConeAbilityDefinition().BuildAbilities();
        AssertAbility(tranqCone[FeatType.TranqCone1], "Tranq Cone I", 1, RecastGroup.TranqCone, 120f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(tranqCone[FeatType.TranqCone2], "Tranq Cone II", 2, RecastGroup.TranqCone, 120f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var pacificationField = new PacificationFieldAbilityDefinition().BuildAbilities()[FeatType.PacificationField1];
        AssertAbility(pacificationField, "Pacification Field", 1, RecastGroup.PacificationField, 180f, 0f, 14, true, false, false, true, AbilityActivationType.Casted);

        var stasisVolley = new StasisVolleyAbilityDefinition().BuildAbilities()[FeatType.StasisVolley1];
        AssertAbility(stasisVolley, "Stasis Volley", 1, RecastGroup.Capstone, 345f, 2f, 15, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void RiflePacificationStatusEffects_MatchCombatBible()
    {
        var spotterStance = new SpotterStanceStatusEffect();
        spotterStance.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(15);
        spotterStance.StatGroup.Stats[StatType.RangedEvasionPercentAdjustment].Should().Be(15);
        spotterStance.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(-10);
        spotterStance.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(0);

        var tranquilized = new TranquilizedStatusEffect();
        tranquilized.Categories.Should().HaveFlag(StatusEffectCategory.Control);
        tranquilized.Categories.Should().HaveFlag(StatusEffectCategory.Debuff);
        tranquilized.ResistanceType.Should().Be(ResistanceType.Mind);

        var pacificationField = new PacificationFieldStatusEffect();
        pacificationField.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);

        var stasisVolley = new StasisVolleyStatusEffect();
        stasisVolley.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
    }
    [Test]
    public void RiflePacificationFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.TranquilizerShot1, "ife_trnqlzrshot1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CripplingShot1, "ife_cripshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SpotterStance1, "ife_spotstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.TranquilizerShot2, "ife_trnqlzrshot2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CripplingShot2, "ife_cripshot2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.TranqCone1, "ife_tranqcone1", "M", "0x3E", "1", "cone", "8", "6", "17"),
            (FeatType.CripplingShot3, "ife_cripshot3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.TranqCone2, "ife_tranqcone2", "M", "0x3E", "1", "cone", "10", "7", "17"),
            (FeatType.PacificationField1, "ife_pacfld1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.StasisVolley1, "ife_stasvol1", "M", "0x3E", "1", "cone", "5", "5", "17")
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
        perk.Category.Should().Be(PerkCategoryType.RiflePacification);

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

    private static Dictionary<PerkType, PerkDetail> BuildRiflePacificationPerksWithout2daLookup()
    {
        var definition = new RiflePerkDefinition();
        var methodNames = new[]
        {
            "ContainmentNet",
            "CripplingShot",
            "FieldSedatives",
            "NeutralizingShot",
            "Overwatch",
            "PacificationField",
            "PinningFire",
            "SoftTarget",
            "SpotterStance",
            "StasisVolley",
            "TranqCone",
            "TranquilizerShot",
            "VeteranTracker"
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
