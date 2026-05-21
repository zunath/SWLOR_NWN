using System.Text.Json;
using System.Text.Json.Serialization;
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

public class VibroknifeShadowTests
{
    [Test]
    public void VibroknifeShadowPerkLevels_MatchCombatBible()
    {
        var perks = BuildVibroknifeShadowPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.PrecisionStrikes], "Precision Strikes", 1, 2, 5, null,
            "Critical hits deal +10% damage.",
            StatType.CriticalDamagePercentAdjustment);
        AssertPerkLevel(perks[PerkType.CheapShot], "Cheap Shot", 1, 2, 8, FeatType.CheapShot1,
            "Deals weapon DMG + 8 to a single target. Inflicts Blind for 6 seconds.");
        AssertPerkLevel(perks[PerkType.Backstab], "Backstab", 1, 2, 10, FeatType.Backstab1,
            "Deals weapon DMG + 20 from behind your target.");
        AssertPerkLevel(perks[PerkType.DeadlyPrecision], "Deadly Precision", 1, 3, 12, FeatType.DeadlyPrecision1,
            "While active, grants +15% critical hit chance, -20% evasion, and -15% defense.");
        AssertPerkLevel(perks[PerkType.EvasiveCombat], "Evasive Combat", 1, 2, 15, FeatType.EvasiveCombat1,
            "Increases evasion by 10%, reduces enmity by 15%, and reduces attack by 15% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.Backstab], "Backstab", 2, 3, 18, FeatType.Backstab2,
            "Deals weapon DMG + 40 from behind your target.");
        AssertPerkLevel(perks[PerkType.CheapShot], "Cheap Shot", 2, 3, 20, FeatType.CheapShot2,
            "Deals weapon DMG + 16 to a single target. Inflicts Blind for 9 seconds.");
        AssertPerkLevel(perks[PerkType.Opportunist], "Opportunist", 1, 3, 22, null,
            "Grants +15% Critical Rate against targets not facing you.",
            StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment);
        AssertPerkLevel(perks[PerkType.AmbushTactics], "Ambush Tactics", 1, 3, 25, null,
            "After dealing a critical hit, your next attack within 8 seconds ignores 20% of defense.",
            StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment,
            StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds);
        AssertPerkLevel(perks[PerkType.ShadowStrike], "Shadow Strike", 1, 3, 28, FeatType.ShadowStrike1,
            "Deals weapon DMG + 30 to a single target. Inflicts 30% Slow for 8 seconds.");
        AssertPerkLevel(perks[PerkType.MarkedForDeath], "Marked for Death", 1, 4, 30, FeatType.MarkedForDeath1,
            "You mark a single target. Your next 3 attacks against them deal +12 DMG each.");
        AssertPerkLevel(perks[PerkType.AssassinsFocus], "Assassin's Focus", 1, 3, 32, null,
            "After landing a critical hit, gain +5% Accuracy for 30 seconds.",
            StatType.CriticalAccuracyPercentAdjustment,
            StatType.CriticalAccuracyDurationSeconds);
        AssertPerkLevel(perks[PerkType.Backstab], "Backstab", 3, 4, 35, FeatType.Backstab3,
            "Deals weapon DMG + 60 from behind your target and knocks down for 3 seconds.");
        AssertPerkLevel(perks[PerkType.EvasiveCombat], "Evasive Combat", 2, 3, 38, FeatType.EvasiveCombat2,
            "Increases evasion by 20%, reduces enmity by 25%, and reduces attack by 25% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.SmokeBomb], "Smoke Bomb", 1, 4, 40, FeatType.SmokeBomb,
            "All enemies in the selected area are afflicted with Smoke Bomb, reducing Accuracy by 20% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.ShadowStrike], "Shadow Strike", 2, 4, 42, FeatType.ShadowStrike2,
            "Deals weapon DMG + 48 to a single target. Inflicts 40% Slow for 12 seconds.");
        AssertPerkLevel(perks[PerkType.Decoy], "Decoy", 1, 3, 45, FeatType.Decoy1,
            "For 12 seconds, enemies targeting you have -25% Accuracy.");
        AssertPerkLevel(perks[PerkType.VitalStrike], "Vital Strike", 1, 4, 50, FeatType.VitalStrike1,
            "Deals weapon DMG + 35. On hit, the target's physical defense is reduced by 10% for 45 seconds.");
    }

    [Test]
    public void VibroknifeShadowAbilities_MatchCombatBible()
    {
        var cheapShot = new CheapShotAbilityDefinition().BuildAbilities();
        AssertAbility(cheapShot[FeatType.CheapShot1], "Cheap Shot I", 1, RecastGroup.CheapShot, 45f, 0f, 4, true, true, true, false, false, AbilityActivationType.Casted);
        AssertAbility(cheapShot[FeatType.CheapShot2], "Cheap Shot II", 2, RecastGroup.CheapShot, 45f, 0f, 6, true, true, true, false, false, AbilityActivationType.Casted);

        var backstab = new BackstabAbilityDefinition().BuildAbilities();
        AssertAbility(backstab[FeatType.Backstab1], "Backstab I", 1, RecastGroup.Backstab, 60f, 0f, 3, true, true, true, false, true, AbilityActivationType.Casted);
        AssertAbility(backstab[FeatType.Backstab2], "Backstab II", 2, RecastGroup.Backstab, 60f, 0f, 5, true, true, true, false, true, AbilityActivationType.Casted);
        AssertAbility(backstab[FeatType.Backstab3], "Backstab III", 3, RecastGroup.Backstab, 60f, 0f, 8, true, true, true, false, true, AbilityActivationType.Casted);

        var deadlyPrecision = new DeadlyPrecisionAbilityDefinition().BuildAbilities()[FeatType.DeadlyPrecision1];
        AssertAbility(deadlyPrecision, "Deadly Precision", 1, RecastGroup.DeadlyPrecision, 180f, 2f, null, false, false, false, false, false, AbilityActivationType.Casted);

        var evasiveCombat = new EvasiveCombatAbilityDefinition().BuildAbilities();
        AssertAbility(evasiveCombat[FeatType.EvasiveCombat1], "Evasive Combat I", 1, RecastGroup.EvasiveCombat, 300f, 0f, 4, false, false, false, false, false, AbilityActivationType.Casted);
        AssertAbility(evasiveCombat[FeatType.EvasiveCombat2], "Evasive Combat II", 2, RecastGroup.EvasiveCombat, 300f, 0f, 8, false, false, false, false, false, AbilityActivationType.Casted);

        var shadowStrike = new ShadowStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(shadowStrike[FeatType.ShadowStrike1], "Shadow Strike I", 1, RecastGroup.ShadowStrike, 60f, 0f, 7, true, true, true, false, false, AbilityActivationType.Casted);
        AssertAbility(shadowStrike[FeatType.ShadowStrike2], "Shadow Strike II", 2, RecastGroup.ShadowStrike, 60f, 0f, 10, true, true, true, false, false, AbilityActivationType.Casted);

        var markedForDeath = new MarkedForDeathAbilityDefinition().BuildAbilities()[FeatType.MarkedForDeath1];
        AssertAbility(markedForDeath, "Marked for Death", 1, RecastGroup.MarkedForDeath, 90f, 0f, 8, true, true, true, false, false, AbilityActivationType.Casted);

        var smokeBomb = new SmokeBombAbilityDefinition().BuildAbilities()[FeatType.SmokeBomb];
        AssertAbility(smokeBomb, "Smoke Bomb", 1, RecastGroup.SmokeBomb, 30f, 2f, 10, true, false, false, true, false, AbilityActivationType.Casted);

        var decoy = new DecoyAbilityDefinition().BuildAbilities()[FeatType.Decoy1];
        AssertAbility(decoy, "Decoy", 1, RecastGroup.Decoy, 30f, 1f, 12, true, false, false, true, false, AbilityActivationType.Casted);

        var vitalStrike = new VitalStrikeAbilityDefinition().BuildAbilities()[FeatType.VitalStrike1];
        AssertAbility(vitalStrike, "Vital Strike", 1, RecastGroup.Capstone, 345f, 0f, 15, true, true, true, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void VibroknifeShadowStatusEffects_MatchCombatBible()
    {
        var deadlyPrecision = new DeadlyPrecisionStatusEffect();
        deadlyPrecision.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(15);
        deadlyPrecision.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);
        deadlyPrecision.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);
        deadlyPrecision.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(0);
        deadlyPrecision.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);

        var decoy = new DecoyStatusEffect();
        decoy.StatGroup.Stats[StatType.AccuracyToStatusSourcePercentAdjustment].Should().Be(-25);
        decoy.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);

        var markedForDeath = new MarkedForDeathStatusEffect();
        markedForDeath.RemainingAttacks.Should().Be(3);
        markedForDeath.StatGroup.Stats[StatType.DamageTakenFlatAdjustment].Should().Be(0);

        var shadowStrike1 = new ShadowStrikeStatusEffect(-30);
        shadowStrike1.Categories.Should().HaveFlag(StatusEffectCategory.Control);
        shadowStrike1.ApplyEffect(0, 0, 8);
        shadowStrike1.StatGroup.Stats[StatType.MovementSpeedPercentAdjustment].Should().Be(-30);

        var shadowStrike2 = new ShadowStrikeStatusEffect(-40);
        shadowStrike2.ApplyEffect(0, 0, 12);
        shadowStrike2.StatGroup.Stats[StatType.MovementSpeedPercentAdjustment].Should().Be(-40);

        var smokeBomb = new SmokeBombStatusEffect();
        smokeBomb.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-20);

        var vitalStrike = new VitalStrikeStatusEffect();
        vitalStrike.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-10);
    }

    [Test]
    public void VibroknifeShadowFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.CheapShot1, "ife_cheapshot1", true, false, "0x02"),
            (FeatType.CheapShot2, "ife_cheapshot2", true, false, "0x02"),
            (FeatType.Backstab1, "ife_bckstb1", true, false, "0x02"),
            (FeatType.Backstab2, "ife_bckstb2", true, false, "0x02"),
            (FeatType.Backstab3, "ife_bckstb3", true, false, "0x02"),
            (FeatType.DeadlyPrecision1, "ife_deadprec1", false, true, "0x01"),
            (FeatType.EvasiveCombat1, "ife_evascmbt1", false, true, "0x01"),
            (FeatType.EvasiveCombat2, "ife_evascmbt2", false, true, "0x01"),
            (FeatType.ShadowStrike1, "ife_shdwstrk1", true, false, "0x02"),
            (FeatType.ShadowStrike2, "ife_shdwstrk2", true, false, "0x02"),
            (FeatType.MarkedForDeath1, "ife_markdeath1", true, false, "0x02"),
            (FeatType.SmokeBomb, "ife_smokbmb", true, false, "0x3E"),
            (FeatType.Decoy1, "ife_decoy1", true, true, "0x01"),
            (FeatType.VitalStrike1, "ife_vitalstrk1", true, false, "0x02")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, isHostile, targetsSelf, expectedTargetType) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            if (isHostile && !targetsSelf)
            {
                featRow["TARGETSELF"].Should().Be("****");
                featRow["HostileFeat"].Should().Be("1");
                spellRow["TargetType"].Should().Be(expectedTargetType);
                spellRow["HostileSetting"].Should().Be("1");
            }
        }
    }

    [Test]
    public void SmokeBombFeatAndSpellTlkEntries_DisplayExpectedText()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");
        var tlkEntries = ReadTlkEntries(root / "SWLOR_Haks" / "swlor2_tlk" / "swlor2_tlk.tlk.json");
        const int CustomTlkOffset = 16777216;
        const string ExpectedName = "Smoke Bomb";
        const string ExpectedDescription =
            "All enemies in the selected area are afflicted with Smoke Bomb, reducing Accuracy by 20% for 12 seconds.";

        var featRow = featRows[(int)FeatType.SmokeBomb];
        var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
        var nameId = int.Parse(featRow["FEAT"]) - CustomTlkOffset;
        var featDescriptionId = int.Parse(featRow["DESCRIPTION"]) - CustomTlkOffset;
        var spellDescriptionId = int.Parse(spellRow["SpellDesc"]) - CustomTlkOffset;

        spellRow["Name"].Should().Be(featRow["FEAT"]);
        tlkEntries[nameId].Should().Be(ExpectedName);
        tlkEntries[featDescriptionId].Should().Be(ExpectedDescription);
        tlkEntries[spellDescriptionId].Should().Be(ExpectedDescription);
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
        perk.Category.Should().Be(PerkCategoryType.VibroknifeShadow);

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
        bool hasCustomValidation,
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
        (ability.CustomValidation != null).Should().Be(hasCustomValidation);

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

    private static Dictionary<PerkType, PerkDetail> BuildVibroknifeShadowPerksWithout2daLookup()
    {
        var definition = new VibroknifePerkDefinition();
        var methodNames = new[]
        {
            "AmbushTactics",
            "Backstab",
            "AssassinsFocus",
            "CheapShot",
            "DeadlyPrecision",
            "Decoy",
            "EvasiveCombat",
            "MarkedForDeath",
            "Opportunist",
            "PrecisionStrikes",
            "ShadowStrike",
            "SmokeBomb",
            "VitalStrike"
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

    private static Dictionary<int, string> ReadTlkEntries(PathInfo path)
    {
        var tlk = JsonSerializer.Deserialize<TlkFile>(File.ReadAllText(path.FullName))!;
        return tlk.Entries.ToDictionary(entry => entry.Id, entry => entry.Text);
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

    private sealed record TlkFile([property: JsonPropertyName("entries")] TlkEntry[] Entries);

    private sealed record TlkEntry(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("text")] string Text);
}
