using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class HeavyVibrobladeOffenseTests
{
    [Test]
    public void HeavyVibrobladeOffensePerkLevels_MatchCombatBible()
    {
        var perks = BuildHeavyVibrobladeOffensePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.EssenceTap], "Essence Tap", 1, 2, 5, null,
            "When you take damage, gain +8% Attack for 15 seconds.",
            StatType.DamageTakenAttackPercentAdjustment,
            StatType.DamageTakenAttackDurationSeconds);
        AssertPerkLevel(perks[PerkType.SoulStrike], "Soul Strike", 1, 2, 8, FeatType.SoulStrike1,
            "Your next attack deals +15 DMG and heals you for 25% of damage dealt.");
        AssertPerkLevel(perks[PerkType.EssenceHunter], "Essence Hunter", 1, 3, 12, FeatType.EssenceHunter1,
            "Your next attack deals +18 DMG and inflicts Essence Drain, reducing the target's Attack by 15% for 12 seconds.");
        AssertPerkLevel(perks[PerkType.SacrificialBlade], "Sacrificial Blade", 1, 2, 15, FeatType.SacrificialBlade1,
            "Deal weapon DMG + 25 to a single target. Costs 8% max HP.");
        AssertPerkLevel(perks[PerkType.SoulDevourer], "Soul Devourer", 1, 4, 18, FeatType.SoulDevourer1,
            "While active, gain +35% Attack and +15% critical chance, but each attack you make deals 40% of the damage back to you. Damage reduced by 1% per MGT. (Minimum 10%)");
        AssertPerkLevel(perks[PerkType.LifeSiphon], "Life Siphon", 1, 3, 20, null,
            "When below 50% HP, your attacks heal you for 15% of damage dealt.",
            StatType.LowHPDamageDealtHPRestoreThresholdPercent,
            StatType.LowHPDamageDealtHPPercentRestore);
        AssertPerkLevel(perks[PerkType.VampiricFury], "Vampiric Fury", 1, 3, 22, null,
            "Critical hits restore HP equal to 40% of damage dealt. Amount healed increases by 1% per MGT. (Maximum 75%)",
            StatType.CriticalHPPercentOfDamageRestore);
        AssertPerkLevel(perks[PerkType.SoulBurst], "Soul Burst", 1, 3, 25, FeatType.SoulBurst1,
            "Deal weapon DMG + 35 to all enemies within area of effect (cone). Costs 40% HP which is reduced by 1% per MGT. (Minimum 10%)");
        AssertPerkLevel(perks[PerkType.SoulStrike], "Soul Strike", 2, 4, 28, FeatType.SoulStrike2,
            "Your next attack deals +30 DMG and heals you for 40% of damage dealt.");
        AssertPerkLevel(perks[PerkType.SoulAmplification], "Soul Amplification", 1, 3, 30, null,
            "When you recover HP, gain +15% Attack for 15 seconds.",
            StatType.HealingReceivedAttackPercentAdjustment,
            StatType.HealingReceivedAttackDurationSeconds);
        AssertPerkLevel(perks[PerkType.SoulSacrifice], "Soul Sacrifice", 1, 3, 32, FeatType.SoulSacrifice1,
            "Sacrifice 50% max HP to gain +35% Attack and +20% critical chance for 30 seconds. HP sacrificed decreases by 1% per MGT. (Minimum 20%)");
        AssertPerkLevel(perks[PerkType.SoulBarrier], "Soul Barrier", 1, 2, 35, null,
            "When HP drops below 50% of maximum, a temporary shield forms which absorbs damage equal to 25% of max HP for 12 seconds. This can only trigger once every 3 minutes.",
            StatType.LowHPNoSaveTemporaryHPThresholdPercent,
            StatType.LowHPNoSaveTemporaryHPPercent,
            StatType.LowHPNoSaveTemporaryHPDurationSeconds,
            StatType.LowHPNoSaveTemporaryHPCooldownSeconds);
        AssertPerkLevel(perks[PerkType.SoulStorm], "Soul Storm", 1, 3, 38, FeatType.SoulStorm1,
            "Sacrifice 40% HP to increase the damage of all nearby allies within the area of effect (sphere) by 20% for 1 minute. HP sacrificed decreases by 1 percentage point per MGT. (Minimum 10%)");
        AssertPerkLevel(perks[PerkType.BlazingSpikes], "Blazing Spikes", 1, 3, 40, FeatType.BlazingSpikes1,
            "While active, this effect delivers 10% of physical damage received back to the attacker. Damage dealt increases by 1% per MGT. (Maximum 40%)");
        AssertPerkLevel(perks[PerkType.Bloodlust], "Bloodlust", 1, 4, 42, FeatType.Bloodlust1,
            "Sacrifice 40% HP in exchange for 20% of your maximum STM restored. Amount of STM restored increased by 1% per MGT. (Maximum: 80%)");
        AssertPerkLevel(perks[PerkType.SoulStrike], "Soul Strike", 3, 3, 45, FeatType.SoulStrike3,
            "Your next attack deals +45 DMG and heals you for 60% of damage dealt. Amount healed increased by 1% per MGT. (Maximum 90%)");
        AssertPerkLevel(perks[PerkType.SoulReaping], "Soul Reaping", 1, 4, 48, null,
            "Defeating an enemy restores 15% max HP and grants +20% Attack for 30 seconds.",
            StatType.DefeatedEnemyHPPercentRestore,
            StatType.DefeatedEnemyAttackPercentAdjustment,
            StatType.DefeatedEnemyAttackDurationSeconds);
        AssertPerkLevel(perks[PerkType.SoulAscension], "Soul Ascension", 1, 4, 50, FeatType.SoulAscension1,
            "For 45 seconds, gain +15% Attack and heal for 20% of physical damage dealt.");
    }

    [Test]
    public void HeavyVibrobladeOffenseAbilities_MatchCombatBible()
    {
        var soulStrike = new SoulStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(soulStrike[FeatType.SoulStrike1], "Soul Strike I", 1, RecastGroup.SoulStrike, 45f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(soulStrike[FeatType.SoulStrike2], "Soul Strike II", 2, RecastGroup.SoulStrike, 45f, 0f, 10, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(soulStrike[FeatType.SoulStrike3], "Soul Strike III", 3, RecastGroup.SoulStrike, 45f, 0f, 15, true, false, true, false, AbilityActivationType.Weapon);

        var essenceHunter = new EssenceHunterAbilityDefinition().BuildAbilities()[FeatType.EssenceHunter1];
        AssertAbility(essenceHunter, "Essence Hunter", 1, RecastGroup.EssenceHunter, 45f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);

        var sacrificialBlade = new SacrificialBladeAbilityDefinition().BuildAbilities()[FeatType.SacrificialBlade1];
        AssertAbility(sacrificialBlade, "Sacrificial Blade", 1, RecastGroup.SacrificialBlade, 120f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);

        var soulDevourer = new SoulDevourerAbilityDefinition().BuildAbilities()[FeatType.SoulDevourer1];
        AssertAbility(soulDevourer, "Soul Devourer", 1, RecastGroup.SoulDevourer, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var soulBurst = new SoulBurstAbilityDefinition().BuildAbilities()[FeatType.SoulBurst1];
        AssertAbility(soulBurst, "Soul Burst", 1, RecastGroup.SoulBurst, 180f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);

        var soulSacrifice = new SoulSacrificeAbilityDefinition().BuildAbilities()[FeatType.SoulSacrifice1];
        AssertAbility(soulSacrifice, "Soul Sacrifice", 1, RecastGroup.SoulSacrifice, 180f, 0f, 12, false, false, false, false, AbilityActivationType.Casted);

        var soulStorm = new SoulStormAbilityDefinition().BuildAbilities()[FeatType.SoulStorm1];
        AssertAbility(soulStorm, "Soul Storm", 1, RecastGroup.SoulStorm, 300f, 0f, 18, false, false, false, true, AbilityActivationType.Casted);

        var blazingSpikes = new BlazingSpikesAbilityDefinition().BuildAbilities()[FeatType.BlazingSpikes1];
        AssertAbility(blazingSpikes, "Blazing Spikes", 1, null, null, 0f, 8, false, false, false, false, AbilityActivationType.Casted);

        var bloodlust = new BloodlustAbilityDefinition().BuildAbilities()[FeatType.Bloodlust1];
        AssertAbility(bloodlust, "Bloodlust", 1, RecastGroup.Bloodlust, 180f, 0f, null, false, false, false, false, AbilityActivationType.Casted);

        var soulAscension = new SoulAscensionAbilityDefinition().BuildAbilities()[FeatType.SoulAscension1];
        AssertAbility(soulAscension, "Soul Ascension", 1, RecastGroup.Capstone, 345f, 0f, 15, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void HeavyVibrobladeOffenseStatusEffects_MatchCombatBible()
    {
        var essenceDrain = new EssenceDrainStatusEffect();
        essenceDrain.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);

        var soulDevourer = new SoulDevourerStatusEffect();
        soulDevourer.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(35);
        soulDevourer.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(15);

        var soulSacrifice = new SoulSacrificeStatusEffect();
        soulSacrifice.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(35);
        soulSacrifice.StatGroup.Stats[StatType.CriticalRatePercentAdjustment].Should().Be(20);

        var soulStorm = new SoulStormStatusEffect();
        soulStorm.StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(20);
        soulStorm.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(0);

        var soulAscension = new SoulAscensionStatusEffect();
        soulAscension.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(15);
        soulAscension.StatGroup.Stats[StatType.PhysicalDamageDealtHPPercentRestore].Should().Be(20);
    }

    [Test]
    public void HeavyVibrobladeOffenseFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.SoulStrike1, "ife_soulstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.EssenceHunter1, "ife_esshunt1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SacrificialBlade1, "ife_sacblade1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SoulDevourer1, "ife_souldev1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SoulBurst1, "ife_soulburst1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.SoulStrike2, "ife_soulstrk2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SoulSacrifice1, "ife_soulsac1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SoulStorm1, "ife_soulstrm1", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.BlazingSpikes1, "ife_blazspk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Bloodlust1, "ife_blood1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SoulStrike3, "ife_soulstrk3", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SoulAscension1, "ife_soulasc1", "0x01", "0", "****", "****", "****", "****")
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
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be(targetShape);
            abilityRow["TargetSizeX"].Should().Be(targetSizeX);
            abilityRow["TargetSizeY"].Should().Be(targetSizeY);
            abilityRow["TargetFlags"].Should().Be(targetFlags);
        }
    }

    [Test]
    public void HeavyVibrobladeOffenseImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();

        var bloodlust = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "HeavyVibroblade" / "BloodlustAbilityDefinition.cs").FullName);
        bloodlust.Should().Contain("SacrificeHitPoints(activator, 40);");
        bloodlust.Should().NotContain("SacrificeHitPoints(activator, 40, 10)");
        bloodlust.Should().Contain("Math.Min(80, 20 + Math.Max(0, GetAbilityScore(activator, AbilityType.Might)))");

        var soulBurst = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "HeavyVibroblade" / "SoulBurstAbilityDefinition.cs").FullName);
        soulBurst.Should().Contain("SacrificeHitPoints(activator, 40, 10);");

        var soulSacrifice = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "HeavyVibroblade" / "SoulSacrificeAbilityDefinition.cs").FullName);
        soulSacrifice.Should().Contain("SacrificeHitPoints(activator, 50, 20);");

        var soulDevourer = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "SoulDevourerStatusEffect.cs").FullName);
        soulDevourer.Should().Contain("Math.Max(10, 40 - Math.Max(0, GetAbilityScore(attacker, AbilityType.Might)))");

        var blazingSpikes = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "BlazingSpikesStatusEffect.cs").FullName);
        blazingSpikes.Should().Contain("Math.Min(40, 10 + Math.Max(0, GetAbilityScore(defender, AbilityType.Might)))");
        blazingSpikes.Should().Contain("Math.Floor(damage * (percent / 100f))");
        blazingSpikes.Should().Contain("if (reflectedDamage <= 0)");
        blazingSpikes.Should().NotContain("PercentOfDamage(damage, percent)");
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
