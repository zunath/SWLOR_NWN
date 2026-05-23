using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Katar;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class KatarVenomCurrentTests
{
    [Test]
    public void KatarVenomCurrentPerkLevels_MatchCombatBible()
    {
        var perks = BuildKatarVenomCurrentPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.StrikingCobra], "Striking Cobra", 1, 3, 2, FeatType.StrikingCobra1,
            "Your next attack deals weapon DMG + 8 and inflicts Poison for 30 seconds.");
        AssertPerkLevel(perks[PerkType.StaticPalm], "Static Palm", 1, 3, 8, FeatType.StaticPalm1,
            "Your next attack deals weapon DMG + 8 and inflicts Disoriented for 8 seconds.");
        AssertPerkLevel(perks[PerkType.VenomRhythm], "Venom Rhythm", 1, 3, 12, null,
            "Attacks against poisoned targets have a 15% chance to deal +6 DMG.",
            StatType.DamageToPoisonedTargetFlatBonusChance,
            StatType.DamageToPoisonedTargetFlatBonus);
        AssertPerkLevel(perks[PerkType.CobraStance], "Cobra Stance", 1, 2, 15, FeatType.CobraStance1,
            "While active, attacks have a 10% chance to inflict Poison for 30 seconds and you gain +10% Attack, but Defense is reduced by 15%.");
        AssertPerkLevel(perks[PerkType.StrikingCobra], "Striking Cobra", 2, 4, 18, FeatType.StrikingCobra2,
            "Your next attack deals weapon DMG + 18 and inflicts Poison for 60 seconds.");
        AssertPerkLevel(perks[PerkType.StaticPalm], "Static Palm", 2, 3, 20, FeatType.StaticPalm2,
            "Your next attack deals weapon DMG + 18 and inflicts Disoriented for 12 seconds.");
        AssertPerkLevel(perks[PerkType.ToxicTempo], "Toxic Tempo", 1, 2, 22, null,
            "Katar abilities deal +8% damage to targets affected by Poison or Disoriented.",
            StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment);
        AssertPerkLevel(perks[PerkType.TwinFangFlurry], "Twin Fang Flurry", 1, 3, 25, FeatType.TwinFangFlurry1,
            "Strike twice for weapon DMG + 10 each. If the target is poisoned, the second strike inflicts Bleed for 30 seconds.");
        AssertPerkLevel(perks[PerkType.VenomSplash], "Venom Splash", 1, 3, 28, FeatType.VenomSplash1,
            "Deals weapon DMG + 18 to enemies in a cone and inflicts Poison for 30 seconds.");
        AssertPerkLevel(perks[PerkType.NeuralShock], "Neural Shock", 1, 3, 30, FeatType.NeuralShock1,
            "Deals weapon DMG + 20. If the target is Disoriented, they become Dazed for 3 seconds.");
        AssertPerkLevel(perks[PerkType.CobraReflexes], "Cobra Reflexes", 1, 2, 32, null,
            "Critical hits against poisoned targets restore 4 STM.",
            StatType.CriticalPoisonedTargetStaminaRestore);
        AssertPerkLevel(perks[PerkType.StrikingCobra], "Striking Cobra", 3, 4, 35, FeatType.StrikingCobra3,
            "Your next attack deals weapon DMG + 28 and inflicts Poison for 60 seconds.");
        AssertPerkLevel(perks[PerkType.StaticPalm], "Static Palm", 3, 3, 38, FeatType.StaticPalm3,
            "Your next attack deals weapon DMG + 28 and inflicts Disoriented for 15 seconds. Poisoned targets also become Dazed for 3 seconds.");
        AssertPerkLevel(perks[PerkType.SpreadingVenom], "Spreading Venom", 1, 2, 40, null,
            "When a poisoned target dies, the nearest enemy within 5 meters becomes poisoned for 30 seconds.",
            StatType.PoisonedDefeatedEnemySpreadRadiusMeters,
            StatType.PoisonedDefeatedEnemySpreadDurationSeconds);
        AssertPerkLevel(perks[PerkType.CurrentOverload], "Current Overload", 1, 4, 42, FeatType.CurrentOverload1,
            "Deals weapon DMG + 35. If the target is Poisoned or Disoriented, consume one effect to deal +25 DMG and inflict Stunned for 3 seconds.");
        AssertPerkLevel(perks[PerkType.ToxicRush], "Toxic Rush", 1, 3, 45, FeatType.ToxicRush1,
            "Gain +20% Haste and +15% Attack for 20 seconds. Attacks against poisoned targets restore 2 STM during this effect.");
        AssertPerkLevel(perks[PerkType.NeurotoxinMastery], "Neurotoxin Mastery", 1, 4, 48, null,
            "Poison effects you apply also reduce the target's Attack by 10%.",
            StatType.OutgoingPoisonAttackPercentAdjustment);
        AssertPerkLevel(perks[PerkType.SerpentsEclipse], "Serpent's Eclipse", 1, 4, 50, FeatType.SerpentsEclipse1,
            "All enemies in an area of effect (sphere) take weapon DMG + 20 poison damage and suffer Poison and Disoriented for 45 seconds. Enemies already affected by either effect take +15 DMG.");
    }

    [Test]
    public void KatarVenomCurrentAbilities_MatchCombatBible()
    {
        var strikingCobra = new StrikingCobraAbilityDefinition().BuildAbilities();
        AssertAbility(strikingCobra[FeatType.StrikingCobra1], "Striking Cobra I", 1, RecastGroup.StrikingCobra, 30f, 0f, 3, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(strikingCobra[FeatType.StrikingCobra2], "Striking Cobra II", 2, RecastGroup.StrikingCobra, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(strikingCobra[FeatType.StrikingCobra3], "Striking Cobra III", 3, RecastGroup.StrikingCobra, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var staticPalm = new StaticPalmAbilityDefinition().BuildAbilities();
        AssertAbility(staticPalm[FeatType.StaticPalm1], "Static Palm I", 1, RecastGroup.StaticPalm, 30f, 0f, 3, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(staticPalm[FeatType.StaticPalm2], "Static Palm II", 2, RecastGroup.StaticPalm, 30f, 0f, 5, true, false, true, false, AbilityActivationType.Weapon);
        AssertAbility(staticPalm[FeatType.StaticPalm3], "Static Palm III", 3, RecastGroup.StaticPalm, 30f, 0f, 8, true, false, true, false, AbilityActivationType.Weapon);

        var cobraStance = new CobraStanceAbilityDefinition().BuildAbilities()[FeatType.CobraStance1];
        AssertAbility(cobraStance, "Cobra Stance", 1, RecastGroup.CobraStance, 180f, 2f, null, false, false, false, false, AbilityActivationType.Casted);

        var twinFangFlurry = new TwinFangFlurryAbilityDefinition().BuildAbilities()[FeatType.TwinFangFlurry1];
        AssertAbility(twinFangFlurry, "Twin Fang Flurry", 1, RecastGroup.TwinFangFlurry, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var venomSplash = new VenomSplashAbilityDefinition().BuildAbilities()[FeatType.VenomSplash1];
        AssertAbility(venomSplash, "Venom Splash", 1, RecastGroup.VenomSplash, 60f, 0f, 8, true, false, false, true, AbilityActivationType.Casted);

        var neuralShock = new NeuralShockAbilityDefinition().BuildAbilities()[FeatType.NeuralShock1];
        AssertAbility(neuralShock, "Neural Shock", 1, RecastGroup.NeuralShock, 60f, 0f, 8, true, true, true, false, AbilityActivationType.Casted);

        var currentOverload = new CurrentOverloadAbilityDefinition().BuildAbilities()[FeatType.CurrentOverload1];
        AssertAbility(currentOverload, "Current Overload", 1, RecastGroup.CurrentOverload, 90f, 0f, 12, true, true, true, false, AbilityActivationType.Casted);

        var toxicRush = new ToxicRushAbilityDefinition().BuildAbilities()[FeatType.ToxicRush1];
        AssertAbility(toxicRush, "Toxic Rush", 1, RecastGroup.ToxicRush, 120f, 0f, 10, false, false, false, false, AbilityActivationType.Casted);

        var serpentsEclipse = new SerpentsEclipseAbilityDefinition().BuildAbilities()[FeatType.SerpentsEclipse1];
        AssertAbility(serpentsEclipse, "Serpent's Eclipse", 1, RecastGroup.Capstone, 345f, 2f, 15, true, false, false, true, AbilityActivationType.Casted);
    }

    [Test]
    public void KatarVenomCurrentStatusEffects_MatchCombatBible()
    {
        var cobraStance = new CobraStanceStatusEffect();
        cobraStance.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(10);
        cobraStance.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(-15);
        cobraStance.StatGroup.Stats[StatType.ForceDefensePercentAdjustment].Should().Be(-15);

        var toxicRush = new ToxicRushStatusEffect();
        toxicRush.StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(20);
        toxicRush.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(15);

        var disoriented = new DisorientedStatusEffect();
        disoriented.StatGroup.Stats[StatType.AccuracyPercentAdjustment].Should().Be(-15);
        disoriented.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-15);
    }

    [Test]
    public void KatarVenomCurrentFeatAndAbilityIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var abilityRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.StrikingCobra1, "ife_strkngcobra1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StrikingCobra2, "ife_strkngcobra2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StrikingCobra3, "ife_strkngcobra3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StaticPalm1, "ife_statpalm1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StaticPalm2, "ife_statpalm2", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.StaticPalm3, "ife_statpalm3", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.CobraStance1, "ife_cobrastnc1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.TwinFangFlurry1, "ife_twinfangflu1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.VenomSplash1, "ife_venspl1", "M", "0x3E", "1", "cone", "5", "5", "17"),
            (FeatType.NeuralShock1, "ife_neuralshok1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.CurrentOverload1, "ife_currovld1", "M", "0x02", "1", "****", "****", "****", "****"),
            (FeatType.ToxicRush1, "ife_toxrush1", "P", "0x01", "0", "****", "****", "****", "****"),
            (FeatType.SerpentsEclipse1, "ife_serpecl1", "M", "0x3E", "1", "sphere", "5", "****", "1")
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
    public void KatarVenomCurrentImplementationDetails_MatchCombatBible()
    {
        var root = FindRepositoryRoot();

        var staticPalm = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Katar" / "StaticPalmAbilityDefinition.cs").FullName);
        staticPalm.Should().Contain("var appliesDazed = StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect));");
        staticPalm.Should().Contain("StatusEffect.ApplyStatusEffect(activator, hitTarget, typeof(DazedStatusEffect), 3f, CombatDamageType.Electrical);");

        var twinFangFlurry = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Katar" / "TwinFangFlurryAbilityDefinition.cs").FullName);
        twinFangFlurry.Should().Contain("Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 10, 0, null, false);");
        twinFangFlurry.Should().Contain("StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect)) ? typeof(BleedStatusEffect) : null");

        var currentOverload = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Katar" / "CurrentOverloadAbilityDefinition.cs").FullName);
        currentOverload.Should().Contain("var damage = consumedStatus == null ? 35 : 60;");
        currentOverload.Should().Contain("typeof(StunnedStatusEffect);");
        currentOverload.Should().Contain("var duration = statusEffect == null ? 0 : 3;");
        currentOverload.Should().Contain("StatusEffect.RemoveStatusEffect(hitTarget, consumedStatus, false);");

        var toxicRush = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "StatusEffectDefinition" / "ToxicRushStatusEffect.cs").FullName);
        toxicRush.Should().Contain("StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect))");
        toxicRush.Should().Contain("Stat.RestoreStamina(attacker, 2);");
        toxicRush.Should().Contain("StatGroup.Stats[StatType.AttackPercentAdjustment] = 15;");
        toxicRush.Should().Contain("StatGroup.Stats[StatType.AttackDelayReductionPercent] = 20;");

        var serpentsEclipse = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Katar" / "SerpentsEclipseAbilityDefinition.cs").FullName);
        serpentsEclipse.Replace("\r\n", "\n").Should().Contain("SkillType.Katar,\n                20,\n                45,");
        serpentsEclipse.Should().Contain("additionalStatusEffects: new[] { typeof(DisorientedStatusEffect) }");
        serpentsEclipse.Should().Contain("damageType: CombatDamageType.Poison");
        serpentsEclipse.Should().Contain("baseDamageAdjustment: creature => IsPoisonedOrDisoriented(creature) ? 15 : 0");
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
        perk.Category.Should().Be(PerkCategoryType.KatarVenomCurrent);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertSkillRequirement(perkLevel, SkillType.Katar, skillRank);

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
        ability.SkillType.Should().Be(SkillType.Katar);
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

    private static Dictionary<PerkType, PerkDetail> BuildKatarVenomCurrentPerksWithout2daLookup()
    {
        var definition = new KatarPerkDefinition();
        var methodNames = new[]
        {
            "CobraReflexes",
            "CobraStance",
            "CurrentOverload",
            "NeuralShock",
            "NeurotoxinMastery",
            "SerpentsEclipse",
            "SpreadingVenom",
            "StaticPalm",
            "StrikingCobra",
            "ToxicRush",
            "ToxicTempo",
            "TwinFangFlurry",
            "VenomRhythm",
            "VenomSplash"
        };

        foreach (var methodName in methodNames)
        {
            typeof(KatarPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(KatarPerkDefinition)
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
