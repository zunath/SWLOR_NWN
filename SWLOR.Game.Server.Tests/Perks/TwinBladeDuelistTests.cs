using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class TwinBladeDuelistTests
{
    [Test]
    public void TwinBladeDuelistPerkLevels_MatchCombatBible()
    {
        var perks = BuildTwinBladeDuelistPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.CenterlineGuard], "Centerline Guard", 1, 2, 5, null,
            "Gain +10 Attack Deflection while wielding a twin blade. After deflecting an attack, your next attack within 8 seconds deals +8 DMG.",
            StatType.AttackDeflection,
            StatType.DeflectionNextSkillAbilityDamageBonus,
            StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds);
        AssertPerkLevel(perks[PerkType.SplitGuardStrike], "Split Guard Strike", 1, 2, 8, FeatType.SplitGuardStrike1,
            "Deals weapon DMG + 10 and grants +15% Defense for 10 seconds.");
        AssertPerkLevel(perks[PerkType.FeintingCut], "Feinting Cut", 1, 3, 12, FeatType.FeintingCut1,
            "Deals weapon DMG + 12 and inflicts Weakened, reducing Attack by 10% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.DuelistStance], "Duelist Stance", 1, 2, 15, FeatType.DuelistStance1,
            "While active, single-target Twin Blade combat abilities deal +15% damage and grant +10 Attack Deflection for 6 seconds, but area Twin Blade abilities deal -15% damage.");
        AssertPerkLevel(perks[PerkType.SplitGuardStrike], "Split Guard Strike", 2, 4, 18, FeatType.SplitGuardStrike2,
            "Deals weapon DMG + 22 and grants +20% Defense for 10 seconds.");
        AssertPerkLevel(perks[PerkType.MirrorStep], "Mirror Step", 1, 3, 20, null,
            "When hit by a target you damaged within the last 6 seconds, you have a 15% chance for your next Twin Blade ability to have no attack delay.",
            StatType.DamageTakenRecentTargetNextAbilityNoDelayChance,
            StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType,
            StatType.DamageTakenRecentTargetWindowSeconds);
        AssertPerkLevel(perks[PerkType.FeintingCut], "Feinting Cut", 2, 2, 22, FeatType.FeintingCut2,
            "Deals weapon DMG + 22 and inflicts Weakened, reducing Attack by 15% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.BindingCross], "Binding Cross", 1, 3, 25, FeatType.BindingCross1,
            "Strikes twice for weapon DMG + 10 each. Inflicts Hamstring for 12 seconds.");
        AssertPerkLevel(perks[PerkType.GuardedFlow], "Guarded Flow", 1, 4, 28, null,
            "Using a single-target Twin Blade ability grants +8 Attack Deflection for 8 seconds.",
            StatType.TwinBladeSingleTargetAbilityAttackDeflection,
            StatType.TwinBladeSingleTargetAbilityAttackDeflectionDurationSeconds);
        AssertPerkLevel(perks[PerkType.SplitGuardStrike], "Split Guard Strike", 3, 3, 30, FeatType.SplitGuardStrike3,
            "Deals weapon DMG + 34 and grants +25% Defense for 10 seconds.");
        AssertPerkLevel(perks[PerkType.PunishingAngle], "Punishing Angle", 1, 2, 32, null,
            "Deal +12% damage to targets affected by Weakened or Hamstring.",
            StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.FeintingCut], "Feinting Cut", 3, 4, 35, FeatType.FeintingCut3,
            "Deals weapon DMG + 32 and inflicts Weakened, reducing Attack by 20% for 15 seconds.");
        AssertPerkLevel(perks[PerkType.ReversalCut], "Reversal Cut", 1, 3, 38, FeatType.ReversalCut1,
            "Can be used after you are hit. Deals weapon DMG + 40 and inflicts Dazed for 3 seconds.");
        AssertPerkLevel(perks[PerkType.PrecisionArc], "Precision Arc", 1, 3, 40, null,
            "Single-target critical hits reduce the target's Defense by 10% for 10 seconds.",
            StatType.SingleTargetCriticalTargetDefensePercentAdjustment,
            StatType.SingleTargetCriticalTargetDefenseDurationSeconds);
        AssertPerkLevel(perks[PerkType.BindingCross], "Binding Cross", 2, 4, 42, FeatType.BindingCross2,
            "Strikes twice for weapon DMG + 18 each. Inflicts Hamstring for 20 seconds and Exposed for 10 seconds.");
        AssertPerkLevel(perks[PerkType.DuelistsChallenge], "Duelist's Challenge", 1, 3, 45, FeatType.DuelistsChallenge1,
            "Mark a target for 20 seconds. You and the target deal +20% damage to each other, but you gain +20% Defense against that target.");
        AssertPerkLevel(perks[PerkType.PerfectBalance], "Perfect Balance", 1, 4, 48, null,
            "Single-target Twin Blade abilities restore 3 STM. Area Twin Blade abilities restore 1 STM per target hit, up to 5 STM. This can only trigger once every 8 seconds.",
            StatType.TwinBladeSingleTargetAbilityStaminaRestore,
            StatType.TwinBladeSingleTargetAbilityStaminaRestoreCooldownSeconds,
            StatType.TwinBladeAreaAbilityCooldownStaminaRestorePerTarget,
            StatType.TwinBladeAreaAbilityCooldownStaminaRestoreMax,
            StatType.TwinBladeAreaAbilityCooldownStaminaRestoreCooldownSeconds);
        AssertPerkLevel(perks[PerkType.FinalForm], "Final Form", 1, 4, 50, FeatType.FinalForm1,
            "For 20 seconds, single-target Twin Blade combat abilities deal +25% damage and you gain +25 Attack Deflection.");
    }

    [Test]
    public void TwinBladeDuelistAbilities_MatchCombatBible()
    {
        var splitGuardStrike = new SplitGuardStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(splitGuardStrike[FeatType.SplitGuardStrike1], "Split Guard Strike I", 1, RecastGroup.SplitGuardStrike, 30f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(splitGuardStrike[FeatType.SplitGuardStrike2], "Split Guard Strike II", 2, RecastGroup.SplitGuardStrike, 30f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(splitGuardStrike[FeatType.SplitGuardStrike3], "Split Guard Strike III", 3, RecastGroup.SplitGuardStrike, 30f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var feintingCut = new FeintingCutAbilityDefinition().BuildAbilities();
        AssertAbility(feintingCut[FeatType.FeintingCut1], "Feinting Cut I", 1, RecastGroup.FeintingCut, 45f, 0f, 4, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(feintingCut[FeatType.FeintingCut2], "Feinting Cut II", 2, RecastGroup.FeintingCut, 45f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(feintingCut[FeatType.FeintingCut3], "Feinting Cut III", 3, RecastGroup.FeintingCut, 45f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var duelistStance = new DuelistStanceAbilityDefinition().BuildAbilities()[FeatType.DuelistStance1];
        AssertAbility(duelistStance, "Duelist Stance", 1, RecastGroup.DuelistStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var bindingCross = new BindingCrossAbilityDefinition().BuildAbilities();
        AssertAbility(bindingCross[FeatType.BindingCross1], "Binding Cross I", 1, RecastGroup.BindingCross, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(bindingCross[FeatType.BindingCross2], "Binding Cross II", 2, RecastGroup.BindingCross, 60f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var reversalCut = new ReversalCutAbilityDefinition().BuildAbilities()[FeatType.ReversalCut1];
        AssertAbility(reversalCut, "Reversal Cut", 1, RecastGroup.ReversalCut, 60f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);
        reversalCut.CustomValidation.Should().NotBeNull();

        var challenge = new DuelistsChallengeAbilityDefinition().BuildAbilities()[FeatType.DuelistsChallenge1];
        AssertAbility(challenge, "Duelist's Challenge", 1, RecastGroup.DuelistsChallenge, 120f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var finalForm = new FinalFormAbilityDefinition().BuildAbilities()[FeatType.FinalForm1];
        AssertAbility(finalForm, "Final Form", 1, RecastGroup.Capstone, 1800f, 2f, 25, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void TwinBladeDuelistStatusEffects_MatchCombatBible()
    {
        var splitGuard1 = new SplitGuardStrikeStatusEffect(15);
        splitGuard1.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(15);
        var splitGuard2 = new SplitGuardStrikeStatusEffect(20);
        splitGuard2.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(20);
        var splitGuard3 = new SplitGuardStrikeStatusEffect(25);
        splitGuard3.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(25);

        var weakened1 = new WeakenedStatusEffect(10);
        weakened1.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
        var weakened2 = new WeakenedStatusEffect();
        weakened2.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        var weakened3 = new WeakenedStatusEffect(20);
        weakened3.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);

        var duelistStance = new DuelistStanceStatusEffect();
        duelistStance.StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment].Should().Be(15);
        duelistStance.StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityAttackDeflection].Should().Be(10);
        duelistStance.StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityAttackDeflectionDurationSeconds].Should().Be(6);
        duelistStance.StatGroup.Stats[StatType.TwinBladeAreaAbilityDamagePercentAdjustment].Should().Be(-15);
        duelistStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);

        var challengeTarget = new DuelistsChallengeStatusEffect();
        challengeTarget.StatGroup.Stats[StatType.DamageToStatusSourcePercentAdjustment].Should().Be(20);
        challengeTarget.StatGroup.Stats[StatType.DamageTakenFromStatusSourcePercentAdjustment].Should().Be(20);
        var challengeSelf = new DuelistsChallengeSelfStatusEffect();
        challengeSelf.StatGroup.Stats[StatType.DefenseAgainstStatusSourcePercentAdjustment].Should().Be(20);

        var finalForm = new FinalFormStatusEffect();
        finalForm.StatGroup.Stats[StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment].Should().Be(25);
        finalForm.StatGroup.Stats[StatType.AttackDeflection].Should().Be(25);
        finalForm.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);
    }

    [Test]
    public void TwinBladeDuelistFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.SplitGuardStrike1, "ife_splitgrdstr1", "M", "0x02", "1"),
            (FeatType.SplitGuardStrike2, "ife_splitgrdstr2", "M", "0x02", "1"),
            (FeatType.SplitGuardStrike3, "ife_splitgrdstr3", "M", "0x02", "1"),
            (FeatType.FeintingCut1, "ife_feintcut1", "M", "0x02", "1"),
            (FeatType.FeintingCut2, "ife_feintcut2", "M", "0x02", "1"),
            (FeatType.FeintingCut3, "ife_feintcut3", "M", "0x02", "1"),
            (FeatType.DuelistStance1, "ife_duelstnc1", "P", "0x01", "0"),
            (FeatType.BindingCross1, "ife_bindcrs1", "M", "0x02", "1"),
            (FeatType.BindingCross2, "ife_bindcrs2", "M", "0x02", "1"),
            (FeatType.ReversalCut1, "ife_revcut1", "M", "0x02", "1"),
            (FeatType.DuelistsChallenge1, "ife_duelchal1", "M", "0x02", "1"),
            (FeatType.FinalForm1, "ife_finalform1", "P", "0x01", "0")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            spellRow["Range"].Should().Be(range);
            spellRow["TargetType"].Should().Be(targetType);
            spellRow["HostileSetting"].Should().Be(hostileSetting);
            spellRow["TargetShape"].Should().Be("****");
            spellRow["TargetSizeX"].Should().Be("****");
            spellRow["TargetSizeY"].Should().Be("****");
            spellRow["TargetFlags"].Should().Be("****");
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
        perk.Category.Should().Be(PerkCategoryType.TwinBladeDuelist);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.TwinBlade, skillRank);

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
        bool requiresTarget,
        bool isSingleTarget,
        bool isArea,
        AbilityActivationType activationType)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.TwinBlade);
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

    private static Dictionary<PerkType, PerkDetail> BuildTwinBladeDuelistPerksWithout2daLookup()
    {
        var definition = new TwinBladePerkDefinition();
        var methodNames = new[]
        {
            "BindingCross",
            "CenterlineGuard",
            "DuelistsChallenge",
            "DuelistStance",
            "FeintingCut",
            "FinalForm",
            "GuardedFlow",
            "MirrorStep",
            "PerfectBalance",
            "PrecisionArc",
            "PunishingAngle",
            "ReversalCut",
            "SplitGuardStrike"
        };

        foreach (var methodName in methodNames)
        {
            typeof(TwinBladePerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(TwinBladePerkDefinition)
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
