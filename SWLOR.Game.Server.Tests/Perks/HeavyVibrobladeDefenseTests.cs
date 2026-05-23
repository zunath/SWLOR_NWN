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

public class HeavyVibrobladeDefenseTests
{
    [Test]
    public void HeavyVibrobladeDefensePerkLevels_MatchCombatBible()
    {
        var perks = BuildHeavyVibrobladeDefensePerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.FortressStrike], "Fortress Strike", 1, 2, 2, FeatType.FortressStrike1,
            "Your next attack deals weapon DMG + 10 and generates extra enmity. You gain +10% Physical Defense for 16 seconds.");
        AssertPerkLevel(perks[PerkType.AngerStrike], "Anger Strike", 1, 2, 8, FeatType.AngerStrike1,
            "Your next attack deals +12 DMG and generates extra enmity.");
        AssertPerkLevel(perks[PerkType.BastionStance], "Bastion Stance", 1, 3, 12, FeatType.BastionStance1,
            "While active, grants +20% to Enmity generation, +15% Defense, +15% Force Defense, -20% Attack, and -20% Force Attack");
        AssertPerkLevel(perks[PerkType.CrushingBlow], "Crushing Blow", 1, 2, 15, FeatType.CrushingBlow1,
            "Deal weapon DMG + 20 and generate significant enmity. Reduces the target's Defense by 15% for 16 seconds.");
        AssertPerkLevel(perks[PerkType.Flash], "Flash", 1, 4, 18, FeatType.Flash1,
            "Enemies within the area of effect (sphere) around you receive the Flash effect, reducing physical and Force ability hit chance by 20% for 30 seconds. You generate significant enmity toward these enemies.");
        AssertPerkLevel(perks[PerkType.LastStand], "Last Stand", 1, 3, 20, null,
            "When reduced below 25% HP, gain a damage shield equal to 20% of maximum HP for 12 seconds. This can only trigger once per 10 minutes.",
            StatType.LowHPTemporaryHPThresholdPercent,
            StatType.LowHPTemporaryHPPercent,
            StatType.LowHPTemporaryHPDurationSeconds,
            StatType.LowHPTemporaryHPCooldownSeconds);
        AssertPerkLevel(perks[PerkType.UnbreakableWill], "Unbreakable Will", 1, 3, 22, null,
            "Grants +5% Attack Deflection. When attacks are deflected, you restore 10% of maximum STM. Deflection increases by 0.5% per MGT. (Maximum: 20%)",
            StatType.AttackDeflection,
            StatType.DeflectionStaminaRestorePercent);
        AssertPerkLevel(perks[PerkType.FortressStrike], "Fortress Strike", 2, 2, 25, FeatType.FortressStrike2,
            "Your next attack deals weapon DMG + 20 and generates extra enmity. You gain +20% Physical Defense for 16 seconds.");
        AssertPerkLevel(perks[PerkType.GuardiansResolve], "Guardian's Resolve", 1, 4, 28, FeatType.GuardiansResolve1,
            "Gain a damage absorption shield equal to 30% of your max HP for 30 seconds. While active, heal for 25% of damage absorbed.");
        AssertPerkLevel(perks[PerkType.DefensiveHarmony], "Defensive Harmony", 1, 3, 30, null,
            "HP restoration used on you is 20% more effective. 10% chance to restore 8 STM when healed. Chance increases by 1% per MGT. (Maximum 40%)",
            StatType.HealingReceivedPercentAdjustment,
            StatType.HealingReceivedStaminaRestoreChance,
            StatType.HealingReceivedStaminaRestoreChanceScalingAbility,
            StatType.HealingReceivedStaminaRestoreChanceMaximum,
            StatType.HealingReceivedStaminaRestore);
        AssertPerkLevel(perks[PerkType.Rampart], "Rampart", 1, 4, 32, FeatType.Rampart1,
            "All allies within the area of effect (sphere) take 15% less physical damage for 1 minute.");
        AssertPerkLevel(perks[PerkType.Earthshatter], "Earthshatter", 1, 3, 35, FeatType.Earthshatter1,
            "You deal weapon DMG + 20 to all enemies within the area of effect (line) from you. Inflicts Force Disruption on each target which disables the use of force abilities for 12 seconds.");
        AssertPerkLevel(perks[PerkType.EdgeOfDarkness], "Edge of Darkness", 1, 3, 38, FeatType.EdgeOfDarkness1,
            "You deal weapon DMG + 15 to all enemies within the area of effect (sphere) from you and generate extra enmity.");
        AssertPerkLevel(perks[PerkType.CriticalWard], "Critical Ward", 1, 2, 40, null,
            "If you would receive a critical hit, downgrade the attack to a normal hit. The attack will do minimum damage to you.",
            StatType.IncomingCriticalHitDowngradeToMinimumDamage);
        AssertPerkLevel(perks[PerkType.FortressStrike], "Fortress Strike", 3, 4, 42, FeatType.FortressStrike3,
            "Your next attack deals weapon DMG + 30 and generates extra enmity. You gain +30% Physical Defense for 16 seconds.");
        AssertPerkLevel(perks[PerkType.BloodWeapon], "Blood Weapon", 1, 3, 45, FeatType.BloodWeapon1,
            "For 20 seconds, 2% of the combat damage you deal is restored to your HP.");
        AssertPerkLevel(perks[PerkType.GuardiansReaping], "Guardian's Reaping", 1, 4, 48, null,
            "Defeating an enemy restores 20% max HP to you and grants +15% Physical Defense to all nearby allies for 25 seconds.",
            StatType.DefeatedEnemyHPPercentRestore,
            StatType.DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment,
            StatType.DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds);
        AssertPerkLevel(perks[PerkType.AbsoluteDefense], "Absolute Defense", 1, 4, 50, FeatType.AbsoluteDefense1,
            "For 45 seconds, nearby party members including you take 15% less physical and Force damage and are immune to Knockdown and Daze.");
    }

    [Test]
    public void HeavyVibrobladeDefenseAbilities_MatchCombatBible()
    {
        var fortressStrike = new FortressStrikeAbilityDefinition().BuildAbilities();
        AssertAbility(fortressStrike[FeatType.FortressStrike1], "Fortress Strike I", 1, RecastGroup.FortressStrike, 30f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(fortressStrike[FeatType.FortressStrike2], "Fortress Strike II", 2, RecastGroup.FortressStrike, 30f, 0f, 7, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(fortressStrike[FeatType.FortressStrike3], "Fortress Strike III", 3, RecastGroup.FortressStrike, 30f, 0f, 10, true, false, true, false, AbilityActivationType.Weapon);

        var angerStrike = new AngerStrikeAbilityDefinition().BuildAbilities()[FeatType.AngerStrike1];
        AssertAbility(angerStrike, "Anger Strike", 1, RecastGroup.AngerStrike, 45f, 0f, 4, true, false, true, false, AbilityActivationType.Weapon);

        var bastionStance = new BastionStanceAbilityDefinition().BuildAbilities()[FeatType.BastionStance1];
        AssertAbility(bastionStance, "Bastion Stance", 1, RecastGroup.BastionStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var crushingBlow = new CrushingBlowAbilityDefinition().BuildAbilities()[FeatType.CrushingBlow1];
        AssertAbility(crushingBlow, "Crushing Blow", 1, RecastGroup.CrushingBlow, 120f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);

        var flash = new FlashAbilityDefinition().BuildAbilities()[FeatType.Flash1];
        AssertAbility(flash, "Flash", 1, RecastGroup.Flash, 90f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var guardiansResolve = new GuardiansResolveAbilityDefinition().BuildAbilities()[FeatType.GuardiansResolve1];
        AssertAbility(guardiansResolve, "Guardian's Resolve", 1, RecastGroup.GuardiansResolve, 90f, 0f, 10, false, false, false, false, AbilityActivationType.Casted);

        var rampart = new RampartAbilityDefinition().BuildAbilities()[FeatType.Rampart1];
        AssertAbility(rampart, "Rampart", 1, RecastGroup.Rampart, 180f, 0f, 12, false, false, false, true, AbilityActivationType.Casted);

        var earthshatter = new EarthshatterAbilityDefinition().BuildAbilities()[FeatType.Earthshatter1];
        AssertAbility(earthshatter, "Earthshatter", 1, RecastGroup.Earthshatter, 90f, 0f, 12, true, false, false, true, AbilityActivationType.Casted);

        var edgeOfDarkness = new EdgeOfDarknessAbilityDefinition().BuildAbilities()[FeatType.EdgeOfDarkness1];
        AssertAbility(edgeOfDarkness, "Edge of Darkness", 1, RecastGroup.EdgeOfDarkness, 300f, 0f, 16, true, false, false, true, AbilityActivationType.Casted);

        var bloodWeapon = new BloodWeaponAbilityDefinition().BuildAbilities()[FeatType.BloodWeapon1];
        AssertAbility(bloodWeapon, "Blood Weapon", 1, RecastGroup.BloodWeapon, 120f, 0f, 14, false, false, false, false, AbilityActivationType.Casted);

        var absoluteDefense = new AbsoluteDefenseAbilityDefinition().BuildAbilities()[FeatType.AbsoluteDefense1];
        AssertAbility(absoluteDefense, "Absolute Defense", 1, RecastGroup.Capstone, 345f, 0f, 15, false, false, false, false, AbilityActivationType.Casted);
    }

    [Test]
    public void HeavyVibrobladeDefenseStatusEffects_MatchCombatBible()
    {
        var bastion = new BastionStanceStatusEffect();
        bastion.StatGroup.Stats[StatType.EnmityPercentAdjustment].Should().Be(20);
        bastion.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(15);
        bastion.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(15);
        bastion.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-20);
        bastion.StatGroup.Stats[StatType.ForceAttackPercentAdjustment].Should().Be(-20);

        var crushingBlow = new CrushingBlowStatusEffect();
        crushingBlow.StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);

        var flash = new FlashStatusEffect(20);
        flash.StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(-20);
        flash.StatGroup.Stats[StatType.AbilityHitChancePercentAdjustment].Should().Be(0);

        var fortress1 = new FortressStrikeStatusEffect(10);
        fortress1.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(10);
        var fortress2 = new FortressStrikeStatusEffect(20);
        fortress2.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(20);
        var fortress3 = new FortressStrikeStatusEffect(30);
        fortress3.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(30);

        var rampart = new RampartStatusEffect();
        rampart.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-15);

        var absoluteDefense = new AbsoluteDefenseStatusEffect();
        absoluteDefense.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-15);
        absoluteDefense.StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment].Should().Be(-15);
        absoluteDefense.StatGroup.Stats[StatType.MindResistance].Should().Be(0);
        absoluteDefense.StatGroup.Stats[StatType.MobilityResistance].Should().Be(0);
    }

    [Test]
    public void HeavyVibrobladeDefenseFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.FortressStrike1, "ife_fortstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.AngerStrike1, "ife_angrstrk1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.BastionStance1, "ife_baststnc1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CrushingBlow1, "ife_crushblow1", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.Flash1, "ife_flash1", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.FortressStrike2, "ife_fortstrk2", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.GuardiansResolve1, "ife_guardres1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.Rampart1, "ife_ramp1", "0x01", "0", "sphere", "5", "****", "17"),
            (FeatType.Earthshatter1, "ife_earth1", "0x3E", "1", "rectangle", "8", "2.5", "17"),
            (FeatType.EdgeOfDarkness1, "ife_edgedark1", "0x01", "1", "sphere", "5", "****", "17"),
            (FeatType.FortressStrike3, "ife_fortstrk3", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.BloodWeapon1, "ife_bldwpn1", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.AbsoluteDefense1, "ife_absdef1", "0x01", "0", "****", "****", "****", "****")
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
    public void HeavyVibrobladeDefenseImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();

        var edgeOfDarkness = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "HeavyVibroblade" / "EdgeOfDarknessAbilityDefinition.cs").FullName);
        edgeOfDarkness.Should().Contain("CombatImpactAreaShape.Sphere");
        edgeOfDarkness.Should().Contain("centerOnActivator: true");
        edgeOfDarkness.Should().Contain("enmityBonus: 350");

        var absoluteDefense = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "HeavyVibroblade" / "AbsoluteDefenseAbilityDefinition.cs").FullName);
        absoluteDefense.Should().Contain("if (!GetIsObjectValid(partyMember))");
        absoluteDefense.Should().Contain("Ability.ApplyTemporaryImmunity(partyMember, CapstoneAbility.ActiveDurationSeconds, ImmunityType.Knockdown)");
        absoluteDefense.Should().Contain("Ability.ApplyTemporaryImmunity(partyMember, CapstoneAbility.ActiveDurationSeconds, ImmunityType.Dazed)");

        var perkDefinition = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "HeavyVibrobladePerkDefinition.cs").FullName)
            .ReplaceLineEndings("\n");
        perkDefinition.Should().Contain("StatType.AttackDeflection,\n                    creature => Math.Min(20, 5 + Math.Max(0, GetAbilityScore(creature, AbilityType.Might)) / 2)");
        perkDefinition.Should().Contain("StatType.DeflectionStaminaRestorePercent,\n                    10)");
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
        perk.Category.Should().Be(PerkCategoryType.HeavyVibrobladeDefense);

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
        ability.SkillType.Should().Be(SkillType.HeavyVibroblade);
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

    private static Dictionary<PerkType, PerkDetail> BuildHeavyVibrobladeDefensePerksWithout2daLookup()
    {
        var definition = new HeavyVibrobladePerkDefinition();
        var methodNames = new[]
        {
            "AbsoluteDefense",
            "AngerStrike",
            "BastionStance",
            "BloodWeapon",
            "CriticalWard",
            "CrushingBlow",
            "DefensiveHarmony",
            "Earthshatter",
            "EdgeOfDarkness",
            "Flash",
            "FortressStrike",
            "GuardiansReaping",
            "GuardiansResolve",
            "LastStand",
            "Rampart",
            "UnbreakableWill"
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
