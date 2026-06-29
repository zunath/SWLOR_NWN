using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade;
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

public class HeavyVibrobladeOffenseTests
{
    [Test]
    public void HeavyVibrobladeOffenseAbilities_MatchCombatBible()
    {
        var soulStrike = new SoulStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(soulStrike[FeatType.SoulStrike1], "Soul Strike I", 1, RecastGroup.SoulStrike, 45f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(soulStrike[FeatType.SoulStrike2], "Soul Strike II", 2, RecastGroup.SoulStrike, 45f, 0f, 10, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(soulStrike[FeatType.SoulStrike3], "Soul Strike III", 3, RecastGroup.SoulStrike, 45f, 0f, 15, true, false, true, false, AbilityActivationType.Weapon);

        var sacrificialBlade = new SacrificialBladeAbilityDefinition().BuildAbilities()[FeatType.SacrificialBlade1];
        AssertAbility(sacrificialBlade, "Sacrificial Blade", 1, RecastGroup.SacrificialBlade, 120f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);

        var soulDevourer = new SoulDevourerAbilityDefinition().BuildAbilities()[FeatType.SoulDevourer1];
        AssertAbility(soulDevourer, "Soul Devourer", 1, RecastGroup.SoulDevourer, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var soulBurst = new SoulBurstAbilityDefinition().BuildAbilities()[FeatType.SoulBurst1];
        AssertAbility(soulBurst, "Soul Burst", 1, RecastGroup.SoulBurst, 180f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);

        var soulStorm = new SoulStormAbilityDefinition().BuildAbilities()[FeatType.SoulStorm1];
        AssertAbility(soulStorm, "Soul Storm", 1, RecastGroup.SoulStorm, 300f, 0f, 18, false, false, false, true, AbilityActivationType.Casted);

        var blazingSpikes = new BlazingSpikesAbilityDefinition().BuildAbilities()[FeatType.BlazingSpikes1];
        AssertAbility(blazingSpikes, "Blazing Spikes", 1, null, null, 0f, 8, false, false, false, false, AbilityActivationType.Casted);

    }

    [Test]
    public void HeavyVibrobladeOffenseStatusEffects_MatchCombatBible()
    {
        var essenceDrain = new EssenceDrainStatusEffect();
        essenceDrain.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        essenceDrain.Name.Should().Be("Essence Drain");
        essenceDrain.Icon.Should().Be(EffectIconType.EssenceDrainStatusEffect);
        essenceDrain.Categories.Should().HaveFlag(StatusEffectCategory.Debuff);
        essenceDrain.CleanseTypes.Should().HaveFlag(StatusEffectCleanseType.Purify);
        essenceDrain.CleanseTypes.Should().HaveFlag(StatusEffectCleanseType.SoothePet);
        essenceDrain.ResistanceType.Should().Be(ResistanceType.Trauma);

        var soulDevourer = new SoulDevourerStatusEffect();
        soulDevourer.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(35);
        soulDevourer.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(15);

        var soulSacrifice = new SoulSacrificeStatusEffect();
        soulSacrifice.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(20);
        soulSacrifice.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(10);

        var soulStorm = new SoulStormStatusEffect();
        soulStorm.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(20);
        soulStorm.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);

        var soulAscension = new SoulAscensionStatusEffect();
        soulAscension.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(15);
        soulAscension.StatGroup.Stats[StatType.PhysicalDamageDealtHPPercentRestore].Should().Be(20);
    }

    [Test]
    public void PersistentTogglePerks_RegisterRefundCleanup()
    {
        var blazingSpikes = new BlazingSpikesAbilityDefinition().BuildAbilities()[FeatType.BlazingSpikes1];
        var soulDevourer = new SoulDevourerAbilityDefinition().BuildAbilities()[FeatType.SoulDevourer1];

        blazingSpikes.StatusEffectTypesRemovedOnPerkRefund.Should().ContainSingle().Which.Should().Be(typeof(BlazingSpikesStatusEffect));
        soulDevourer.StatusEffectTypesRemovedOnPerkRefund.Should().ContainSingle().Which.Should().Be(typeof(SoulDevourerStatusEffect));
    }

    [Test]
    public void EssenceHunter_AppliesVisibleDebuffFromActivatedWeaponAbilityTarget()
    {
        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var perks = BuildHeavyVibrobladeOffensePerksWithout2daLookup();
        var essenceHunterPerk = perks[PerkType.EssenceHunter];
        var activatedRiders = ExtractMethod(combatSource, "private static void ApplyAbilityActivatedRiders(");
        var heavyVibrobladeActivated = ExtractMethod(combatSource, "private static void ApplyHeavyVibrobladeActivatedEffects(");
        var essenceHunter = ExtractMethod(combatSource, "private static void ApplyHeavyVibrobladeOffenseActivatedEffects(");
        var statusRiders = ExtractMethod(combatSource, "private static void ApplyAbilityStatusRiders(");

        AssertPerkLevel(
            essenceHunterPerk,
            "Essence Hunter",
            1,
            3,
            12,
            FeatType.EssenceHunterTrait,
            "Heavy Vibroblade Offense weapon abilities also inflict Essence Drain, reducing the target's Attack by 15% for 12 seconds.",
            StatType.HeavyVibrobladeOffenseEssenceHunter,
            StatType.HeavyVibrobladeOffenseEssenceHunterTriggerPrimaryPerkType);
        AssertStatBonus(
            essenceHunterPerk.PerkLevels[1],
            StatType.HeavyVibrobladeOffenseEssenceHunterTriggerPrimaryPerkType,
            (int)PerkType.SoulStrike);

        activatedRiders.Should().Contain("ApplyHeavyVibrobladeActivatedEffects(activator, target, ability);");
        heavyVibrobladeActivated.Should().Contain("ApplyHeavyVibrobladeOffenseActivatedEffects(activator, target, ability);");
        statusRiders.Should().NotContain("ApplyHeavyVibrobladeOffense");

        essenceHunter.Should().Contain("ability.ActivationType != AbilityActivationType.Weapon");
        essenceHunter.Should().Contain("AbilityMatchesAnyPerkTypeStat(");
        essenceHunter.Should().Contain("StatType.HeavyVibrobladeOffenseEssenceHunter");
        essenceHunter.Should().Contain("StatType.HeavyVibrobladeOffenseEssenceHunterTriggerPrimaryPerkType");
        essenceHunter.Should().Contain("StatusEffect.ApplyStatusEffect(activator, target, typeof(EssenceDrainStatusEffect), 12f, CombatDamageType.Physical);");
    }

    [Test]
    public void VampiricFury_MatchesCombatBibleRestoreValues()
    {
        var root = FindRepositoryRoot();
        var perkSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "HeavyVibrobladePerkDefinition.cs").FullName);
        var combatSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var perks = BuildHeavyVibrobladeOffensePerksWithout2daLookup();
        var vampiricFury = perks[PerkType.VampiricFury];

        AssertPerkLevel(
            vampiricFury,
            "Vampiric Fury",
            1,
            3,
            22,
            FeatType.VampiricFuryTrait,
            "Critical hits restore HP equal to 25% of damage dealt, increased by 1 percentage point per MGT to a maximum of 45%. This can trigger once every 6 seconds.",
            StatType.CriticalHPPercentOfDamageRestore,
            StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds);

        perkSource.Should().Contain("Math.Min(45, 25 + Math.Max(0, GetAbilityScore(creature, AbilityType.Might)))");
        perkSource.Should().Contain("StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds");
        perkSource.Should().NotContain("EquipmentPredicates.HasMainHandHeavyVibroblade");
        AssertStatBonus(vampiricFury.PerkLevels[1], StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds, 6);
        combatSource.Should().Contain("TryUseStatTrigger(attacker, StatType.CriticalHPPercentOfDamageRestore, hpRestoreCooldown)");
    }

