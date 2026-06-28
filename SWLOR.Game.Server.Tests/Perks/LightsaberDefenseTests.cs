using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LightsaberDefenseTests
{
    [Test]
    public void LightsaberDefenseStatusEffects_MatchCombatBible()
    {
        var tauntingDeflection = new TauntingDeflectionStatusEffect();
        tauntingDeflection.StatGroup.Stats[StatType.AttackDeflection].Should().Be(10);
        tauntingDeflection.StatGroup.Stats[StatType.DeflectionFPRestore].Should().Be(2);
        tauntingDeflection.StatGroup.Stats[StatType.DeflectionEnmityPercentAdjustment].Should().Be(20);

        var deflectingAura = new DeflectingAuraStatusEffect();
        deflectingAura.StatGroup.Stats[StatType.AttackDeflection].Should().Be(15);

        var impenetrableGuard = new ImpenetrableGuardStatusEffect();
        impenetrableGuard.StatGroup.Stats[StatType.AttackDeflection].Should().Be(15);
        impenetrableGuard.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(10);
        impenetrableGuard.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);
        impenetrableGuard.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-20);
        impenetrableGuard.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(0);
        impenetrableGuard.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(0);

        var guardianMaster = new GuardianMasterStatusEffect();
        guardianMaster.Name.Should().Be("Guardian Master");
        guardianMaster.StatGroup.Stats[StatType.AttackDeflection].Should().Be(0);
        guardianMaster.StatGroup.Stats[StatType.DeflectionFPRestore].Should().Be(4);
        guardianMaster.StatGroup.Stats[StatType.DeflectionEnmityPercentAdjustment].Should().Be(50);
        guardianMaster.StatGroup.Stats[StatType.AttackDeflectionChanceCap].Should().Be(10);
    }

    [Test]
    public void LightsaberDefenseTraitStatValues_MatchCombatBible()
    {
        var root = FindSourceRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "LightsaberPerkDefinition.cs").FullName);

        source.Should().Contain("StatType.AttackDeflection, 8");
        source.Should().Contain("StatType.AttackDeflection, 14");
        source.Should().Contain("StatType.AttackDeflection, 20");
        source.Should().Contain("StatType.DeflectionNextAutoAttackCriticalRateSkillType, (int)SkillType.Lightsaber");
        source.Should().Contain("StatType.DeflectionNextAutoAttackCriticalRatePercentAdjustment, 20");
        source.Should().Contain("StatType.DeflectionNextAutoAttackCriticalRateWindowSeconds, 15");
        source.Should().NotContain("StatType.DeflectionNextSkillAbilityCriticalRatePercentAdjustment, 20");
        source.Should().NotContain("StatType.AttackDeflection, creature => EquipmentPredicates.HasMainHandLightsaber(creature)");

        var deflectionRiposte = BuildLightsaberDefensePerksWithout2daLookup()[PerkType.DeflectionRiposte];
        AssertStatBonus(deflectionRiposte.PerkLevels[1], StatType.DeflectionNextAutoAttackCriticalRateSkillType, (int)SkillType.Lightsaber);
        AssertStatBonus(deflectionRiposte.PerkLevels[1], StatType.DeflectionNextAutoAttackCriticalRatePercentAdjustment, 20);
        AssertStatBonus(deflectionRiposte.PerkLevels[1], StatType.DeflectionNextAutoAttackCriticalRateWindowSeconds, 15);
        Stat.GetStatTypeCategory(StatType.DeflectionNextAutoAttackCriticalRateSkillType).Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.DeflectionNextAutoAttackCriticalRatePercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.DeflectionNextAutoAttackCriticalRateWindowSeconds).Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.NextAutoAttackCriticalRateSkillType).Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.NextAutoAttackCriticalRatePercentAdjustment).Should().Be(StatTypeCategory.BeneficialWhenPositive);
    }

    [Test]
    public void DeflectionRiposte_GrantsAutoAttackCriticalRateBonus()
    {
        var root = FindSourceRepositoryRoot();
        var statSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Stat.cs").FullName);
        var combatSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var attackRollSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Native" / "ResolveAttackRoll.cs").FullName);

        statSource.Should().Contain("StatType.DeflectionNextAutoAttackCriticalRateSkillType");
        statSource.Should().Contain("StatType.DeflectionNextAutoAttackCriticalRatePercentAdjustment");
        statSource.Should().Contain("Combat.GrantNextAutoAttackCriticalRateBonus(");
        combatSource.Should().Contain("public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType)");
        combatSource.Should().Contain("StatType.NextAutoAttackCriticalRateSkillType");
        combatSource.Should().Contain("StatType.NextAutoAttackCriticalRatePercentAdjustment");
        attackRollSource.Should().Contain("criticalModifier += Combat.ConsumeNextAutoAttackCriticalRateBonus(attacker.m_idSelf, weaponSkillType);");
    }

    [Test]
    public void AttackDeflectionChanceCap_AddsAdjustmentsToDefaultCap()
    {
        var root = FindSourceRepositoryRoot();
        var source = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Stat.cs").FullName);

        source.Should().Contain("var capAdjustment = GetStatAdjustment(creature, StatType.AttackDeflectionChanceCap);");
        source.Should().Contain("var cap = DefaultAttackDeflectionChanceCap + capAdjustment;");
        source.Should().Contain("return Math.Clamp(cap, DefaultAttackDeflectionChanceCap, MaximumDeflectionChanceCap);");
    }

    [Test]
    public void LightsaberDefenseFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.TauntingDeflection1, "ife_tauntdefl1", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.PunishingStrike1, "ife_punstrk1", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.GuardiansChallenge1, "ife_guardchal1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.ImpenetrableGuard1, "ife_impengrd1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardiansChallenge2, "ife_guardchal2", "0x3E", "1", "rectangle", "8", "2.5", "17"),
            (FeatType.GuardianMaster1, "ife_guardmstr1", "0x01", "0", "****", "****", "****", "****")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            spellRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

            spellRow["TargetType"].Should().Be(targetType);
            spellRow["HostileSetting"].Should().Be(hostileSetting);
            spellRow["TargetShape"].Should().Be(targetShape);
            spellRow["TargetSizeX"].Should().Be(targetSizeX);
            spellRow["TargetSizeY"].Should().Be(targetSizeY);
            spellRow["TargetFlags"].Should().Be(targetFlags);
        }
    }

    [Test]
    public void LightsaberDefenseAreaAbilities_DeclareTargetingAndImpactVisuals()
    {
        var challengeAbilities = new GuardiansChallengeAbilityDefinition().BuildAbilities();
        var challenge1 = challengeAbilities[FeatType.GuardiansChallenge1];
        var challenge2 = challengeAbilities[FeatType.GuardiansChallenge2];
        var punishingStrike = new PunishingStrikeAbilityDefinition().BuildAbilities()[FeatType.PunishingStrike1];

        challenge1.Targeting.Should().NotBeNull();
        challenge1.Targeting!.Spell.Should().Be(Spell.GuardiansChallenge1);
        challenge1.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Cone);
        challenge1.Targeting.SizeX.Should().Be(5f);
        challenge1.Targeting.SizeY.Should().Be(5f);
        challenge1.Targeting.Flags.Should().Be(AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

        challenge2.Targeting.Should().NotBeNull();
        challenge2.Targeting!.Spell.Should().Be(Spell.GuardiansChallenge2);
        challenge2.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Rect);
        challenge2.Targeting.SizeX.Should().Be(8f);
        challenge2.Targeting.SizeY.Should().Be(2.5f);
        challenge2.Targeting.Flags.Should().Be(AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

        punishingStrike.Targeting.Should().NotBeNull();
        punishingStrike.Targeting!.Spell.Should().Be(Spell.PunishingStrike1);
        punishingStrike.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
        punishingStrike.Targeting.SizeX.Should().Be(5f);
        punishingStrike.Targeting.SizeY.Should().Be(0f);
        punishingStrike.Targeting.Flags.Should().Be(AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);

        var root = FindSourceRepositoryRoot();
        var challengeSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Lightsaber" / "GuardiansChallengeAbilityDefinition.cs").FullName);
        var punishingSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Lightsaber" / "PunishingStrikeAbilityDefinition.cs").FullName);

        challengeSource.Should().Contain("targetVisualEffect: ChallengeVisualEffect");
        challengeSource.Should().Contain("areaVisualEffect: ChallengeVisualEffect");
        challengeSource.Should().Contain("ChallengeVisualEffect = VisualEffect.Vfx_Fnf_Howl_Odd");
        punishingSource.Should().Contain("Ability.ApplyTelegraphedCombatImpact(");
        punishingSource.Should().Contain("centerOnActivator: true");
        punishingSource.Should().Contain("AreaVisualEffect = VisualEffect.Vfx_Fnf_Swinging_Blade");
        punishingSource.Should().Contain("TargetVisualEffect = VisualEffect.Vfx_Com_Blood_Spark_Medium");
        punishingSource.Should().Contain("targetVisualEffect: TargetVisualEffect");
        punishingSource.Should().Contain("areaVisualEffect: AreaVisualEffect");
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
        perk.Category.Should().Be(PerkCategoryType.LightsaberDefense);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Lightsaber, skillRank);
        AssertCharacterRequirement(perkLevel, CharacterType.ForceSensitive);

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

    private static void AssertCharacterRequirement(PerkLevel level, CharacterType characterType)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementCharacterType>()
            .Should()
            .ContainSingle()
            .Which;

        typeof(PerkRequirementCharacterType)
            .GetField("_requiredCharacterType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(requirement)
            .Should()
            .Be(characterType);
    }

    private static void AssertStatBonus(PerkLevel level, StatType statType, int expectedValue)
    {
        level.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == statType)
            .Which
            .Calculate(0)
            .Should()
            .Be(expectedValue);
    }

    private static Dictionary<PerkType, PerkDetail> BuildLightsaberDefensePerksWithout2daLookup()
    {
        var definition = new LightsaberPerkDefinition();
        var methodNames = new[]
        {
            "DeflectionCounter",
            "DeflectionMastery",
            "DeflectionRiposte",
            "DeflectionTraining",
            "DeflectivePresence",
            "GuardianMaster",
            "GuardiansChallenge",
            "GuardiansInfluence",
            "ImpenetrableGuard",
            "OverwhelmingDefense",
            "PunishingStrike",
            "ReactiveDeflection",
            "TauntingDeflection",
            "GuardiansChallenge"
        };

        foreach (var methodName in methodNames)
        {
            typeof(LightsaberPerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(LightsaberPerkDefinition)
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
