using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class SaberstaffTempestTests
{
    [Test]
    public void SaberstaffTempestAbilities_MatchCombatBible()
    {
        var doubleStrike = new DoubleStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(doubleStrike[FeatType.DoubleStrike1], "Double Strike I", 1, RecastGroup.DoubleStrike, 60f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(doubleStrike[FeatType.DoubleStrike2], "Double Strike II", 2, RecastGroup.DoubleStrike, 60f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(doubleStrike[FeatType.DoubleStrike3], "Double Strike III", 3, RecastGroup.DoubleStrike, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(doubleStrike[FeatType.DoubleStrike4], "Double Strike IV", 4, RecastGroup.DoubleStrike, 60f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var circleSlash = new CircleSlashAbilityDefinition().BuildAbilities();
        AssertAbility(circleSlash[FeatType.CircleSlash1], "Circle Slash I", 1, RecastGroup.CircleSlash, 60f, 0f, 5, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(circleSlash[FeatType.CircleSlash2], "Circle Slash II", 2, RecastGroup.CircleSlash, 60f, 0f, 7, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(circleSlash[FeatType.CircleSlash3], "Circle Slash III", 3, RecastGroup.CircleSlash, 60f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var tempestStance = new TempestStanceAbilityDefinition().BuildAbilities()[FeatType.TempestStance1];
        AssertAbility(tempestStance, "Tempest Stance", 1, RecastGroup.TempestStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var maelstromArc = new MaelstromArcAbilityDefinition().BuildAbilities();
        AssertAbility(maelstromArc[FeatType.MaelstromArc1], "Maelstrom Arc I", 1, RecastGroup.MaelstromArc, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(maelstromArc[FeatType.MaelstromArc2], "Maelstrom Arc II", 2, RecastGroup.MaelstromArc, 60f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var tempestRelease = new TempestReleaseAbilityDefinition().BuildAbilities()[FeatType.TempestRelease1];
        AssertAbility(tempestRelease, "Tempest Release", 1, RecastGroup.TempestRelease, 120f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);

        var saberCyclone = new SaberCycloneAbilityDefinition().BuildAbilities()[FeatType.SaberCyclone1];
        AssertAbility(saberCyclone, "Saber Cyclone", 1, RecastGroup.Capstone, 345f, 0f, 15, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void SaberstaffTempestStatusEffects_MatchCombatBible()
    {
        var tempestStance = new TempestStanceStatusEffect();
        tempestStance.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(15);
        tempestStance.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(10);
        tempestStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-20);
        tempestStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-20);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);

        var forceErosion = new ForceErosionStatusEffect();
        forceErosion.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-10);
    }

    [Test]
    public void TempestRelease_ScalesDamageWithCurrentForcePoints()
    {
        InvokePrivateStatic<int>("CalculateForcePointDamageBonus", 0).Should().Be(0);
        InvokePrivateStatic<int>("CalculateForcePointDamageBonus", 9).Should().Be(0);
        InvokePrivateStatic<int>("CalculateForcePointDamageBonus", 10).Should().Be(2);
        InvokePrivateStatic<int>("CalculateForcePointDamageBonus", 55).Should().Be(10);
        InvokePrivateStatic<int>("CalculateForcePointDamageBonus", 100).Should().Be(20);
        InvokePrivateStatic<int>("CalculateForcePointDamageBonus", 140).Should().Be(20);
    }

    [Test]
    public void SaberstaffTempestTraitStatValues_MatchCombatBible()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "SaberstaffPerkDefinition.cs").FullName);
        var perks = BuildSaberstaffTempestPerksWithout2daLookup();

        source.Should().Contain("StatType.AttackDeflection, 8");
        source.Should().Contain("StatType.AttackDeflection, 16");
        source.Should().NotContain("StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandSaberstaff");

        perks[PerkType.FlowOfTheMaelstrom]
            .PerkLevels[1]
            .StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == StatType.SaberstaffAreaAbilityAttackDeflection)
            .Which
            .Calculate(0)
            .Should()
            .Be(8);
    }

    [Test]
    public void SaberstaffTempestFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.DoubleStrike1, "ife_dblstrk1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DoubleStrike2, "ife_dblstrk2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DoubleStrike3, "ife_dblstrk3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DoubleStrike4, "ife_dblstrk4", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CircleSlash1, "ife_circslsh1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.CircleSlash2, "ife_circslsh2", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.CircleSlash3, "ife_circslsh3", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.TempestStance1, "ife_tempstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.MaelstromArc1, "ife_maelarc1", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.MaelstromArc2, "ife_maelarc2", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.TempestRelease1, "ife_temprel1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.SaberCyclone1, "ife_sabrcycl1", "P", "0x01", "1", "sphere", "5", "****", "17")
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

            if (featType is FeatType.DoubleStrike1 or FeatType.DoubleStrike2 or FeatType.DoubleStrike3 or FeatType.DoubleStrike4)
            {
                featRow["TARGETSELF"].Should().Be("****");
                featRow["HostileFeat"].Should().Be("1");
            }
        }
    }

    [Test]
    public void SaberstaffTempestImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();

        var doubleStrike = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Saberstaff" / "DoubleStrikeAbilityDefinition.cs").FullName);
        doubleStrike.Should().Contain("bonusStatus: typeof(ForceErosionStatusEffect)");
        doubleStrike.Should().Contain("bonusDamage: 15");

        var tempestRelease = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Saberstaff" / "TempestReleaseAbilityDefinition.cs").FullName);
        tempestRelease.Should().Contain("private const int BaseDamage = 20;");
        tempestRelease.Should().Contain("private const int ForcePointStepSize = 10;");
        tempestRelease.Should().Contain("private const int DamageBonusPerForcePointStep = 2;");
        tempestRelease.Should().Contain("private const int MaximumForcePointDamageBonus = 20;");

        var saberCyclone = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Saberstaff" / "SaberCycloneAbilityDefinition.cs").FullName);
        saberCyclone.Should().Contain("private const float PulseIntervalSeconds = 6f;");
        saberCyclone.Should().Contain("private const int InitialDamage = 18;");
        saberCyclone.Should().Contain("private const int PulseDamage = 8;");
        saberCyclone.Should().Contain("private const int FPRestorePerTarget = 1;");
        saberCyclone.Should().Contain("private const int MaximumFPRestorePerPulse = 5;");
        saberCyclone.Should().Contain("CombatAreaPulses.SchedulePulses(");
        saberCyclone.Should().Contain("Combat.ApplyAbilityImpactEffects(activator, summary);");
        saberCyclone.Should().Contain("Math.Min(MaximumFPRestorePerPulse, summary.ImpactedTargetCount * FPRestorePerTarget)");
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
        perk.Category.Should().Be(PerkCategoryType.SaberstaffTempest);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Saberstaff, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Saberstaff);
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

    private static T InvokePrivateStatic<T>(string methodName, params object[] args)
    {
        return (T)typeof(TempestReleaseAbilityDefinition)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args)!;
    }

    private static Dictionary<PerkType, PerkDetail> BuildSaberstaffTempestPerksWithout2daLookup()
    {
        var definition = new SaberstaffPerkDefinition();
        var methodNames = new[]
        {
            "CircleSlash",
            "DoubleStrike",
            "FlowOfTheMaelstrom",
            "ForceGyre",
            "ForceMomentum",
            "MaelstromArc",
            "SaberCyclone",
            "SpinningDeflection",
            "TempestFocus",
            "TempestRelease",
            "TempestStance"
        };

        foreach (var methodName in methodNames)
        {
            typeof(SaberstaffPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(SaberstaffPerkDefinition)
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