    [Test]
    public void SoulBarrier_EvaluatesLowHPTriggersAfterHeavyVibrobladeHitPointSpend()
    {
        var root = FindRepositoryRoot();
        var baseSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "HeavyVibroblade" / "HeavyVibrobladeActiveAbilityDefinitionBase.cs").FullName);
        var combatSource = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "Combat.cs").FullName);
        var sacrificeHitPoints = ExtractMethod(baseSource, "protected static void SacrificeHitPoints(uint activator, int basePercent, int minimumPercent)");
        var damageTakenEffects = ExtractMethod(combatSource, "public static void ApplyDamageTakenEffects(uint defender, uint attacker, int damage)");
        var lowHPTriggers = ExtractMethod(combatSource, "public static void ApplyLowHPDamageTakenEffects(");
        const string damageCall = "ApplyEffectToObject(DurationType.Instant, EffectDamage(amount), activator);";
        const string lowHPCall = "Combat.ApplyLowHPDamageTakenEffects(activator, amount);";

        damageTakenEffects.Should().Contain("ApplyLowHPDamageTakenEffects(defender, damage);");
        lowHPTriggers.Should().Contain("ApplyLowHPNoSaveTemporaryHPEffect(defender, damage);");
        sacrificeHitPoints.Should().Contain("AssignCommand(activator, () =>");
        sacrificeHitPoints.Should().Contain(damageCall);
        sacrificeHitPoints.Should().Contain(lowHPCall);
        sacrificeHitPoints.IndexOf(damageCall, StringComparison.Ordinal)
            .Should()
            .BeLessThan(sacrificeHitPoints.IndexOf(lowHPCall, StringComparison.Ordinal));
    }

    [Test]
    public void HeavyVibrobladeOffenseFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.SoulStrike1, "ife_soulstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SacrificialBlade1, "ife_sacblade1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SoulDevourer1, "ife_souldev1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SoulBurst1, "ife_soulburst1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.SoulStrike2, "ife_soulstrk2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SoulStorm1, "ife_soulstrm1", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.BlazingSpikes1, "ife_blazspk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SoulStrike3, "ife_soulstrk3", "0x01", "0", "****", "****", "****", "****"),
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{featIcon}.tga").FullName).Should().BeTrue();

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
        perk.Category.Should().Be(PerkCategoryType.HeavyVibrobladeOffense);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.HeavyVibroblade, skillRank);

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
        RecastGroup? recastGroup,
        float? recastSeconds,
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
        ability.SkillType.Should().Be(SkillType.HeavyVibroblade);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(activationType);
        ability.IsHostileAbility.Should().Be(isHostile);
        ability.RequiresTarget.Should().Be(requiresTarget);
        ability.IsSingleTargetAbility.Should().Be(isSingleTarget);
        ability.IsAreaAbility.Should().Be(isArea);
        ability.BreaksStealth.Should().BeTrue();

        if (recastGroup.HasValue && recastSeconds.HasValue)
        {
            ability.RecastGroup.Should().Be(recastGroup.Value);
            ability.RecastDelay(0).Should().Be(recastSeconds.Value);
        }
        else
        {
            ability.RecastDelay.Should().BeNull();
        }

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

    private static void AssertStatBonus(PerkLevel level, StatType statType, int value)
    {
        level.StatBonuses
            .Should()
            .ContainSingle(x => x.Stat == statType)
            .Which
            .Calculate(0)
            .Should()
            .Be(value);
    }

    private static Dictionary<PerkType, PerkDetail> BuildHeavyVibrobladeOffensePerksWithout2daLookup()
    {
        var definition = new HeavyVibrobladePerkDefinition();
        var methodNames = new[]
        {
            "BlazingSpikes",
            "Bloodlust",
            "EssenceHunter",
            "EssenceTap",
            "LifeSiphon",
            "SacrificialBlade",
            "SoulAmplification",
            "SoulAscension",
            "SoulBarrier",
            "SoulBurst",
            "SoulDevourer",
            "SoulReaping",
            "SoulSacrifice",
            "SoulStorm",
            "SoulStrike",
            "VampiricFury"
        };

        foreach (var methodName in methodNames)
        {
            typeof(HeavyVibrobladePerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(HeavyVibrobladePerkDefinition)
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

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        openBraceIndex.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
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
