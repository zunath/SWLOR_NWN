using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Engineering and Espionage seat-skill bonuses (plan section 4, W11n): repairs, the Hypermatter Injector, power
/// routing, jams and paints, the cloak reveal range, and the cloak edge cases settled with them (docked ships, a restart,
/// the Afterburner, Ion Web and Interdictor). Numbers are checked by tools/SpaceRevampBalance/checks.py section Q.
/// </summary>
public class SpaceSeatSkillTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
    private const uint Ship = 0x10;
    private const uint Hunter = 0x20;

    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return directory!.FullName;
    }

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine));

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, signature);

        var depth = 0;
        for (var index = source.IndexOf('{', start); index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(start, index - start + 1);
        }

        throw new InvalidOperationException($"Could not extract '{signature}'.");
    }

    // The per-rank numbers

    [TestCase(0, 0, 0, 0, 0)]
    [TestCase(1, 0, 1, 0, 1)]
    [TestCase(10, 2, 5, 2, 10)]
    [TestCase(25, 5, 13, 5, 25)]
    [TestCase(40, 8, 20, 8, 40)]
    [TestCase(50, 10, 25, 10, 50)]
    [TestCase(80, 10, 25, 10, 50)]
    [TestCase(-5, 0, 0, 0, 0)]
    public void EachBonus_GrowsEvenlyWithRank_AndStopsAtFifty(int rank, int support, int routing, int ewar, int reveal)
    {
        ShipSeatSkillRules.EngineeringSupportPercent(rank).Should().Be(support);
        ShipSeatSkillRules.EngineeringRoutingPercent(rank).Should().Be(routing);
        ShipSeatSkillRules.EspionageElectronicWarfarePercent(rank).Should().Be(ewar);
        ShipSeatSkillRules.EspionageRevealPercent(rank).Should().Be(reveal);
    }

    [Test]
    public void TheMaximums_FollowTheExistingStandards()
    {
        ShipSeatSkillRules.MaximumRank.Should().Be(50);
        ShipSeatSkillRules.EngineeringRoutingMaximumPercent.Should().Be(SWLOR.Game.Server.Service.SpaceCommand.LeadershipMaximumBonusPercent,
            "routing strength grows as Leadership's orders do");
        ShipSeatSkillRules.EngineeringSupportMaximumPercent.Should().BeLessThan(15, "below the Repair Efficiency perk line's +15%");
        ShipSeatSkillRules.EspionageElectronicWarfareMaximumPercent.Should().Be(ShipSeatSkillRules.EngineeringSupportMaximumPercent);
        CloakTuning.EspionageRevealPercentPerRank.Should().Be(ScanningTuning.EspionagePercentPerRank, "the Probe Launcher's scan bonus");
        CloakTuning.EspionageRevealMaximumRank.Should().Be(50);
    }

    [Test]
    public void EachModule_ReadsItsSeatSkill()
    {
        foreach (var type in new[] { ShipModuleType.ShieldRepairer, ShipModuleType.HullRepairer, ShipModuleType.RepairFieldGenerator, ShipModuleType.HypermatterInjector })
        {
            ShipSeatSkillRules.GetBonusSkill(type).Should().Be(SkillType.Engineering, type.ToString());
            ShipSeatSkillRules.GetModuleBonusPercent(type, 50).Should().Be(10);
        }

        foreach (var type in new[] { ShipModuleType.Jammer, ShipModuleType.TargetPainter, ShipModuleType.CapitalEwar })
        {
            ShipSeatSkillRules.GetBonusSkill(type).Should().Be(SkillType.Espionage, type.ToString());
            ShipSeatSkillRules.GetModuleBonusPercent(type, 50).Should().Be(10);
        }

        foreach (var type in new[] { ShipModuleType.CombatLaser, ShipModuleType.IonWeb, ShipModuleType.Interdictor, ShipModuleType.Afterburner,
                     ShipModuleType.CloakingDevice, ShipModuleType.ShieldHardener, ShipModuleType.ProbeLauncher })
        {
            ShipSeatSkillRules.GetBonusSkill(type).Should().Be(SkillType.Invalid, type.ToString());
            ShipSeatSkillRules.GetModuleBonusPercent(type, 50).Should().Be(0);
        }
    }

    [Test]
    public void TheModuleBonuses_AreWiredIntoEveryModuleThatTakesThem()
    {
        var definitions = ReadSource("Feature", "ShipModuleDefinition", "RevampModuleDefinition.cs");
        ExtractMethod(definitions, "private void Repairer(").Should().Contain("Space.ApplySeatSkillBonus(activator, type,");
        definitions.Should().Contain("Repairer(step, ShipModuleType.ShieldRepairer, ShipModuleActions.RepairShield)")
            .And.Contain("Repairer(step, ShipModuleType.HullRepairer, ShipModuleActions.RepairHull)");
        ExtractMethod(definitions, "private void RepairField(").Should().Contain("Space.ApplySeatSkillBonus(activator, ShipModuleType.RepairFieldGenerator,");
        ExtractMethod(definitions, "private void HypermatterInjector(").Should().Contain("Space.ApplySeatSkillBonus(activator, ShipModuleType.HypermatterInjector,");
        ExtractMethod(definitions, "private void EWarArray(").Should().Contain("Space.ApplySeatSkillBonus(activator, ShipModuleType.CapitalEwar,");

        var ewar = ReadSource("Service", "SpaceElectronicWarfare.cs");
        ExtractMethod(ewar, "public static void ApplyJam(").Should().Contain("ApplySeatSkillBonus(activator, ShipModuleType.Jammer, step.TargetAccuracy)");
        ExtractMethod(ewar, "public static void ApplyPaint(").Should().Contain("ApplySeatSkillBonus(activator, ShipModuleType.TargetPainter, step.PaintPercent)");

        // Whoever works the module: the crew member at its seat, otherwise the pilot; droids, NPCs and DMs have no rank.
        var bonus = ExtractMethod(ReadSource("Service", "SpaceCrewStations.cs"), "public static int GetSeatSkillBonusPercent(");
        bonus.Should().Contain("SpaceCrew.GetStatSource(ship)").And.Contain("GetIsPC(source)").And.Contain("!GetIsDM(source)");

        // A Command ability's repair (Emergency Repair) is not a module and takes no Engineering bonus.
        ExtractMethod(ReadSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs"), "public static int ApplyRepairEfficiency(")
            .Should().NotContain("SeatSkill");
    }

    // Power routing

    [Test]
    public void Engineering_MakesTheRoutingsGainLarger_ButNeverItsPenalties()
    {
        PowerRoutingRules.WeaponCapacitorPercent(ShipPowerRouting.Weapons, 25).Should().Be(-31);
        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Shields, 25).Should().Be(19);
        PowerRoutingRules.SpeedPercent(ShipPowerRouting.Engines, 25).Should().Be(25);

        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Weapons, 25).Should().Be(-10);
        PowerRoutingRules.SpeedPercent(ShipPowerRouting.Weapons, 25).Should().Be(-10);
        PowerRoutingRules.WeaponDamagePercent(ShipPowerRouting.Shields).Should().Be(-5);
        PowerRoutingRules.SpeedPercent(ShipPowerRouting.Shields, 25).Should().Be(-10);
        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Engines, 25).Should().Be(-10);
        PowerRoutingRules.WeaponCapacitorPercent(ShipPowerRouting.Balanced, 25).Should().Be(0);
        PowerRoutingRules.SpeedPercent(ShipPowerRouting.Balanced, 25).Should().Be(0);

        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Shields, -50).Should().Be(15, "a negative bonus never weakens the gain");
        PowerRoutingRules.ShieldRegenPercent(ShipPowerRouting.Shields).Should().Be(15, "no rank keeps the W8b numbers");
    }

    [Test]
    public void TheRoutingMath_TakesTheBonus()
    {
        PowerRoutingRules.ApplyShieldRegen(10, ShipPowerRouting.Shields, 25).Should().BeApproximately(11.9, 1e-9);
        PowerRoutingRules.ApplySpeed(1.0, ShipPowerRouting.Engines, 25).Should().BeApproximately(1.25, 1e-9);
        PowerRoutingRules.ApplySpeed(1.0, ShipPowerRouting.Weapons, 25).Should().BeApproximately(0.9, 1e-9);
        PowerRoutingRules.ApplyCapacitorCost(5, ShipPowerRouting.Weapons, true, 25).Should().Be(3, "5 x 0.69 = 3.45");
        PowerRoutingRules.ApplyCapacitorCost(5, ShipPowerRouting.Weapons, true).Should().Be(4, "5 x 0.75 = 3.75");
        PowerRoutingRules.ApplyCapacitorCost(4, ShipPowerRouting.Weapons, false, 25).Should().Be(4, "repairers keep their cost");
        PowerRoutingRules.GetEffect(ShipPowerRouting.Weapons, 25).Should().StartWith("weapons use 31% less capacitor");
        PowerRoutingRules.GetEffect(ShipPowerRouting.Shields, 25).Should().StartWith("shield regen +19%");
        PowerRoutingRules.GetEffect(ShipPowerRouting.Engines, 25).Should().StartWith("speed +25%");
        new ShipStatus().PowerRoutingBonusPercent.Should().Be(0, "saved ships load with no bonus");
    }

    [Test]
    public void TheEngineersRank_IsSavedWithTheRouting_AndEveryReaderTakesIt()
    {
        var set = ExtractMethod(ReadSource("Service", "SpacePowerRouting.cs"), "public static bool SetPowerRouting(");
        set.Should().Contain("ShipSeatSkillRules.EngineeringRoutingPercent(GetPlayerSkillRank(player, SkillType.Engineering))")
            .And.Contain("PowerRoutingRules.GetEffect(mode, status.PowerRoutingBonusPercent)");

        var routing = ReadSource("Service", "SpacePowerRouting.cs");
        routing.Should().Contain("PowerRoutingRules.ApplySpeed(status.SpeedMultiplier, status.PowerRouting, status.PowerRoutingBonusPercent)")
            .And.Contain("ShipToggleRules.IsOffensive(detail.Type), status.PowerRoutingBonusPercent)");
        ExtractMethod(ReadSource("Service", "Space.cs"), "public static double GetShipShieldRegenPerSecond(")
            .Should().Contain("shipStatus.PowerRoutingBonusPercent");
        ReadSource("Service", "SpaceService", "ShipHudStats.cs").Should().Contain("status.PowerRouting, status.PowerRoutingBonusPercent)");

        // Every reset to Balanced (launch, the Engineer leaving, destruction) clears the bonus too.
        foreach (var file in new[] { "Space.cs", "SpaceCrewStations.cs", "SpaceDeath.cs" })
        {
            var source = ReadSource("Service", file);
            var resets = source.Split("PowerRouting = ShipPowerRouting.Balanced;").Length - 1;
            var clears = source.Split("PowerRoutingBonusPercent = 0;").Length - 1;
            resets.Should().BeGreaterThan(0, file);
            clears.Should().Be(resets, file);
        }
    }

    [Test]
    public void TheRoutingFeatDescriptions_NameTheEngineeringBonus()
    {
        using var tlk = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(Root(), "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json")));
        var text = tlk.RootElement.GetProperty("entries").EnumerateArray()
            .ToDictionary(x => x.GetProperty("id").GetInt32(), x => x.TryGetProperty("text", out var t) ? t.GetString() : string.Empty);

        var bonus = $"Engineering rank makes this gain larger: 0.5% per rank, up to {ShipSeatSkillRules.EngineeringRoutingMaximumPercent}% larger at rank {ShipSeatSkillRules.MaximumRank}. The penalties do not change.";
        foreach (var id in new[] { 193137, 193139, 193141 })
        {
            text[id].Should().Contain(bonus, id.ToString());
            text[id].All(c => c < 128).Should().BeTrue("game text is ASCII");
        }

        text[193135].Should().NotContain("Engineering rank", "Balanced has no gain");
    }

    // Cloak reveal

    [Test]
    public void Espionage_StretchesTheRevealRange_UpToHalfAgain()
    {
        ShipCloakRules.GetObserverRevealMeters(10, 0).Should().BeApproximately(10, 1e-9);
        ShipCloakRules.GetObserverRevealMeters(10, 50).Should().BeApproximately(15, 1e-9);
        ShipCloakRules.GetObserverRevealMeters(5, 50).Should().BeApproximately(7.5, 1e-9, "Mk X and the Barracuda");
        ShipCloakRules.GetObserverRevealMeters(10, 500).Should().BeApproximately(15, 1e-9, "capped at +50%");
        ShipCloakRules.GetObserverRevealMeters(10, -20).Should().BeApproximately(10, 1e-9);

        ShipCloakRules.IsHidden(ShipCloakPhase.Cloaked, false, 12, 10, false).Should().BeTrue();
        ShipCloakRules.IsHidden(ShipCloakPhase.Cloaked, false, 12, 10, false, 20).Should().BeFalse("12 m is within 10 x 1.2");
        ShipCloakRules.IsHidden(ShipCloakPhase.Cloaked, false, 15.1, 10, false, 50).Should().BeTrue();
        ShipCloakRules.IsHidden(ShipCloakPhase.Cloaked, false, 15, 10, false, 50).Should().BeFalse();
    }

    [Test]
    public void TheCloakBook_TakesTheObserversBonus()
    {
        var book = new ShipCloakBook();
        book.Begin(Ship, Now, new ShipCloakProfile { EngageSeconds = 2, RevealMeters = 10, SpeedMultiplier = 0.5 }, 21);
        var cloaked = Now.AddSeconds(3);

        book.IsHiddenFrom(Ship, Hunter, false, 14, cloaked).Should().BeTrue();
        book.IsHiddenFrom(Ship, Hunter, false, 14, cloaked, 40).Should().BeFalse();
        book.IsHiddenFrom(Ship, Hunter, false, 16, cloaked, 50).Should().BeTrue("beyond the largest stretch");
    }

    [Test]
    public void AShipSeesThroughACloak_WithItsBestEspionageRank()
    {
        ShipSeatSkillRules.ShipEspionageRank(10, 40).Should().Be(40, "the Sensors officer's rank");
        ShipSeatSkillRules.ShipEspionageRank(45, 0).Should().Be(45, "an officer never shortens the pilot's sight");
    }

    [Test]
    public void TheRevealBonus_IsWiredIntoEveryVisibilityCheck()
    {
        var cloak = ReadSource("Service", "SpaceCloak.cs");
        ExtractMethod(cloak, "public static bool IsShipHiddenFrom(").Should().Contain("IsCloakHiddenFrom(target, observer,");
        ExtractMethod(cloak, "private static void RefreshCloakVisibility(").Should().Contain("IsCloakHiddenFrom(ship, player,");

        var hidden = ExtractMethod(cloak, "private static bool IsCloakHiddenFrom(");
        hidden.Should().Contain("ShipCloakRules.GetObserverRevealMeters(state.Profile.RevealMeters, int.MaxValue)",
            "the rank is only read where it can matter");

        var bonus = ExtractMethod(cloak, "private static int GetCloakRevealBonusPercent(");
        bonus.Should().Contain("SpaceCrew.TryGetSeat(observer, out var crewShip, out _)", "crew look through their ship's sensors")
            .And.Contain("SpaceCrew.GetSeatOccupant(ship, ShipCrewSeat.Sensors)")
            .And.Contain("SkillType.Espionage")
            .And.Contain("!GetIsPC(ship)", "NPC ships have no rank");
    }

    // Cloak edge cases

    [Test]
    public void ADockedShip_CannotCloak()
    {
        ShipCloakRules.GetStartError(ShipSize.Small, ShipRole.Fighter, false, true).Should().Be(ShipCloakRules.DockedMessage);
        ShipCloakRules.GetStartError(ShipSize.Medium, ShipRole.BlockadeRunner, true, true).Should().Be(ShipCloakRules.DockedMessage);
        ShipCloakRules.GetStartError(ShipSize.Small, ShipRole.Fighter, false).Should().BeEmpty();

        ExtractMethod(ReadSource("Service", "SpaceCloak.cs"), "public static string CloakError(")
            .Should().Contain("IsDockedAboardHost(activeShip, out _)")
            .And.Contain("ShipCloakRules.GetStartError(hull.Size, hull.Role, hasCargo, isDockedAboard)");
        // Docking itself ends a cloak: it takes the pilot out of space mode, which clears every toggle.
        ExtractMethod(ReadSource("Service", "Space.cs"), "public static void ExitSpaceMode(").Should().Contain("ClearShipToggleState(player, dbShip.Status)");
    }

    [Test]
    public void ACloak_DropsOverARestart_AndLeavesNoGhostBehind()
    {
        var cloak = ReadSource("Service", "SpaceCloak.cs");
        var clear = ExtractMethod(cloak, "private static void ClearCloakState(");
        clear.Should().Contain("RemoveStaleCloakEffect(ship);");
        clear.IndexOf("RemoveStaleCloakEffect", StringComparison.Ordinal)
            .Should().BeGreaterThan(clear.IndexOf("RestoreCloakVisibility", StringComparison.Ordinal), "the effect goes even with no cloak on record");
        ExtractMethod(cloak, "private static void RemoveStaleCloakEffect(").Should().Contain("RemoveEffectByTag(creature, CloakEffectTag)");

        var space = ReadSource("Service", "Space.cs");
        ExtractMethod(space, "public static void EnterServer(").Should().Contain("RemoveStaleCloakEffect(player);");
        ExtractMethod(space, "public static void EnterSpaceMode(").Should().Contain("ClearShipToggleState(player, dbPlayerShip.Status)");
        cloak.Should().Contain("private static readonly ShipCloakBook _cloaks = new();", "cloaks live in memory only");
    }

    [Test]
    public void TheAfterburner_CannotRunWhileCloakingOrCloaked()
    {
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Engaging, 0, false, false).Should().NotBeEmpty();
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Cloaked, 0, false, false).Should().NotBeEmpty();
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Off, 5, false, false).Should().BeEmpty("the decloak lock holds weapons only");

        // Beginning a cloak switches every other toggle off, the Afterburner included, and the speed is reapplied.
        var begin = ExtractMethod(ReadSource("Service", "SpaceCloak.cs"), "private static void BeginCloak(");
        begin.Should().Contain("ShipToggleAutoOffReason.Cloaked").And.Contain("ApplyShipSpeed(ship, status)");
        ExtractMethod(ReadSource("Service", "SpaceToggles.cs"), "private static void TurnOffToggle(").Should().Contain("IsAfterburnerSlot(status, slotNumber)");
    }

    [Test]
    public void TheIonWebAndInterdictor_NeedTheShipInSight_AndBreakOrBlockACloak()
    {
        // Neither reaches a ship it cannot see: a toggle's target must not be hidden, and a cloaked target is no target.
        ReadSource("Service", "SpaceToggles.cs").Should().Contain("var targetValid = GetIsObjectValid(target) && !IsShipHiddenFrom(creature, target);");
        ExtractMethod(ReadSource("Service", "Space.cs"), "public static (uint, ShipStatus) GetCurrentTarget(").Should().Contain("IsShipHiddenFrom(player, target)");

        var ewar = ReadSource("Service", "SpaceElectronicWarfare.cs");
        ExtractMethod(ewar, "public static void ApplyIonWeb(").Should().Contain("BreakCloakByElectronicWarfare(target,");
        ExtractMethod(ewar, "public static void ApplyInterdiction(").Should().Contain("BreakCloakByElectronicWarfare(target,")
            .And.Contain("ShutDownAfterburners(target, targetStatus)");
        ExtractMethod(ReadSource("Service", "SpaceCloak.cs"), "public static string CloakError(").Should().Contain("status.IsInterdicted(DateTime.UtcNow)");
    }

    // Tools, Bible and plan

    [Test]
    public void TheNumbers_AreInTheBalanceTools()
    {
        var tools = Path.Combine(Root(), "tools", "SpaceRevampBalance");
        File.ReadAllText(Path.Combine(tools, "model.py")).Should().Contain(
            $"SEAT_SKILL = dict(support={ShipSeatSkillRules.EngineeringSupportMaximumPercent}, routing={ShipSeatSkillRules.EngineeringRoutingMaximumPercent}, ewar={ShipSeatSkillRules.EspionageElectronicWarfareMaximumPercent})");
        File.ReadAllText(Path.Combine(tools, "checks.py")).Should().Contain("## Q. Seat-skill rank bonuses (W11n)");
        var cloak = File.ReadAllText(Path.Combine(tools, "cloak.py"));
        cloak.Should().Contain($"ESPIONAGE_REVEAL_PERCENT_PER_RANK = {CloakTuning.EspionageRevealPercentPerRank}")
            .And.Contain($"ESPIONAGE_REVEAL_MAXIMUM_RANK = {CloakTuning.EspionageRevealMaximumRank}");
        File.ReadAllText(Path.Combine(tools, "bible_tabs.py")).Should().Contain("[\"Seat skills\",");
    }

    [Test]
    public void ThePlan_ListsTheSeatSkillBonuses()
    {
        var plan = ReadSource("Readmes", "SpaceRevampPlan.md");
        plan.Should().Contain("Added by the seat-skill update (W11n)");
        plan.Should().NotContain("Engineering and Espionage rank bonuses (section 4): no numbers or rule in the plan or the Bible.");
    }
}
