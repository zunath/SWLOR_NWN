using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Spear;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class SpearDamageTests
{
    [Test]
    public void SpearDamagePerkLevels_MatchCombatBible()
    {
        var perks = BuildSpearDamagePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.Flanking], "Flanking", 1, 3, 5, null,
            "Attacks from the side deal +10% damage.",
            StatType.SideAttackDamagePercentAdjustment);
        AssertPerkLevel(perks[PerkType.LateralStrike], "Lateral Strike", 1, 3, 8, null,
            "Attacks from the side restore 2 STM. This can only trigger once every 4 seconds.",
            StatType.SideAttackStaminaRestore,
            StatType.SideAttackStaminaRestoreCooldownSeconds);
        AssertPerkLevel(perks[PerkType.FlankingStance], "Flanking Stance", 1, 3, 12, FeatType.FlankingStance1,
            "While active, attacks from the side deal +20% damage and have +15% accuracy. Your defense and force defense are reduced by 25%.");
        AssertPerkLevel(perks[PerkType.SideAssault], "Side Assault", 1, 2, 15, FeatType.SideAssault1,
            "Your next attack deals +12 DMG. If you are facing the side of your target, this increases to +16 DMG.");
        AssertPerkLevel(perks[PerkType.BreachStrike], "Breach Strike", 1, 4, 18, FeatType.BreachStrike1,
            "Deal weapon DMG + 10. Inflicts Breach, which reduces Evasion and Defense by 20% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.FlankingBarrage], "Flanking Barrage", 1, 3, 20, FeatType.FlankingBarrage1,
            "Deal weapon DMG + 20 from the side to your target and reduce their Attack by 12% for 8 seconds.");
        AssertPerkLevel(perks[PerkType.LateralStrike], "Lateral Strike", 2, 2, 22, null,
            "Attacks from the side restore 6 STM. This can only trigger once every 4 seconds.",
            StatType.SideAttackStaminaRestore,
            StatType.SideAttackStaminaRestoreCooldownSeconds);
        AssertPerkLevel(perks[PerkType.SweepingFlank], "Sweeping Flank", 1, 3, 25, FeatType.SweepingFlank1,
            "Deal weapon DMG + 18 to all enemies within area of effect (cone). Inflicts Exposed, which reduces defense by 15% for 30 seconds.");
        AssertPerkLevel(perks[PerkType.ImprovedAttentiveness], "Improved Attentiveness", 1, 3, 28, FeatType.ImprovedAttentiveness1,
            "Your party members, excluding you, gain +15% physical and Force ability hit chance for 1 minute.");
        AssertPerkLevel(perks[PerkType.SideAssault], "Side Assault", 2, 3, 30, FeatType.SideAssault2,
            "Your next attack deals +25 DMG. If you are facing the side of your target, this increases to +35 DMG.");
        AssertPerkLevel(perks[PerkType.Flanking], "Flanking", 2, 2, 32, null,
            "Attacks from the side have +10% accuracy and +8% critical chance.",
            StatType.SideAttackDamagePercentAdjustment,
            StatType.SideAttackHitChancePercentAdjustment,
            StatType.SideAttackCriticalRatePercentAdjustment);
        AssertPerkLevel(perks[PerkType.OpportunistsFlow], "Opportunist's Flow", 1, 4, 35, null,
            "After dealing damage from a side attack, your next attack's delay is 20% quicker.",
            StatType.SideAttackDelayReductionPercent,
            StatType.SideAttackDelayReductionDurationSeconds);
        AssertPerkLevel(perks[PerkType.RestorationStrike], "Restoration Strike", 1, 3, 38, null,
            "Critical hit chance increases by 10%. Additionally, if you were at the side of your target, critical hits have a 35% chance to restore 15 STM.",
            StatType.CriticalRatePercentAdjustment,
            StatType.CriticalSideAttackStaminaRestoreChance,
            StatType.CriticalSideAttackStaminaRestore);
        AssertPerkLevel(perks[PerkType.HamperingBarrage], "Hampering Barrage", 1, 2, 40, FeatType.HamperingBarrage1,
            "Deal weapon DMG + 30 to all enemies within area of effect (cone). Inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.SideAssault], "Side Assault", 3, 4, 42, FeatType.SideAssault3,
            "Your next attack deals +35 DMG. If you are facing the side of your target, this increases to +50 DMG.");
        AssertPerkLevel(perks[PerkType.CalmingStance], "Calming Stance", 1, 3, 45, FeatType.CalmingStance1,
            "While active, your STM regenerates by 3 every second. Your attack, force attack, defense, and force defense are reduced by 40%.");
        AssertPerkLevel(perks[PerkType.AdaptivePrecisionStrike], "Adaptive Precision Strike", 1, 4, 48, null,
            "Attacks from the side have a 5% chance to bypass 35% of your target's Evasion. This chance increases by 1% per PER. (Maximum 30%)",
            StatType.SideAttackEvasionIgnoreChance,
            StatType.SideAttackEvasionIgnoreChanceScalingAbility,
            StatType.SideAttackEvasionIgnoreChanceMaximum,
            StatType.SideAttackEvasionIgnorePercent);
        AssertPerkLevel(perks[PerkType.CripplingDefense], "Crippling Defense", 1, 4, 50, FeatType.CripplingDefense1,
            "All enemies within area of effect (sphere) around you receive Crippled Defense, reducing physical and Force damage mitigation by 35% for 15 seconds. Additionally restores 25 STM.");
    }

    [Test]
    public void SpearDamageAbilities_MatchCombatBible()
    {
        var flankingStance = new FlankingStanceAbilityDefinition().BuildAbilities()[FeatType.FlankingStance1];
        AssertAbility(flankingStance, "Flanking Stance", 1, RecastGroup.FlankingStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var sideAssault = new SideAssaultAbilityDefinition().BuildAbilities();
        AssertAbility(sideAssault[FeatType.SideAssault1], "Side Assault I", 1, RecastGroup.SideAssault, 12f, 0f, 6, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(sideAssault[FeatType.SideAssault2], "Side Assault II", 2, RecastGroup.SideAssault, 12f, 0f, 12, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(sideAssault[FeatType.SideAssault3], "Side Assault III", 3, RecastGroup.SideAssault, 12f, 0f, 18, true, false, true, false, AbilityActivationType.Weapon);

        var breachStrike = new BreachStrikeAbilityDefinition().BuildAbilities()[FeatType.BreachStrike1];
        AssertAbility(breachStrike, "Breach Strike", 1, RecastGroup.BreachStrike, 45f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var flankingBarrage = new FlankingBarrageAbilityDefinition().BuildAbilities()[FeatType.FlankingBarrage1];
        AssertAbility(flankingBarrage, "Flanking Barrage", 1, RecastGroup.FlankingBarrage, 120f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);
        flankingBarrage.CustomValidation.Should().NotBeNull();

        var sweepingFlank = new SweepingFlankAbilityDefinition().BuildAbilities()[FeatType.SweepingFlank1];
        AssertAbility(sweepingFlank, "Sweeping Flank", 1, RecastGroup.SweepingFlank, 60f, 2f, 10, true, false, false, true, AbilityActivationType.Casted);

        var improvedAttentiveness = new ImprovedAttentivenessAbilityDefinition().BuildAbilities()[FeatType.ImprovedAttentiveness1];
        AssertAbility(improvedAttentiveness, "Improved Attentiveness", 1, RecastGroup.ImprovedAttentiveness, 300f, 2f, 8, false, false, false, false, AbilityActivationType.Casted);

        var hamperingBarrage = new HamperingBarrageAbilityDefinition().BuildAbilities()[FeatType.HamperingBarrage1];
        AssertAbility(hamperingBarrage, "Hampering Barrage", 1, RecastGroup.HamperingBarrage, 60f, 2f, 14, true, false, false, true, AbilityActivationType.Casted);

        var calmingStance = new CalmingStanceAbilityDefinition().BuildAbilities()[FeatType.CalmingStance1];
        AssertAbility(calmingStance, "Calming Stance", 1, RecastGroup.CalmingStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var cripplingDefense = new CripplingDefenseAbilityDefinition().BuildAbilities()[FeatType.CripplingDefense1];
        AssertAbility(cripplingDefense, "Crippling Defense", 1, RecastGroup.Capstone, 1800f, 3f, 25, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void SpearDamageStatusEffects_MatchCombatBible()
    {
        var flankingStance = new FlankingStanceStatusEffect();
        flankingStance.StatGroup.Stats[StatType.SideAttackDamagePercentAdjustment].Should().Be(20);
        flankingStance.StatGroup.Stats[StatType.SideAttackHitChancePercentAdjustment].Should().Be(15);
        flankingStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-25);
        flankingStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-25);
        flankingStance.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);

        var breach = new BreachStatusEffect();
        breach.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-20);
        breach.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-20);

        var flankingBarrage = new FlankingBarrageStatusEffect();
        flankingBarrage.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-12);

        var exposed = new ExposedStatusEffect();
        exposed.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);

        var improvedAttentiveness = new ImprovedAttentivenessStatusEffect();
        improvedAttentiveness.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(15);
        improvedAttentiveness.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(0);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);

        var calming = new CalmingStanceStatusEffect();
        calming.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-40);
        calming.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-40);
        calming.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-40);
        calming.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-40);

        var cripplingDefense = new CripplingDefenseStatusEffect();
        cripplingDefense.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-35);
        cripplingDefense.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-35);
    }

    [Test]
    public void SpearDamageFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.FlankingStance1, "ife_flankstnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SideAssault1, "ife_sideaslt1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SideAssault2, "ife_sideaslt2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SideAssault3, "ife_sideaslt3", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.BreachStrike1, "ife_brchstrk1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FlankingBarrage1, "ife_flankbarr1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.SweepingFlank1, "ife_swpngflnk1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.ImprovedAttentiveness1, "ife_impatten1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.HamperingBarrage1, "ife_hampbarr1", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.CalmingStance1, "ife_calmstnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CripplingDefense1, "ife_cripdef1", "0x01", "1", "sphere", "5", "****", "17")
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
    public void SpearDamageImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();

        var sideAssault = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Spear" / "SideAssaultAbilityDefinition.cs").FullName);
        var normalizedSideAssault = sideAssault.Replace("\r\n", "\n");
        sideAssault.Should().Contain("Combat.IsAttackerBesideTarget(activator, target)");
        normalizedSideAssault.Should().Contain("12,\n                16,\n                6");
        normalizedSideAssault.Should().Contain("25,\n                35,\n                12");
        normalizedSideAssault.Should().Contain("35,\n                50,\n                18");

        var improvedAttentiveness = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Spear" / "ImprovedAttentivenessAbilityDefinition.cs").FullName);
        improvedAttentiveness.Should().Contain("if (partyMember == activator || !GetIsObjectValid(partyMember))");
        improvedAttentiveness.Should().Contain("60f");

        var cripplingDefense = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Spear" / "CripplingDefenseAbilityDefinition.cs").FullName);
        cripplingDefense.Replace("\r\n", "\n").Should().Contain("25,\n                true,\n                restoreStamina: 25");
        cripplingDefense.Should().Contain("restoreStamina: 25");
        cripplingDefense.Should().Contain("activationDelay: 3f");
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
        perk.Category.Should().Be(PerkCategoryType.SpearDamage);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Spear, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Spear);
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

    private static Dictionary<PerkType, PerkDetail> BuildSpearDamagePerksWithout2daLookup()
    {
        var definition = new SpearPerkDefinition();
        var methodNames = new[]
        {
            "AdaptivePrecisionStrike",
            "BreachStrike",
            "CalmingStance",
            "CripplingDefense",
            "FlankingBarrage",
            "Flanking",
            "FlankingStance",
            "HamperingBarrage",
            "ImprovedAttentiveness",
            "LateralStrike",
            "OpportunistFlow",
            "RestorationStrike",
            "SideAssault",
            "SweepingFlank"
        };

        foreach (var methodName in methodNames)
        {
            typeof(SpearPerkDefinition)
                .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(SpearPerkDefinition)
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
