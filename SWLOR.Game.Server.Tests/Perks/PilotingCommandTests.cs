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
/// The Command style of Piloting (plan section 6, Appendix A): the perk rows, prices and ranks of the Design Bible
/// tab "Piloting (Revamp)", the space abilities they grant and the stats the traits set.
/// </summary>
public class PilotingCommandTests
{
    // Rank, SP price, STM and cooldown are the Bible rows. The numbers are the ground Leadership and First Aid
    // perks'. Formation is an aura and costs no STM.
    private static readonly (FeatType Feat, string Name, int Rank, int Price, int Stamina, float Cooldown)[] Abilities =
    {
        (FeatType.CommandEmergencyRepair1, "Emergency Repair I", 2, 2, 4, 10.5f),
        (FeatType.CommandFocusFire1, "Focus Fire I", 10, 2, 4, 24f),
        (FeatType.CommandEmergencyRepair2, "Emergency Repair II", 12, 2, 6, 10.5f),
        (FeatType.CommandBreakFormation1, "Break Formation I", 18, 3, 7, 45f),
        (FeatType.CommandFormation, "Formation", 20, 4, 0, 30f),
        (FeatType.CommandEmergencyRepair3, "Emergency Repair III", 28, 3, 7, 10.5f),
        (FeatType.CommandFocusFire2, "Focus Fire II", 30, 4, 6, 24f),
        (FeatType.CommandBreakFormation2, "Break Formation II", 32, 3, 9, 45f),
        (FeatType.CommandFocusFire3, "Focus Fire III", 38, 4, 9, 30f),
        (FeatType.CommandEmergencyRepair4, "Emergency Repair IV", 40, 5, 8, 10.5f),
        (FeatType.CommandFleetAction, "Fleet Action", 50, 6, 15, 90f),
    };

