using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Perks;

public class TwinBladeCycloneTests
{
    [Test]
    public void TwinBladeCyclonePerkLevels_MatchCombatBible()
    {
        var perks = BuildTwinBladeCyclonePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.CrossCut], "Cross Cut", 1, 3, 5, FeatType.CrossCut1,
            "Instantly attacks twice, each for weapon DMG + 8, and inflicts Disoriented for 8 seconds.");
        AssertPerkLevel(perks[PerkType.SpinningWhirl], "Spinning Whirl", 1, 3, 8, FeatType.SpinningWhirl1,
            "Attacks up to 3 nearby enemies for weapon DMG + 10 each.");
        AssertPerkLevel(perks[PerkType.Momentum], "Momentum", 1, 3, 12, null,
            "Twin Blade abilities that hit 2 or more enemies grant +5% Haste for 8 seconds, up to +15%.",
            StatType.TwinBladeAreaAbilityMinTargetsHasteThreshold,
            StatType.TwinBladeAreaAbilityHastePercentAdjustment,
            StatType.TwinBladeAreaAbilityHasteDurationSeconds,
            StatType.TwinBladeAreaAbilityHastePercentMax);
        AssertPerkLevel(perks[PerkType.CycloneStance], "Cyclone Stance", 1, 2, 15, FeatType.CycloneStance1,
            "While active, grants +15% Haste and +10% Attack, but reduces Defense by 20%.");
        AssertPerkLevel(perks[PerkType.CrossCut], "Cross Cut", 2, 4, 18, FeatType.CrossCut2,
            "Instantly attacks twice, each for weapon DMG + 17, and inflicts Disoriented for 10 seconds.");
        AssertPerkLevel(perks[PerkType.SpinningWhirl], "Spinning Whirl", 2, 3, 20, FeatType.SpinningWhirl2,
            "Attacks up to 3 nearby enemies for weapon DMG + 18 each.");
        AssertPerkLevel(perks[PerkType.FlowingFootwork], "Flowing Footwork", 1, 2, 22, null,
            "After using a Twin Blade combat ability, gain +10% Evasion for 8 seconds.",
            StatType.TwinBladeAbilityUsedEvasionPercentAdjustment,
            StatType.TwinBladeAbilityUsedEvasionDurationSeconds);
        AssertPerkLevel(perks[PerkType.BladeVortex], "Blade Vortex", 1, 3, 25, FeatType.BladeVortex1,
            "Deals weapon DMG + 18 to all nearby enemies.");
        AssertPerkLevel(perks[PerkType.CrossCut], "Cross Cut", 3, 3, 28, FeatType.CrossCut3,
            "Instantly attacks twice, each for weapon DMG + 25, and inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.SpinningWhirl], "Spinning Whirl", 3, 3, 30, FeatType.SpinningWhirl3,
            "Attacks up to 3 nearby enemies for weapon DMG + 28 each.");
        AssertPerkLevel(perks[PerkType.Momentum], "Momentum", 2, 2, 32, null,
            "Momentum can stack up to +25% Haste and restores 2 STM whenever a stack is gained.",
            StatType.TwinBladeAreaAbilityMinTargetsHasteThreshold,
            StatType.TwinBladeAreaAbilityHastePercentAdjustment,
            StatType.TwinBladeAreaAbilityHasteDurationSeconds,
            StatType.TwinBladeAreaAbilityHastePercentMax,
            StatType.TwinBladeAreaAbilityStaminaRestoreOnHasteStack);
        AssertPerkLevel(perks[PerkType.BladeVortex], "Blade Vortex", 2, 4, 35, FeatType.BladeVortex2,
            "Deals weapon DMG + 26 to all nearby enemies and inflicts Exposed for 12 seconds.");
        AssertPerkLevel(perks[PerkType.SweepingAdvance], "Sweeping Advance", 1, 3, 38, FeatType.SweepingAdvance1,
            "Deals weapon DMG + 24 to enemies in a line. If this hits 3 or more enemies, restore 6 STM and gain +10% Haste for 8 seconds.");
        AssertPerkLevel(perks[PerkType.EdgeRhythm], "Edge Rhythm", 1, 2, 40, null,
            "Every third auto-attack with a twin blade deals +15 DMG to a nearby enemy.",
            StatType.AutoAttackCycleDamageSkillType,
            StatType.AutoAttackCycleRequiredCount,
            StatType.AutoAttackCycleDamage,
            StatType.AutoAttackCycleRadiusMeters);
        AssertPerkLevel(perks[PerkType.CrossCut], "Cross Cut", 4, 4, 42, FeatType.CrossCut4,
            "Instantly attacks twice, each for weapon DMG + 34. Inflicts Disoriented and Hamstring for 12 seconds.");
        AssertPerkLevel(perks[PerkType.StormRelease], "Storm Release", 1, 3, 45, FeatType.StormRelease1,
            "Consume all Momentum stacks to deal weapon DMG + 15 per stack to all nearby enemies.");
        AssertPerkLevel(perks[PerkType.CycloneMastery], "Cyclone Mastery", 1, 4, 48, null,
            "Area Twin Blade abilities gain +10% critical chance and restore 1 STM per target hit, up to 5 STM.",
            StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment,
            StatType.TwinBladeAreaAbilityStaminaRestorePerTarget,
            StatType.TwinBladeAreaAbilityStaminaRestoreMax);
        AssertPerkLevel(perks[PerkType.TempestBloom], "Tempest Bloom", 1, 4, 50, FeatType.TempestBloom1,
            "Deal weapon DMG + 20 to nearby enemies. For 45 seconds, pulse every 6 seconds, dealing light physical damage and applying a Tempest mark. Each mark increases physical damage taken by 2% to a maximum of 3 stacks.");
    }

    [Test]
    public void TwinBladeCycloneAbilities_MatchCombatBible()
    {
        var crossCut = new CrossCutAbilityDefinition().BuildAbilities();
        AssertAbility(crossCut[FeatType.CrossCut1], "Cross Cut I", 1, SkillType.TwinBlade, RecastGroup.CrossCut, 60f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(crossCut[FeatType.CrossCut2], "Cross Cut II", 2, SkillType.TwinBlade, RecastGroup.CrossCut, 60f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(crossCut[FeatType.CrossCut3], "Cross Cut III", 3, SkillType.TwinBlade, RecastGroup.CrossCut, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(crossCut[FeatType.CrossCut4], "Cross Cut IV", 4, SkillType.TwinBlade, RecastGroup.CrossCut, 60f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var spinningWhirl = new SpinningWhirlAbilityDefinition().BuildAbilities();
        AssertAbility(spinningWhirl[FeatType.SpinningWhirl1], "Spinning Whirl I", 1, SkillType.TwinBlade, RecastGroup.SpinningWhirl, 60f, 0f, 5, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(spinningWhirl[FeatType.SpinningWhirl2], "Spinning Whirl II", 2, SkillType.TwinBlade, RecastGroup.SpinningWhirl, 60f, 0f, 7, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(spinningWhirl[FeatType.SpinningWhirl3], "Spinning Whirl III", 3, SkillType.TwinBlade, RecastGroup.SpinningWhirl, 60f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var cycloneStance = new CycloneStanceAbilityDefinition().BuildAbilities()[FeatType.CycloneStance1];
        AssertAbility(cycloneStance, "Cyclone Stance", 1, SkillType.Invalid, RecastGroup.CycloneStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var bladeVortex = new BladeVortexAbilityDefinition().BuildAbilities();
        AssertAbility(bladeVortex[FeatType.BladeVortex1], "Blade Vortex I", 1, SkillType.TwinBlade, RecastGroup.BladeVortex, 75f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(bladeVortex[FeatType.BladeVortex2], "Blade Vortex II", 2, SkillType.TwinBlade, RecastGroup.BladeVortex, 75f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var sweepingAdvance = new SweepingAdvanceAbilityDefinition().BuildAbilities()[FeatType.SweepingAdvance1];
        AssertAbility(sweepingAdvance, "Sweeping Advance", 1, SkillType.TwinBlade, RecastGroup.SweepingAdvance, 60f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var stormRelease = new StormReleaseAbilityDefinition().BuildAbilities()[FeatType.StormRelease1];
        AssertAbility(stormRelease, "Storm Release", 1, SkillType.TwinBlade, RecastGroup.StormRelease, 120f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);
        stormRelease.CustomValidation.Should().NotBeNull();

        var tempestBloom = new TempestBloomAbilityDefinition().BuildAbilities()[FeatType.TempestBloom1];
        AssertAbility(tempestBloom, "Tempest Bloom", 1, SkillType.TwinBlade, RecastGroup.Capstone, 345f, 0f, 15, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void TwinBladeCycloneStatusEffects_MatchCombatBible()
    {
        var cyclone = new CycloneStanceStatusEffect();
        cyclone.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(15);
        cyclone.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(10);
        cyclone.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-20);
        cyclone.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-20);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);

        var exposed = new ExposedStatusEffect();
        exposed.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);

        var tempestMark = new TempestMarkStatusEffect();
        tempestMark.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(2);
        tempestMark.StackingType.Should().Be(StatusEffectStackType.UnlimitedStacking);
    }

    [Test]
    public void StormRelease_RequiresConsumesAndCountsMomentumStacks()
    {
        const uint creature = 0x00ABCDEF;
        var ability = new StormReleaseAbilityDefinition().BuildAbilities()[FeatType.StormRelease1];

        TemporaryStatModifier.Clear(creature);
        try
        {
            ability.CustomValidation!(creature, OBJECT_INVALID, 1, default).Should().Be("You have no Momentum stacks.");

            TemporaryStatModifier.Add(
                creature,
                StatType.AttackDelayReductionPercent,
                15,
                30f,
                StatType.TwinBladeAreaAbilityHastePercentAdjustment);

            ability.CustomValidation!(creature, OBJECT_INVALID, 1, default).Should().BeEmpty();
            InvokePrivateStatic<int>("GetMomentumStackCount", creature).Should().Be(3);
            InvokePrivateStatic<int>("ConsumeMomentumStacks", creature).Should().Be(3);
            TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.AttackDelayReductionPercent,
                StatType.TwinBladeAreaAbilityHastePercentAdjustment).Should().Be(0);
        }
        finally
        {
            TemporaryStatModifier.Clear(creature);
        }
    }

    [Test]
    public void TwinBladeCycloneFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.CrossCut1, "ife_crscut1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CrossCut2, "ife_crscut2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CrossCut3, "ife_crscut3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CrossCut4, "ife_crscut4", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SpinningWhirl1, "ife_spnnngwhrl1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.SpinningWhirl2, "ife_spnnngwhrl2", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.SpinningWhirl3, "ife_spnnngwhrl3", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.CycloneStance1, "ife_cyclstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.BladeVortex1, "ife_bladevort1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.BladeVortex2, "ife_bladevort2", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.SweepingAdvance1, "ife_swpngdvnc1", "M", "0x3E", "1", "rectangle", "8", "2.5", "17"),
            (FeatType.StormRelease1, "ife_strmrel1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.TempestBloom1, "ife_tempblm1", "P", "0x01", "1", "sphere", "5", "****", "17")
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

            if (featType is FeatType.CrossCut1 or FeatType.CrossCut2 or FeatType.CrossCut3 or FeatType.CrossCut4)
            {
                featRow["TARGETSELF"].Should().Be("****");
                featRow["HostileFeat"].Should().Be("1");
            }
        }
    }

    [Test]
    public void TwinBladeCycloneImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();

        var sweepingAdvance = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "TwinBlade" / "SweepingAdvanceAbilityDefinition.cs").FullName);
        sweepingAdvance.Should().Contain("private const int MomentumTargetThreshold = 3;");
        sweepingAdvance.Should().Contain("private const int MomentumStaminaRestore = 6;");
        sweepingAdvance.Should().Contain("private const int MomentumHastePercent = 10;");
        sweepingAdvance.Should().Contain("private const int MomentumDurationSeconds = 8;");

        var stormRelease = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "TwinBlade" / "StormReleaseAbilityDefinition.cs").FullName);
        stormRelease.Should().Contain("private const int DamagePerMomentumStack = 15;");
        stormRelease.Should().Contain("DamagePerMomentumStack * momentumStacks");
        stormRelease.Should().Contain("TemporaryStatModifier.Consume(");

        var tempestBloom = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "TwinBlade" / "TempestBloomAbilityDefinition.cs").FullName);
        tempestBloom.Should().Contain("private const float PulseIntervalSeconds = 6f;");
        tempestBloom.Should().Contain("private const int InitialDamage = 20;");
        tempestBloom.Should().Contain("private const int PulseDamage = 8;");
        tempestBloom.Should().Contain("private const int MaximumMarkStacks = 3;");
        tempestBloom.Should().Contain("CombatAreaPulses.SchedulePulses(");
        tempestBloom.Should().Contain("Combat.ApplyAbilityImpactEffects(activator, summary);");
        tempestBloom.Should().Contain("afterSuccessfulHit: ApplyTempestMark");
        tempestBloom.Should().Contain("typeof(TempestMarkStatusEffect)");
        tempestBloom.Should().Contain("activeStacks >= MaximumMarkStacks");
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
        perk.Category.Should().Be(PerkCategoryType.TwinBladeCyclone);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.TwinBlade, skillRank);

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
        SkillType skillType,
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
        ability.SkillType.Should().Be(skillType);
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
        return (T)typeof(StormReleaseAbilityDefinition)
            .GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, args)!;
    }

    private static Dictionary<PerkType, PerkDetail> BuildTwinBladeCyclonePerksWithout2daLookup()
    {
        var definition = new TwinBladePerkDefinition();
        var methodNames = new[]
        {
            "BladeVortex",
            "CrossCut",
            "CycloneMastery",
            "CycloneStance",
            "EdgeRhythm",
            "FlowingFootwork",
            "Momentum",
            "SpinningWhirl",
            "StormRelease",
            "SweepingAdvance",
            "TempestBloom"
        };

        foreach (var methodName in methodNames)
        {
            typeof(TwinBladePerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(TwinBladePerkDefinition)
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
