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
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// The Bulwark style of Piloting (plan section 6, Appendix A): the perk rows, prices and ranks of the Design Bible
/// tab "Piloting", the space abilities they grant and the stats the traits set.
/// </summary>
public class PilotingBulwarkTests
{
    // Rank, SP price, STM and cooldown are the Bible rows. The numbers are the ground Vibroblade, Heavy Vibroblade
    // and Armor perks'. A goad costs no STM.
    private static readonly (FeatType Feat, string Name, int Rank, int Price, int Stamina, float Cooldown)[] Abilities =
    {
        (FeatType.BulwarkShieldRam1, "Shield Ram I", 2, 2, 3, 20f),
        (FeatType.BulwarkDefiance1, "Defiance I", 10, 2, 4, 18f),
        (FeatType.BulwarkShieldRam2, "Shield Ram II", 12, 2, 5, 20f),
        (FeatType.BulwarkHail1, "Hail I", 18, 3, 0, 6f),
        (FeatType.BulwarkShieldRam3, "Shield Ram III", 28, 3, 6, 20f),
        (FeatType.BulwarkDefiance2, "Defiance II", 30, 4, 7, 18f),
        (FeatType.BulwarkHail2, "Hail II", 32, 3, 0, 12f),
        (FeatType.BulwarkDefiance3, "Defiance III", 38, 4, 10, 18f),
        (FeatType.BulwarkShieldRam4, "Shield Ram IV", 40, 5, 8, 20f),
        (FeatType.BulwarkInvincible, "Invincible", 50, 6, 15, 90f),
    };

