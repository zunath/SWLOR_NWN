using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Piloting;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// The Ordnance style of Piloting (plan section 6, Appendix A): the perk rows, prices and ranks of the Design Bible
/// tab "Piloting", the space abilities they grant and the stats the traits set.
/// </summary>
public class PilotingOrdnanceTests
{
    // Rank, SP price, STM and cooldown are the Bible rows. The numbers are the ground Throwing Ordnance perks'.
    private static readonly (FeatType Feat, string Name, int Rank, int Price, int Stamina, float Cooldown)[] Abilities =
    {
        (FeatType.OrdnanceVolley1, "Volley I", 2, 2, 3, 18f),
        (FeatType.OrdnanceFlareMissile1, "Flare Missile I", 10, 2, 4, 24f),
        (FeatType.OrdnanceVolley2, "Volley II", 12, 2, 5, 18f),
        (FeatType.OrdnanceConcussionBarrage1, "Concussion Barrage I", 18, 3, 6, 30f),
        (FeatType.OrdnanceVolley3, "Volley III", 28, 3, 8, 18f),
        (FeatType.OrdnanceFlareMissile2, "Flare Missile II", 30, 4, 8, 24f),
        (FeatType.OrdnanceConcussionBarrage2, "Concussion Barrage II", 32, 3, 9, 30f),
        (FeatType.OrdnanceFlareMissile3, "Flare Missile III", 38, 4, 10, 24f),
        (FeatType.OrdnanceVolley4, "Volley IV", 40, 5, 12, 18f),
        (FeatType.OrdnanceSaturationStrike, "Saturation Strike", 50, 6, 15, 90f),
    };

    private static Dictionary<FeatType, AbilityDetail> BuildAbilities()
    {
        var abilities = new Dictionary<FeatType, AbilityDetail>();
        foreach (var definition in new IAbilityListDefinition[]
                 {
                     new VolleyAbilityDefinition(),
                     new FlareMissileAbilityDefinition(),
                     new ConcussionBarrageAbilityDefinition(),
                     new OrdnanceStanceAbilityDefinition(),
                     new SaturationStrikeAbilityDefinition()
                 })
        {
            foreach (var (feat, detail) in definition.BuildAbilities())
                abilities[feat] = detail;
        }

        return abilities;
    }

    /// <summary>
    /// The perks as defined, without the 2DA lookup that finishing the builder performs (it needs the engine).
    /// </summary>
    private static Dictionary<PerkType, PerkDetail> BuildPerks()
    {
        var definition = new PilotingOrdnancePerkDefinition();
        foreach (var method in typeof(PilotingOrdnancePerkDefinition)
                     .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0)
                     .OrderBy(x => x.MetadataToken))
        {
            method.Invoke(definition, null);
        }