    private static Dictionary<FeatType, AbilityDetail> BuildAbilities()
    {
        var abilities = new Dictionary<FeatType, AbilityDetail>();
        foreach (var definition in new IAbilityListDefinition[]
                 {
                     new EmergencyRepairAbilityDefinition(),
                     new FocusFireAbilityDefinition(),
                     new BreakFormationAbilityDefinition(),
                     new FormationAbilityDefinition(),
                     new FleetActionAbilityDefinition()
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
        var definition = new PilotingCommandPerkDefinition();
        foreach (var method in typeof(PilotingCommandPerkDefinition)
                     .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0)
                     .OrderBy(x => x.MetadataToken))
        {
            method.Invoke(definition, null);
        }

        var builder = typeof(PilotingCommandPerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition)!;
        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    [Test]
    public void EveryCommandAbility_IsASpaceOnlyPilotingAbilityWithTheBibleCostAndCooldown()
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

        abilities.Should().HaveCount(Abilities.Length);
    }

    [Test]
    public void EmergencyRepair_IsASupportCastAtAPartyShipThatAcceptsTheCasterToo()
    {
        var abilities = BuildAbilities();

        foreach (var feat in new[]
                 {
                     FeatType.CommandEmergencyRepair1, FeatType.CommandEmergencyRepair2,
                     FeatType.CommandEmergencyRepair3, FeatType.CommandEmergencyRepair4
                 })
        {
            var repair = abilities[feat];
            repair.IsHostileAbility.Should().BeFalse(feat.ToString());
            repair.IsSingleTargetAbility.Should().BeTrue(feat.ToString());
            repair.RequiresTarget.Should().BeTrue(feat.ToString());
            repair.AllowsSelfQuickCast.Should().BeTrue($"{feat} accepts its own ship");
            repair.HasExplicitMaxRange.Should().BeTrue(feat.ToString());
            repair.MaxRange.Should().Be(PilotingAbilityConfiguration.CommandRangeCeiling);
            repair.CustomValidation.Should().NotBeNull($"{feat} must turn away a ship that is not in the party or in range");
            repair.Targeting.Should().BeNull($"{feat} takes its target from the cursor, not an area marker");
        }
    }

    [Test]
    public void TheOrders_AreSelfCenteredAreasSizedByCommandRange()
    {
        var abilities = BuildAbilities();
        var orders = new[]
        {
            (FeatType.CommandFocusFire1, Spell.CommandFocusFire1), (FeatType.CommandFocusFire2, Spell.CommandFocusFire2),
            (FeatType.CommandFocusFire3, Spell.CommandFocusFire3), (FeatType.CommandFormation, Spell.CommandFormation),
            (FeatType.CommandFleetAction, Spell.CommandFleetAction)
        };

        foreach (var (feat, spell) in orders)
        {
            var order = abilities[feat];
            order.IsHostileAbility.Should().BeFalse(feat.ToString());
            order.IsAreaAbility.Should().BeTrue(feat.ToString());
            order.RequiresTarget.Should().BeFalse($"{feat} shows no cursor");
            order.Targeting!.Spell.Should().Be(spell);
            order.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
            order.Targeting.Flags.Should().Be(AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf);
            order.Targeting.SizeX.Should().Be(SpaceRadii.CommandRange);
            order.Targeting.SizeResolver.Should().NotBeNull($"{feat}'s marker follows the pilot's command range");
        }
    }

    [Test]
    public void BreakFormation_IsAHostileSelfCenteredAreaThatNeedsAnEnemyInRange()
    {
        var abilities = BuildAbilities();

        foreach (var (feat, spell) in new[]
                 {
                     (FeatType.CommandBreakFormation1, Spell.CommandBreakFormation1),
                     (FeatType.CommandBreakFormation2, Spell.CommandBreakFormation2)
                 })
        {
            var debuff = abilities[feat];
            debuff.IsHostileAbility.Should().BeTrue(feat.ToString());
            debuff.IsAreaAbility.Should().BeTrue(feat.ToString());
            debuff.RequiresTarget.Should().BeFalse($"{feat} shows no cursor");
            debuff.CustomValidation.Should().NotBeNull($"{feat} must turn the pilot away before paying when no enemy is in range");
            debuff.Targeting!.Spell.Should().Be(spell);
            debuff.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
            debuff.Targeting.Flags.Should().Be(AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf);
            debuff.Targeting.SizeResolver.Should().NotBeNull();
        }
    }

    [Test]
    public void TheShipAccuracyStat_IsSetByTheGroundStatusesTheOrdersReuse()
    {
        var flash = new FlashStatusEffect(12);
        flash.StatGroup.Stats[StatType.SpaceAccuracyPercentAdjustment].Should().Be(-12);
        flash.StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment].Should().Be(-12, "the ground status is unchanged");

        var weakened = new WeakenedStatusEffect(15);
        weakened.StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15, "a ship reads Attack %");

        var statuses = ReadServerSource("Feature", "StatusEffectDefinition");
        statuses.Should().Contain("StatType.SpaceAccuracyPercentAdjustment] = ScaleBySourceSocial(5, 7)");
        statuses.Should().Contain("StatType.SpaceAccuracyPercentAdjustment] = ScaleBySourceSocial(6, 8)");
        statuses.Should().Contain("StatType.SpaceLeadershipDamageTakenPercentAdjustment] = reduction");
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

        perks.Values.Should().OnlyContain(perk => perk.Category == PerkCategoryType.PilotingCommand);
        perks.Should().HaveCount(9);
        perks.Values.SelectMany(perk => perk.PerkLevels.Values).Sum(level => level.Price)
            .Should().Be(60, "a Command style costs 60 SP like the other styles");
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

        Stat(PerkType.CommandMarkTarget, 1, StatType.SpaceMarkTargetDamagePercent).Should().Be(8);
        Stat(PerkType.CommandMarkTarget, 1, StatType.SpaceMarkTargetMaximumDamagePercent).Should().Be(10);
        Stat(PerkType.CommandMarkTarget, 1, StatType.SpaceMarkTargetDurationSeconds).Should().Be(30);
        Stat(PerkType.CommandMarkTarget, 1, StatType.SpaceMarkTargetCooldownSeconds).Should().Be(45);
        perks[PerkType.CommandMarkTarget].PerkLevels[1].StatBonuses
            .Should().NotContain(x => x.Stat == StatType.SpaceMarkTargetAccuracyPercent);
        Stat(PerkType.CommandMarkTarget, 2, StatType.SpaceMarkTargetDamagePercent).Should().Be(12);
        Stat(PerkType.CommandMarkTarget, 2, StatType.SpaceMarkTargetMaximumDamagePercent).Should().Be(15);
        Stat(PerkType.CommandMarkTarget, 2, StatType.SpaceMarkTargetAccuracyPercent).Should().Be(10);
        Stat(PerkType.CommandMarkTarget, 2, StatType.SpaceMarkTargetMaximumAccuracyPercent).Should().Be(12);
        Stat(PerkType.CommandMarkTarget, 2, StatType.SpaceMarkTargetCooldownSeconds).Should().Be(45);