    private static Dictionary<FeatType, AbilityDetail> BuildAbilities()
    {
        var abilities = new Dictionary<FeatType, AbilityDetail>();
        foreach (var definition in new IAbilityListDefinition[]
                 {
                     new ShieldRamAbilityDefinition(),
                     new DefianceAbilityDefinition(),
                     new HailAbilityDefinition(),
                     new BulwarkStanceAbilityDefinition(),
                     new InvincibleAbilityDefinition()
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
        var definition = new PilotingBulwarkPerkDefinition();
        foreach (var method in typeof(PilotingBulwarkPerkDefinition)
                     .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0)
                     .OrderBy(x => x.MetadataToken))
        {
            method.Invoke(definition, null);
        }

        var builder = typeof(PilotingBulwarkPerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    [Test]
    public void EveryBulwarkAbility_IsASpaceOnlyPilotingAbilityWithTheBibleCostAndCooldown()
    {
        var abilities = BuildAbilities();

        foreach (var (feat, name, _, _, stamina, cooldown) in Abilities)
        {
            var ability = abilities[feat];
            ability.Name.Should().Be(name);
            ability.CanBeUsedInSpace.Should().BeTrue(name);
            ability.SkillType.Should().Be(SkillType.Piloting, name);
            ability.ActivationDelay(0, 0, 0).Should().Be(0f, $"{name} has no activation delay");
            ability.RecastDelay(0).Should().Be(cooldown, name);

            var requirements = ability.Requirements.OfType<AbilityRequirementStamina>().ToArray();
            if (stamina == 0)
                requirements.Should().BeEmpty($"{name} costs no STM");
            else
                requirements.Single().RequiredSTM.Should().Be(stamina, name);
        }
    }

    [Test]
    public void ShieldRamAndDefiance_AreRidersWithNoTargetAndNoCursor()
    {
        var abilities = BuildAbilities();

        foreach (var feat in new[]
                 {
                     FeatType.BulwarkShieldRam1, FeatType.BulwarkShieldRam2, FeatType.BulwarkShieldRam3,
                     FeatType.BulwarkShieldRam4, FeatType.BulwarkDefiance1, FeatType.BulwarkDefiance2,
                     FeatType.BulwarkDefiance3
                 })
        {
            var rider = abilities[feat];
            rider.IsHostileAbility.Should().BeFalse($"{feat} only arms the next weapon hit");
            rider.RequiresTarget.Should().BeFalse($"{feat} shows no cursor");
            rider.IsAreaAbility.Should().BeFalse(feat.ToString());
            rider.CustomValidation.Should().NotBeNull($"{feat} must turn a ship with no weapon away before it pays");
        }
    }

    [Test]
    public void Hail_GoadsAShipOrAShipAndTheShipsNearIt_WithAnAimedCursor()
    {
        var abilities = BuildAbilities();

        var hail1 = abilities[FeatType.BulwarkHail1];
        hail1.IsHostileAbility.Should().BeTrue();
        hail1.IsSingleTargetAbility.Should().BeTrue();
        hail1.RequiresTarget.Should().BeTrue();
        hail1.HasExplicitMaxRange.Should().BeTrue();
        hail1.MaxRange.Should().Be(PilotingAbilityConfiguration.GoadRange);

        var hail2 = abilities[FeatType.BulwarkHail2];
        hail2.IsHostileAbility.Should().BeTrue();
        hail2.IsAreaAbility.Should().BeTrue();
        hail2.RequiresTarget.Should().BeTrue("a goad is aimed at a chosen ship");
        hail2.HasExplicitMaxRange.Should().BeTrue();
        hail2.MaxRange.Should().Be(PilotingAbilityConfiguration.GoadRange);
        hail2.Targeting.Spell.Should().Be(Spell.BulwarkHail2);
        hail2.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
        hail2.Targeting.SizeX.Should().Be(SpaceRadii.NearTarget);
        hail2.Targeting.Flags.Should().HaveFlag(AbilityTargetingFlags.HarmsEnemies);
        hail2.Targeting.Flags.Should().NotHaveFlag(AbilityTargetingFlags.OriginOnSelf);
    }

    [Test]
    public void BulwarkStance_IsTheDefensiveStancesStatusWithTheShipDefenseStat()
    {
        var stance = BuildAbilities()[FeatType.BulwarkShipStance];

        stance.CanBeUsedInSpace.Should().BeTrue();
        stance.IsHostileAbility.Should().BeFalse();
        stance.ActivationDelay(0, 0, 0).Should().Be(0f);
        stance.RecastDelay(0).Should().Be(30f);
        stance.StatusEffectTypesRemovedOnPerkRefund.Should().Contain(typeof(DefensiveStanceStatusEffect));
    }

    [Test]
    public void TheDefenseStatuses_GiveAShipItsOwnDefenseStat_AndKeepTheGroundStats()
    {
        // Applied by a status, the stats are read when it applies; the stance sets its stats on Apply, so check
        // the constructors that set them directly.
        var fortress = new FortressStrikeStatusEffect(20);
        fortress.StatGroup.Stats[StatType.SpaceDefensePercentAdjustment].Should().Be(20);
        fortress.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(20, "the ground status is unchanged");

        var unbreakable = new UnbreakableStatusEffect(40);
        unbreakable.StatGroup.Stats[StatType.SpaceDefensePercentAdjustment].Should().Be(40);
        unbreakable.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment].Should().Be(40);

        var invincible = new InvincibleStatusEffect();
        invincible.StatGroup.Stats[StatType.SpaceDamageTakenPercentAdjustment].Should().Be(-50);
        invincible.StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment].Should().Be(-50);
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

        perks.Values.Should().OnlyContain(perk => perk.Category == PerkCategoryType.PilotingBulwark);
        perks.Values.SelectMany(perk => perk.PerkLevels.Values).Sum(level => level.Price)
            .Should().Be(60, "a Bulwark style costs 60 SP like the other styles");
        perks.Values.SelectMany(perk => perk.PerkLevels.Values).Should().HaveCount(18);
        perks.Values.SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.Requirements.OfType<PerkRequirementSkill>())
            .Should().OnlyContain(requirement => requirement.Type == SkillType.Piloting);
    }

