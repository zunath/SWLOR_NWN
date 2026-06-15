using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Pistol;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class PistolGunslingerTests
{
    [Test]
    public void PistolGunslingerPerkLevels_MatchCombatBible()
    {
        var perks = BuildPistolGunslingerPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.QuickDraw], "Quick Draw", 1, 3, 2, FeatType.QuickDraw1,
            "Instantly deals weapon DMG + 12 to your target.");
        AssertPerkLevel(perks[PerkType.DoubleShot], "Double Shot", 1, 3, 8, FeatType.DoubleShot1,
            "Instantly attacks twice, each for weapon DMG + 7.");
        AssertPerkLevel(perks[PerkType.RapidShot], "Rapid Shot", 1, 3, 12, FeatType.RapidShotTrait,
            "Reduces pistol attack delay by 10%.",
            StatType.AttackDelayReductionPercent);
        AssertPerkLevel(perks[PerkType.GunfighterStance], "Gunfighter Stance", 1, 2, 15, FeatType.GunfighterStance1,
            "While active, grants +15% Attack and +10% Haste, but reduces Defense by 15%.");
        AssertPerkLevel(perks[PerkType.QuickDraw], "Quick Draw", 2, 4, 18, FeatType.QuickDraw2,
            "Instantly deals weapon DMG + 24 to your target.");
        AssertPerkLevel(perks[PerkType.DoubleShot], "Double Shot", 2, 3, 20, FeatType.DoubleShot2,
            "Instantly attacks twice, each for weapon DMG + 15.");
        AssertPerkLevel(perks[PerkType.DeadeyeReload], "Deadeye Reload", 1, 2, 22, FeatType.DeadeyeReloadTrait,
            "After using a pistol combat ability, your next auto-attack within 6 seconds deals +10 DMG.",
            StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType,
            StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType,
            StatType.AbilityUsedNextSkillAutoAttackDamageBonus,
            StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds);
        AssertPerkLevel(perks[PerkType.FanTheHammer], "Fan the Hammer", 1, 3, 25, FeatType.FanTheHammer1,
            "Fires at up to 3 enemies in a cone for weapon DMG + 12 each.");
        AssertPerkLevel(perks[PerkType.QuickDraw], "Quick Draw", 3, 3, 28, FeatType.QuickDraw3,
            "Instantly deals weapon DMG + 36 to your target.");
        AssertPerkLevel(perks[PerkType.DoubleShot], "Double Shot", 3, 3, 30, FeatType.DoubleShot3,
            "Instantly attacks twice, each for weapon DMG + 24.");
        AssertPerkLevel(perks[PerkType.RapidShot], "Rapid Shot", 2, 2, 32, null,
            "Reduces pistol attack delay by 20% total.",
            StatType.AttackDelayReductionPercent);
        AssertPerkLevel(perks[PerkType.FanTheHammer], "Fan the Hammer", 2, 4, 35, FeatType.FanTheHammer2,
            "Fires at up to 5 enemies in a cone for weapon DMG + 20 each.");
        AssertPerkLevel(perks[PerkType.HighNoon], "High Noon", 1, 3, 38, FeatType.HighNoonTrait,
            "Your first pistol attack after entering combat gains +30% critical chance and deals +20 DMG.",
            StatType.OpeningAutoAttackSkillType,
            StatType.OpeningAutoAttackCriticalRatePercentAdjustment,
            StatType.OpeningAutoAttackDamageBonus,
            StatType.OpeningAutoAttackIdleSeconds);
        AssertPerkLevel(perks[PerkType.ReloadTempo], "Reload Tempo", 1, 2, 40, FeatType.ReloadTempoTrait,
            "Defeating an enemy restores 10 STM and reduces Quick Draw cooldowns by 15 seconds.",
            StatType.DefeatedEnemyStaminaRestore,
            StatType.DefeatedEnemyRecastReductionGroup,
            StatType.DefeatedEnemyRecastReductionSeconds);
        AssertPerkLevel(perks[PerkType.QuickDraw], "Quick Draw", 4, 4, 42, FeatType.QuickDraw4,
            "Instantly deals weapon DMG + 50. Targets below 30% HP take an additional +20 DMG.");
        AssertPerkLevel(perks[PerkType.GunslingerFocus], "Gunslinger Focus", 1, 3, 45, FeatType.GunslingerFocus1,
            "For 20 seconds, Quick Draw and Double Shot abilities cost 2 less STM and deal +10 DMG.");
        AssertPerkLevel(perks[PerkType.RapidShot], "Rapid Shot", 3, 4, 48, null,
            "Reduces pistol attack delay by 30% total. Pistol auto-attacks have a 10% chance to restore 2 STM.",
            StatType.AttackDelayReductionPercent,
            StatType.AutoAttackStaminaRestoreChance,
            StatType.AutoAttackStaminaRestore);
        AssertPerkLevel(perks[PerkType.DeadMansHand], "Dead Man's Hand", 1, 4, 50, FeatType.DeadMansHand1,
            "Fire five shots at your target and nearby enemies, prioritizing the primary target. Secondary targets cannot be hit more than twice. For 45 seconds, gain +10% physical ability critical chance.");
    }

    [Test]
    public void PistolGunslingerAbilities_MatchCombatBible()
    {
        var quickDraw = new QuickDrawAbilityDefinition().BuildAbilities();
        AssertAbility(quickDraw[FeatType.QuickDraw1], "Quick Draw I", 1, RecastGroup.QuickDraw, 30f, 0f, 3, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(quickDraw[FeatType.QuickDraw2], "Quick Draw II", 2, RecastGroup.QuickDraw, 30f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(quickDraw[FeatType.QuickDraw3], "Quick Draw III", 3, RecastGroup.QuickDraw, 30f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(quickDraw[FeatType.QuickDraw4], "Quick Draw IV", 4, RecastGroup.QuickDraw, 30f, 0f, 10, true, true, true, false, AbilityActivationType.Casted);

        var doubleShot = new DoubleShotAbilityDefinition().BuildAbilities();
        AssertAbility(doubleShot[FeatType.DoubleShot1], "Double Shot I", 1, RecastGroup.DoubleShot, 45f, 0f, 5, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(doubleShot[FeatType.DoubleShot2], "Double Shot II", 2, RecastGroup.DoubleShot, 45f, 0f, 6, true, true, true, false, AbilityActivationType.Casted);
        AssertAbility(doubleShot[FeatType.DoubleShot3], "Double Shot III", 3, RecastGroup.DoubleShot, 45f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var gunfighterStance = new GunfighterStanceAbilityDefinition().BuildAbilities()[FeatType.GunfighterStance1];
        AssertAbility(gunfighterStance, "Gunfighter Stance", 1, RecastGroup.GunfighterStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var fanTheHammer = new FanTheHammerAbilityDefinition().BuildAbilities();
        AssertAbility(fanTheHammer[FeatType.FanTheHammer1], "Fan the Hammer I", 1, RecastGroup.FanTheHammer, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);
        AssertAbility(fanTheHammer[FeatType.FanTheHammer2], "Fan the Hammer II", 2, RecastGroup.FanTheHammer, 75f, 0f, 10, true, false, false, true, AbilityActivationType.Casted);

        var gunslingerFocus = new GunslingerFocusAbilityDefinition().BuildAbilities()[FeatType.GunslingerFocus1];
        AssertAbility(gunslingerFocus, "Gunslinger Focus", 1, RecastGroup.GunslingerFocus, 120f, 0f, 6, false, false, false, false, AbilityActivationType.Casted);

        var deadMansHand = new DeadMansHandAbilityDefinition().BuildAbilities()[FeatType.DeadMansHand1];
        AssertAbility(deadMansHand, "Dead Man's Hand", 1, RecastGroup.Capstone, 345f, 2f, 15, true, true, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void PistolGunslingerStatusEffects_MatchCombatBible()
    {
        var gunfighterStance = new GunfighterStanceStatusEffect();
        gunfighterStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(15);
        gunfighterStance.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(10);
        gunfighterStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-15);
        gunfighterStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-15);

        var gunslingerFocus = new GunslingerFocusStatusEffect();
        gunslingerFocus.StatGroup.Stats[StatType.AbilityDamageFlatAdjustmentPerkType].Should().Be((int)PerkType.QuickDraw);
        gunslingerFocus.StatGroup.Stats[StatType.AbilityDamageFlatAdjustmentSecondaryPerkType].Should().Be((int)PerkType.DoubleShot);
        gunslingerFocus.StatGroup.Stats[StatType.AbilityDamageFlatAdjustment].Should().Be(10);
        gunslingerFocus.StatGroup.Stats[StatType.AbilityStaminaCostFlatAdjustmentPerkType].Should().Be((int)PerkType.QuickDraw);
        gunslingerFocus.StatGroup.Stats[StatType.AbilityStaminaCostFlatAdjustmentSecondaryPerkType].Should().Be((int)PerkType.DoubleShot);
        gunslingerFocus.StatGroup.Stats[StatType.AbilityStaminaCostFlatAdjustment].Should().Be(-2);

        var deadMansHand = new DeadMansHandStatusEffect();
        deadMansHand.StatGroup.Stats[StatType.AbilityCriticalRatePercentAdjustment].Should().Be(10);
    }

    [Test]
    public void PistolGunslingerFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.QuickDraw1, "ife_qckdrw1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DoubleShot1, "ife_dblshot1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.GunfighterStance1, "ife_gunfstnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.QuickDraw2, "ife_qckdrw2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DoubleShot2, "ife_dblshot2", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FanTheHammer1, "ife_fanhamr1", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.QuickDraw3, "ife_qckdrw3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.DoubleShot3, "ife_dblshot3", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.FanTheHammer2, "ife_fanhamr2", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.QuickDraw4, "ife_qckdrw4", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.GunslingerFocus1, "ife_gunsfoc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.DeadMansHand1, "ife_deadmanhand1", "M", "0x02", "1", "sphere", "5", "****", "1")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
        {
            var featRow = featRows[(int)featType];
            var abilityRow = abilityRows[int.Parse(featRow["SPELLID"])];
            var featIcon = featRow["ICON"];

            featIcon.Should().Be(expectedIcon);
            abilityRow["IconResRef"].Should().Be(featIcon);
            seenIcons.Add(featIcon).Should().BeTrue($"{featType} should have a unique icon");
            File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{featIcon}.tga").FullName).Should().BeTrue();

            abilityRow["Range"].Should().Be(range);
            abilityRow["TargetType"].Should().Be(targetType);
            abilityRow["HostileSetting"].Should().Be(hostileSetting);
            abilityRow["TargetShape"].Should().Be(targetShape);
            abilityRow["TargetSizeX"].Should().Be(targetSizeX);
            abilityRow["TargetSizeY"].Should().Be(targetSizeY);
            abilityRow["TargetFlags"].Should().Be(targetFlags);
        }
    }

    [Test]
    public void PistolGunslingerImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();

        var quickDraw = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Pistol" / "QuickDrawAbilityDefinition.cs").FullName);
        quickDraw.Should().Contain("private const float LowHPThreshold = 0.3f;");
        quickDraw.Should().Contain("private const int LowHPDamageBonus = 20;");
        quickDraw.Should().Contain("ApplyQuickDraw(activator, target, targetLocation, 12, false);");
        quickDraw.Should().Contain("ApplyQuickDraw(activator, target, targetLocation, 24, false);");
        quickDraw.Should().Contain("ApplyQuickDraw(activator, target, targetLocation, 36, false);");
        quickDraw.Should().Contain("ApplyQuickDraw(activator, target, targetLocation, 50, true);");
        quickDraw.Should().Contain("GetCurrentHitPoints(target) < GetMaxHitPoints(target) * LowHPThreshold");

        var pistolPerks = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "PistolPerkDefinition.cs").FullName);
        pistolPerks.Should().Contain("StatType.AutoAttackStaminaRestoreChance, creature => EquipmentPredicates.HasPistol(creature) ? 10 : 0");
        pistolPerks.Should().Contain("StatType.AutoAttackStaminaRestore, creature => EquipmentPredicates.HasPistol(creature) ? 2 : 0");

        var doubleShot = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Pistol" / "DoubleShotAbilityDefinition.cs").FullName);
        doubleShot.Should().Contain("private const int HitCount = 2;");
        doubleShot.Should().Contain("ApplyDoubleShot(activator, target, targetLocation, 7);");
        doubleShot.Should().Contain("ApplyDoubleShot(activator, target, targetLocation, 15);");
        doubleShot.Should().Contain("ApplyDoubleShot(activator, target, targetLocation, 24);");

        var fanTheHammer = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Pistol" / "FanTheHammerAbilityDefinition.cs").FullName);
        fanTheHammer.Should().Contain("SkillType.Pistol, 12, 0, null");
        fanTheHammer.Should().Contain("maxTargets: 3");
        fanTheHammer.Should().Contain("SkillType.Pistol, 20, 0, null");
        fanTheHammer.Should().Contain("maxTargets: 5");

        var gunslingerFocus = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "GunslingerFocusStatusEffect.cs").FullName);
        gunslingerFocus.Should().Contain("public const int DamageBonus = 10;");
        gunslingerFocus.Should().Contain("public const int StaminaCostReduction = 2;");
        gunslingerFocus.Should().Contain("StatGroup.Stats[StatType.AbilityDamageFlatAdjustment] = DamageBonus;");
        gunslingerFocus.Should().Contain("StatGroup.Stats[StatType.AbilityStaminaCostFlatAdjustment] = -StaminaCostReduction;");

        var deadMansHand = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Pistol" / "DeadMansHandAbilityDefinition.cs").FullName);
        deadMansHand.Should().Contain("private const int ShotCount = 5;");
        deadMansHand.Should().Contain("private const int SecondaryShotLimit = 2;");
        deadMansHand.Should().Contain("private const float SecondaryRadius = 5f;");
        deadMansHand.Should().Contain("SkillType.Pistol, 10, 0, null");
        deadMansHand.Should().Contain("typeof(DeadMansHandStatusEffect)");
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
        perk.Category.Should().Be(PerkCategoryType.PistolGunslinger);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Pistol, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Pistol);
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

    private static Dictionary<PerkType, PerkDetail> BuildPistolGunslingerPerksWithout2daLookup()
    {
        var definition = new PistolPerkDefinition();
        var methodNames = new[]
        {
            "DeadMansHand",
            "DeadeyeReload",
            "DoubleShot",
            "FanTheHammer",
            "GunfighterStance",
            "GunslingerFocus",
            "HighNoon",
            "QuickDraw",
            "RapidShot",
            "ReloadTempo"
        };

        foreach (var methodName in methodNames)
        {
            typeof(PistolPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(PistolPerkDefinition)
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
