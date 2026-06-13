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

        var forceIntercept = new ForceInterceptAbilityDefinition().BuildAbilities()[FeatType.ForceIntercept1];
        AssertAbility(forceIntercept, "Force Intercept", 1, RecastGroup.ForceIntercept, 45f, 0f, 5, false, true, true, false, AbilityActivationType.Casted, 15f);

        var purifyingWave = new PurifyingWaveAbilityDefinition().BuildAbilities()[FeatType.PurifyingWave1];
        AssertAbility(purifyingWave, "Purifying Wave", 1, RecastGroup.PurifyingWave, 90f, 1.5f, 7, false, false, false, true, AbilityActivationType.Casted, 5f);

        var lastStand = new LastStandOfTheLightAbilityDefinition().BuildAbilities()[FeatType.LastStandOfTheLight1];
        AssertAbility(lastStand, "Last Stand of the Light", 1, RecastGroup.Capstone, 345f, 1.5f, 10, false, true, true, false, AbilityActivationType.Casted, 15f);
    }

    [Test]
    public void ForceLightGuardianStatusEffects_MatchCombatBible()
    {
        var aura = new CourageousResolve1StatusEffect();
        aura.ApplyEffect(0, 0, 12);
        aura.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(0);
        aura.StatGroup.Stats[StatType.MindResistance].Should().Be(10);

        var intercept = new ForceIntercept1StatusEffect();
        intercept.StatGroup.Stats[StatType.DamageTakenRedirectToStatusSourcePercent].Should().Be(50);

        var lastStand = new LastStandOfTheLight1StatusEffect();
        lastStand.ApplyEffect(0, 0, 45);
        lastStand.StatGroup.Stats[StatType.FatalDamageTemporaryHPPercent].Should().Be(15);
        lastStand.StatGroup.Stats[StatType.FatalDamageTemporaryHPDurationSeconds].Should().Be(45);

        var root = FindRepositoryRoot();
        var knockdown = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "KnockdownStatusEffect.cs").FullName);
        knockdown.Should().Contain("StatusEffect.HasStatusEffect(creature, GetType())");
        knockdown.Should().Contain("Ability.HasTemporaryImmunity(creature, ImmunityType.Knockdown)");
        knockdown.Should().Contain("ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);");
        knockdown.Should().Contain("protected override void Remove(uint creature)");
        knockdown.Should().Contain("Ability.ApplyTemporaryImmunity(creature, 0f, ImmunityType.Knockdown);");
    }

    [Test]
    public void ForceLightGuardianTraitStatValues_MatchCombatBible()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "ForceLightGuardianPerkDefinition.cs").FullName);

        source.Should().Contain("EquipmentPredicates.HasMainHandLightsaber(creature) || EquipmentPredicates.HasMainHandVibroblade(creature) ? 4 : 0");
        source.Should().Contain("EquipmentPredicates.HasMainHandLightsaber(creature) || EquipmentPredicates.HasMainHandVibroblade(creature) ? 10 : 0");
    }

    [Test]
    public void LastStandOfTheLight_HasDyingFallbackBeforeForcedPlayerDeath()
    {
        var root = FindRepositoryRoot();
        var combat = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var death = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Death.cs").FullName);

        combat.Should().Contain("public static bool TryPreventFatalDamageAndGrantTemporaryHP(");
        combat.Should().Contain("var isDyingFallback = restoreToOneHP && currentHP <= 0;");
        combat.Should().Contain("SetCurrentHitPoints(defender, 1);");

        death.Should().Contain("if (Combat.TryPreventFatalDamageAndGrantTemporaryHP(player, 0, restoreToOneHP: true))");
        death.Should().Contain("return;");
        death.Should().Contain("ApplyEffectToObject(DurationType.Instant, EffectDeath(), player);");
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
            (FeatType.ForcePush2, "ife_forcepsh2", "M", "0x3E", "1", "rectangle", "8", "2.5", "17"),
            (FeatType.GuardianWard3, "ife_guardwrd3", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForceIntercept1, "ife_forceintc1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForceLeap2, "ife_forcelp2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.PurifyingWave1, "ife_prfyngwv1", "P", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.GuardianWard4, "ife_guardwrd4", "M", "0x03", "0", "****", "****", "****", "****"),
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
            "CourageousResolve",
            "DeflectivePresence",
            "ForceIntercept",
            "ForceLeap",
            "ForcePush",
            "GuardianWard",
            "LastStandOfTheLight",
            "PurifyingWave",
            "ReflectiveBarrier"
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
