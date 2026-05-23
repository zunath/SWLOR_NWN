using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class VibrobladeDefenseTests
{
    [Test]
    public void VibrobladeDefensePerkLevels_MatchCombatBible()
    {
        var perks = BuildVibrobladeDefensePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.ShieldTraining], "Shield Training", 1, 3, 2, null,
            "When you successfully deflect an attack with a shield, gain +3% Evasion and +3% Enmity for 10 seconds.",
            StatType.DeflectionEvasionPercentAdjustment,
            StatType.DeflectionEvasionEnmityPercentAdjustment);

        AssertPerkLevel(perks[PerkType.ShieldBash], "Shield Bash", 1, 2, 8, FeatType.ShieldBash1,
            "Bashes an enemy for 12 DMG and inflicts Dazed for 3 seconds.");
        AssertPerkLevel(perks[PerkType.Bulwark], "Bulwark", 1, 2, 10, null,
            "Grants +15 Shield Deflection with shield equipped.",
            StatType.ShieldDeflection);
        AssertPerkLevel(perks[PerkType.FortifiedPosition], "Fortified Position", 1, 3, 15, null,
            "Grants +2 Mind, +2 Trauma, +2 Mobility Resistance with shield equipped.",
            StatType.MindResistance,
            StatType.TraumaResistance,
            StatType.MobilityResistance);
        AssertPerkLevel(perks[PerkType.ShieldBash], "Shield Bash", 2, 3, 18, FeatType.ShieldBash2,
            "Bashes an enemy for 24 DMG and inflicts Dazed for 6 seconds.");
        AssertPerkLevel(perks[PerkType.Alacrity], "Alacrity", 1, 3, 20, null,
            "Restore 4 STM when your shield deflects an attack.",
            StatType.DeflectionStaminaRestore);
        AssertPerkLevel(perks[PerkType.DefensiveStance], "Defensive Stance", 1, 3, 22, FeatType.DefensiveStance1,
            "While active, grants +20% to Enmity generation, +15% Defense, +15% Force Defense, -20% Attack, and -20% Force Attack");
        AssertPerkLevel(perks[PerkType.ShieldWall], "Shield Wall", 1, 4, 25, FeatType.ShieldWall1,
            "Channel for up to 6s. Allies within 5m gain +15% Physical Defense, you gain +25% Enmity for 1 minute.");
        AssertPerkLevel(perks[PerkType.Bulwark], "Bulwark", 2, 3, 28, null,
            "Grants +25 Shield Deflection with shield equipped total.",
            StatType.ShieldDeflection);
        AssertPerkLevel(perks[PerkType.GuardiansRiposte], "Guardian's Riposte", 1, 3, 30, null,
            "Receive Guardian's Riposte after deflecting an attack with a shield. Your next attack within 12s deals +10 DMG.",
            StatType.DeflectionNextSkillAbilityDamageBonus,
            StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds);
        AssertPerkLevel(perks[PerkType.CoveringStrike], "Covering Strike", 1, 3, 32, FeatType.CoveringStrike1,
            "Strike in a line for weapon DMG + 20. Enemies hit generate +25% Enmity toward you for 12s.");
        AssertPerkLevel(perks[PerkType.ShieldBash], "Shield Bash", 3, 4, 35, FeatType.ShieldBash3,
            "Bashes an enemy for 36 DMG and inflicts Stunned for 3 seconds.");
        AssertPerkLevel(perks[PerkType.FortifiedPosition], "Fortified Position", 2, 3, 38, null,
            "Grants +4 Mind, +4 Trauma, +4 Mobility Resistance with shield equipped total.",
            StatType.MindResistance,
            StatType.TraumaResistance,
            StatType.MobilityResistance);
        AssertPerkLevel(perks[PerkType.Bulwark], "Bulwark", 3, 4, 40, null,
            "Grants +35 Shield Deflection with shield equipped total.",
            StatType.ShieldDeflection);
        AssertPerkLevel(perks[PerkType.Unbreakable], "Unbreakable", 1, 4, 42, null,
            "When reduced below 25% HP with shield equipped, gain +40% Physical Defense for 10s. Once per 5min.",
            StatType.LowHPPhysicalDefenseThresholdPercent,
            StatType.LowHPPhysicalDefensePercentAdjustment,
            StatType.LowHPPhysicalDefenseDurationSeconds,
            StatType.LowHPPhysicalDefenseCooldownSeconds);
        AssertPerkLevel(perks[PerkType.DefensiveStance], "Defensive Stance", 2, 4, 45, FeatType.DefensiveStance2,
            "While active, grants +30% to Enmity generation, +20% Defense, +20% Force Defense, -20% Attack, and -20% Force Attack");
        AssertPerkLevel(perks[PerkType.Invincible], "Invincible", 1, 4, 50, FeatType.Invincible1,
            "For 45 seconds, you take 50% less physical damage and are immune to Knockdown and Daze.");
    }

    [Test]
    public void VibrobladeDefenseAbilities_MatchCombatBible()
    {
        var shieldBash = new ShieldBashAbilityDefinition().BuildAbilities();
        AssertAbility(shieldBash[FeatType.ShieldBash1], "Shield Bash I", 1, RecastGroup.ShieldBash, 60f, 0f, 3, true, AbilityActivationType.Weapon);
        AssertAbility(shieldBash[FeatType.ShieldBash2], "Shield Bash II", 2, RecastGroup.ShieldBash, 60f, 0f, 5, true, AbilityActivationType.Weapon);
        AssertAbility(shieldBash[FeatType.ShieldBash3], "Shield Bash III", 3, RecastGroup.ShieldBash, 60f, 0f, 8, true, AbilityActivationType.Weapon);

        var defensiveStance = new DefensiveStanceAbilityDefinition().BuildAbilities();
        AssertAbility(defensiveStance[FeatType.DefensiveStance1], "Defensive Stance I", 1, RecastGroup.DefensiveStance, 180f, 2f, null, false, AbilityActivationType.Casted);
        AssertAbility(defensiveStance[FeatType.DefensiveStance2], "Defensive Stance II", 2, RecastGroup.DefensiveStance, 180f, 2f, null, false, AbilityActivationType.Casted);

        var shieldWall = new ShieldWallAbilityDefinition().BuildAbilities()[FeatType.ShieldWall1];
        AssertAbility(shieldWall, "Shield Wall", 1, RecastGroup.ShieldWall, 120f, 6f, 10, false, AbilityActivationType.Casted);

        var coveringStrike = new CoveringStrikeAbilityDefinition().BuildAbilities()[FeatType.CoveringStrike1];
        AssertAbility(coveringStrike, "Covering Strike", 1, RecastGroup.CoveringStrike, 45f, 0f, 6, true, AbilityActivationType.Casted);

        var invincible = new InvincibleAbilityDefinition().BuildAbilities()[FeatType.Invincible1];
        AssertAbility(invincible, "Invincible", 1, RecastGroup.Capstone, 345f, 1f, 15, false, AbilityActivationType.Casted);
    }

    [Test]
    public void VibrobladeDefenseStatusEffects_MatchCombatBible()
    {
        Stat.GetStatTypeCategory(StatType.DeflectionEvasionPercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.DeflectionEvasionEnmityPercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenPositive);

        var coveringStrike = new CoveringStrikeStatusEffect();
        coveringStrike.Name.Should().Be("Covering Strike");
        coveringStrike.Categories.Should().Be(StatusEffectCategory.Debuff);
        coveringStrike.StackingType.Should().Be(StatusEffectStackType.StackFromMultipleSources);
        coveringStrike.StatGroup.Stats[StatType.EnmityToStatusSourcePercentAdjustment].Should().Be(25);

        var shieldWallAlly = new ShieldWallStatusEffect();
        shieldWallAlly.ApplyEffect(1, 2, 60);
        shieldWallAlly.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(15);
        shieldWallAlly.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(0);

        var shieldWallSelf = new ShieldWallStatusEffect();
        shieldWallSelf.ApplyEffect(1, 1, 60);
        shieldWallSelf.StatGroup.Stats.Should().NotContainKey(StatType.PhysicalDefensePercentAdjustment);
        shieldWallSelf.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(25);

        var invincible = new InvincibleStatusEffect();
        invincible.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-50);
    }

    [Test]
    public void VibrobladeDefenseFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.CoveringStrike1, "ife_covstrk1"),
            (FeatType.DefensiveStance1, "ife_defstnc1"),
            (FeatType.DefensiveStance2, "ife_defstnc2"),
            (FeatType.Invincible1, "ife_invin1"),
            (FeatType.ShieldWall1, "ife_shldwall1"),
            (FeatType.ShieldBash1, "ife_shldbsh1"),
            (FeatType.ShieldBash2, "ife_shldbsh2"),
            (FeatType.ShieldBash3, "ife_shldbsh3")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();
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
        perk.Category.Should().Be(PerkCategoryType.VibrobladeDefense);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Vibroblade, skillRank);

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statTypes.Length > 0)
            perkLevel.StatBonuses.Select(x => x.Stat).Should().Contain(statTypes);
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
        AbilityActivationType activationType)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(activationType);
        ability.IsHostileAbility.Should().Be(isHostile);
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

    private static Dictionary<PerkType, PerkDetail> BuildVibrobladeDefensePerksWithout2daLookup()
    {
        var definition = new VibrobladePerkDefinition();
        var methodNames = new[]
        {
            "ShieldTraining",
            "ShieldBash",
            "Bulwark",
            "FortifiedPosition",
            "Alacrity",
            "DefensiveStance",
            "ShieldWall",
            "GuardiansRiposte",
            "CoveringStrike",
            "Unbreakable",
            "Invincible"
        };

        foreach (var methodName in methodNames)
        {
            typeof(VibrobladePerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(VibrobladePerkDefinition)
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