        foreach (var (rank, percent) in new[] { (1, 5), (2, 10), (3, 15) })
            Stat(PerkType.CommandRepairEfficiency, rank, StatType.SpaceRepairEfficiencyPercentAdjustment).Should().Be(percent);

        Stat(PerkType.CommandCrewMorale, 1, StatType.SpaceCrewEffectivenessPercentAdjustment).Should().Be(10);

        Stat(PerkType.CommandPresence, 1, StatType.SpaceCommandRangeBonusMeters).Should().Be(16, "the ground's 4 m x 4");
        Stat(PerkType.CommandPresence, 1, StatType.SpaceCommandDurationBonusBaseSeconds).Should().Be(4);
        Stat(PerkType.CommandPresence, 1, StatType.SpaceCommandDurationBonusMaximumSeconds).Should().Be(5);
    }

    [Test]
    public void TheTraits_NeverSetTheGroundStatsThatAddUpWithAPilotsGroundPerks()
    {
        var groundStats = new[]
        {
            StatType.HostileAbilityPartyBuffDamagePercent, StatType.HostileAbilityPartyBuffMaximumDamagePercent,
            StatType.HostileAbilityPartyBuffAccuracyPercent, StatType.HostileAbilityPartyBuffMaximumAccuracyPercent,
            StatType.HostileAbilityPartyBuffDurationSeconds, StatType.HostileAbilityPartyBuffCooldownSeconds,
            StatType.HostileAbilityPartyBuffNameStrRef, StatType.HostileAbilityPartyBuffIcon,
            StatType.OutgoingAbilityHealingPercentAdjustment,
            StatType.LeadershipCommandRadiusBonusMeters, StatType.LeadershipCommandDurationBonusBaseSeconds,
            StatType.LeadershipCommandDurationBonusMaximumSeconds
        };

        BuildPerks().Values
            .SelectMany(perk => perk.PerkLevels.Values)
            .SelectMany(level => level.StatBonuses)
            .Select(bonus => bonus.Stat)
            .Should().NotIntersectWith(groundStats);
    }

    [Test]
    public void EmergencyRepair_DoublesTheGroundMedKitsFlatAmountForAShipsHull()
    {
        // Med Kit I-IV restore 11, 42, 80 and 108 HP plus 2, 3.5, 5 and 6% of maximum HP. A ship's hull plus shield is
        // about twice a ground Normal HP in the balance model, and a repair restores hull only.
        var abilities = BuildAbilities();
        abilities.Keys.Should().Contain(FeatType.CommandEmergencyRepair4);

        var perks = BuildPerks();
        var flats = new[] { 22, 84, 160, 216 };
        var percents = new[] { "2%", "3.5%", "5%", "6%" };
        for (var rank = 1; rank <= 4; rank++)
        {
            perks[PerkType.CommandEmergencyRepair].PerkLevels[rank].Description
                .Should().Be($"Repair an allied ship within command range for {flats[rank - 1]} hull plus {percents[rank - 1]} of its max hull plus WIL scaling.");
        }

        // A 425 hull ship (a rank 45 Fighter in the balance model) repaired by a 10 WIL and a 26 WIL pilot.
        EmergencyRepairAbilityDefinition.GetRepairAmount(new HealAmount(22, 2f), 425, 10).Should().Be(20);
        EmergencyRepairAbilityDefinition.GetRepairAmount(new HealAmount(22, 2f), 425, 26).Should().Be(39);
        EmergencyRepairAbilityDefinition.GetRepairAmount(new HealAmount(216, 6f), 425, 10).Should().Be(152);
        EmergencyRepairAbilityDefinition.GetRepairAmount(new HealAmount(216, 6f), 425, 26).Should().Be(303);
    }

    [Test]
    public void RepairEfficiency_AddsItsPercentRoundedUp_AndZeroLeavesARepairAlone()
    {
        SpaceCombatMath.ApplyRepairEfficiency(100, 0).Should().Be(100);
        SpaceCombatMath.ApplyRepairEfficiency(100, 5).Should().Be(105);
        SpaceCombatMath.ApplyRepairEfficiency(101, 15).Should().Be(117, "rounded up, as the ground's healing is");
        SpaceCombatMath.ApplyRepairEfficiency(0, 15).Should().Be(0);
    }

    [Test]
    public void CrewMorale_RaisesTheCrewEffectivenessMultiplier_AndLeadershipHasAHookThatAddsNothingYet()
    {
        SpaceCrew.EffectivenessMultiplier(0, 0).Should().Be(1.0);
        SpaceCrew.EffectivenessMultiplier(10, 0).Should().BeApproximately(1.10, 1e-9);
        SpaceCrew.EffectivenessMultiplier(10, 5).Should().BeApproximately(1.15, 1e-9);
        SpaceCrew.EffectivenessMultiplier(-5, -5).Should().Be(1.0, "a penalty never lowers it below normal");

        SpaceCommand.GetLeadershipBonusPercent(0u).Should().Be(0, "the crew update fills in Leadership's scaling");
        SpaceCombatMath.ScaleByPercent(8, 0).Should().Be(8);
        SpaceCombatMath.ScaleByPercent(10, 50).Should().Be(15);
    }

    [Test]
    public void MarkTarget_WaitsOutItsCooldown()
    {
        var readyAt = new DateTime(2026, 10, 27, 12, 0, 45, DateTimeKind.Utc);

        SpaceCommand.IsMarkTargetReady(readyAt, readyAt.AddSeconds(-1)).Should().BeFalse();
        SpaceCommand.IsMarkTargetReady(readyAt, readyAt).Should().BeTrue();
        SpaceCommand.IsMarkTargetReady(readyAt, readyAt.AddSeconds(30)).Should().BeTrue();
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        if (Directory.Exists(full))
        {
            return string.Join(
                "\n",
                Directory.GetFiles(full, "*.cs").Select(file => File.ReadAllText(file).Replace("\r\n", "\n")));
        }

        return File.ReadAllText(full).Replace("\r\n", "\n");
    }

    [Test]
    public void TheSpaceCode_ReadsTheCommandStatsFromTheRightPlaces()
    {
        var space = ReadServerSource("Service", "Space.cs");
        space.Should().Contain("StatType.SpaceAccuracyPercentAdjustment");
        space.Should().Contain("StatType.SpaceLeadershipDamageTakenPercentAdjustment");
        space.Should().Contain("SpaceCommand.Clear(player);");

        var command = ReadServerSource("Service", "SpaceCommand.cs");
        command.Should().Contain("SpaceCombatEvents.HostileAbilityUsed += OnHostileAbilityUsed;");
        command.Should().Contain("SpaceAuras.RegisterRangeBonus(GetRangeBonus);");
        command.Should().Contain("StatType.SpaceMarkTargetDamagePercent");

        var actions = ReadServerSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs");
        actions.Should().Contain("amount = ApplyRepairEfficiency(activator, amount);");
        ReadServerSource("Feature", "ShipModuleDefinition", "RevampModuleDefinition.cs")
            .Should().Contain("ShipModuleActions.ApplyRepairEfficiency(activator, step.Repair)");
        ReadServerSource("Feature", "ShipModuleDefinition", "RepairFieldGeneratorModuleDefinition.cs")
            .Should().Contain("ShipModuleActions.ApplyRepairEfficiency(activator, repairAmount + bonusRecovery)");

        var crew = ReadServerSource("Service", "SpaceService", "SpaceCrew.cs");
        crew.Should().Contain("StatType.SpaceCrewEffectivenessPercentAdjustment");
        crew.Should().Contain("SpaceCommand.GetLeadershipBonusPercent(ship)");

        var formation = ReadServerSource("Feature", "AbilityDefinition", "Piloting", "FormationAbilityDefinition.cs");
        formation.Should().Contain("SpaceAuras.ApplyAura(activator, aura)");
        formation.Should().Contain("typeof(WatchfulPresence2StatusEffect)");
    }
}
