using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Piloting;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// The Ace style of Piloting (plan section 6, Appendix A): the perk rows, prices and ranks of the Design Bible tab
/// "Piloting", the space abilities they grant and the stats the traits set.
/// </summary>
public class PilotingAceTests
{
    // Rank, SP price, STM and cooldown are the Bible rows. The numbers are the ground Pistol and Rifle perks'.
    private static readonly (FeatType Feat, string Name, int Rank, int Price, int Stamina, float Cooldown)[] Abilities =
    {
        (FeatType.AceDisablingShot1, "Disabling Shot I", 2, 2, 3, 18f),
        (FeatType.AceDoubleShot1, "Double Shot I", 10, 2, 4, 24f),
        (FeatType.AceDisablingShot2, "Disabling Shot II", 12, 2, 5, 18f),
        (FeatType.AcePointBlankBurst1, "Point Blank Burst I", 18, 3, 6, 30f),
        (FeatType.AceDisablingShot3, "Disabling Shot III", 28, 3, 8, 18f),
        (FeatType.AceDoubleShot2, "Double Shot II", 30, 4, 8, 24f),
        (FeatType.AcePointBlankBurst2, "Point Blank Burst II", 32, 3, 9, 30f),
        (FeatType.AceDoubleShot3, "Double Shot III", 38, 4, 10, 24f),
        (FeatType.AceDisablingShot4, "Disabling Shot IV", 40, 5, 12, 18f),
        (FeatType.AceInTheHole, "Ace in the Hole", 50, 6, 15, 90f),
    };

