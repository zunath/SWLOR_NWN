using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class VibroknifeSaboteurTests
{
    [Test]
    public void VibroknifeSaboteurAbilities_MatchCombatBible()
    {
        var enfeeblingStrike = new EnfeeblingStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(enfeeblingStrike[FeatType.EnfeeblingStrike1], "Enfeebling Strike I", 1, RecastGroup.EnfeeblingStrike, 45f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(enfeeblingStrike[FeatType.EnfeeblingStrike2], "Enfeebling Strike II", 2, RecastGroup.EnfeeblingStrike, 45f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(enfeeblingStrike[FeatType.EnfeeblingStrike3], "Enfeebling Strike III", 3, RecastGroup.EnfeeblingStrike, 45f, 0f, 7, true, true, true, false, AbilityActivationType.Casted);

        var hamstring = new HamstringAbilityDefinition().BuildAbilities();
        AssertAbility(hamstring[FeatType.Hamstring1], "Hamstring I", 1, RecastGroup.Hamstring, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(hamstring[FeatType.Hamstring2], "Hamstring II", 2, RecastGroup.Hamstring, 30f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(hamstring[FeatType.Hamstring3], "Hamstring III", 3, RecastGroup.Hamstring, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var nerveStrike = new NerveStrikeAbilityDefinition().BuildAbilities()[FeatType.NerveStrike1];
        AssertAbility(nerveStrike, "Nerve Strike", 1, RecastGroup.NerveStrike, 60f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);

        var debilitatingStance = new DebilitatingStanceAbilityDefinition().BuildAbilities()[FeatType.DebilitatingStance1];
        AssertAbility(debilitatingStance, "Debilitating Stance", 1, RecastGroup.DebilitatingStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var incapacitate = new IncapacitateAbilityDefinition().BuildAbilities()[FeatType.Incapacitate1];
        AssertAbility(incapacitate, "Incapacitate", 1, RecastGroup.Incapacitate, 120f, 2f, 10, true, false, false, true, AbilityActivationType.Casted);

        var systemicShutdown = new SystemicShutdownAbilityDefinition().BuildAbilities()[FeatType.SystemicShutdown1];
        AssertAbility(systemicShutdown, "Systemic Shutdown", 1, RecastGroup.Capstone, 345f, 3f, 15, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void VibroknifeSaboteurStaticStatBonuses_MatchCombatBible()
    {
        var perks = BuildVibroknifeSaboteurPerksWithout2daLookup();

        AssertStatBonus(perks[PerkType.CalculatedStrikes].PerkLevels[1], StatType.AutoAttackTargetAccuracyPercentAdjustmentChance, 15);
        AssertStatBonus(perks[PerkType.CalculatedStrikes].PerkLevels[1], StatType.AutoAttackTargetAccuracyPercentAdjustment, -10);
        AssertStatBonus(perks[PerkType.CalculatedStrikes].PerkLevels[1], StatType.AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds, 6);

        AssertStatBonus(perks[PerkType.ExploitWeakness].PerkLevels[1], StatType.DamageToDebuffedTargetPercentAdjustment, 12);
    }

    [Test]
    public void VibroknifeSaboteurStatusEffects_MatchCombatBible()
    {
        var weakened1 = new WeakenedStatusEffect(10);
        weakened1.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);

        var weakened2 = new WeakenedStatusEffect();
        weakened2.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);

        var weakened3 = new WeakenedStatusEffect(20);
        weakened3.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);

        var exhausted1 = new ExhaustedStatusEffect();
        exhausted1.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-10);
        exhausted1.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-10);

        var exhausted2 = new ExhaustedStatusEffect(15);
        exhausted2.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-15);
        exhausted2.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-15);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);

        var debilitatingStance = new DebilitatingStanceStatusEffect();
        debilitatingStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);

        var incapacitate = new IncapacitateStatusEffect();
        incapacitate.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);

        var toxin = new ToxinStatusEffect();
        toxin.Frequency.Should().Be(6f);
        toxin.ResistanceType.Should().Be(SWLOR.Game.Server.Service.CombatService.ResistanceType.Poison);

        var hamstring = new HamstringStatusEffect();
        hamstring.ApplyEffect(0, 0, 12);
        hamstring.StatGroup.Stats[StatType.MovementSpeedPercentAdjustment].Should().Be(-20);

        var vulnerable = new VulnerableStatusEffect();
        vulnerable.Name.Should().Be("Vulnerable");
        vulnerable.Categories.Should().HaveFlag(StatusEffectCategory.Debuff);
        vulnerable.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-10);
    }

    [Test]
    public void VibroknifeSaboteurFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.DebilitatingStance1, "ife_debilstnc1", "0x01", false, "****", "****"),
            (FeatType.EnfeeblingStrike1, "ife_enfbstrk1", "0x02", true, "****", "1"),
            (FeatType.EnfeeblingStrike2, "ife_enfbstrk2", "0x02", true, "****", "1"),
            (FeatType.EnfeeblingStrike3, "ife_enfbstrk3", "0x02", true, "****", "1"),
            (FeatType.Hamstring1, "ife_hamstr1", "0x01", false, "****", "****"),
            (FeatType.Hamstring2, "ife_hamstr2", "0x01", false, "****", "****"),
            (FeatType.Hamstring3, "ife_hamstr3", "0x01", false, "****", "****"),
            (FeatType.Incapacitate1, "ife_incap1", "0x01", true, "sphere", "****"),
            (FeatType.NerveStrike1, "ife_nervstrk1", "0x02", true, "****", "1"),
            (FeatType.SystemicShutdown1, "ife_sysshut1", "0x3E", true, "sphere", "1"),
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, targetType, isHostile, targetShape, hostileFeat) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

            spellRow["TargetType"].Should().Be(targetType);
            spellRow["HostileSetting"].Should().Be(isHostile ? "1" : "0");
            spellRow["TargetShape"].Should().Be(targetShape);
            featRow["HostileFeat"].Should().Be(hostileFeat);
        }
    }

    [Test]
    public void CascadeFailure_AddsVisibleConeAndVulnerableOverlayToIncapacitate()
    {
        var root = FindRepositoryRoot();
        var ability = new IncapacitateAbilityDefinition().BuildAbilities()[FeatType.Incapacitate1];
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Vibroknife" / "IncapacitateAbilityDefinition.cs").FullName)
            .Replace("\r\n", "\n");

        var additionalTargeting = ability.AdditionalActivationTargeting
            .Should()
            .ContainSingle()
            .Which;
        additionalTargeting.Shape.Should().Be(AbilityTargetingShapeType.Cone);
        additionalTargeting.SizeX.Should().Be(5f);
        additionalTargeting.SizeY.Should().Be(5f);
        additionalTargeting.UpdatesClientTargeting.Should().BeFalse();

        source.Should().Contain("Stat.GetStatAdjustment(activator, StatType.VibroknifeSaboteurCascadeFailure)");
        source.Should().Contain(".AddActivationTargetingCone(");
        source.Should().Contain("CascadeFailureConeLengthResolver");
        source.Should().Contain("Telegraph.CreateConeTelegraph(");
        source.Should().Contain("private const float CascadeFailureConeLength = 5f;");
        source.Should().Contain("private const float CascadeFailureConeWidth = 5f;");
        source.Should().Contain("private const float CascadeFailureVulnerableDurationSeconds = 12f;");
        source.Should().Contain("typeof(VulnerableStatusEffect)");
        source.Should().Contain("PlaysSoundOnImpact(IncapacitateImpactSound)");
        source.Should().Contain("VisualEffect.Vfx_Fnf_Pwstun");
        source.Should().Contain("VisualEffect.Vfx_Imp_Stun");
        source.Should().Contain("VisualEffect.Vfx_Imp_Dazed_S");
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
        perk.Category.Should().Be(PerkCategoryType.VibroknifeSaboteur);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Vibroknife, skillRank);

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

    private static Dictionary<PerkType, PerkDetail> BuildVibroknifeSaboteurPerksWithout2daLookup()
    {
        var definition = new VibroknifePerkDefinition();
        var methodNames = new[]
        {
            "AfflictionMastery",
            "CalculatedStrikes",
            "CascadeFailure",
            "CripplingPrecision",
            "DebilitatingStance",
            "EnfeeblingStrike",
            "ExploitWeakness",
            "Hamstring",
            "Incapacitate",
            "NerveStrike",
            "SapVitality",
            "SystemicShutdown",
            "ToxicCoating"
        };

        foreach (var methodName in methodNames)
        {
            typeof(VibroknifePerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(VibroknifePerkDefinition)
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
