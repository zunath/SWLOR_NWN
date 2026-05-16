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
    public void RiflePacificationPerkLevels_MatchCombatBible()
    {
        var perks = BuildRiflePacificationPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.TranquilizerShot], "Tranquilizer Shot", 1, 2, 5, FeatType.TranquilizerShot1,
            "Your next attack tranquilizes the target for up to 8 seconds. Damage breaks the effect prematurely.");
        AssertPerkLevel(perks[PerkType.CripplingShot], "Crippling Shot", 1, 2, 8, FeatType.CripplingShot1,
            "Your next attack deals weapon DMG + 12 and inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.PinningFire], "Pinning Fire", 1, 3, 12, FeatType.PinningFire1,
            "Deals weapon DMG + 10 and inflicts Dazed for 2 seconds.");
        AssertPerkLevel(perks[PerkType.SpotterStance], "Spotter Stance", 1, 2, 15, FeatType.SpotterStance1,
            "While active, grants +15% Accuracy and +15% Evasion against ranged attacks, but reduces Haste by 10%.");
        AssertPerkLevel(perks[PerkType.TranquilizerShot], "Tranquilizer Shot", 2, 4, 18, FeatType.TranquilizerShot2,
            "Your next attack tranquilizes the target for up to 14 seconds. Damage breaks the effect prematurely.");
        AssertPerkLevel(perks[PerkType.CripplingShot], "Crippling Shot", 2, 3, 20, FeatType.CripplingShot2,
            "Your next attack deals weapon DMG + 22 and inflicts Disoriented for 15 seconds.");
        AssertPerkLevel(perks[PerkType.SoftTarget], "Soft Target", 1, 2, 22, null,
            "Deal +10% rifle damage to enemies affected by Disoriented, Dazed, or tranquilizer effects.",
            StatType.DamageToDisorientedDazedTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.TranqCone], "Tranq Cone", 1, 3, 25, FeatType.TranqCone1,
            "Tranquilizes up to 3 enemies in a cone for up to 8 seconds. Damage breaks the effect prematurely.");
        AssertPerkLevel(perks[PerkType.PinningFire], "Pinning Fire", 2, 4, 28, FeatType.PinningFire2,
            "Deals weapon DMG + 18 to enemies in a line. Inflicts Knockdown for 3 seconds.");
        AssertPerkLevel(perks[PerkType.Overwatch], "Overwatch", 1, 3, 30, FeatType.Overwatch1,
            "Deal weapon DMG + 20 and interrupt your target's current ability activation. Inflicts Foggy Mind for 12 seconds.");
        AssertPerkLevel(perks[PerkType.FieldSedatives], "Field Sedatives", 1, 2, 32, null,
            "After a tranquilizer effect ends, the target's Attack is reduced by 10% for 10 seconds.",
            StatType.TranquilizeExpiredAttackPercentAdjustment,
            StatType.TranquilizeExpiredAttackDurationSeconds);
        AssertPerkLevel(perks[PerkType.CripplingShot], "Crippling Shot", 3, 4, 35, FeatType.CripplingShot3,
            "Your next attack deals weapon DMG + 34 and inflicts Disoriented for 20 seconds.");
        AssertPerkLevel(perks[PerkType.TranqCone], "Tranq Cone", 2, 3, 38, FeatType.TranqCone2,
            "Tranquilizes up to 5 enemies in a cone for up to 10 seconds. Damage breaks the effect prematurely.");
        AssertPerkLevel(perks[PerkType.ContainmentNet], "Containment Net", 1, 3, 40, null,
            "Enemies affected by your Disoriented effects suffer an additional -10% Evasion and -10% Attack.",
            StatType.OutgoingDisorientedAttackPercentAdjustment,
            StatType.OutgoingDisorientedEvasionPercentAdjustment);
        AssertPerkLevel(perks[PerkType.NeutralizingShot], "Neutralizing Shot", 1, 4, 42, FeatType.NeutralizingShot1,
            "Deals weapon DMG + 30, removes one beneficial combat effect, and inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.PacificationField], "Pacification Field", 1, 3, 45, FeatType.PacificationField1,
            "Creates a field for 15 seconds. Enemies inside suffer -10% Attack and become Dazed for 2 seconds every 5 seconds.");
        AssertPerkLevel(perks[PerkType.VeteranTracker], "Veteran Tracker", 1, 4, 48, null,
            "Rifle damage increases by 15% against enemies affected by any control effect.",
            StatType.DamageToControlTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.StasisVolley], "Stasis Volley", 1, 4, 50, FeatType.StasisVolley1,
            "All enemies in a cone take weapon DMG + 25 and are tranquilized for up to 12 seconds. Damage breaks the effect prematurely.");
    }

    [Test]
    public void RiflePacificationAbilities_MatchCombatBible()
    {
        var tranquilizerShot = new TranquilizerShotAbilityDefinition().BuildAbilities();
        AssertAbility(tranquilizerShot[FeatType.TranquilizerShot1], "Tranquilizer Shot I", 1, RecastGroup.TranquilizerShot, 60f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(tranquilizerShot[FeatType.TranquilizerShot2], "Tranquilizer Shot II", 2, RecastGroup.TranquilizerShot, 60f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);

        var cripplingShot = new CripplingShotAbilityDefinition().BuildAbilities();
        AssertAbility(cripplingShot[FeatType.CripplingShot1], "Crippling Shot I", 1, RecastGroup.CripplingShot, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(cripplingShot[FeatType.CripplingShot2], "Crippling Shot II", 2, RecastGroup.CripplingShot, 30f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(cripplingShot[FeatType.CripplingShot3], "Crippling Shot III", 3, RecastGroup.CripplingShot, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var pinningFire = new PinningFireAbilityDefinition().BuildAbilities();
        AssertAbility(pinningFire[FeatType.PinningFire1], "Pinning Fire I", 1, RecastGroup.PinningFire, 45f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(pinningFire[FeatType.PinningFire2], "Pinning Fire II", 2, RecastGroup.PinningFire, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var spotterStance = new SpotterStanceAbilityDefinition().BuildAbilities()[FeatType.SpotterStance1];
        AssertAbility(spotterStance, "Spotter Stance", 1, RecastGroup.SpotterStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var tranqCone = new TranqConeAbilityDefinition().BuildAbilities();
        AssertAbility(tranqCone[FeatType.TranqCone1], "Tranq Cone I", 1, RecastGroup.TranqCone, 120f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(tranqCone[FeatType.TranqCone2], "Tranq Cone II", 2, RecastGroup.TranqCone, 120f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var overwatch = new OverwatchAbilityDefinition().BuildAbilities()[FeatType.Overwatch1];
        AssertAbility(overwatch, "Overwatch", 1, RecastGroup.Overwatch, 120f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var neutralizingShot = new NeutralizingShotAbilityDefinition().BuildAbilities()[FeatType.NeutralizingShot1];
        AssertAbility(neutralizingShot, "Neutralizing Shot", 1, RecastGroup.NeutralizingShot, 90f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var pacificationField = new PacificationFieldAbilityDefinition().BuildAbilities()[FeatType.PacificationField1];
        AssertAbility(pacificationField, "Pacification Field", 1, RecastGroup.PacificationField, 180f, 0f, 14, true, false, false, true, AbilityActivationType.Casted);

        var stasisVolley = new StasisVolleyAbilityDefinition().BuildAbilities()[FeatType.StasisVolley1];
        AssertAbility(stasisVolley, "Stasis Volley", 1, RecastGroup.Capstone, 1800f, 2f, 25, true, false, false, true, AbilityActivationType.Casted);
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
    }

    [Test]
    public void RiflePacificationSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();

        var overwatch = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Rifle" / "OverwatchAbilityDefinition.cs").FullName);
        overwatch.Should().Contain("afterSuccessfulHit: InterruptActivation");
        overwatch.Should().Contain("ClearAllActions");
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
            (FeatType.CripplingShot1, "ife_cripshot1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.PinningFire1, "ife_pinfire1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SpotterStance1, "ife_spotstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.TranquilizerShot2, "ife_trnqlzrshot2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CripplingShot2, "ife_cripshot2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.TranqCone1, "ife_tranqcone1", "M", "0x3E", "1", "cone", "8", "6", "17"),
            (FeatType.PinningFire2, "ife_pinfire2", "M", "0x3E", "1", "rectangle", "2.5", "8", "17"),
            (FeatType.Overwatch1, "ife_ovrw1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CripplingShot3, "ife_cripshot3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.TranqCone2, "ife_tranqcone2", "M", "0x3E", "1", "cone", "10", "7", "17"),
            (FeatType.NeutralizingShot1, "ife_neutshot1", "M", "0x02", "1", "****", "****", "****", "****"),
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
        AbilityActivationType activationType)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Rifle);
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
