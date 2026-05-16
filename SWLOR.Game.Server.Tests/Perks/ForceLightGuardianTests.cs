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

public class ForceLightGuardianTests
{
    [Test]
    public void ForceLightGuardianPerkLevels_MatchCombatBible()
    {
        var perks = BuildForceLightGuardianPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.GuardianWard], "Guardian Ward", 1, 2, null, FeatType.GuardianWard1,
            "Grants a single ally temporary HP equal to 6% of the target's maximum HP plus WIL scaling for 30 seconds.");
        AssertPerkLevel(perks[PerkType.ForcePush], "Force Push", 1, 2, 5, FeatType.ForcePush1,
            "Knock down one target for 2 seconds. slows movement for 3 seconds.");
        AssertPerkLevel(perks[PerkType.GuardianWard], "Guardian Ward", 2, 3, 8, FeatType.GuardianWard2,
            "Grants a single ally temporary HP equal to 9% of the target's maximum HP plus WIL scaling for 30 seconds.");
        AssertPerkLevel(perks[PerkType.LightGuardianDeflectivePresence], "Deflective Presence", 1, 3, 12, null,
            "While a one-handed lightsaber or vibroblade is equipped, Light Guardian combat powers increase attack deflection effectiveness by 8% for 10 seconds.",
            StatType.LightGuardianPowerAttackDeflection,
            StatType.LightGuardianPowerAttackDeflectionDurationSeconds);
        AssertPerkLevel(perks[PerkType.ForceLeap], "Force Leap", 1, 3, 15, FeatType.ForceLeap1,
            "Leap to a hostile target up to 15m away, dealing 10 force DMG plus WIL scaling and interrupting activation.");
        AssertPerkLevel(perks[PerkType.SoothingGuard], "Soothing Guard", 1, 3, 18, FeatType.SoothingGuard1,
            "Removes one poison, bleed, burn, shock, or disease effect from an ally and grants 10% damage reduction for 8 seconds.");
        AssertPerkLevel(perks[PerkType.ForcePush], "Force Push", 2, 2, 22, FeatType.ForcePush2,
            "Knock down up to 2 targets in a line for 2 seconds. slows movement for 3 seconds.");
        AssertPerkLevel(perks[PerkType.GuardianWard], "Guardian Ward", 3, 4, 25, FeatType.GuardianWard3,
            "Grants a single ally temporary HP equal to 12% of the target's maximum HP plus WIL scaling for 30 seconds.");
        AssertPerkLevel(perks[PerkType.AuraOfCourage], "Courageous Resolve", 1, 3, 28, FeatType.AuraOfCourage1,
            "Nearby party members take 5% less Force damage and gain +10% resistance to fear, daze, and confusion for 30 seconds.");
        AssertPerkLevel(perks[PerkType.ForceIntercept], "Force Intercept", 1, 4, 30, FeatType.ForceIntercept1,
            "Leap to an ally up to 15m away and absorb 50% of the next hit they take within 8 seconds.");
        AssertPerkLevel(perks[PerkType.ForceLeap], "Force Leap", 2, 3, 35, FeatType.ForceLeap2,
            "Leap to a hostile target up to 18m away, dealing 18 force DMG plus WIL scaling and interrupting activation.");
        AssertPerkLevel(perks[PerkType.ReflectiveBarrier], "Reflective Barrier", 1, 3, 38, FeatType.ReflectiveBarrier1,
            "Grants a single ally a barrier for 20 seconds. While active, 15% of force and energy damage taken, plus WIL scaling, is reflected to the attacker.");
        AssertPerkLevel(perks[PerkType.PurifyingWave], "Purifying Wave", 1, 4, 40, FeatType.PurifyingWave1,
            "Removes one major negative effect from nearby allies and restores HP equal to 8% of each target's maximum HP plus WIL scaling.");
        AssertPerkLevel(perks[PerkType.GuardianWard], "Guardian Ward", 4, 4, 42, FeatType.GuardianWard4,
            "Grants a single ally temporary HP equal to 15% of the target's maximum HP plus WIL scaling for 30 seconds.");
        AssertPerkLevel(perks[PerkType.BastionOfLight], "Bastion of Light", 1, 4, 45, FeatType.BastionOfLight1,
            "Nearby allies gain temporary HP equal to 10% of maximum HP plus WIL scaling and take 10% less force damage for 20 seconds.");
        AssertPerkLevel(perks[PerkType.ForcePush], "Force Push", 3, 3, 48, FeatType.ForcePush3,
            "Knock down up to 3 targets in a cone for 2 seconds. slows movement for 4 seconds.");
        AssertPerkLevel(perks[PerkType.LastStandOfTheLight], "Last Stand of the Light", 1, 5, 50, FeatType.LastStandOfTheLight1,
            "For 12 seconds, damage that would drop the target below 1 HP is prevented once and the target gains temporary HP equal to 20% of maximum HP plus WIL scaling.");

        AssertUniversalForcePower(perks[PerkType.ForcePush]);
        AssertUniversalForcePower(perks[PerkType.ForceLeap]);
    }

    [Test]
    public void ForceLightGuardianAbilities_MatchCombatBible()
    {
        var guardianWard = new GuardianWardAbilityDefinition().BuildAbilities();
        AssertAbility(guardianWard[FeatType.GuardianWard1], "Guardian Ward I", 1, RecastGroup.GuardianWard, 18f, 1f, 2, false, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(guardianWard[FeatType.GuardianWard2], "Guardian Ward II", 2, RecastGroup.GuardianWard, 18f, 1f, 3, false, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(guardianWard[FeatType.GuardianWard3], "Guardian Ward III", 3, RecastGroup.GuardianWard, 18f, 1f, 4, false, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(guardianWard[FeatType.GuardianWard4], "Guardian Ward IV", 4, RecastGroup.GuardianWard, 18f, 1f, 6, false, true, true, false, AbilityActivationType.Casted, 15f);

        var forcePush = new ForcePushAbilityDefinition().BuildAbilities();
        AssertAbility(forcePush[FeatType.ForcePush1], "Force Push I", 1, RecastGroup.ForcePush, 24f, 0f, 2, true, true, true, false, AbilityActivationType.Casted, 8f);
        AssertAbility(forcePush[FeatType.ForcePush2], "Force Push II", 2, RecastGroup.ForcePush, 24f, 0f, 3, true, false, false, true, AbilityActivationType.Casted, 5f);
        AssertAbility(forcePush[FeatType.ForcePush3], "Force Push III", 3, RecastGroup.ForcePush, 24f, 0f, 4, true, false, false, true, AbilityActivationType.Casted, 5f);

        var forceLeap = new ForceLeapAbilityDefinition().BuildAbilities();
        AssertAbility(forceLeap[FeatType.ForceLeap1], "Force Leap I", 1, RecastGroup.ForceLeap, 30f, 0f, 3, true, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(forceLeap[FeatType.ForceLeap2], "Force Leap II", 2, RecastGroup.ForceLeap, 30f, 0f, 4, true, true, true, false, AbilityActivationType.Casted, 18f);

        var soothingGuard = new SoothingGuardAbilityDefinition().BuildAbilities()[FeatType.SoothingGuard1];
        AssertAbility(soothingGuard, "Soothing Guard I", 1, RecastGroup.SoothingGuard, 36f, 1f, 4, false, true, true, false, AbilityActivationType.Casted, 15f);

        var auraOfCourage = new AuraOfCourageAbilityDefinition().BuildAbilities()[FeatType.AuraOfCourage1];
        AssertAbility(auraOfCourage, "Courageous Resolve", 1, RecastGroup.AuraOfCourage, 60f, 1f, 5, false, false, false, true, AbilityActivationType.Casted, 5f);

        var forceIntercept = new ForceInterceptAbilityDefinition().BuildAbilities()[FeatType.ForceIntercept1];
        AssertAbility(forceIntercept, "Force Intercept", 1, RecastGroup.ForceIntercept, 45f, 0f, 5, false, true, true, false, AbilityActivationType.Casted, 15f);

        var reflectiveBarrier = new ReflectiveBarrierAbilityDefinition().BuildAbilities()[FeatType.ReflectiveBarrier1];
        AssertAbility(reflectiveBarrier, "Reflective Barrier", 1, RecastGroup.ReflectiveBarrier, 75f, 1f, 5, false, true, true, false, AbilityActivationType.Casted, 15f);

        var purifyingWave = new PurifyingWaveAbilityDefinition().BuildAbilities()[FeatType.PurifyingWave1];
        AssertAbility(purifyingWave, "Purifying Wave", 1, RecastGroup.PurifyingWave, 90f, 1.5f, 7, false, false, false, true, AbilityActivationType.Casted, 5f);

        var bastionOfLight = new BastionOfLightAbilityDefinition().BuildAbilities()[FeatType.BastionOfLight1];
        AssertAbility(bastionOfLight, "Bastion of Light", 1, RecastGroup.BastionOfLight, 120f, 1.5f, 8, false, false, false, true, AbilityActivationType.Casted, 5f);

        var lastStand = new LastStandOfTheLightAbilityDefinition().BuildAbilities()[FeatType.LastStandOfTheLight1];
        AssertAbility(lastStand, "Last Stand of the Light", 1, RecastGroup.LastStandOfTheLight, 300f, 1.5f, 10, false, true, true, false, AbilityActivationType.Casted, 15f);
    }

    [Test]
    public void ForceLightGuardianStatusEffects_MatchCombatBible()
    {
        var soothingGuard = new SoothingGuard1StatusEffect();
        soothingGuard.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(-10);
        soothingGuard.StatGroup.Stats[StatType.TraumaResistance].Should().Be(0);

        var aura = new AuraOfCourage1StatusEffect();
        aura.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-5);
        aura.StatGroup.Stats[StatType.MindResistance].Should().Be(10);

        var intercept = new ForceIntercept1StatusEffect();
        intercept.StatGroup.Stats[StatType.DamageTakenPercentAdjustment].Should().Be(-50);

        var bastion = new BastionOfLight1StatusEffect();
        bastion.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-10);
    }

    [Test]
    public void ForceLightGuardianFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.GuardianWard1, "ife_guardwrd1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForcePush1, "ife_forcepsh1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.GuardianWard2, "ife_guardwrd2", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForceLeap1, "ife_forcelp1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SoothingGuard1, "ife_sthnggrd1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForcePush2, "ife_forcepsh2", "M", "0x3E", "1", "rectangle", "2.5", "8", "17"),
            (FeatType.GuardianWard3, "ife_guardwrd3", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.AuraOfCourage1, "ife_rcrg1", "P", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.ForceIntercept1, "ife_forceintc1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForceLeap2, "ife_forcelp2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ReflectiveBarrier1, "ife_rflctvbrrr1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.PurifyingWave1, "ife_prfyngwv1", "P", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.GuardianWard4, "ife_guardwrd4", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.BastionOfLight1, "ife_bastlght1", "P", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.ForcePush3, "ife_forcepsh3", "M", "0x3E", "1", "cone", "6", "5", "17"),
            (FeatType.LastStandOfTheLight1, "ife_laststndlgh1", "M", "0x03", "0", "****", "****", "****", "****")
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
        string description,
        params StatType[] statTypes)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.ForceLight);

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
        int fpCost,
        bool isHostile,
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType,
        float maxRange)
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

    private static Dictionary<PerkType, PerkDetail> BuildForceLightGuardianPerksWithout2daLookup()
    {
        var definition = new ForceLightGuardianPerkDefinition();
        var methodNames = new[]
        {
            "AuraOfCourage",
            "BastionOfLight",
            "DeflectivePresence",
            "ForceIntercept",
            "ForceLeap",
            "ForcePush",
            "GuardianWard",
            "LastStandOfTheLight",
            "PurifyingWave",
            "ReflectiveBarrier",
            "SoothingGuard"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ForceLightGuardianPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ForceLightGuardianPerkDefinition)
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
