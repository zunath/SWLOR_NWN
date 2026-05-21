using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.Devices;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class DevicesGrenadierTests
{
    [Test]
    public void DevicesGrenadierPerkLevels_MatchCombatBible()
    {
        var perks = BuildDevicesGrenadierPerksWithout2daLookup();

        AssertPerkLevel(perks[PerkType.FragGrenade], "Frag Grenade", 1, 2, null, FeatType.FragGrenade1,
            "Deals 18 fire DMG plus PER scaling to enemies in a 3m blast. Consumes explosives.");
        AssertPerkLevel(perks[PerkType.BlastRadius], "Blast Radius", 1, 2, 5, null,
            "Grenade abilities gain +1m blast radius.",
            (StatType.GrenadeRadiusBonusTenths, 10));
        AssertPerkLevel(perks[PerkType.ConcussionGrenade], "Concussion Grenade", 1, 3, 8, FeatType.ConcussionGrenade1,
            "Deals 14 electrical DMG plus PER scaling in a 3m blast and knock down for 2 seconds.");
        AssertPerkLevel(perks[PerkType.FlashGrenade], "Flash Grenade", 1, 3, 12, FeatType.FlashGrenade1,
            "Attempts to inflict Flash, reducing physical and Force ability hit chance by 8% for 20 seconds in a 4m blast. Consumes explosives.");
        AssertPerkLevel(perks[PerkType.FragGrenade], "Frag Grenade", 2, 3, 15, FeatType.FragGrenade2,
            "Deals 32 fire DMG plus PER scaling to enemies in a 3m blast and attempts to inflict Bleed.");
        AssertPerkLevel(perks[PerkType.IonGrenade], "Ion Grenade", 1, 3, 18, FeatType.IonGrenade1,
            "Deals 20 electrical DMG plus PER scaling in a 3m blast. Deals 50% bonus damage to droids. Consumes explosives.");
        AssertPerkLevel(perks[PerkType.BlastRadius], "Blast Radius", 2, 2, 22, null,
            "Grenade abilities gain +2m blast radius.",
            (StatType.GrenadeRadiusBonusTenths, 20));
        AssertPerkLevel(perks[PerkType.AdhesiveGrenade], "Adhesive Grenade", 1, 4, 25, FeatType.AdhesiveGrenade1,
            "Slows enemies in a 4m blast for 6 seconds and immobilizes them for 3 seconds.");
        AssertPerkLevel(perks[PerkType.ConcussionGrenade], "Concussion Grenade", 2, 3, 28, FeatType.ConcussionGrenade2,
            "Deals 28 electrical DMG plus PER scaling in a 3m blast and knock down for 2 seconds.");
        AssertPerkLevel(perks[PerkType.ClusterGrenade], "Cluster Grenade", 1, 4, 30, FeatType.ClusterGrenade1,
            "Throws three small grenades at nearby enemies, each dealing 18 fire DMG plus PER scaling in a small blast.");
        AssertPerkLevel(perks[PerkType.FlashGrenade], "Flash Grenade", 2, 3, 35, FeatType.FlashGrenade2,
            "Attempts to inflict Flash, reducing physical and Force ability hit chance by 14% for 20 seconds in a 4m blast. Consumes explosives.");
        AssertPerkLevel(perks[PerkType.IonGrenade], "Ion Grenade", 2, 3, 38, FeatType.IonGrenade2,
            "Deals 34 electrical DMG plus PER scaling in a 3m blast. Deals 60% bonus damage to droids and Shock.");
        AssertPerkLevel(perks[PerkType.FragGrenade], "Frag Grenade", 3, 4, 40, FeatType.FragGrenade3,
            "Deals 48 fire DMG plus PER scaling to enemies in a 3m blast and attempts to inflict Bleed.");
        AssertPerkLevel(perks[PerkType.AdhesiveGrenade], "Adhesive Grenade", 2, 4, 42, FeatType.AdhesiveGrenade2,
            "Slows enemies in a 4m blast for 8 seconds and immobilizes them for 4 seconds.");
        AssertPerkLevel(perks[PerkType.BlastRadius], "Blast Radius", 3, 4, 45, null,
            "Grenade abilities gain +3m blast radius, and Flash Grenade and Adhesive Grenade non-save effect strength increases by 5%.",
            (StatType.GrenadeRadiusBonusTenths, 30),
            (StatType.GrenadeControlPotencyBonus, 5));
        AssertPerkLevel(perks[PerkType.ConcussionGrenade], "Concussion Grenade", 3, 3, 48, FeatType.ConcussionGrenade3,
            "Deals 42 electrical DMG plus PER scaling in a 3m blast and knock down for 3 seconds.");
        AssertPerkLevel(perks[PerkType.ThermalDetonator], "Thermal Detonator", 1, 5, 50, FeatType.ThermalDetonator1,
            "Deals moderate fire DMG plus PER scaling in a 5m blast and inflicts Burning for 45 seconds.");
    }

    [Test]
    public void DevicesGrenadierAbilities_MatchCombatBible()
    {
        var fragGrenade = new FragGrenadeAbilityDefinition().BuildAbilities();
        AssertAbility(fragGrenade[FeatType.FragGrenade1], "Frag Grenade I", 1, RecastGroup.FragGrenade, 12f, 1f, 2, "explosives", 1);
        AssertAbility(fragGrenade[FeatType.FragGrenade2], "Frag Grenade II", 2, RecastGroup.FragGrenade, 12f, 1f, 3);
        AssertAbility(fragGrenade[FeatType.FragGrenade3], "Frag Grenade III", 3, RecastGroup.FragGrenade, 12f, 1f, 5);

        var concussionGrenade = new ConcussionGrenadeAbilityDefinition().BuildAbilities();
        AssertAbility(concussionGrenade[FeatType.ConcussionGrenade1], "Concussion Grenade I", 1, RecastGroup.ConcussionGrenade, 24f, 1f, 3);
        AssertAbility(concussionGrenade[FeatType.ConcussionGrenade2], "Concussion Grenade II", 2, RecastGroup.ConcussionGrenade, 24f, 1f, 4);
        AssertAbility(concussionGrenade[FeatType.ConcussionGrenade3], "Concussion Grenade III", 3, RecastGroup.ConcussionGrenade, 24f, 1f, 6);

        var flashGrenade = new FlashGrenadeAbilityDefinition().BuildAbilities();
        AssertAbility(flashGrenade[FeatType.FlashGrenade1], "Flash Grenade I", 1, RecastGroup.FlashGrenade, 24f, 1f, 2, "explosives", 1);
        AssertAbility(flashGrenade[FeatType.FlashGrenade2], "Flash Grenade II", 2, RecastGroup.FlashGrenade, 24f, 1f, 3, "explosives", 1);

        var ionGrenade = new IonGrenadeAbilityDefinition().BuildAbilities();
        AssertAbility(ionGrenade[FeatType.IonGrenade1], "Ion Grenade I", 1, RecastGroup.IonGrenade, 18f, 1f, 3, "explosives", 1);
        AssertAbility(ionGrenade[FeatType.IonGrenade2], "Ion Grenade II", 2, RecastGroup.IonGrenade, 18f, 1f, 5);

        var adhesiveGrenade = new AdhesiveGrenadeAbilityDefinition().BuildAbilities();
        AssertAbility(adhesiveGrenade[FeatType.AdhesiveGrenade1], "Adhesive Grenade I", 1, RecastGroup.AdhesiveGrenade, 30f, 1f, 4);
        AssertAbility(adhesiveGrenade[FeatType.AdhesiveGrenade2], "Adhesive Grenade II", 2, RecastGroup.AdhesiveGrenade, 30f, 1f, 5);

        var clusterGrenade = new ClusterGrenadeAbilityDefinition().BuildAbilities()[FeatType.ClusterGrenade1];
        AssertAbility(clusterGrenade, "Cluster Grenade", 1, RecastGroup.ClusterGrenade, 45f, 1f, 5);

        var thermalDetonator = new ThermalDetonatorAbilityDefinition().BuildAbilities()[FeatType.ThermalDetonator1];
        AssertAbility(thermalDetonator, "Thermal Detonator", 1, RecastGroup.Capstone, 345f, 1.5f, 15, "explosives", 1);
    }

    [Test]
    public void DevicesGrenadierAbilities_DeclareDynamicSphereTargeting()
    {
        var fragGrenade = new FragGrenadeAbilityDefinition().BuildAbilities();
        AssertTargeting(fragGrenade[FeatType.FragGrenade1], Spell.FragGrenade1, 3f);
        AssertTargeting(fragGrenade[FeatType.FragGrenade2], Spell.FragGrenade2, 3f);
        AssertTargeting(fragGrenade[FeatType.FragGrenade3], Spell.FragGrenade3, 3f);

        var concussionGrenade = new ConcussionGrenadeAbilityDefinition().BuildAbilities();
        AssertTargeting(concussionGrenade[FeatType.ConcussionGrenade1], Spell.ConcussionGrenade1, 3f);
        AssertTargeting(concussionGrenade[FeatType.ConcussionGrenade2], Spell.ConcussionGrenade2, 3f);
        AssertTargeting(concussionGrenade[FeatType.ConcussionGrenade3], Spell.ConcussionGrenade3, 3f);

        var flashGrenade = new FlashGrenadeAbilityDefinition().BuildAbilities();
        AssertTargeting(flashGrenade[FeatType.FlashGrenade1], Spell.FlashGrenade1, 4f);
        AssertTargeting(flashGrenade[FeatType.FlashGrenade2], Spell.FlashGrenade2, 4f);

        var ionGrenade = new IonGrenadeAbilityDefinition().BuildAbilities();
        AssertTargeting(ionGrenade[FeatType.IonGrenade1], Spell.IonGrenade1, 3f);
        AssertTargeting(ionGrenade[FeatType.IonGrenade2], Spell.IonGrenade2, 3f);

        var adhesiveGrenade = new AdhesiveGrenadeAbilityDefinition().BuildAbilities();
        AssertTargeting(adhesiveGrenade[FeatType.AdhesiveGrenade1], Spell.AdhesiveGrenade1, 4f);
        AssertTargeting(adhesiveGrenade[FeatType.AdhesiveGrenade2], Spell.AdhesiveGrenade2, 4f);

        var clusterGrenade = new ClusterGrenadeAbilityDefinition().BuildAbilities();
        AssertTargeting(clusterGrenade[FeatType.ClusterGrenade1], Spell.ClusterGrenade1, 2f);

        var thermalDetonator = new ThermalDetonatorAbilityDefinition().BuildAbilities();
        AssertTargeting(thermalDetonator[FeatType.ThermalDetonator1], Spell.ThermalDetonator1, 5f);
    }

    [Test]
    public void GrenadeRadiusCalculation_UsesTenthsAsMeterFractions()
    {
        DeviceAbilityEffects.CalculateGrenadeRadius(3f, 10).Should().Be(4f);
        DeviceAbilityEffects.CalculateGrenadeRadius(3f, 20).Should().Be(5f);
        DeviceAbilityEffects.CalculateGrenadeRadius(3f, 30).Should().Be(6f);
    }

    [Test]
    public void AbilityTargetingDetail_OnlyAppliesDynamicSizeWhenFeatIsKnown()
    {
        var targeting = new AbilityTargetingDetail(
            Spell.FragGrenade1,
            AbilityTargetingShapeType.Sphere,
            3f,
            0f,
            AbilityTargetingFlags.HarmsEnemies,
            (_, baseSize) => baseSize + 1.25f);

        targeting.ResolveSizeX(0, false).Should().Be(3f);
        targeting.ResolveSizeX(0, true).Should().Be(4.25f);
    }

    [Test]
    public void DevicesGrenadierStatusEffects_MatchCombatBible()
    {
        new FlashGrenade1StatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(-8);
        new FlashGrenade2StatusEffect().StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(-14);

        var adhesiveSlow = new AdhesiveGrenadeSlowStatusEffect();
        adhesiveSlow.StatGroup.Stats[StatType.MovementSpeedPercentAdjustment].Should().Be(-50);
        adhesiveSlow.ResistanceType.Should().Be(ResistanceType.Mobility);

        new AdhesiveGrenadeSlowStatusEffect(55)
            .StatGroup.Stats[StatType.MovementSpeedPercentAdjustment]
            .Should()
            .Be(-55);
    }

    [Test]
    public void DevicesGrenadierSources_IncludeBibleBehavior()
    {
        var root = FindRepositoryRoot();

        var fragGrenade = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "FragGrenadeAbilityDefinition.cs").FullName);
        fragGrenade.Should().Contain("18");
        fragGrenade.Should().Contain("32");
        fragGrenade.Should().Contain("48");
        fragGrenade.Should().Contain("DeviceAbilityEffects.ApplyGrenadeRadiusBonus(activator, 3f)");
        fragGrenade.Should().Contain("typeof(BleedStatusEffect)");
        fragGrenade.Should().Contain("ApplyEffectAtLocation(");
        fragGrenade.Should().Contain("EffectVisualEffect(VisualEffect.Fnf_Fireball)");
        fragGrenade.Should().Contain("areaVisualEffect: VisualEffect.None");

        var concussionGrenade = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "ConcussionGrenadeAbilityDefinition.cs").FullName);
        concussionGrenade.Should().Contain("14");
        concussionGrenade.Should().Contain("28");
        concussionGrenade.Should().Contain("42");
        concussionGrenade.Should().Contain("typeof(KnockdownStatusEffect)");
        concussionGrenade.Should().Contain("AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation)");
        concussionGrenade.Should().Contain("ApplyEffectAtLocation(");
        concussionGrenade.Should().Contain("EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion)");
        concussionGrenade.Should().Contain("areaVisualEffect: VisualEffect.None");

        var flashGrenade = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "FlashGrenadeAbilityDefinition.cs").FullName);
        flashGrenade.Should().Contain("new FlashGrenade1StatusEffect(GetFlashPenalty(activator, 8))");
        flashGrenade.Should().Contain("new FlashGrenade2StatusEffect(GetFlashPenalty(activator, 14))");
        flashGrenade.Should().Contain("DeviceAbilityEffects.ApplyGrenadeRadiusBonus(activator, 4f)");

        var ionGrenade = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "IonGrenadeAbilityDefinition.cs").FullName);
        ionGrenade.Should().Contain("ApplyIonGrenade(activator, target, targetLocation, 20, 50, null)");
        ionGrenade.Should().Contain("ApplyIonGrenade(activator, target, targetLocation, 34, 60, typeof(ShockStatusEffect))");
        ionGrenade.Should().Contain("IsDroid");

        var adhesiveGrenade = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "AdhesiveGrenadeAbilityDefinition.cs").FullName);
        adhesiveGrenade.Should().Contain("ApplyAdhesiveGrenade");
        adhesiveGrenade.Should().Contain("DeviceAbilityEffects.ApplyGrenadeRadiusBonus(activator, 4f)");
        adhesiveGrenade.Should().Contain("new AdhesiveGrenadeSlowStatusEffect");
        adhesiveGrenade.Should().Contain("typeof(ImmobilizedStatusEffect)");
        adhesiveGrenade.Should().Contain("slowDuration");
        adhesiveGrenade.Should().Contain("immobilizeDuration");

        var clusterGrenade = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "ClusterGrenadeAbilityDefinition.cs").FullName);
        clusterGrenade.Should().Contain("GrenadeCount = 3");
        clusterGrenade.Should().Contain("SmallBlastRadius = 2f");
        clusterGrenade.Should().Contain("GetClusterGrenadeTargets");
        clusterGrenade.Should().NotContain("centerOnActivator: true");

        var thermalDetonator = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "Devices" / "ThermalDetonatorAbilityDefinition.cs").FullName);
        thermalDetonator.Should().Contain("RequirementItem(\"explosives\", 1)");
        thermalDetonator.Should().Contain("typeof(BurnStatusEffect)");
        thermalDetonator.Should().Contain("DeviceAbilityEffects.ApplyGrenadeRadiusBonus(activator, 5f)");
        thermalDetonator.Should().Contain("AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation)");
        thermalDetonator.Should().Contain("ApplyEffectAtLocation(");
        thermalDetonator.Should().Contain("EffectVisualEffect(VisualEffect.Fnf_Fireball)");
        thermalDetonator.Should().Contain("areaVisualEffect: VisualEffect.None");
    }

    [Test]
    public void DevicesGrenadierFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var feats = new[]
        {
            (FeatType.FragGrenade1, "ife_frggrnd1", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.ConcussionGrenade1, "ife_cncssngrnd1", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.FlashGrenade1, "ife_flashgrnd1", "M", "0x3E", "1", "sphere", "4", "****", "1"),
            (FeatType.FragGrenade2, "ife_frggrnd2", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.IonGrenade1, "ife_ngrnd1", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.AdhesiveGrenade1, "ife_dhsvgrnd1", "M", "0x3E", "1", "sphere", "4", "****", "1"),
            (FeatType.ConcussionGrenade2, "ife_cncssngrnd2", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.ClusterGrenade1, "ife_clstrgrnd1", "M", "0x3E", "1", "sphere", "2", "****", "1"),
            (FeatType.FlashGrenade2, "ife_flashgrnd2", "M", "0x3E", "1", "sphere", "4", "****", "1"),
            (FeatType.IonGrenade2, "ife_ngrnd2", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.FragGrenade3, "ife_frggrnd3", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.AdhesiveGrenade2, "ife_dhsvgrnd2", "M", "0x3E", "1", "sphere", "4", "****", "1"),
            (FeatType.ConcussionGrenade3, "ife_cncssngrnd3", "M", "0x3E", "1", "sphere", "3", "****", "1"),
            (FeatType.ThermalDetonator1, "ife_thrmldtntr1", "M", "0x3E", "1", "sphere", "5", "****", "1")
        };
        var seenIcons = new HashSet<string>();

        foreach (var (featType, expectedIcon, range, targetType, hostileSetting, targetShape, targetSizeX, targetSizeY, targetFlags) in feats)
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
            spellRow["TargetShape"].Should().Be(targetShape);
            spellRow["TargetSizeX"].Should().Be(targetSizeX);
            spellRow["TargetSizeY"].Should().Be(targetSizeY);
            spellRow["TargetFlags"].Should().Be(targetFlags);
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
        params (StatType Stat, int Value)[] statBonuses)
    {
        perk.Name.Should().Be(name);
        perk.Category.Should().Be(PerkCategoryType.DevicesGrenadier);

        var perkLevel = perk.PerkLevels[level];
        perkLevel.Price.Should().Be(price);
        perkLevel.Description.Should().Be(description);
        AssertCharacterRequirement(perkLevel, CharacterType.Standard);

        if (skillRank.HasValue)
            AssertSkillRequirement(perkLevel, SkillType.Devices, skillRank.Value);
        else
            perkLevel.Requirements.OfType<PerkRequirementSkill>().Should().BeEmpty();

        if (grantedFeat.HasValue)
            perkLevel.GrantedFeats.Should().ContainSingle().Which.Should().Be(grantedFeat.Value);
        else
            perkLevel.GrantedFeats.Should().BeEmpty();

        if (statBonuses.Length > 0)
        {
            foreach (var (stat, value) in statBonuses)
            {
                AssertStatBonus(perkLevel, stat, value);
            }
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
        int staminaCost,
        string itemResref = null,
        int itemQuantity = 0)
    {
        ability.Name.Should().Be(name);
        ability.AbilityLevel.Should().Be(level);
        ability.SkillType.Should().Be(SkillType.Devices);
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.ActivationDelay(0, 0, level).Should().Be(activationSeconds);
        ability.ActivationType.Should().Be(AbilityActivationType.Casted);
        ability.IsHostileAbility.Should().BeTrue();
        ability.RequiresTarget.Should().BeFalse();
        ability.IsSingleTargetAbility.Should().BeFalse();
        ability.IsAreaAbility.Should().BeTrue();
        ability.BreaksStealth.Should().BeTrue();

        ability.Requirements
            .OfType<AbilityRequirementStamina>()
            .Should()
            .ContainSingle()
            .Which
            .RequiredSTM
            .Should()
            .Be(staminaCost);
        ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();

        if (itemResref == null)
        {
            ability.Requirements.OfType<AbilityRequirementItem>().Should().BeEmpty();
        }
        else
        {
            var itemRequirement = ability.Requirements
                .OfType<AbilityRequirementItem>()
                .Should()
                .ContainSingle()
                .Which;
            itemRequirement.ItemResref.Should().Be(itemResref);
            itemRequirement.Quantity.Should().Be(itemQuantity);
        }
    }

    private static void AssertTargeting(AbilityDetail ability, Spell spell, float radius)
    {
        ability.Targeting.Should().NotBeNull();
        var targeting = ability.Targeting;
        targeting.Spell.Should().Be(spell);
        targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
        targeting.SizeX.Should().Be(radius);
        targeting.SizeY.Should().Be(0f);
        targeting.Flags.Should().Be(AbilityTargetingFlags.HarmsEnemies);
        targeting.SizeResolver.Should().NotBeNull();
        Assert.That(
            targeting.SizeResolver.Method,
            Is.EqualTo(((AbilityTargetingSizeResolver)DeviceAbilityEffects.ApplyGrenadeRadiusBonus).Method));
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

    private static Dictionary<PerkType, PerkDetail> BuildDevicesGrenadierPerksWithout2daLookup()
    {
        var definition = new DevicesGrenadierPerkDefinition();
        var methodNames = new[]
        {
            "AdhesiveGrenade",
            "BlastRadius",
            "ClusterGrenade",
            "ConcussionGrenade",
            "FlashGrenade",
            "FragGrenade",
            "IonGrenade",
            "ThermalDetonator"
        };

        foreach (var methodName in methodNames)
        {
            typeof(DevicesGrenadierPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(DevicesGrenadierPerkDefinition)
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