    private static Dictionary<FeatType, AbilityDetail> BuildAbilities()
    {
        var abilities = new Dictionary<FeatType, AbilityDetail>();
        foreach (var definition in new IAbilityListDefinition[]
                 {
                     new DisablingShotAbilityDefinition(),
                     new DoubleShotAbilityDefinition(),
                     new PointBlankBurstAbilityDefinition(),
                     new AceStanceAbilityDefinition(),
                     new AceInTheHoleAbilityDefinition()
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
        var definition = new PilotingAcePerkDefinition();
        foreach (var method in typeof(PilotingAcePerkDefinition)
                     .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0)
                     .OrderBy(x => x.MetadataToken))
        {
            method.Invoke(definition, null);
        }

        var builder = typeof(PilotingAcePerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    [Test]
    public void EveryAceAbility_IsASpaceOnlyPilotingAbilityWithTheBibleCostAndCooldown()
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
            ability.CustomValidation.Should().NotBeNull($"{name} must turn a ship with no weapon away before it pays");
        }
    }

    [Test]
    public void TargetedAceShots_NeedATarget_AndTheBurstIsSelfCentered()
    {
        var abilities = BuildAbilities();

        foreach (var feat in new[]
                 {
                     FeatType.AceDisablingShot1, FeatType.AceDisablingShot2, FeatType.AceDisablingShot3,
                     FeatType.AceDisablingShot4, FeatType.AceDoubleShot1, FeatType.AceDoubleShot2,
                     FeatType.AceDoubleShot3, FeatType.AceInTheHole
                 })
        {
            abilities[feat].RequiresTarget.Should().BeTrue(feat.ToString());
            abilities[feat].IsSingleTargetAbility.Should().BeTrue(feat.ToString());
        }

        foreach (var (feat, spell) in new[]
                 {
                     (FeatType.AcePointBlankBurst1, Spell.AcePointBlankBurst1),
                     (FeatType.AcePointBlankBurst2, Spell.AcePointBlankBurst2)
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
    public void AceStance_IsThePistolsSkirmisherStatusAsTheOnlyStance()
    {
        var stance = BuildAbilities()[FeatType.AceStance];

        stance.CanBeUsedInSpace.Should().BeTrue();
        stance.IsHostileAbility.Should().BeFalse();
        stance.ActivationDelay(0, 0, 0).Should().Be(0f);
        stance.RecastDelay(0).Should().Be(30f);
        stance.StatusEffectTypesRemovedOnPerkRefund.Should().Contain(typeof(SkirmisherStanceStatusEffect));

        var status = new SkirmisherStanceStatusEffect();
        status.StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(15);
        status.StatGroup.Stats[StatType.MovementSpeedPercentAdjustment].Should().Be(10);
        status.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
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

        perks.Values.Should().OnlyContain(perk => perk.Category == PerkCategoryType.PilotingAce);
        perks.Values.SelectMany(perk => perk.PerkLevels.Values).Sum(level => level.Price)
            .Should().Be(60, "an Ace style costs 60 SP like the other styles");
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

        Stat(PerkType.AceMobileFootwork, 1, StatType.SpaceHostileAbilityUsedEvasionPercentAdjustment).Should().Be(8);
        Stat(PerkType.AceMobileFootwork, 1, StatType.SpaceHostileAbilityUsedEvasionDurationSeconds).Should().Be(30);
        Stat(PerkType.AceKitingInstinct, 1, StatType.SpaceHostileAbilityUsedMovementSpeedPercentAdjustment).Should().Be(10);
        Stat(PerkType.AceKitingInstinct, 1, StatType.SpaceHostileAbilityUsedMovementSpeedDurationSeconds).Should().Be(30);
        Stat(PerkType.AceHighNoon, 1, StatType.CriticalDamageHighHPTargetThresholdPercent).Should().Be(70);
        Stat(PerkType.AceHighNoon, 1, StatType.CriticalDamageHighHPTargetPercentAdjustment).Should().Be(15);

        foreach (var (rank, perHit, maximum) in new[] { (1, 1, 5), (2, 2, 10), (3, 3, 15) })
        {
            Stat(PerkType.AceSustainedFire, rank, StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit).Should().Be(perHit);
            Stat(PerkType.AceSustainedFire, rank, StatType.SpaceEnergyRepeatedTargetDamageBonusMax).Should().Be(maximum);
            Stat(PerkType.AceSustainedFire, rank, StatType.SpaceEnergyRepeatedTargetDamageDurationSeconds).Should().Be(30);
        }
    }

    [Test]
    public void EvasiveReload_DiscountsOnlyThePilotsOwnAbilities()
    {
        var level = BuildPerks()[PerkType.AceEvasiveReload].PerkLevels[1];

        level.StatBonuses.Single(x => x.Stat == StatType.AvoidedAttackNextSkillAbilitySkillType).Calculate(0)
            .Should().Be((int)SkillType.Piloting, "a ground ability must not use the space discount");
        level.StatBonuses.Single(x => x.Stat == StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment).Calculate(0).Should().Be(-3);
        level.StatBonuses.Single(x => x.Stat == StatType.AvoidedAttackNextSkillAbilityWindowSeconds).Calculate(0).Should().Be(30);
    }

    [Test]
    public void AceInTheHole_GrantsTheCycleLimitedDeadMansHand()
    {
        var status = new DeadMansHandStatusEffect();

        status.RemainingCycles.Should().Be(3);
        status.StatGroup.Stats[StatType.WeaponCriticalRatePercentAdjustment].Should().Be(20);
        status.StatGroup.Stats[StatType.WeaponAttackNoDelay].Should().Be(1);

        status.ConsumeWeaponCycle();
        status.ConsumeWeaponCycle();
        status.IsFlaggedForRemoval.Should().BeFalse();
        status.ConsumeWeaponCycle();
        status.RemainingCycles.Should().Be(0);
        status.IsFlaggedForRemoval.Should().BeTrue("the effect ends after its third weapon cycle");

        status.ConsumeWeaponCycle();
        status.RemainingCycles.Should().Be(0, "a spent effect cannot go below zero");
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
    public void TheSpaceCode_ReadsTheAceTraitsFromTheRightPlaces()
    {
        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("if (!shot.IsAbility)\n                criticalRate += Stat.GetStatAdjustment(source, StatType.WeaponCriticalRatePercentAdjustment);",
            "weapon cycles, not ability shots, take the weapon critical rate");
        space.Should().Contain("SpaceSustainedFire.RegisterHit(activator, target, shot.Family)");
        space.Should().Contain("SpaceHostileAbilityRewards.Clear(player);");
        space.Should().Contain("SpaceSustainedFire.Clear(player);");

        var toggles = ReadServerSource("Service", "SpaceToggles.cs");
        toggles.Should().Contain("SpaceWeaponCycles.HasNoDelayCycle(creature, detail.Type)");
        toggles.Should().Contain("SpaceWeaponCycles.ConsumeCycle(creature, detail.Type);");
    }
}
