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
    public void VibrobladeDefenseAbilities_MatchCombatBible()
    {
        var shieldBash = new ShieldBashAbilityDefinition().BuildAbilities();
        AssertAbility(shieldBash[FeatType.ShieldBash1], "Shield Bash I", 1, RecastGroup.ShieldBash, 60f, 0f, 3, true, AbilityActivationType.Casted);
        AssertAbility(shieldBash[FeatType.ShieldBash2], "Shield Bash II", 2, RecastGroup.ShieldBash, 60f, 0f, 5, true, AbilityActivationType.Casted);
        AssertAbility(shieldBash[FeatType.ShieldBash3], "Shield Bash III", 3, RecastGroup.ShieldBash, 60f, 0f, 8, true, AbilityActivationType.Casted);
        AssertShieldBashAnimation(shieldBash[FeatType.ShieldBash1]);
        AssertShieldBashAnimation(shieldBash[FeatType.ShieldBash2]);
        AssertShieldBashAnimation(shieldBash[FeatType.ShieldBash3]);
        AssertShieldBashTargeting(shieldBash[FeatType.ShieldBash1]);
        AssertShieldBashTargeting(shieldBash[FeatType.ShieldBash2]);
        AssertShieldBashTargeting(shieldBash[FeatType.ShieldBash3]);

        var defensiveStance = new DefensiveStanceAbilityDefinition().BuildAbilities();
        AssertAbility(defensiveStance[FeatType.DefensiveStance1], "Defensive Stance I", 1, RecastGroup.DefensiveStance, 180f, 2f, null, false, AbilityActivationType.Casted);
        AssertAbility(defensiveStance[FeatType.DefensiveStance2], "Defensive Stance II", 2, RecastGroup.DefensiveStance, 180f, 2f, null, false, AbilityActivationType.Casted);

        var shieldWall = new ShieldWallAbilityDefinition().BuildAbilities()[FeatType.ShieldWall1];
        AssertAbility(shieldWall, "Shield Wall", 1, RecastGroup.ShieldWall, 120f, 6f, 10, false, AbilityActivationType.Casted);
        AssertActivationAnimation(shieldWall, "Shield_Wall");

        var coveringStrike = new CoveringStrikeAbilityDefinition().BuildAbilities()[FeatType.CoveringStrike1];
        AssertAbility(coveringStrike, "Covering Strike", 1, RecastGroup.CoveringStrike, 45f, 0f, 6, true, AbilityActivationType.Casted);
        AssertImpactAnimation(coveringStrike, "Covering_Strike");

        var invincible = new InvincibleAbilityDefinition().BuildAbilities()[FeatType.Invincible1];
        AssertAbility(invincible, "Invincible", 1, RecastGroup.Capstone, 345f, 1f, 15, false, AbilityActivationType.Casted);
        AssertActivationAnimation(invincible, "Invincible");
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

        var defensiveStance1 = BuildDefensiveStanceStats(1);
        defensiveStance1.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);
        defensiveStance1.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-20);
        defensiveStance1.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(15);
        defensiveStance1.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(15);
        defensiveStance1.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(20);

        var defensiveStance2 = BuildDefensiveStanceStats(2);
        defensiveStance2.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);
        defensiveStance2.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-20);
        defensiveStance2.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(20);
        defensiveStance2.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(20);
        defensiveStance2.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(30);

        var invincible = new InvincibleStatusEffect();
        invincible.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-50);
    }

    [Test]
    public void InvincibleStatusEffect_AppliesProtectiveDurationVisualEffect()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            "InvincibleStatusEffect.cs"));

        source.Should().Contain("VisualEffect.Dur_Prot_Premonition");
        source.Should().Contain("TagNativeEffect(EffectVisualEffect");
        source.Should().Contain("ApplyEffectToObject(DurationType.Temporary, effect, creature, duration)");
    }

    [Test]
    public void VibrobladeDefenseFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

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
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();
        }
    }

    private static DefensiveStanceStatusEffect BuildDefensiveStanceStats(int level)
    {
        var status = new DefensiveStanceStatusEffect(level);
        status.ApplyEffect(1, 1, -1);

        return status;
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

    private static void AssertShieldBashAnimation(AbilityDetail ability)
    {
        AssertImpactAnimation(ability, "Shield_Bash");
    }

    private static void AssertActivationAnimation(AbilityDetail ability, string replacementAnimationName)
    {
        ability.AnimationType.Should().Be(Animation.FireForgetTaunt);
        ability.AnimationSourceAnimationName.Should().Be("taunt");
        ability.AnimationReplacementAnimationName.Should().Be(replacementAnimationName);
        ability.AnimationRestoreDelaySeconds.Should().Be(1.1f);
    }

    private static void AssertImpactAnimation(AbilityDetail ability, string replacementAnimationName)
    {
        ability.ImpactAnimationType.Should().Be(Animation.FireForgetTaunt);
        ability.ImpactAnimationSourceAnimationName.Should().Be("taunt");
        ability.ImpactAnimationReplacementAnimationName.Should().Be(replacementAnimationName);
        ability.ImpactAnimationRestoreDelaySeconds.Should().Be(1.1f);
    }

    private static void AssertShieldBashTargeting(AbilityDetail ability)
    {
        ability.RequiresTarget.Should().BeFalse();
        ability.UsesActiveAttackTarget.Should().BeTrue();
        ability.CustomValidation.Should().NotBeNull();
        ability.ImpactAction.Should().NotBeNull();
        ability.IsSingleTargetAbility.Should().BeTrue();
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