        var builder = typeof(PilotingOrdnancePerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    [Test]
    public void EveryOrdnanceAbility_IsASpaceOnlyPilotingAbilityWithTheBibleCostAndCooldown()
    {
        var abilities = BuildAbilities();

        foreach (var (feat, name, _, _, stamina, cooldown) in Abilities)
        {
            var ability = abilities[feat];
            ability.Name.Should().Be(name);
            ability.CanBeUsedInSpace.Should().BeTrue(name);
            ability.SkillType.Should().Be(SkillType.Piloting, name);
            ability.IsHostileAbility.Should().BeTrue(name);
            ability.ActivationDelay(0, 0, 0).Should().Be(0f, $"{name} has no activation delay");
            ability.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM.Should().Be(stamina, name);
            ability.RecastDelay(0).Should().Be(cooldown, name);
            ability.CustomValidation.Should().NotBeNull($"{name} must turn a ship with no launcher away before it pays");
        }
    }

    [Test]
    public void Volley_AimsAtAShipAndBlastsTheShipsNearIt()
    {
        var abilities = BuildAbilities();

        foreach (var (feat, spell) in new[]
                 {
                     (FeatType.OrdnanceVolley1, Spell.OrdnanceVolley1), (FeatType.OrdnanceVolley2, Spell.OrdnanceVolley2),
                     (FeatType.OrdnanceVolley3, Spell.OrdnanceVolley3), (FeatType.OrdnanceVolley4, Spell.OrdnanceVolley4)
                 })
        {
            var volley = abilities[feat];
            volley.IsAreaAbility.Should().BeTrue();
            volley.RequiresTarget.Should().BeTrue("a volley is fired at a chosen ship");
            volley.HasExplicitMaxRange.Should().BeTrue();
            volley.MaxRange.Should().Be(PilotingAbilityConfiguration.LauncherRange);
            volley.Targeting.Spell.Should().Be(spell);
            volley.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
            volley.Targeting.SizeX.Should().Be(SpaceRadii.NearTarget, "the 5 m ground blast is 20 m in space");
            volley.Targeting.Flags.Should().HaveFlag(AbilityTargetingFlags.HarmsEnemies);
            volley.Targeting.Flags.Should().NotHaveFlag(AbilityTargetingFlags.OriginOnSelf);
        }
    }

    [Test]
    public void FlareMissile_IsASingleTargetShot_AndTheBurstsAreSelfCentered()
    {
        var abilities = BuildAbilities();

        foreach (var feat in new[] { FeatType.OrdnanceFlareMissile1, FeatType.OrdnanceFlareMissile2, FeatType.OrdnanceFlareMissile3 })
        {
            abilities[feat].RequiresTarget.Should().BeTrue(feat.ToString());
            abilities[feat].IsSingleTargetAbility.Should().BeTrue(feat.ToString());
        }

        foreach (var (feat, spell) in new[]
                 {
                     (FeatType.OrdnanceConcussionBarrage1, Spell.OrdnanceConcussionBarrage1),
                     (FeatType.OrdnanceConcussionBarrage2, Spell.OrdnanceConcussionBarrage2),
                     (FeatType.OrdnanceSaturationStrike, Spell.OrdnanceSaturationStrike)
                 })
        {
            var burst = abilities[feat];
            burst.IsAreaAbility.Should().BeTrue();
            burst.RequiresTarget.Should().BeFalse("a self-centered area shows no cursor");
            burst.Targeting.Spell.Should().Be(spell);
            burst.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
            burst.Targeting.SizeX.Should().Be(SpaceRadii.NearShip, "the 5 m ground burst is 20 m in space");
            burst.Targeting.Flags.Should().HaveFlag(AbilityTargetingFlags.OriginOnSelf);
        }
    }

    [Test]
    public void OrdnanceStance_IsTheThrowingStancesStatusWithTheSpaceLauncherStats()
    {
        var stance = BuildAbilities()[FeatType.OrdnanceShipStance];

        stance.CanBeUsedInSpace.Should().BeTrue();
        stance.IsHostileAbility.Should().BeFalse();
        stance.ActivationDelay(0, 0, 0).Should().Be(0f);
        stance.RecastDelay(0).Should().Be(30f);
        stance.StatusEffectTypesRemovedOnPerkRefund.Should().Contain(typeof(OrdnanceStanceStatusEffect));

        var status = new OrdnanceStanceStatusEffect();
        status.StatGroup.Stats[StatType.SpaceLauncherAreaAbilityDamagePercentAdjustment].Should().Be(10);
        status.StatGroup.Stats[StatType.SpaceLauncherSingleTargetDamagePercentAdjustment].Should().Be(-10);
        status.StatGroup.Stats[StatType.ThrowingAreaAbilityDamagePercentAdjustment].Should().Be(10, "the ground stance keeps its area bonus");
        status.StatGroup.Stats[StatType.ThrowingSingleTargetDamagePercentAdjustment].Should().Be(-10, "the ground stance's described single-target penalty");
        status.StatGroup.Stats[StatType.SingleTargetDamagePercentAdjustment].Should().Be(0,
            "the generic single-target stat is read in space too and would cut every ship weapon");
    }

    [Test]
    public void OrdnanceStance_AppliesItsSingleTargetPenaltyToThrownDamageOnly()
    {
        var effects = (Dictionary<uint, CreatureStatusEffect>)typeof(StatusEffect)
            .GetField("_creatureEffects", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        const uint creature = 61307;
        var stance = new CreatureStatusEffect();
        stance.Add(new OrdnanceStanceStatusEffect());
        effects[creature] = stance;
        try
        {
            Combat.ApplyThrowingAbilityShapeDamageModifier(creature, SkillType.Throwing, 100, true, false).Should().Be(90,
                "single-target thrown damage is reduced by 10%");
            Combat.ApplyThrowingAbilityShapeDamageModifier(creature, SkillType.Throwing, 100, false, true).Should().Be(110,
                "thrown area abilities deal +10% damage");
            Combat.ApplyThrowingAbilityShapeDamageModifier(creature, SkillType.Throwing, 100, false, false).Should().Be(100);
            Combat.ApplyThrowingAbilityShapeDamageModifier(creature, SkillType.Rifle, 100, true, false).Should().Be(100,
                "only Throwing damage takes the stance");
        }
        finally
        {
            effects.Remove(creature);
        }

        var source = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..",
            "SWLOR.Game.Server", "Service", "Combat.cs"));
        source.Should().Contain("ApplyThrowingAbilityShapeDamageModifier(attacker, skillType, damage, isSingleTarget, isArea)");
        source.Should().Contain("ApplyThrowingAbilityShapeDamageModifier(attacker, skillType, damage, true, false)",
            "a thrown auto-attack is single-target damage");
    }

