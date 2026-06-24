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
    public void MarkedForDeathBonusDamage_UsesSharedTriggeredDamagePath()
    {
        var root = FindRepositoryRoot();
        var markedForDeath = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "MarkedForDeathStatusEffect.cs").FullName);
        var combat = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);

        markedForDeath.Should().Contain("Combat.ApplyTriggeredDamage(Source, defender, DamageBonus, damageType);");
        markedForDeath.Should().NotContain("EffectDamage(DamageBonus)");
        combat.Should().Contain("public static int ApplyTriggeredDamage");
        combat.Should().NotContain("ApplyRiderDamage");
        combat.Should().Contain("damage = ApplyDamageTakenModifiers(target, damage, activator, damageType);");
        combat.Should().Contain("ApplyDamageDealtEffects(activator, target, damage, skillType, damageType, CombatDamageDeliveryType.Triggered);");
        combat.Should().Contain("StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered);");
    }

    [Test]
    public void BackstabAbilities_PlayBladeSoundOnImpact()
    {
        var backstab = new BackstabAbilityDefinition().BuildAbilities();

        backstab[FeatType.Backstab1].ImpactSound.Should().Be("cb_sw_blade1");
        backstab[FeatType.Backstab2].ImpactSound.Should().Be("cb_sw_blade1");
        backstab[FeatType.Backstab3].ImpactSound.Should().Be("cb_sw_blade1");
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
            (FeatType.ShadowStrike1, "ife_shdwstrk1", true, false, "0x02"),
            (FeatType.ShadowStrike2, "ife_shdwstrk2", true, false, "0x02"),
            (FeatType.SmokeBomb1, "ife_smokbmb1", true, false, "0x3E"),
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
        const string ExpectedName = "Smoke Bomb I";
        const string ExpectedDescription =
            "All enemies in the selected area are afflicted with Smoke Bomb, reducing Accuracy by 20% for 12 seconds.";

        var featRow = featRows[(int)FeatType.SmokeBomb1];
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
