using System.Reflection;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ForceUniversalTests
{
    [Test]
    public void ForceUniversalPerkLevels_MatchCombatBible()
    {
        var perks = BuildForceUniversalPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.ForcePush], "Force Push", 1, 2, 5, FeatType.ForcePush1,
            "Deals 8 force DMG to one target in a 5m x 5m cone, knocks it down for 6 seconds, and slows its movement for 12 seconds.");
        AssertPerkLevel(perks[PerkType.ForcePush], "Force Push", 2, 3, 28, FeatType.ForcePush2,
            "Deals 12 force DMG to up to 2 targets in an 8m x 5m cone, knocks them down for 6 seconds, and slows their movement for 12 seconds.");
        AssertPerkLevel(perks[PerkType.ForcePush], "Force Push", 3, 4, 48, FeatType.ForcePush3,
            "Deals 18 force DMG to up to 3 targets in a 10m x 5m cone, knocks them down for 6 seconds, and slows their movement for 12 seconds.");

        AssertPerkLevel(perks[PerkType.ForceLeap], "Force Leap", 1, 3, 10, FeatType.ForceLeap1,
            "Leap to a hostile target up to 15m away, dealing 10 force DMG plus WIL scaling and interrupting activation.");
        AssertPerkLevel(perks[PerkType.ForceLeap], "Force Leap", 2, 4, 30, FeatType.ForceLeap2,
            "Leap to a hostile target up to 18m away, dealing 18 force DMG plus WIL scaling and interrupting activation.");

        AssertUniversalForcePower(perks[PerkType.ForcePush]);
        AssertUniversalForcePower(perks[PerkType.ForceLeap]);
        perks.Keys.Should().NotContain(PerkType.FuryStance);
    }

    [Test]
    public void ForceUniversalAbilities_MatchCombatBible()
    {
        var forcePush = new ForcePushAbilityDefinition().BuildAbilities();
        AssertAbility(forcePush[FeatType.ForcePush1], "Force Push I", 1, RecastGroup.ForcePush, 45f, 0f, 2, true, false, false, true, AbilityActivationType.Casted, 5f);
        AssertAbility(forcePush[FeatType.ForcePush2], "Force Push II", 2, RecastGroup.ForcePush, 45f, 0f, 3, true, false, false, true, AbilityActivationType.Casted, 5f);
        AssertAbility(forcePush[FeatType.ForcePush3], "Force Push III", 3, RecastGroup.ForcePush, 45f, 0f, 4, true, false, false, true, AbilityActivationType.Casted, 5f);

        var forceLeap = new ForceLeapAbilityDefinition().BuildAbilities();
        AssertAbility(forceLeap[FeatType.ForceLeap1], "Force Leap I", 1, RecastGroup.ForceLeap, 18f, 0f, 3, true, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(forceLeap[FeatType.ForceLeap2], "Force Leap II", 2, RecastGroup.ForceLeap, 18f, 0f, 4, true, true, true, false, AbilityActivationType.Casted, 18f);
    }

    [Test]
    public void ForcePush_UsesScalingConeTargeting()
    {
        var forcePush = new ForcePushAbilityDefinition().BuildAbilities();

        AssertForcePushConeTargeting(forcePush[FeatType.ForcePush1], Spell.ForcePush1, 5f);
        AssertForcePushConeTargeting(forcePush[FeatType.ForcePush2], Spell.ForcePush2, 8f);
        AssertForcePushConeTargeting(forcePush[FeatType.ForcePush3], Spell.ForcePush3, 10f);
    }

    [Test]
    public void ForcePush_UsesOnlyKotorImpactSfx()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForcePushAbilityDefinition.cs").FullName);

        source.Should().Contain(".DisplaysVisualEffectWhenActivating(VisualEffect.None)");
        source.Should().Contain(".PlaysSoundOnImpact(\"ksfx_frc_push\")");
        source.Should().Contain(".PlaysSoundOnImpact(\"ksfx_frc_wave\")");
        source.Should().Contain("targetVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst_Silent");
        source.Should().NotContain(".DisplaysVisualEffectWhenActivating()");
        source.Should().NotContain("VisualEffect.Vfx_Imp_Pulse_Negative");
    }

    [Test]
    public void ThrowLightsaber_UsesLegacySaberThrowAnimation()
    {
        var abilities = new ThrowLightsaberAbilityDefinition().BuildAbilities();

        abilities[FeatType.ThrowLightsaber1].ImpactAnimationType.Should().Be(Animation.SaberThrow);
        abilities[FeatType.ThrowLightsaber2].ImpactAnimationType.Should().Be(Animation.SaberThrow);
        abilities[FeatType.ThrowLightsaber3].ImpactAnimationType.Should().Be(Animation.SaberThrow);

        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ThrowLightsaberAbilityDefinition.cs").FullName);

        source.Should().Contain("ActionPlayAnimation(Animation.SaberThrow, 2)");
        source.Should().NotContain("Animation.CastOutAnimation");
    }

    [Test]
    public void ThrowLightsaber_PathProjectionIncludesStationaryTargetAtFloatingPointEndpoint()
    {
        var origin = new Vector3(394.638855f, -247.003616f, 5.14350939f);
        var destination = new Vector3(389.8544f, -245.746658f, 4.644441f);

        InvokeThrowLightsaberPathCheck(origin, destination, destination).Should().BeTrue();
    }

    [Test]
    public void ThrowLightsaber_PathProjectionRejectsTargetsOutsideLineBounds()
    {
        var origin = Vector3.Zero;
        var destination = new Vector3(10f, 0f, 0f);

        InvokeThrowLightsaberPathCheck(origin, destination, new Vector3(5f, 1.25f, 0f)).Should().BeTrue();
        InvokeThrowLightsaberPathCheck(origin, destination, new Vector3(-0.01f, 0f, 0f)).Should().BeFalse();
        InvokeThrowLightsaberPathCheck(origin, destination, new Vector3(10.01f, 0f, 0f)).Should().BeFalse();
        InvokeThrowLightsaberPathCheck(origin, destination, new Vector3(5f, 1.26f, 0f)).Should().BeFalse();
    }

    [Test]
    public void ThrowLightsaber_PreservesSelectedTargetAndReportsNoValidTargets()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ThrowLightsaberAbilityDefinition.cs").FullName);

        source.Should().Contain("candidate => candidate == target || IsTargetAlongPath");
        source.Should().Contain("Combat.BuildAbilityNoTargetCombatLogMessage");
    }

    [Test]
    public void ForceLeap_UsesLegacyLeapAnimationAndLandsOutsideTheTarget()
    {
        var abilities = new ForceLeapAbilityDefinition().BuildAbilities();

        abilities[FeatType.ForceLeap1].ImpactAnimationType.Should().Be(Animation.Invalid);
        abilities[FeatType.ForceLeap2].ImpactAnimationType.Should().Be(Animation.Invalid);

        var root = FindRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceLeapAbilityDefinition.cs").FullName);

        source.Should().Contain("ActionPlayAnimation(Animation.ForceLeap, LeapAnimationSpeed, LeapAnimationDurationSeconds)");
        source.Should().Contain("private const float ArrivalDistanceMeters = 1.5f;");
        source.Should().Contain("ActionJumpToLocation(destination)");
        source.Should().Contain("ActionDoCommand(() => SetFacingPoint(GetPosition(target)))");
        source.Should().NotContain("ActionJumpToObject(target)");
        source.Should().NotContain("UsesImpactAnimation(Animation.ForceLeap)");
        source.Should().NotContain("VisualEffect.Vfx_Fnf_Summon_Monster_1");
    }

    [Test]
    public void ForceUniversalFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.ForcePush1, "ife_forcepsh1", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.ForceLeap1, "ife_forcelp1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForcePush2, "ife_forcepsh2", "M", "0x3E", "1", "cone", "8", "5", "17"),
            (FeatType.ForceLeap2, "ife_forcelp2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForcePush3, "ife_forcepsh3", "M", "0x3E", "1", "cone", "10", "5", "17")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

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

    private static void AssertForcePushConeTargeting(AbilityDetail ability, Spell spell, float length)
    {
        ability.Targeting.Should().NotBeNull();
        ability.Targeting!.Spell.Should().Be(spell);
        ability.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Cone);
        ability.Targeting.SizeX.Should().Be(length);
        ability.Targeting.SizeY.Should().Be(5f);
        ability.Targeting.Flags.Should().Be(
            AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);
    }

    private static bool InvokeThrowLightsaberPathCheck(Vector3 origin, Vector3 destination, Vector3 candidate)
    {
        var method = typeof(ThrowLightsaberAbilityDefinition)
            .GetMethod("IsPositionAlongPath", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (bool)method.Invoke(null, new object[] { origin, destination, candidate })!;
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

    private static Dictionary<PerkType, PerkDetail> BuildForceUniversalPerksWithout2daLookup()
    {
        var definition = new ForceUniversalPerkDefinition();
        var methodNames = new[]
        {
            "ForcePush",
            "ThrowLightsaber",
            "ForceLeap",
            "Precognition",
            "ForceConvergence"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ForceUniversalPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ForceUniversalPerkDefinition)
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
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")))
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