    [Test]
    public void ThePerks_FollowTheBibleScheduleAndOwnTheirFeats()
    {
        var perks = BuildPerks();

        foreach (var (feat, _, rank, price, _, _) in Abilities)
        {
            var level = perks.Values
                .SelectMany(perk => perk.PerkLevels.Values)
                .Single(x => x.GrantedFeats.Contains(feat));

            level.Price.Should().Be(price, feat.ToString());
            level.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank.Should().Be(rank, feat.ToString());
        }

        perks.Values.Should().OnlyContain(perk => perk.Category == PerkCategoryType.PilotingOrdnance);
        perks.Values.SelectMany(perk => perk.PerkLevels.Values).Sum(level => level.Price)
            .Should().Be(60, "an Ordnance style costs 60 SP like the other styles");
        perks.Values.SelectMany(perk => perk.PerkLevels.Values).Should().HaveCount(18);
        perks.Values.SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.Requirements.OfType<PerkRequirementSkill>())
            .Should().OnlyContain(requirement => requirement.Type == SkillType.Piloting);
    }

    [Test]
    public void TheTraits_SetTheSpaceStatsTheLauncherCodeReads()
    {
        var perks = BuildPerks();

        int Stat(PerkType perk, int rank, StatType stat) =>
            perks[perk].PerkLevels[rank].StatBonuses.Single(x => x.Stat == stat).Calculate(0);

        Stat(PerkType.OrdnanceProximityWarheads, 1, StatType.SpaceLauncherSplashChance).Should().Be(15);
        Stat(PerkType.OrdnanceProximityWarheads, 1, StatType.SpaceLauncherSplashDamage).Should().Be(8);

        foreach (var (rank, damage) in new[] { (1, 3), (2, 5), (3, 7) })
        {
            Stat(PerkType.OrdnanceShrapnelWarheads, rank, StatType.SpaceLauncherAreaFragmentationDamage).Should().Be(damage);
            Stat(PerkType.OrdnanceShrapnelWarheads, rank, StatType.SpaceLauncherAreaFragmentationPulseSeconds).Should().Be(3);
            Stat(PerkType.OrdnanceShrapnelWarheads, rank, StatType.SpaceLauncherAreaFragmentationDurationSeconds).Should().Be(30);
        }

        Stat(PerkType.OrdnanceClusterWarheads, 1, StatType.SpaceLauncherAreaClusterDamageBonus).Should().Be(10);
        Stat(PerkType.OrdnanceClusterWarheads, 1, StatType.SpaceLauncherAreaClusterMaximumTargets).Should().Be(1);
        Stat(PerkType.OrdnanceSaturationFire, 1, StatType.SpaceLauncherAreaHitSequenceCountRequired).Should().Be(2);
        Stat(PerkType.OrdnanceSaturationFire, 1, StatType.SpaceLauncherAreaHitSequenceWindowSeconds).Should().Be(30);
        Stat(PerkType.OrdnanceSaturationFire, 1, StatType.SpaceLauncherAreaHitSequenceExposedDurationSeconds).Should().Be(30);
        Stat(PerkType.OrdnanceBombardiersRhythm, 1, StatType.SpaceLauncherAreaMinTargetsStaminaRestoreThreshold).Should().Be(2);
        Stat(PerkType.OrdnanceBombardiersRhythm, 1, StatType.SpaceLauncherAreaMinTargetsStaminaRestore).Should().Be(4);
    }

    [Test]
    public void TheTraits_NeverSetTheGroundThrowingStats()
    {
        // The ground stats pick their skill through selector stats and add up with the pilot's ground perks.
        var groundStats = new[]
        {
            StatType.AutoAttackSplashChance, StatType.AutoAttackSplashDamage,
            StatType.AreaAbilityFragmentationDamage, StatType.AreaAbilityFragmentationSkillType,
            StatType.ThrowingBombardierClusterStormDamageBonus, StatType.AreaAbilityTargetHitSequenceCountRequired,
            StatType.ThrowingAreaAbilityMinTargetsStaminaRestore
        };

        BuildPerks().Values
            .SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.StatBonuses)
            .Select(bonus => bonus.Stat)
            .Should().NotIntersectWith(groundStats);
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return File.ReadAllText(Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()))
            .Replace("\r\n", "\n");
    }

    [Test]
    public void TheSpaceCode_ReadsTheOrdnanceStatsFromTheRightPlaces()
    {
        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("if (shot.Family == SpaceWeaponFamily.Launcher)",
            "only a launcher shot takes the launcher shape stats");
        space.Should().Contain("StatType.SpaceLauncherSingleTargetDamagePercentAdjustment");
        space.Should().Contain("StatType.SpaceLauncherAreaAbilityDamagePercentAdjustment");
        space.Should().Contain("SpaceOrdnance.Clear(player);");

        var ordnance = ReadServerSource("Service", "SpaceOrdnance.cs");
        ordnance.Should().Contain("SpaceCombatEvents.ShotLanded += OnShotLanded;");
        ordnance.Should().Contain("e.Family != SpaceWeaponFamily.Launcher || e.FromAbility",
            "Proximity Warheads belongs to the launcher's own cycles, as the ground splash belongs to auto-attacks");
    }

    [Test]
    public void ACrewMembersStationAbility_ReadsTheirOwnPerks_AndTheShipDealsTheDamage()
    {
        // A gunner's Concussion Barrage or Saturation Strike runs with the ship as the activator; the perks, the hit
        // sequence, the fragmentation zones and the STM refund are the gunner's (SpaceCrew.GetAbilityUser).
        var ordnance = ReadServerSource("Service", "SpaceOrdnance.cs");
        var resolved = ordnance[ordnance.IndexOf("public static void OnAreaAbilityResolved(", StringComparison.Ordinal)..];
        resolved.Should().Contain("var user = SpaceCrew.GetAbilityUser(activator);");
        ordnance.Should().NotContain("Stat.GetStatAdjustment(activator,", "every area perk reads the ability user");
        foreach (var stat in new[]
                 {
                     "SpaceLauncherAreaFragmentationDamage", "SpaceLauncherAreaHitSequenceCountRequired",
                     "SpaceLauncherAreaClusterDamageBonus", "SpaceLauncherAreaMinTargetsStaminaRestore",
                 })
        {
            ordnance.Should().Contain($"Stat.GetStatAdjustment(user, StatType.{stat})", stat);
        }

        ordnance.Should().Contain("var key = (user, ship);", "each user keeps their own Exposed count")
            .And.Contain("Combat.RestoreAbilityHitStamina(user, ability, restore, sendFeedback: false);", "the user paid the STM")
            .And.Contain("Ability.GetActiveAbilityImpactSummary(activator)?.Ability;", "the ship runs the impact");
        var zones = ordnance[ordnance.IndexOf("public static void StartFragmentationZones(", StringComparison.Ordinal)..];
        zones.Should().Contain("var user = SpaceCrew.GetAbilityUser(activator);")
            .And.Contain("TemporaryStatModifier.Replace(\n                user, StatType.SpaceLauncherAreaFragmentationDamage");
        ordnance.Should().Contain("Space.ApplyStatusToShip(activator, ship, new FragmentationStatusEffect(damage, pulse), duration);")
            .And.Contain("Space.ApplyStatusToShip(activator, ship, typeof(ExposedStatusEffect), exposedSeconds);")
            .And.Contain("ApplyTriggeredDamage(activator, ship, damage);", "the ship stays the damage source");

        // Mobile Footwork and Kiting Instinct read the user's perks and give the bonus to the ship.
        var rewards = ReadServerSource("Service", "SpaceHostileAbilityRewards.cs");
        rewards.Should().Contain("var user = SpaceCrew.GetAbilityUser(e.Activator);")
            .And.Contain("var amount = Stat.GetStatAdjustment(user, amountStat);")
            .And.Contain("var duration = Stat.GetStatAdjustment(user, durationStat);")
            .And.Contain("TemporaryStatModifier.Replace(ship, granted, amount, duration, amountStat);");

        // Defiance's hit lands later from a weapon cycle, so its rider carries the user whose Anger Strike perks apply.
        var bulwark = ReadServerSource("Service", "SpaceBulwark.cs");
        bulwark.Should().Contain("StatSource = SpaceCrew.GetAbilityUser(ship),")
            .And.Contain("var user = GetIsObjectValid(e.Rider.StatSource) ? e.Rider.StatSource : e.Attacker;")
            .And.Contain("Stat.GetStatAdjustment(user, StatType.SpaceDefianceEnmityBonus)")
            .And.Contain("Stat.GetStatAdjustment(user, StatType.SpaceDefianceNextAttackDamageBonus)")
            .And.Contain("Stat.GetStatAdjustment(user, StatType.SpaceDefianceNextAttackDamageDurationSeconds)");
    }

    [Test]
    public void ARiderWithoutAStatSource_ReadsTheShipsOwnPerks()
    {
        // A rider armed by the pilot, or saved before riders named their user, falls back to the consuming ship.
        new SpaceShotRider().StatSource.Should().Be(SWLOR.NWN.API.NWScript.NWScript.OBJECT_INVALID);
        Newtonsoft.Json.JsonConvert.DeserializeObject<SpaceShotRider>("{\"Key\":\"BulwarkDefiance\",\"Amount\":10}")!
            .StatSource.Should().Be(SWLOR.NWN.API.NWScript.NWScript.OBJECT_INVALID);
    }
}
