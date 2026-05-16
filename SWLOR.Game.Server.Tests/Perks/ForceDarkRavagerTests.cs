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
            "Deals 18 force DMG plus WIL scaling to one target and reduce evasion chance by 4% for 20 seconds.");
        AssertPerkLevel(perks[PerkType.ForceBody], "Force Body", 1, 2, 5, FeatType.ForceBody1,
            "For 30 seconds, your damaging Dark powers restore 1 FP, but each cast costs HP equal to 2% of your maximum HP.");
        AssertPerkLevel(perks[PerkType.ForceLightning], "Force Lightning", 1, 3, 8, FeatType.ForceLightning1,
            "Deals 14 force DMG plus WIL scaling to up to 3 targets with an electrical visual.");
        AssertPerkLevel(perks[PerkType.ForceDrain], "Force Drain", 1, 3, 12, FeatType.ForceDrain1,
            "Deals 16 force DMG plus WIL scaling and heals you for 35% of damage dealt.");
        AssertPerkLevel(perks[PerkType.SaberRend], "Saber Rend", 1, 3, 15, FeatType.SaberRend1,
            "Your next melee attack deals +12 force DMG plus WIL scaling. Requires a melee weapon.");
        AssertPerkLevel(perks[PerkType.ForceRage], "Force Rage", 1, 3, 18, FeatType.ForceRage1,
            "Increases outgoing weapon and force damage by 8% and critical damage by 10% for 20 seconds, but increases damage taken by 5%.");
        AssertPerkLevel(perks[PerkType.ForceSpark], "Force Spark", 2, 2, 22, FeatType.ForceSpark2,
            "Deals 32 force DMG plus WIL scaling to one target and reduce evasion chance by 6% for 20 seconds.");
        AssertPerkLevel(perks[PerkType.ForceLightning], "Force Lightning", 2, 4, 25, FeatType.ForceLightning2,
            "Deals 24 force DMG plus WIL scaling to up to 4 targets with an electrical visual.");
        AssertPerkLevel(perks[PerkType.ForceDrain], "Force Drain", 2, 3, 28, FeatType.ForceDrain2,
            "Deals 28 force DMG plus WIL scaling and heals you for 40% of damage dealt.");
        AssertPerkLevel(perks[PerkType.DevouringStrike], "Devouring Strike", 1, 4, 30, FeatType.DevouringStrike1,
            "Deals 12 force DMG plus WIL scaling to one target. If the target is below 35% HP, damage is increased by 40%.");
        AssertPerkLevel(perks[PerkType.SaberRend], "Saber Rend", 2, 3, 35, FeatType.SaberRend2,
            "Your next melee attack deals +24 force DMG plus WIL scaling. Requires a melee weapon.");
        AssertPerkLevel(perks[PerkType.ForceBody], "Force Body", 2, 3, 38, FeatType.ForceBody2,
            "For 30 seconds, damaging Dark powers restore FP. Each cast costs HP, reduced when you damage a target below 50% HP.");
        AssertPerkLevel(perks[PerkType.ForceMaelstrom], "Force Maelstrom", 1, 4, 40, FeatType.ForceMaelstrom1,
            "Deals 10 force DMG plus WIL scaling to nearby enemies and pulls them slightly toward you.");
        AssertPerkLevel(perks[PerkType.ForceDrain], "Force Drain", 3, 4, 42, FeatType.ForceDrain3,
            "Deals 44 force DMG plus WIL scaling and heals you for 45% of damage dealt.");
        AssertPerkLevel(perks[PerkType.ForceRage], "Force Rage", 2, 4, 45, FeatType.ForceRage2,
            "Increases outgoing weapon and force damage by 14% and critical damage by 15% for 20 seconds, but increases damage taken by 8%.");
        AssertPerkLevel(perks[PerkType.ForceSpark], "Force Spark", 3, 3, 48, FeatType.ForceSpark3,
            "Deals 50 force DMG plus WIL scaling to one target and reduce evasion chance by 8% for 20 seconds.");
        AssertPerkLevel(perks[PerkType.HungerOfTheDark], "Hunger of the Dark", 1, 5, 50, FeatType.HungerOfTheDark1,
            "For 12 seconds, Dark damage you deal heals you for 25% of damage dealt and defeated enemies restore FP.");

        AssertUniversalForcePower(perks[PerkType.SaberRend]);
    }

    [Test]
    public void ForceDarkRavagerAbilities_MatchCombatBible()
    {
        var forceSpark = new ForceSparkAbilityDefinition().BuildAbilities();
        AssertAbility(forceSpark[FeatType.ForceSpark1], "Force Spark I", 1, RecastGroup.ForceSpark, 6f, 1f, 3, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceSpark[FeatType.ForceSpark2], "Force Spark II", 2, RecastGroup.ForceSpark, 6f, 1f, 4, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceSpark[FeatType.ForceSpark3], "Force Spark III", 3, RecastGroup.ForceSpark, 6f, 1f, 6, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);

        var forceBody = new ForceBodyAbilityDefinition().BuildAbilities();
        AssertAbility(forceBody[FeatType.ForceBody1], "Force Body I", 1, RecastGroup.ForceBody, 180f, 0f, 2, null, false, false, true, false, AbilityActivationType.Casted, 5f, false);
        AssertAbility(forceBody[FeatType.ForceBody2], "Force Body II", 2, RecastGroup.ForceBody, 180f, 0f, 4, null, false, false, true, false, AbilityActivationType.Casted, 5f, false);

        var forceLightning = new ForceLightningAbilityDefinition().BuildAbilities();
        AssertAbility(forceLightning[FeatType.ForceLightning1], "Force Lightning I", 1, RecastGroup.ForceLightning, 24f, 1.5f, 4, null, true, true, false, true, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceLightning[FeatType.ForceLightning2], "Force Lightning II", 2, RecastGroup.ForceLightning, 24f, 1.5f, 6, null, true, true, false, true, AbilityActivationType.Casted, 15f, true);

        var forceDrain = new ForceDrainAbilityDefinition().BuildAbilities();
        AssertAbility(forceDrain[FeatType.ForceDrain1], "Force Drain I", 1, RecastGroup.ForceDrain, 18f, 1f, 4, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceDrain[FeatType.ForceDrain2], "Force Drain II", 2, RecastGroup.ForceDrain, 18f, 1f, 6, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);
        AssertAbility(forceDrain[FeatType.ForceDrain3], "Force Drain III", 3, RecastGroup.ForceDrain, 18f, 1f, 8, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);

        var saberRend = new SaberRendAbilityDefinition().BuildAbilities();
        AssertAbility(saberRend[FeatType.SaberRend1], "Saber Rend I", 1, RecastGroup.SaberRend, 18f, 0f, 3, 1, true, false, true, false, AbilityActivationType.Weapon, 5f, false);
        AssertAbility(saberRend[FeatType.SaberRend2], "Saber Rend II", 2, RecastGroup.SaberRend, 18f, 0f, 4, 2, true, false, true, false, AbilityActivationType.Weapon, 5f, false);

        var forceRage = new ForceRageAbilityDefinition().BuildAbilities();
        AssertAbility(forceRage[FeatType.ForceRage1], "Force Rage I", 1, RecastGroup.ForceRage, 60f, 0f, 5, null, false, false, true, false, AbilityActivationType.Casted, 5f, false);
        AssertAbility(forceRage[FeatType.ForceRage2], "Force Rage II", 2, RecastGroup.ForceRage, 60f, 0f, 8, null, false, false, true, false, AbilityActivationType.Casted, 5f, false);

        var devouringStrike = new DevouringStrikeAbilityDefinition().BuildAbilities()[FeatType.DevouringStrike1];
        AssertAbility(devouringStrike, "Devouring Strike", 1, RecastGroup.DevouringStrike, 30f, 1f, 7, null, true, true, true, false, AbilityActivationType.Casted, 15f, true);

        var forceMaelstrom = new ForceMaelstromAbilityDefinition().BuildAbilities()[FeatType.ForceMaelstrom1];
        AssertAbility(forceMaelstrom, "Force Maelstrom", 1, RecastGroup.ForceMaelstrom, 75f, 1.5f, 8, null, true, false, false, true, AbilityActivationType.Casted, 5f, true);

        var hunger = new HungerOfTheDarkAbilityDefinition().BuildAbilities()[FeatType.HungerOfTheDark1];
        AssertAbility(hunger, "Hunger of the Dark", 1, RecastGroup.HungerOfTheDark, 180f, 0f, 10, null, false, false, true, false, AbilityActivationType.Casted, 5f, false);
    }

    [Test]
    public void ForceDarkRavagerStatusEffects_MatchCombatBible()
    {
        new ForceSpark1StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-4);
        new ForceSpark2StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-6);
        new ForceSpark3StatusEffect().StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-8);

        var forceBody1 = new ForceBody1StatusEffect();
        forceBody1.StatGroup.Stats[StatType.DarkForceDamageFPRestore].Should().Be(1);
        forceBody1.StatGroup.Stats[StatType.DarkForceDamageHPCostPercent].Should().Be(2);

        var forceBody2 = new ForceBody2StatusEffect();
        forceBody2.StatGroup.Stats[StatType.DarkForceDamageFPRestore].Should().Be(1);
        forceBody2.StatGroup.Stats[StatType.DarkForceDamageHPCostPercent].Should().Be(2);
        forceBody2.StatGroup.Stats[StatType.DarkForceDamageLowTargetHPCostPercent].Should().Be(1);
        forceBody2.StatGroup.Stats[StatType.DarkForceDamageLowTargetHPThresholdPercent].Should().Be(50);

        var forceRage1 = new ForceRage1StatusEffect();
        forceRage1.StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(8);
        forceRage1.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        forceRage1.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);
        forceRage1.StatGroup.Stats[StatType.CriticalDamagePercentAdjustment].Should().Be(10);
        forceRage1.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(5);

        var forceRage2 = new ForceRage2StatusEffect();
        forceRage2.StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(14);
        forceRage2.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(0);
        forceRage2.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);
        forceRage2.StatGroup.Stats[StatType.CriticalDamagePercentAdjustment].Should().Be(15);
        forceRage2.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(8);

        var hunger = new HungerOfTheDark1StatusEffect();
        hunger.StatGroup.Stats[StatType.DarkForceDamageHPPercentRestore].Should().Be(25);
        hunger.StatGroup.Stats[StatType.DefeatedEnemyFPRestore].Should().BeGreaterThan(0);
    }

    [Test]
    public void ForceMaelstrom_SourceIncludesBiblePull()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceMaelstromAbilityDefinition.cs").FullName);

        source.Should().Contain("afterSuccessfulHit");
        source.Should().Contain("PullTowardActivator");
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
        forceLightning.Should().NotContain("damageType: CombatDamageType.Electrical");
        forceLightning.Should().Contain("VisualEffect.Vfx_Com_Hit_Electrical");
    }

    [Test]
    public void ForceDarkRavagerFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.ForceSpark1, "ife_forcesprk1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceBody1, "ife_forcebdy1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ForceLightning1, "ife_forcelghtnn1", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ForceDrain1, "ife_forcedrn1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SaberRend1, "ife_sabrrnd1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ForceRage1, "ife_forcerg1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ForceSpark2, "ife_forcesprk2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceLightning2, "ife_forcelghtnn2", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ForceDrain2, "ife_forcedrn2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DevouringStrike1, "ife_dvrngstrk1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SaberRend2, "ife_sabrrnd2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ForceBody2, "ife_forcebdy2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ForceMaelstrom1, "ife_forcemael1", "P", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.ForceDrain3, "ife_forcedrn3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceRage2, "ife_forcerg2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.ForceSpark3, "ife_forcesprk3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.HungerOfTheDark1, "ife_hngrdrk1", "P", "0x01", "0", "****", "****", "****", "****")
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
        int? skillRank,
        FeatType? grantedFeat,
        string description)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.ForceDark);

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
            "ForceBody",
            "ForceDrain",
            "ForceLightning",
            "ForceMaelstrom",
            "ForceRage",
            "ForceSpark",
            "HungerOfTheDark",
            "SaberRend"
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
