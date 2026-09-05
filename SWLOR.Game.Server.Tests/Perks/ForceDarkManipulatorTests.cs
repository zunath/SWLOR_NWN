using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ForceDarkManipulatorTests
{
    private const int CustomTlkOffset = 16777216;

    [Test]
    public void ForceDarkManipulatorPerks_MatchCombatBible()
    {
        var perks = BuildForceDarkManipulatorPerksWithout2daLookup();
        AssertPerkLevel(
            perks[PerkType.CreepingTerror],
            "Creeping Terror",
            1,
            2,
            2,
            FeatType.CreepingTerror1,
            "Creates a visible 5m field within 15m for 30 seconds. Enemies inside are Hobbled and take 10 force DMG plus WIL scaling every 3 seconds.");
        AssertPerkLevel(
            perks[PerkType.CreepingTerror],
            "Creeping Terror",
            2,
            3,
            15,
            FeatType.CreepingTerror2,
            "Creates a visible 5m field within 15m for 30 seconds. Enemies inside are Hobbled and take 14 force DMG plus WIL scaling every 3 seconds.");
        AssertPerkLevel(
            perks[PerkType.CreepingTerror],
            "Creeping Terror",
            3,
            4,
            38,
            FeatType.CreepingTerror3,
            "Creates a visible 8m field within 15m for 30 seconds. Enemies inside are Hobbled and take 18 force DMG plus WIL scaling every 3 seconds.");
    }

    [Test]
    public void ForceDarkManipulatorAbilities_MatchCombatBible()
    {
        var creepingTerror = new CreepingTerrorAbilityDefinition().BuildAbilities();
        AssertAbility(creepingTerror[FeatType.CreepingTerror1], "Creeping Terror I", 1, RecastGroup.CreepingTerror, 32f, 1f, 4, true, false, false, true, AbilityActivationType.Casted, 15f, true);
        AssertAbility(creepingTerror[FeatType.CreepingTerror2], "Creeping Terror II", 2, RecastGroup.CreepingTerror, 32f, 1f, 6, true, false, false, true, AbilityActivationType.Casted, 15f, true);
        AssertAbility(creepingTerror[FeatType.CreepingTerror3], "Creeping Terror III", 3, RecastGroup.CreepingTerror, 32f, 1.5f, 8, true, false, false, true, AbilityActivationType.Casted, 15f, true);

        var weakenResolve = new WeakenResolveAbilityDefinition().BuildAbilities();
        AssertAbility(weakenResolve[FeatType.WeakenResolve1], "Weaken Resolve I", 1, RecastGroup.WeakenResolve, 12f, 1f, 3, true, true, true, false, AbilityActivationType.Casted, 15f, false);
        AssertAbility(weakenResolve[FeatType.WeakenResolve2], "Weaken Resolve II", 2, RecastGroup.WeakenResolve, 12f, 1f, 5, true, true, true, false, AbilityActivationType.Casted, 15f, false);

        var nightmareField = new NightmareFieldAbilityDefinition().BuildAbilities()[FeatType.NightmareField1];
        AssertAbility(nightmareField, "Nightmare Field", 1, RecastGroup.NightmareField, 36f, 1.5f, 7, true, false, false, true, AbilityActivationType.Casted, 5f, false);

        var forceChoke = new ForceChokeAbilityDefinition().BuildAbilities();
        AssertAbility(forceChoke[FeatType.ForceChoke1], "Force Choke I", 1, RecastGroup.ForceChoke, 45f, 1.5f, 2, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceChoke[FeatType.ForceChoke2], "Force Choke II", 2, RecastGroup.ForceChoke, 45f, 1.5f, 3, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceChoke[FeatType.ForceChoke3], "Force Choke III", 3, RecastGroup.ForceChoke, 45f, 1.5f, 4, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceChoke[FeatType.ForceChoke4], "Force Choke IV", 4, RecastGroup.ForceChoke, 45f, 1.5f, 5, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        forceChoke[FeatType.ForceChoke1].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
        forceChoke[FeatType.ForceChoke2].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
        forceChoke[FeatType.ForceChoke3].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);
        forceChoke[FeatType.ForceChoke4].ImpactAnimationType.Should().Be(Animation.CastOutAnimation);

        var eclipse = new EclipseOfResolveAbilityDefinition().BuildAbilities()[FeatType.EclipseOfResolve1];
        AssertAbility(eclipse, "Eclipse of Resolve", 1, RecastGroup.Capstone, 90f, 1.5f, 10, true, false, false, true, AbilityActivationType.Casted, 5f, false);
    }

    [Test]
    public void ForceDarkManipulatorStatusEffects_MatchCombatBible()
    {
        new WeakenResolve1StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(5);
        new WeakenResolve2StatusEffect().StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(10);

        var nightmare = new NightmareField1StatusEffect();
        nightmare.StatGroup.Stats[StatType.Accuracy].Should().Be(-10);
        nightmare.StatGroup.Stats[StatType.Evasion].Should().Be(-10);

        var eclipse = new EclipseOfResolve1StatusEffect();
        eclipse.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(-15);
        eclipse.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);
        eclipse.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);
        eclipse.StatGroup.Stats[StatType.FPCostPercentAdjustment].Should().Be(25);
        eclipse.StatGroup.Stats[StatType.AbilityStaminaCostPercentAdjustment].Should().Be(25);

        var creepingTerrorDot = new CreepingTerrorDamageStatusEffect();
        creepingTerrorDot.Name.Should().Be("Creeping Terror");
        creepingTerrorDot.Frequency.Should().Be(3f);
        creepingTerrorDot.ResistanceType.Should().Be(SWLOR.Game.Server.Service.CombatService.ResistanceType.Disruption);

        var forceChokeDot = new ForceChokeDamageStatusEffect();
        forceChokeDot.Name.Should().Be("Force Choke");
        forceChokeDot.Frequency.Should().Be(3f);
        forceChokeDot.ResistanceType.Should().Be(SWLOR.Game.Server.Service.CombatService.ResistanceType.Disruption);
    }

    [Test]
    public void ForceDarkManipulatorSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();

        var creepingTerror = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "CreepingTerrorAbilityDefinition.cs").FullName);
        creepingTerror.Should().Contain("CreateCreepingTerrorField");
        creepingTerror.Should().Contain("CombatAreaPulses.SchedulePulses");
        creepingTerror.Should().Contain("Ability.CaptureRepeatedAbilityImpact(activator,",
            "periodic field pulses must not spend limited attack charges");
        creepingTerror.Should().Contain("HasCustomValidation(ValidateTargetingRange)");
        creepingTerror.Should().Contain("AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation)");
        creepingTerror.Should().Contain("GetDistanceBetweenLocations(GetLocation(activator), location) <= FieldRange");
        creepingTerror.Should().Contain("var scaledPulseDamage = AbilityEffectScaling.ScaleDirectEffect");
        creepingTerror.Should().Contain("ApplyCreepingTerrorPulse(activator, location, scaledPulseDamage, radius)");
        creepingTerror.Should().Contain("AbilityAreaEffects.CreatePersistentSphereIndicator(");
        creepingTerror.Should().Contain("ApplyCreepingTerrorDamage");
        creepingTerror.Should().Contain("Ability.ApplyHostileCombatImpact");
        creepingTerror.Should().Contain("statusEffect: typeof(HobbleStatusEffect)");
        creepingTerror.Should().Contain("awardsCombatPoints: false");
        creepingTerror.Should().Contain("Combat.ApplyDamageTakenModifiers(target, damage, activator, CombatDamageType.Force)");
        creepingTerror.Should().Contain("CreepingTerror1Damage = 10");
        creepingTerror.Should().Contain("CreepingTerror2Damage = 14");
        creepingTerror.Should().Contain("CreepingTerror3Damage = 18");
        creepingTerror.Should().Contain("HobbleRefreshDurationSeconds");
        creepingTerror.Should().Contain("LargeFieldRadius = 8f");
        creepingTerror.Should().Contain("EffectAreaOfEffect(areaOfEffect)");
        creepingTerror.Should().Contain("AreaOfEffect.CreepingTerrorTentacles");
        creepingTerror.Should().Contain("AreaOfEffect.CreepingTerrorLargeTentacles");
        creepingTerror.Should().NotContain("StatusEffect.ApplyStatusEffect(activator, hostile, typeof(HobbleStatusEffect)");
        creepingTerror.Should().NotContain("AssignCommand(");
        creepingTerror.Should().NotContain("EffectDamage(damage, CombatDamageType.Force.GetNWScriptDamageType())");
        creepingTerror.Should().NotContain("Vfx_Fnf_Howl_Mind");
        creepingTerror.Should().NotContain("Vfx_Imp_Pulse_Negative");
        creepingTerror.Should().NotContain("ApplyForceDamageOverTime");
        creepingTerror.Should().NotContain("ApplyCombatImpact");
        creepingTerror.Should().NotContain("ApplyTelegraphedCombatImpact");

        var forceChoke = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceChokeAbilityDefinition.cs").FullName);
        forceChoke.Should().Contain("ForceChokeDamageStatusEffect(totalDamage)");
        forceChoke.Should().Contain("ApplyForceChokeEffects");
        forceChoke.Should().Contain("AssignCommand(target, () => ActionPlayAnimation(Animation.ForceChoke))");
        forceChoke.Should().NotContain(".UsesImpactAnimation(Animation.ForceChoke)");

        var weakenResolve = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "WeakenResolveAbilityDefinition.cs").FullName);
        weakenResolve.Should().Contain(".PlaysSoundOnImpact(\"ksfx_frc_mind\")");
        weakenResolve.Should().Contain("VisualEffect.Vfx_Dur_Aura_Pulse_Red_Black");
        weakenResolve.Should().NotContain("VisualEffect.Vfx_Imp_Pulse_Negative");

        var eclipse = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "EclipseOfResolveAbilityDefinition.cs").FullName);
        eclipse.Should().Contain(".PlaysSoundOnImpact(\"ksfx_frc_night\")");
        eclipse.Should().Contain("VisualEffect.Vfx_Dur_Aura_Pulse_Red_Black");
        eclipse.Should().NotContain("VisualEffect.Vfx_Imp_Pulse_Negative");
        eclipse.Should().NotContain("VisualEffect.Vfx_Fnf_Howl_Mind");

        var iconManifest = File.ReadAllText((root / "SWLOR.Game.Server" / "Readmes" / "GameplayIconManifest.csv").FullName);
        iconManifest.Should().Contain("\"Ability\",\"WeakenResolve1\",\"WeakenResolve1\",\"Harmful\"");
        iconManifest.Should().Contain("\"Ability\",\"WeakenResolve2\",\"WeakenResolve2\",\"Harmful\"");
        iconManifest.Should().NotContain("\"Ability\",\"WeakenResolve1\",\"WeakenResolve1\",\"Beneficial\"");
        iconManifest.Should().NotContain("\"Ability\",\"WeakenResolve2\",\"WeakenResolve2\",\"Beneficial\"");

        var immobilized = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "ImmobilizedStatusEffect.cs").FullName);
        immobilized.Should().Contain("Enmity.AttackHighestEnmityTarget(creature)");

        var staminaRequirement = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "AbilityService" / "AbilityRequirementStamina.cs").FullName);
        staminaRequirement.Should().Contain("AbilityStaminaCostPercentAdjustment");

        var forceDamageOverTime = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "ForceDamageOverTimeStatusEffectBase.cs").FullName);
        forceDamageOverTime.Should().Contain("Ability.ApplyDarkForceDamageRestoration(Source, damage)");
    }

    [Test]
    public void ForceDarkManipulatorFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.CreepingTerror1, "ife_crpngtrrr1", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.ForceChoke2, "ife_forcechk2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.WeakenResolve1, "ife_wknres1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CreepingTerror2, "ife_crpngtrrr2", "M", "0x3E", "1", "sphere", "5", "****", "1"),
            (FeatType.ForceChoke3, "ife_forcechk3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.NightmareField1, "ife_nghtmrfld1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.WeakenResolve2, "ife_wknres2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceChoke1, "ife_forcechk1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CreepingTerror3, "ife_crpngtrrr3", "M", "0x3E", "1", "sphere", "8", "****", "1"),
            (FeatType.ForceChoke4, "ife_forcechk4", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.EclipseOfResolve1, "ife_eclres1", "P", "0x01", "1", "sphere", "5", "****", "17")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            abilityRow["IconResRef"].Should().Be(featIcon);
            if (featType is FeatType.CreepingTerror1 or FeatType.CreepingTerror2 or FeatType.CreepingTerror3)
            {
                featRow["TARGETSELF"].Should().Be("****");
                featRow["HostileFeat"].Should().Be("1");
            }

            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be(targetShape);
            abilityRow["TargetSizeX"].Should().Be(targetSizeX);
            abilityRow["TargetSizeY"].Should().Be(targetSizeY);
            abilityRow["TargetFlags"].Should().Be(targetFlags);
        }
    }

    [Test]
    public void CreepingTerrorFeatAndAbilityDescriptions_MatchImplementedValues()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "sw_tlk" / "sw_tlk.tlk.json");
        var descriptions = new[]
        {
            (FeatType.CreepingTerror1, "Creates a visible 5m field within 15m for 30 seconds. Enemies inside are Hobbled and take 10 force DMG plus WIL scaling every 3 seconds."),
            (FeatType.CreepingTerror2, "Creates a visible 5m field within 15m for 30 seconds. Enemies inside are Hobbled and take 14 force DMG plus WIL scaling every 3 seconds."),
            (FeatType.CreepingTerror3, "Creates a visible 8m field within 15m for 30 seconds. Enemies inside are Hobbled and take 18 force DMG plus WIL scaling every 3 seconds.")
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
    public void CreepingTerrorPersistentVfx_IsVisualOnlyTentacleField()
    {
        var root = FindRepositoryRoot();
        var persistentVfx = Read2da(root / "SWLOR_Haks" / "sw_2da" / "vfx_persistent.2da");
        AssertCreepingTerrorTentacleField(
            persistentVfx[(int)AreaOfEffect.CreepingTerrorTentacles],
            "AOE_CREEPING_TERROR_TENTACLES",
            "5");
        AssertCreepingTerrorTentacleField(
            persistentVfx[(int)AreaOfEffect.CreepingTerrorLargeTentacles],
            "AOE_CREEPING_TERROR_TENTACLES_L",
            "8");
    }

    private static void AssertPerkLevel(
        PerkDetail perk,
        string name,
        int level,
        int price,
        int? skillRank,
        FeatType? grantedFeat,
        string description)
    {
        perk.Name.Should().Be(name);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertCharacterRequirement(perkLevel, CharacterType.ForceSensitive);

        if (skillRank.HasValue)
            AssertSkillRequirement(perkLevel, SkillType.Force, skillRank.Value);
        else
            perkLevel.Requirements.OfType<PerkRequirementSkill>().Should().BeEmpty();

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();
    }

    private static void AssertAbility(
        AbilityDetail ability,
        string name,
        int level,
        RecastGroup recastGroup,
        float recastSeconds,
        float activationSeconds,
        int fpCost,
        bool isHostile,
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType,
        float maxRange,
        bool triggersDarkForceConversion)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Force);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(activationType);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.MaxRange.Should().Be(maxRange);
        ability.TriggersDarkForceConversion.Should().Be(triggersDarkForceConversion);
        ability.BreaksStealth.Should().BeTrue();

        ability.Requirements
            .OfType<AbilityRequirementFP>()
            .Should()
            .ContainSingle()
            .Which
            .RequiredFP
            .Should()
            .Be(fpCost);
        ability.Requirements.OfType<AbilityRequirementStamina>().Should().BeEmpty();
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

    private static void AssertCharacterRequirement(PerkLevel level, CharacterType characterType)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementCharacterType>()
            .Should()
            .ContainSingle()
            .Which;

        typeof(PerkRequirementCharacterType)
            .GetField("_requiredCharacterType", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(characterType);
    }

    private static void AssertUniversalForcePower(PerkDetail perk)
    {
        perk.ForceAffinityType.Should().BeNull();
        perk.StatBonuses.Select(x => x.Stat).Should().NotContain(StatType.ForceAffinity);
    }

    private static void AssertCreepingTerrorTentacleField(
        IReadOnlyDictionary<string, string> row,
        string label,
        string radius)
    {
        row["LABEL"].Should().Be(label);
        row["SHAPE"].Should().Be("C");
        row["RADIUS"].Should().Be(radius);
        row["ONENTER"].Should().Be("****");
        row["ONEXIT"].Should().Be("****");
        row["HEARTBEAT"].Should().Be("****");
        row["MODEL01"].Should().Be("vps_tentacle");
        row["MODEL02"].Should().Be("vps_tentacle");
        row["MODEL03"].Should().Be("vps_tentacle");
        row["SoundDuration"].Should().Be("****");
    }

    private static Dictionary<PerkType, PerkDetail> BuildForceDarkManipulatorPerksWithout2daLookup()
    {
        var definition = new ForceDarkManipulatorPerkDefinition();
        var methodNames = new[]
        {
            "CollapseWill",
            "CreepingTerror",
            "EclipseOfResolve",
            "ForceChoke",
            "NightmareField",
            "WeakenResolve"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ForceDarkManipulatorPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ForceDarkManipulatorPerkDefinition)
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
