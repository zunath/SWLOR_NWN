using System.Reflection;
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

public class ForceDarkRavagerTests
{
    [Test]
    public void ForceDarkRavagerPerkLevels_MatchCombatBible()
    {
        var perks = BuildForceDarkRavagerPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.ForceSpark], "Force Spark", 1, 2, null, FeatType.ForceSpark1,
            "Deals 16 force DMG plus WIL scaling to one target and reduces Evasion by 4% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.ForceLightning], "Force Lightning", 1, 3, 10, FeatType.ForceLightning1,
            "Deals 10 force DMG plus WIL scaling to one target, then arcs to up to two enemies within 5m for 50% damage. Affected targets suffer Shock for 30 seconds.");
        AssertPerkLevel(perks[PerkType.ForceDrain], "Force Drain", 1, 3, 12, FeatType.ForceDrain1,
            "Deals 14 force DMG plus WIL scaling to one target and heals you for 30% of damage dealt. If the target is below 50% HP, healing increases to 40%.");
        AssertPerkLevel(perks[PerkType.FuryStance], "Fury Stance", 1, 3, 12, FeatType.FuryStance1,
            "While active, gain +8% weapon and force damage and +10% critical damage, but take 5% more damage and suffer -5% Defense and Force Defense. Only one stance may be active.");
        AssertPerkLevel(perks[PerkType.ForceSpark], "Force Spark", 2, 3, 18, FeatType.ForceSpark2,
            "Deals 30 force DMG plus WIL scaling to one target and reduces Evasion by 6% for 30 seconds.");
        perks[PerkType.ForceSpark].PerkLevels.Should().NotContainKey(3);
        perks[PerkType.ForceSpark].PerkLevels.Values
            .SelectMany(level => level.GrantedFeats)
            .Should()
            .NotContain(FeatType.ForceSpark3);
        AssertPerkLevel(perks[PerkType.ForceLightning], "Force Lightning", 2, 4, 22, FeatType.ForceLightning2,
            "Deals 18 force DMG plus WIL scaling to one target, then arcs to up to three enemies within 5m for 50% damage. Affected targets suffer Shock for 30 seconds.");
        AssertPerkLevel(perks[PerkType.ForceLightning], "Force Lightning", 3, 4, 42, FeatType.ForceLightning3,
            "Deals 40 force DMG plus WIL scaling to one target, then arcs to up to three enemies within 5m for 50% damage. Affected targets suffer Shock for 30 seconds.");
        AssertPerkLevel(perks[PerkType.ForceDrain], "Force Drain", 2, 3, 25, FeatType.ForceDrain2,
            "Deals 24 force DMG plus WIL scaling to one target and heals you for 35% of damage dealt. If the target is below 50% HP, healing increases to 45%.");
        AssertPerkLevel(perks[PerkType.DevouringStrike], "Devouring Strike", 1, 4, 28, FeatType.DevouringStrikeTrait,
            "Alter powers that damage enemies deal 15% more damage to targets below 35% HP.");
        AssertPerkLevel(perks[PerkType.CruelMomentum], "Cruel Momentum", 1, 4, 28, FeatType.CruelMomentumTrait,
            "When an enemy you damaged within the last 6 seconds is defeated, restore 2 FP and gain +5% Force ability Accuracy for 30 seconds. This can trigger once every 10 seconds.");
        AssertPerkLevel(perks[PerkType.UnstablePressure], "Unstable Pressure", 1, 4, 32, FeatType.UnstablePressureTrait,
            "Force Spark and Force Lightning mark affected enemies with unstable pressure for 30 seconds, reducing Evasion by 5%. Enemies below 35% HP also suffer +5% force damage taken while marked.");
        AssertPerkLevel(perks[PerkType.ForceDrain], "Force Drain", 3, 4, 38, FeatType.ForceDrain3,
            "Deals 36 force DMG plus WIL scaling to one target and heals you for 40% of damage dealt. If the target is below 50% HP, healing increases to 50%.");
        AssertPerkLevel(perks[PerkType.FuryStance], "Fury Stance", 2, 4, 42, FeatType.FuryStance2,
            "While active, gain +12% weapon and force damage and +15% critical damage, but take 5% more damage and suffer -5% Defense and Force Defense. Only one stance may be active.");
        AssertPerkLevel(perks[PerkType.HungerOfTheDark], "Hunger of the Dark", 1, 5, 50, FeatType.HungerOfTheDark1,
            "For 45 seconds, Dark damage you deal heals you for 12% of damage dealt and defeated enemies restore 3 FP.");

        perks[PerkType.DevouringStrike].PerkLevels[1].StatBonuses.Select(x => x.Stat).Should().Contain(new[]
        {
            StatType.DarkForceTargetLowHPDamageThresholdPercent,
            StatType.DarkForceTargetLowHPDamagePercentAdjustment
        });
    }

    [Test]
    public void ForceDarkRavagerAbilities_MatchCombatBible()
    {
        var forceSpark = new ForceSparkAbilityDefinition().BuildAbilities();
        AssertAbility(forceSpark[FeatType.ForceSpark1], "Force Spark I", 1, RecastGroup.ForceSpark, 6f, 1f, 3, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceSpark[FeatType.ForceSpark2], "Force Spark II", 2, RecastGroup.ForceSpark, 6f, 1f, 4, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        forceSpark.Should().NotContainKey(FeatType.ForceSpark3);

        var forceLightning = new ForceLightningAbilityDefinition().BuildAbilities();
        AssertAbility(forceLightning[FeatType.ForceLightning1], "Force Lightning I", 1, RecastGroup.ForceLightning, 15f, 1.5f, 4, null, true, true, false, true, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceLightning[FeatType.ForceLightning2], "Force Lightning II", 2, RecastGroup.ForceLightning, 15f, 1.5f, 6, null, true, true, false, true, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceLightning[FeatType.ForceLightning3], "Force Lightning III", 3, RecastGroup.ForceLightning, 15f, 1.5f, 8, null, true, true, false, true, AbilityActivationType.Casted, 15f, true);

        var forceDrain = new ForceDrainAbilityDefinition().BuildAbilities();
        AssertAbility(forceDrain[FeatType.ForceDrain1], "Force Drain I", 1, RecastGroup.ForceDrain, 12f, 1f, 4, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceDrain[FeatType.ForceDrain2], "Force Drain II", 2, RecastGroup.ForceDrain, 12f, 1f, 6, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceDrain[FeatType.ForceDrain3], "Force Drain III", 3, RecastGroup.ForceDrain, 12f, 1f, 8, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);

        var furyStance = new FuryStanceAbilityDefinition().BuildAbilities();
        AssertAbility(furyStance[FeatType.FuryStance1], "Fury Stance I", 1, RecastGroup.FuryStance, 30f, 2f, 5, null, false, false, false, false, AbilityActivationType.Casted, 5f, false);
        AssertAbility(furyStance[FeatType.FuryStance2], "Fury Stance II", 2, RecastGroup.FuryStance, 30f, 2f, 8, null, false, false, false, false, AbilityActivationType.Casted, 5f, false);

        var hunger = new HungerOfTheDarkAbilityDefinition().BuildAbilities()[FeatType.HungerOfTheDark1];
        AssertAbility(hunger, "Hunger of the Dark", 1, RecastGroup.Capstone, 90f, 0f, 10, null, false, false, true, false, AbilityActivationType.Casted, 5f, false);
    }

    [Test]
    public void ForceDarkRavagerStatusEffects_MatchCombatBible()
    {
        new ForceSpark1StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-4);
        new ForceSpark2StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-6);
        new ForceSpark3StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-8);

        var furyStance1 = new FuryStance1StatusEffect();
        furyStance1.StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(8);
        furyStance1.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        furyStance1.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);
        furyStance1.StatGroup.Stats[StatType.CriticalDamagePercentAdjustment].Should().Be(10);
        furyStance1.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(5);
        furyStance1.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-5);
        furyStance1.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-5);

        var furyStance2 = new FuryStance2StatusEffect();
        furyStance2.StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(12);
        furyStance2.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        furyStance2.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);
        furyStance2.StatGroup.Stats[StatType.CriticalDamagePercentAdjustment].Should().Be(15);
        furyStance2.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(5);
        furyStance2.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-5);
        furyStance2.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-5);

        var unstablePressure = new UnstablePressureStatusEffect();
        unstablePressure.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-5);

        var cruelMomentum = new CruelMomentumStatusEffect();
        cruelMomentum.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustmentSkillType].Should().Be((int)SkillType.Force);
        cruelMomentum.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(5);

        var hunger = new HungerOfTheDark1StatusEffect();
        hunger.StatGroup.Stats[StatType.DarkForceDamageHPPercentRestore].Should().Be(12);
        hunger.StatGroup.Stats[StatType.DefeatedEnemyFPRestore].Should().Be(3);
    }

    [Test]
    public void ForceDarkRavagerSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();

        var ability = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Ability.cs").FullName);
        ability.Should().Contain("ApplyDarkForceCastConversion(activator, target)");
        ability.Should().Contain("ApplyDarkForceDamageRestoration(activator, damage)");

        var forceDamageOverTime = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "ForceDamageOverTimeStatusEffectBase.cs").FullName);
        forceDamageOverTime.Should().Contain("Ability.ApplyDarkForceDamageRestoration(Source, damage)");

        var forceLightning = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceLightningAbilityDefinition.cs").FullName);
        forceLightning.Should().Contain("damageType: CombatDamageType.Force");
        forceLightning.Should().Contain("effectDamageType: DamageType.Electrical");
        forceLightning.Should().NotContain("damageType: CombatDamageType.Electrical");
        forceLightning.Should().Contain("VisualEffect.Vfx_Beam_Silent_Lightning");
        forceLightning.Should().Contain("VisualEffect.Vfx_Imp_Lightning_S");
        forceLightning.Should().NotContain("EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Hand, true");
        forceLightning.Should().NotContain("VisualEffect.Vfx_Com_Hit_Electrical");

        var forceSpark = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceSparkAbilityDefinition.cs").FullName);
        forceSpark.Should().Contain(".PlaysSoundOnImpact(\"ksfx_frc_lightn\")");
        forceSpark.Should().Contain("VisualEffect.Vfx_Imp_Mirv_Electric");
        forceSpark.Should().NotContain("ksfx_use_force");
        forceSpark.Should().NotContain("VisualEffect.Vfx_Imp_Pulse_Negative");

        var forceDrain = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceDrainAbilityDefinition.cs").FullName);
        forceDrain.Should().Contain("VisualEffect.Vfx_Beam_Drain");
        forceDrain.Should().Contain("VisualEffect.Vfx_Dur_Aura_Pulse_Red_Black");
        forceDrain.Should().NotContain("EffectBeam(VisualEffect.Vfx_Beam_Drain, activator, BodyNode.Hand, true");
        forceDrain.Should().NotContain("VisualEffect.Vfx_Imp_Pulse_Negative");
        forceDrain.Should().NotContain("VisualEffect.Vfx_Imp_Evil_Help");
    }

    [Test]
    public void ForceDarkRavagerFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.ForceSpark1, "ife_forcespark1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceLightning1, "ife_forcezap1", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ForceDrain1, "ife_forcedrain1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FuryStance1, "ife_furystance1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ForceSpark2, "ife_forcespark2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceLightning2, "ife_forcezap2", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ForceLightning3, "ife_fzap3", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ForceDrain2, "ife_forcedrain2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceDrain3, "ife_forcedrain3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FuryStance2, "ife_furystance2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.HungerOfTheDark1, "ife_hungerdark1", "P", "0x01", "0", "****", "****", "****", "****")
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
        perk.ForceAffinityType.Should().Be(ForceAffinityType.Dark);
        perk.StatBonuses.Should().ContainSingle(x =>
            x.Stat == StatType.ForceAffinity &&
            x.Calculate(0) == (int)ForceAffinityType.Dark);

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
        int? staminaCost,
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

    private static Dictionary<PerkType, PerkDetail> BuildForceDarkRavagerPerksWithout2daLookup()
    {
        var definition = new ForceDarkRavagerPerkDefinition();
        var methodNames = new[]
        {
            "DevouringStrike",
            "ForceDrain",
            "ForceLightning",
            "ForceSpark",
            "FuryStance",
            "HungerOfTheDark",
            "CruelMomentum",
            "UnstablePressure"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ForceDarkRavagerPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ForceDarkRavagerPerkDefinition)
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
