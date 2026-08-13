using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Tests.Perks;

public class ForceLightConsularTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        Environment.SetEnvironmentVariable(
            "SWLOR_APP_LOG_DIRECTORY",
            Path.Combine(TestContext.CurrentContext.WorkDirectory, "logs") + Path.DirectorySeparatorChar);
        Log.Register();
    }

    [Test]
    public void ForceLightConsularStatusEffects_MatchCombatBible()
    {
        new ForceJudgment1StatusEffect().StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(-4);
        new ForceJudgment2StatusEffect().StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(-6);
        new ForceJudgment3StatusEffect().StatGroup.Stats[StatType.WeaponAndForceDamageDealtPercentAdjustment].Should().Be(-8);

        var forceSanctuary = new ForceSanctuary1StatusEffect();
        forceSanctuary.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-5);

        new SereneFocusStatusEffect().Frequency.Should().Be(6f);
        new HarmonicRestorationStatusEffect().StatGroup.Stats[StatType.TraumaResistance].Should().Be(10);

        var confusion = new ConfusionStatusEffect();
        confusion.Name.Should().Be("Confusion");
        confusion.Icon.Should().Be(EffectIconType.ConfusionStatusEffect);
        confusion.Categories.Should().Be(
            StatusEffectCategory.Debuff |
            StatusEffectCategory.Control |
            StatusEffectCategory.HardCrowdControl);
        confusion.CleanseTypes.Should().Be(StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet);
        confusion.ResistanceType.Should().Be(ResistanceType.Mind);

        var root = FindSourceRepositoryRoot();
        var confusionSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "ConfusionStatusEffect.cs").FullName);
        confusionSource.Should().Contain("public override string CanApply(uint creature)");
        confusionSource.Should().Contain("Ability.HasHardCrowdControlImmunity(creature, ImmunityType.Confused)");
        confusionSource.Should().Contain("Target is temporarily immune to confusion.");
    }

    [Test]
    public void ThrowRockAbilities_MatchCombatBible()
    {
        var throwRock = new ThrowRockAbilityDefinition().BuildAbilities();

        AssertAbility(throwRock[FeatType.ThrowRock1], "Throw Rock I", 1, RecastGroup.ThrowRock, 6f, 1.5f, 3, true, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(throwRock[FeatType.ThrowRock2], "Throw Rock II", 2, RecastGroup.ThrowRock, 6f, 1.5f, 4, true, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(throwRock[FeatType.ThrowRock3], "Throw Rock III", 3, RecastGroup.ThrowRock, 6f, 1.5f, 5, true, true, true, false, AbilityActivationType.Casted, 15f);

        foreach (var ability in throwRock.Values)
        {
            ability.AnimationType.Should().Be(Animation.CastOutAnimation);
            ability.ImpactAnimationType.Should().Be(Animation.Invalid);
            ability.ActivationVisualEffect.Should().Be(VisualEffect.None);
        }
    }

    [Test]
    public void ForceBurstAbilities_RestoreTargetedTelekineticAreaDamage()
    {
        var forceBurst = new ForceBurstAbilityDefinition().BuildAbilities();

        AssertAbility(forceBurst[FeatType.ForceBurst1], "Force Burst I", 1, RecastGroup.ForceBurst, 15f, 1.5f, 4, true, true, false, true, AbilityActivationType.Casted, 15f);
        AssertAbility(forceBurst[FeatType.ForceBurst2], "Force Burst II", 2, RecastGroup.ForceBurst, 15f, 1.5f, 5, true, true, false, true, AbilityActivationType.Casted, 15f);
        AssertAbility(forceBurst[FeatType.ForceBurst3], "Force Burst III", 3, RecastGroup.ForceBurst, 15f, 1.5f, 6, true, true, false, true, AbilityActivationType.Casted, 15f);

        var expectations = new[]
        {
            (FeatType.ForceBurst1, Spell.ForceBurst1),
            (FeatType.ForceBurst2, Spell.ForceBurst2),
            (FeatType.ForceBurst3, Spell.ForceBurst3)
        };
        foreach (var (feat, spell) in expectations)
        {
            var targeting = forceBurst[feat].Targeting;
            targeting.Should().NotBeNull();
            targeting!.Spell.Should().Be(spell);
            targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
            targeting.SizeX.Should().Be(5f);
            targeting.SizeY.Should().Be(0f);
            targeting.Flags.Should().Be(AbilityTargetingFlags.HarmsEnemies);
        }
    }

    [Test]
    public void OffensiveLightConsularPowers_MeetOrdinaryDathomirSoloTargets()
    {
        const int attackerAttackAndAccuracy = 148;
        const int attackerWillpower = 40;
        const int squellbugEvasion = 155;
        const int squellbugPhysicalDefense = 111;
        const int squellbugVitality = 31;
        const int squellbugForceDefense = 101;
        const int squellbugWillpower = 21;
        const int squellbugHP = 897;
        const int fullLightAffinityHitChance = 5;
        const double fullLightAffinityMagnitude = 1.5;

        var hitChanceAdjustments = new[]
        {
            GetAbilityConstant<int>(typeof(ThrowRockAbilityDefinition), "HitChancePercentAdjustment"),
            GetAbilityConstant<int>(typeof(ForceJudgmentAbilityDefinition), "HitChancePercentAdjustment"),
            GetAbilityConstant<int>(typeof(RadiantLanceAbilityDefinition), "HitChancePercentAdjustment"),
            GetAbilityConstant<int>(typeof(ForceBurstAbilityDefinition), "HitChancePercentAdjustment")
        };
        hitChanceAdjustments.Should().OnlyContain(adjustment => adjustment == 10);

        var hitRate = Combat.CalculateHitRate(
            attackerAttackAndAccuracy,
            squellbugEvasion,
            hitChanceAdjustments[0] + fullLightAffinityHitChance);

        hitRate.Should().BeGreaterThanOrEqualTo(75);

        var expectedDamagePerSecond =
            ExpectedDamagePerUse(
                GetAbilityConstant<int>(typeof(ThrowRockAbilityDefinition), "Rank3BaseDamage"),
                attackerAttackAndAccuracy,
                attackerWillpower,
                squellbugPhysicalDefense,
                squellbugVitality,
                hitRate,
                fullLightAffinityMagnitude) / 6f +
            ExpectedDamagePerUse(
                GetAbilityConstant<int>(typeof(ForceJudgmentAbilityDefinition), "Rank3BaseDamage"),
                attackerAttackAndAccuracy,
                attackerWillpower,
                squellbugForceDefense,
                squellbugWillpower,
                hitRate,
                fullLightAffinityMagnitude) / 15f +
            ExpectedDamagePerUse(
                GetAbilityConstant<int>(typeof(RadiantLanceAbilityDefinition), "Rank3BaseDamage"),
                attackerAttackAndAccuracy,
                attackerWillpower,
                squellbugForceDefense,
                squellbugWillpower,
                hitRate,
                fullLightAffinityMagnitude) / 18f +
            ExpectedDamagePerUse(
                GetAbilityConstant<int>(typeof(ForceBurstAbilityDefinition), "Rank3BaseDamage"),
                attackerAttackAndAccuracy,
                attackerWillpower,
                squellbugForceDefense,
                squellbugWillpower,
                hitRate,
                fullLightAffinityMagnitude) / 15f;

        var estimatedSecondsToDefeat = squellbugHP / expectedDamagePerSecond;
        estimatedSecondsToDefeat.Should().BeInRange(20d, 30d);
    }

    [Test]
    public void ThrowRockAbilities_ReuseMasterVisualEffects()
    {
        var root = FindSourceRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ThrowRockAbilityDefinition.cs").FullName);

        source.Should().Contain("DisplaysVisualEffectWhenActivating(VisualEffect.None)");
        source.Should().Contain("UsesAnimation(Animation.CastOutAnimation)");
        source.Should().NotContain("UsesImpactAnimation(");
        source.Should().Contain(": VisualEffect.Vfx_Imp_Mirv_Rock;");
        source.Should().Contain("VisualEffect.Vfx_Imp_Mirv_Rock3");
        source.Should().Contain("VisualEffect.Vfx_Imp_Dust_Explosion");
        source.Should().Contain("playImpactAnimation: false");
        source.Should().NotContain("VisualEffect.Vfx_Imp_Pulse_Nature");
    }

    [Test]
    public void Renewal_RestoresTheBiblePercentEveryThreeSecondsForThirtySeconds()
    {
        var root = FindSourceRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "RenewalAbilityDefinition.cs").FullName);

        source.Should().Contain("ApplyRenewal(activator, target, \"Renewal I\", 20f);");
        source.Should().Contain("ApplyRenewal(activator, target, \"Renewal II\", 40f);");
        source.Should().Contain("ApplyRenewal(activator, target, \"Renewal III\", 60f);");
        source.Should().Contain("totalPercent * Ability.GetActiveForceAffinityMagnitudeMultiplier(activator)");
        source.Should().Contain("new RegenerativeHealingStatusEffect(name, affinityAdjustedTotalPercent, 10)");
        source.Should().Contain("30f);");
    }

    [Test]
    public void ForceSanctuary_UsesSingleAllyPulseAndPersistentAreaVisual()
    {
        var root = FindSourceRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "ForceSanctuaryAbilityDefinition.cs").FullName);

        source.Should().Contain("private const VisualEffect AllyPulseVisualEffect = VisualEffect.Vfx_Imp_Holy_Aid;");
        source.Should().Contain("private const VisualEffect AreaMarkerVisualEffect = VisualEffect.Dur_Sanctuary;");
        source.Should().Contain("areaMarkerVisualEffect: AreaMarkerVisualEffect");
        source.Should().Contain("areaMarkerVisualEffectScale: AreaMarkerVisualEffectScale");
        source.Should().Contain("VisualEffect.None");
        source.Should().NotContain("VisualEffect.Vfx_Imp_Healing_M");
    }

    [Test]
    public void MindTrick_UsesConfusionAndStatContest()
    {
        var mindTrick = new MindTrickAbilityDefinition().BuildAbilities();

        AssertAbility(mindTrick[FeatType.MindTrick1], "Mind Trick I", 1, RecastGroup.MindTrick, 45f, 1f, 4, true, true, true, false, AbilityActivationType.Casted, 15f);
        AssertAbility(mindTrick[FeatType.MindTrick2], "Mind Trick II", 2, RecastGroup.MindTrick, 45f, 1f, 5, true, true, true, false, AbilityActivationType.Casted, 15f);

        var root = FindSourceRepositoryRoot();
        var abilitySource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Force" / "MindTrickAbilityDefinition.cs").FullName);
        var perkSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "ForceLightConsularPerkDefinition.cs").FullName);

        abilitySource.Should().Contain("typeof(ConfusionStatusEffect)");
        abilitySource.Should().NotContain("typeof(FoggyMindStatusEffect)");
        abilitySource.Should().Contain("casterWillpower - targetWillpower");
        abilitySource.Should().Contain("Math.Round");
        abilitySource.Should().Contain("MidpointRounding.AwayFromZero");
        abilitySource.Should().NotContain("Math.Ceiling((casterWillpower - targetWillpower) * WillpowerContestDurationSeconds)");
        abilitySource.Should().Contain("statusResistanceType: ResistanceType.Mind");
        perkSource.Should().NotContain("failure chance");
    }

    [Test]
    public void ForceLightConsularFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var classFeatRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "CLS_FEAT_FIGHT.2da");

        var feats = new[]
        {
            (FeatType.Benevolence1, "ife_bnvlnc1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.Renewal1, "ife_rnwl1", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.MindTrick1, "ife_mndtrck1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceBurst1, "ife_fburst1", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ThrowRock1, "ife_throwrock1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceJudgment1, "ife_forcejdg1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.Benevolence2, "ife_bnvlnc2", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForceBurst2, "ife_fburst2", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ThrowRock2, "ife_throwrock2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceJudgment2, "ife_forcejdg2", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.Renewal2, "ife_rnwl2", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.MindTrick2, "ife_mndtrck2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceSanctuary1, "ife_forcesnctry1", "M", "0x3E", "0", "sphere", "4", "****", "1"),
            (FeatType.Benevolence3, "ife_bnvlnc3", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.Renewal3, "ife_rnwl3", "M", "0x03", "0", "****", "****", "****", "****"),
            (FeatType.ForceBurst3, "ife_fburst3", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.ThrowRock3, "ife_throwrock3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ForceJudgment3, "ife_forcejdg3", "M", "0x02", "1", "sphere", "5", "****", "1"),
            (FeatType.RadiantLance1, "ife_radlance1", "M", "0x3E", "1", "rectangle", "8", "2.5", "17"),
            (FeatType.RadiantLance2, "ife_radlance2", "M", "0x3E", "1", "rectangle", "8", "2.5", "17"),
            (FeatType.RadiantLance3, "ife_radlance3", "M", "0x3E", "1", "rectangle", "8", "2.5", "17")
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

            if (featType is FeatType.ForceBurst1 or FeatType.ForceBurst2 or FeatType.ForceBurst3)
            {
                classFeatRows.Should().ContainSingle(
                    row => row.Value["FeatIndex"] == ((int)featType).ToString(),
                    $"{featType} must be available from the fighter radial menu");
            }
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

    private static double ExpectedDamagePerUse(
        int baseDamage,
        int attackerAttack,
        int attackerStat,
        int defenderDefense,
        int defenderStat,
        int hitRate,
        double affinityMagnitude)
    {
        var (minimumDamage, maximumDamage) = Combat.CalculateDamageRange(
            attackerAttack,
            baseDamage,
            attackerStat,
            defenderDefense,
            defenderStat,
            0);

        return (minimumDamage + maximumDamage) / 2d * affinityMagnitude * hitRate / 100d;
    }

    private static T GetAbilityConstant<T>(Type abilityDefinitionType, string fieldName)
    {
        var field = abilityDefinitionType.GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);

        field.Should().NotBeNull($"{abilityDefinitionType.Name} should declare {fieldName}");
        field!.IsLiteral.Should().BeTrue();
        return (T)field.GetRawConstantValue()!;
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

    private static Dictionary<PerkType, PerkDetail> BuildForceLightConsularPerksWithout2daLookup()
    {
        var definition = new ForceLightConsularPerkDefinition();
        var methodNames = new[]
        {
            "Benevolence",
            "ForceBurst",
            "ForceJudgment",
            "ForceMend",
            "ForceSanctuary",
            "HarmonicRestoration",
            "MindTrick",
            "RadiantLance",
            "Renewal",
            "SereneFocus",
            "ThrowRock"
        };

        foreach (var methodName in methodNames)
        {
            typeof(ForceLightConsularPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(ForceLightConsularPerkDefinition)
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
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static PathInfo FindSourceRepositoryRoot()
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

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN source repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