    [Test]
    public void TheTraits_SetTheStatsTheSpaceCodeReads()
    {
        var perks = BuildPerks();

        int Stat(PerkType perk, int rank, StatType stat) =>
            perks[perk].PerkLevels[rank].StatBonuses.Single(x => x.Stat == stat).Calculate(0);

        Stat(PerkType.BulwarkAngerStrike, 1, StatType.SpaceDefianceEnmityBonus).Should().Be(150);
        Stat(PerkType.BulwarkAngerStrike, 1, StatType.SpaceDefianceNextAttackDamageBonus).Should().Be(12);
        Stat(PerkType.BulwarkAngerStrike, 1, StatType.SpaceDefianceNextAttackDamageDurationSeconds).Should().Be(30);

        foreach (var (rank, percent) in new[] { (1, 8), (2, 10), (3, 12) })
            Stat(PerkType.BulwarkBulwark, rank, StatType.DefensePercentAdjustment).Should().Be(percent);

        foreach (var stat in new[] { StatType.MindResistance, StatType.TraumaResistance, StatType.MobilityResistance })
            Stat(PerkType.BulwarkFortification, 1, stat).Should().Be(15);

        Stat(PerkType.BulwarkCriticalWard, 1, StatType.SpaceIncomingCriticalHitDowngradeToMinimumDamage).Should().Be(1);
        perks[PerkType.BulwarkCriticalWard].PerkLevels[1].StatBonuses
            .Should().Contain(x => x.Stat == StatType.SpaceIncomingCriticalHitDowngradeCooldownMilliseconds);

        Stat(PerkType.BulwarkUnbreakable, 1, StatType.SpaceLowHullDefenseThresholdPercent).Should().Be(25);
        Stat(PerkType.BulwarkUnbreakable, 1, StatType.SpaceLowHullDefensePercentAdjustment).Should().Be(40);
        Stat(PerkType.BulwarkUnbreakable, 1, StatType.SpaceLowHullDefenseDurationSeconds).Should().Be(30);
        Stat(PerkType.BulwarkUnbreakable, 1, StatType.SpaceLowHullDefenseCooldownSeconds).Should().Be(180);
    }

    [Test]
    public void TheTraits_NeverSetTheGroundStatsThatAddUpWithAPilotsGroundPerks()
    {
        var groundStats = new[]
        {
            StatType.IncomingCriticalHitDowngradeToMinimumDamage, StatType.IncomingCriticalHitDowngradeCooldownMilliseconds,
            StatType.LowHPPhysicalDefenseThresholdPercent, StatType.LowHPPhysicalDefensePercentAdjustment,
            StatType.LowHPPhysicalDefenseDurationSeconds, StatType.LowHPPhysicalDefenseCooldownSeconds,
            StatType.HeavyVibrobladeDefenseAbilityEnmityBonus, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.ShieldEquippedPhysicalDefensePercentAdjustment
        };

        BuildPerks().Values
            .SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.StatBonuses)
            .Select(bonus => bonus.Stat)
            .Should().NotIntersectWith(groundStats);
    }

    [Test]
    public void TheBulwarkArithmetic_IsTheGroundsOwn()
    {
        // Shield Bash: a percent of Defense, rounded up.
        SpaceCombatMath.PercentOfDefense(100, 4).Should().Be(4);
        SpaceCombatMath.PercentOfDefense(101, 4).Should().Be(5);
        SpaceCombatMath.PercentOfDefense(0, 10).Should().Be(0);

        // Critical Ward: 16 s, 0.5 s less per MGT, never under 12 s.
        SpaceCombatMath.CriticalWardCooldownMilliseconds(0).Should().Be(16000);
        SpaceCombatMath.CriticalWardCooldownMilliseconds(4).Should().Be(14000);
        SpaceCombatMath.CriticalWardCooldownMilliseconds(8).Should().Be(12000);
        SpaceCombatMath.CriticalWardCooldownMilliseconds(40).Should().Be(12000);
        SpaceCombatMath.CriticalWardCooldownMilliseconds(-5).Should().Be(16000);
    }

    [Test]
    public void AngerStrike_WaitsOutTheWindowFromTheMomentDefianceWasUsed()
    {
        var used = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);

        SpaceBulwark.AngerStrikeSeconds(used, used, 30).Should().Be(30f);
        SpaceBulwark.AngerStrikeSeconds(used, used.AddSeconds(12), 30).Should().Be(18f);
        SpaceBulwark.AngerStrikeSeconds(used, used.AddSeconds(30), 30).Should().BeLessThanOrEqualTo(0f);
        SpaceBulwark.AngerStrikeSeconds(used, used, 0).Should().Be(0f, "a pilot without the perk gets no bonus");
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
    public void TheSpaceCode_ReadsTheBulwarkStatsFromTheRightPlaces()
    {
        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("SpaceBulwark.TryWardCriticalHit(target)");
        space.Should().Contain("StatType.SpaceDefensePercentAdjustment");
        space.Should().Contain("StatType.SpaceDamageTakenPercentAdjustment");
        space.Should().Contain("SpaceBulwark.Clear(player);");

        var bulwark = ReadServerSource("Service", "SpaceBulwark.cs");
        bulwark.Should().Contain("SpaceCombatEvents.HullDamaged += OnHullDamaged;");
        bulwark.Should().Contain("SpaceAbilities.RegisterRiderHandler(DefianceKey, OnDefianceHit);");
        bulwark.Should().Contain("e.CrossedBelow(threshold)");
    }
}
