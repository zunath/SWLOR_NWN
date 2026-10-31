using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Cloaking (plan section 32): the Cloaking Device's catalog and fit rules, the role bonus and cargo rule, what a
/// cloaked ship cannot do, the decloak lock, both ways to reveal a cloaked ship and the stealth block's replacement.
/// Numbers come from tools/SpaceRevampBalance/cloak.py (CloakTuning).
/// </summary>
public class CloakingTests
{
    private static readonly DateTime Now = new(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
    private const uint Ship = 0x10;
    private const uint Hunter = 0x20;
    private const uint Friend = 0x30;

    private static ShipCloakProfile Profile(double engage = 4, int reveal = 10, double speed = 0.5) =>
        new() { EngageSeconds = engage, RevealMeters = reveal, SpeedMultiplier = speed };

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine));

    private static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return directory!.FullName;
    }

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

    private static ShipModuleDetail Device() => new RevampModuleDefinition().BuildShipModules()["sm_cloak_00"];

    // Catalog

    [Test]
    public void CloakingDevice_IsAMidSlotPilotingToggleLadder()
    {
        var steps = RevampModuleCatalog.Steps.Where(x => x.Family == ShipModuleFamily.CloakingDevice).ToList();
        steps.Select(x => x.Rank).Should().Equal(0, 5, 10, 15, 20, 25, 30, 35, 40, 45);
        steps.Should().OnlyContain(x => x.Row == ShipModulePowerType.Mid && x.Skill == SkillType.Piloting &&
                                        x.Activation == ShipModuleActivation.Toggle && x.CycleSeconds == 1.0 &&
                                        x.CapacitorCost == 2 && !x.Capital && x.Texture == "iit_smcloak");
        steps.Select(x => x.RevealRange).Should().Equal(10, 10, 9, 9, 8, 8, 7, 7, 6, 5);
        ((int)ShipModuleFamily.CloakingDevice).Should().Be(45);
        ((int)ShipModuleType.CloakingDevice).Should().Be(26);
        ((int)ShipToggleAutoOffReason.Cloaked).Should().Be(8);
    }

    [Test]
    public void CloakingDevice_RecipesFollowTheDataAnalyzer_AndUseSensorSuites()
    {
        var steps = RevampModuleCatalog.Steps.Where(x => x.Family == ShipModuleFamily.CloakingDevice).ToList();
        steps.Select(x => (int)x.RecipeType).Should().Equal(Enumerable.Range(7533, 10));
        ((int)RecipeType.ShipDataAnalyzer00).Should().Be(7532, "no earlier recipe moved");
        foreach (var step in steps)
        {
            var tier = Math.Min(5, step.Rank / 10 + 1);
            step.Components.Should().Contain(x => x.Resref == $"sc_sensor_{tier}" && x.Quantity == 1);
        }
    }

    [Test]
    public void CloakingDevice_IsSelfCentered_AndNotAWeapon()
    {
        ShipToggleRules.GetTargetMode(ShipModuleType.CloakingDevice).Should().Be(ShipToggleTargetMode.SelfCentered);
        ShipToggleRules.IsOffensive(ShipModuleType.CloakingDevice).Should().BeFalse();
        var detail = Device();
        detail.Type.Should().Be(ShipModuleType.CloakingDevice);
        detail.Activation.Should().Be(ShipModuleActivation.Toggle);
        detail.CanTargetSelf.Should().BeTrue();
        detail.ModuleValidationAction.Should().NotBeNull();
        detail.ModuleActivatedAction.Should().NotBeNull();
    }

    [Test]
    public void EveryCloakingNumber_IsInTheGeneratedTuning()
    {
        CloakTuning.CycleSeconds.Should().Be(1.0);
        CloakTuning.Capacitor.Should().Be(2);
        CloakTuning.EngageSeconds.Should().Be(4.0);
        CloakTuning.RoleEngageSeconds.Should().Be(2.0);
        CloakTuning.SpeedPercent.Should().Be(50);
        CloakTuning.RoleSpeedPercent.Should().Be(65);
        CloakTuning.DecloakLockSeconds.Should().Be(5.0);
        CloakTuning.RevealBaseMeters.Should().Be(10);
        CloakTuning.SweepRevealSeconds.Should().Be(10.0);
        CloakTuning.PirateFreighterEngageSeconds.Should().Be(2.0);
        CloakTuning.BarracudaRevealMeters.Should().Be(5);
    }

    // Fit rules

    [Test]
    public void OnlySmallHullsAndBlockadeRunners_CanFitACloakingDevice()
    {
        foreach (var hull in RevampShipCatalog.Hulls)
        {
            var expected = hull.Size == ShipSize.Small || hull.Role == ShipRole.BlockadeRunner;
            ShipCloakRules.CanFit(hull.Size, hull.Role).Should().Be(expected, hull.Name);

            var error = ShipFitRules.GetFitError(new ShipDetail { Hull = hull, CapitalShip = hull.Size == ShipSize.Capital }, Device());
            if (expected)
                error.Should().BeEmpty(hull.Name);
            else
                error.Should().Be(ShipCloakRules.FitMessage, hull.Name);
        }
    }

    [Test]
    public void TheFitRule_NamesTheHullsThatFit()
    {
        RevampShipCatalog.Hulls.Where(x => x.Role == ShipRole.BlockadeRunner).Should().NotBeEmpty();
        foreach (var role in new[] { ShipRole.Fighter, ShipRole.Interceptor, ShipRole.Bomber, ShipRole.Explorer })
            ShipCloakRules.CanFit(ShipSize.Small, role).Should().BeTrue(role.ToString());

        ShipCloakRules.CanFit(ShipSize.Medium, ShipRole.BlockadeRunner).Should().BeTrue();
        foreach (var role in new[] { ShipRole.Gunship, ShipRole.Logistics, ShipRole.MiningBarge, ShipRole.LightFreighter })
            ShipCloakRules.CanFit(ShipSize.Medium, role).Should().BeFalse(role.ToString());

        foreach (var role in new[] { ShipRole.Frigate, ShipRole.Industrial, ShipRole.HeavyFreighter })
            ShipCloakRules.CanFit(ShipSize.Large, role).Should().BeFalse(role.ToString());

        foreach (var role in new[] { ShipRole.Cruiser, ShipRole.CommandShip })
            ShipCloakRules.CanFit(ShipSize.Capital, role).Should().BeFalse(role.ToString());
    }

    [Test]
    public void AnotherModule_IsNotHeldToTheCloakFitRule()
    {
        var medium = new ShipDetail { Hull = new ShipHullEntry { Size = ShipSize.Medium, Role = ShipRole.Gunship } };
        var probe = new RevampModuleDefinition().BuildShipModules()["sm_probe_00"];
        ShipFitRules.GetFitError(medium, probe).Should().BeEmpty();
    }

    // Role bonus and the cargo rule

    [TestCase(ShipRole.Interceptor, 2.0, 65)]
    [TestCase(ShipRole.Bomber, 2.0, 65)]
    [TestCase(ShipRole.Explorer, 2.0, 65)]
    [TestCase(ShipRole.Fighter, 4.0, 50)]
    [TestCase(ShipRole.BlockadeRunner, 4.0, 50)]
    public void RoleBonus_IsAFasterCloakAndASmallerSpeedPenalty(ShipRole role, double engage, int speedPercent)
    {
        ShipCloakRules.GetEngageSeconds(role, "sd_anything").Should().Be(engage);
        ShipCloakRules.GetSpeedMultiplier(role).Should().BeApproximately(speedPercent / 100.0, 1e-9);
        ShipCloakRules.HasRoleBonus(role).Should().Be(engage < 4.0);
    }

    [Test]
    public void TheRoleBonus_IsStrictlyBetterThanTheBase()
    {
        CloakTuning.RoleEngageSeconds.Should().BeLessThan(CloakTuning.EngageSeconds);
        CloakTuning.RoleSpeedPercent.Should().BeGreaterThan(CloakTuning.SpeedPercent);
    }

    [Test]
    public void HullSignatures_SpeedTheCloakAndShortenTheRevealRange()
    {
        ShipCloakRules.GetEngageSeconds(ShipRole.BlockadeRunner, "sd_piratefreight").Should().Be(2.0);
        ShipCloakRules.GetEngageSeconds(ShipRole.BlockadeRunner, "sd_barracuda").Should().Be(4.0);
        ShipCloakRules.GetRevealMeters(10, "sd_barracuda").Should().Be(5);
        ShipCloakRules.GetRevealMeters(5, "sd_barracuda").Should().Be(5);
        ShipCloakRules.GetRevealMeters(10, "sd_piratefreight").Should().Be(10);
        ShipCloakRules.GetRevealMeters(0, "sd_other").Should().Be(10, "a device with no number reveals at the base range");
        RevampShipCatalog.Hulls.Select(x => x.Tag).Should().Contain(new[] { CloakTuning.PirateFreighterTag, CloakTuning.BarracudaTag });
    }

    [Test]
    public void OnlyBlockadeRunners_CanCloakWithCargoAboard()
    {
        ShipCloakRules.GetStartError(ShipSize.Medium, ShipRole.BlockadeRunner, true).Should().BeEmpty();
        ShipCloakRules.GetStartError(ShipSize.Medium, ShipRole.BlockadeRunner, false).Should().BeEmpty();
        foreach (var role in new[] { ShipRole.Fighter, ShipRole.Interceptor, ShipRole.Bomber, ShipRole.Explorer })
        {
            ShipCloakRules.GetStartError(ShipSize.Small, role, false).Should().BeEmpty(role.ToString());
            ShipCloakRules.GetStartError(ShipSize.Small, role, true).Should().Be(ShipCloakRules.CargoMessage, role.ToString());
        }
    }

    [Test]
    public void AHullThatCannotCloak_IsRefusedWhateverItCarries()
    {
        ShipCloakRules.GetStartError(ShipSize.Large, ShipRole.Frigate, false).Should().Be(ShipCloakRules.HullMessage);
        ShipCloakRules.GetStartError(ShipSize.Medium, ShipRole.Gunship, false).Should().Be(ShipCloakRules.HullMessage);
    }

    [Test]
    public void TheCargoRule_IsReCheckedEveryCycleThroughTheDevicesValidation()
    {
        var error = ExtractMethod(ReadSource("Service", "SpaceCloak.cs"), "public static string CloakError(");
        error.Should().Contain("var activeShip = GetIsPC(ship) ? Trade.GetActiveShip(ship) : null;")
            .And.Contain("Trade.GetUsedVolume(activeShip) > 0");
        error.Should().Contain("ShipCloakRules.GetStartError(");

        ExtractMethod(ReadSource("Feature", "ShipModuleDefinition", "RevampModuleDefinition.cs"), "private void CloakingDevice(")
            .Should().Contain("Space.CloakError(activator, status)");
        ReadSource("Service", "SpaceToggles.cs").Should().Contain("detail.ModuleValidationAction?.Invoke(creature, status, target, targetStatus, module.ModuleBonus)",
            "the toggle loop validates the running device every cycle, so cargo picked up mid-cloak switches it off");
    }

    // Phases

    [Test]
    public void ACloak_EngagesThenVanishes()
    {
        ShipCloakRules.GetPhase(Now, 4, Now).Should().Be(ShipCloakPhase.Engaging);
        ShipCloakRules.GetPhase(Now, 4, Now.AddSeconds(3.9)).Should().Be(ShipCloakPhase.Engaging);
        ShipCloakRules.GetPhase(Now, 4, Now.AddSeconds(4)).Should().Be(ShipCloakPhase.Cloaked);
        ShipCloakRules.GetEngageSecondsLeft(Now, 4, Now.AddSeconds(1.5)).Should().Be(3);
        ShipCloakRules.GetEngageSecondsLeft(Now, 4, Now.AddSeconds(9)).Should().Be(0);
    }
    // What a cloaked ship cannot do

    [Test]
    public void ACloakingOrCloakedShip_UsesNoModules_ButItsOwnDevice()
    {
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Engaging, 0, false, false).Should().Contain("cloaking");
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Cloaked, 0, false, false).Should().Contain("cloaked");
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Cloaked, 0, false, true).Should().Contain("cloaked");
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Engaging, 0, true, false).Should().BeEmpty();
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Cloaked, 0, true, false).Should().BeEmpty("the device can be pressed again to switch it off");
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Off, 0, false, true).Should().BeEmpty();
    }

    [Test]
    public void ACloakingOrCloakedShip_UsesNoAbilities()
    {
        ShipCloakRules.GetAbilityBlock(ShipCloakPhase.Off).Should().BeEmpty();
        ShipCloakRules.GetAbilityBlock(ShipCloakPhase.Engaging).Should().Contain("abilities");
        ShipCloakRules.GetAbilityBlock(ShipCloakPhase.Cloaked).Should().Contain("abilities");
    }

    [Test]
    public void EveryOtherToggle_SwitchesOffWithTheCloakedReason()
    {
        var check = new ShipToggleCheck { ShipCloaked = true, TargetValid = true, Capacitor = 10, RequiredCapacitor = 1 };
        ShipToggleRules.Evaluate(check).Should().Be(ShipToggleAutoOffReason.Cloaked);
        ShipToggleRules.Evaluate(new ShipToggleCheck { ShipCloaked = false, TargetValid = true, Capacitor = 10, RequiredCapacitor = 1 })
            .Should().Be(ShipToggleAutoOffReason.None);
        ShipToggleRules.Evaluate(new ShipToggleCheck { ShipInSpace = false, ShipCloaked = true }).Should().Be(ShipToggleAutoOffReason.ShipLeftSpace);
        ShipToggleRules.AutoOffMessage(ShipToggleAutoOffReason.Cloaked, "Laser Cannon Mk I", string.Empty)
            .Should().Be("Laser Cannon Mk I switched off: the ship is cloaked.");
    }

    [Test]
    public void TheModuleAndAbilityGates_AreWiredIn()
    {
        var activation = ExtractMethod(ReadSource("Service", "Space.cs"), "private static void ActivateShipModuleCore(");
        activation.Should().Contain("GetCloakModuleBlock(activator, shipModuleDetails)");
        activation.IndexOf("TurnOffToggle(activator, activatorShipStatus, slotNumber)", StringComparison.Ordinal)
            .Should().BeLessThan(activation.IndexOf("GetCloakModuleBlock(", StringComparison.Ordinal), "a running device can still be switched off");

        ReadSource("Service", "Ability.cs").Should().Contain("Space.GetCloakAbilityBlock(activator)");
        ExtractMethod(ReadSource("Service", "SpaceAbilities.cs"), "public static string GetFireError(")
            .Should().Contain("Space.GetCloakWeaponBlock(ship)");
        ExtractMethod(ReadSource("Feature", "ShipModuleDefinition", "ShipModuleActions.cs"), "public static ShipShotResult FireStep(")
            .Should().Contain("Space.IsShipFiringLocked(activator)");
        ReadSource("Service", "SpaceToggles.cs").Should().Contain("ShipCloaked = detail.Type != ShipModuleType.CloakingDevice && GetCloakPhase(creature) != ShipCloakPhase.Off");
    }

    [Test]
    public void BeginningACloak_SwitchesTheOtherTogglesOffAndSlowsTheShip()
    {
        var begin = ExtractMethod(ReadSource("Service", "SpaceCloak.cs"), "private static void BeginCloak(");
        begin.Should().Contain("ShipToggleAutoOffReason.Cloaked");
        begin.Should().Contain("Toggles(status).Where(x => x.Slot != slot)");
        begin.Should().Contain("ApplyShipSpeed(ship, status)");
        ReadSource("Service", "SpacePowerRouting.cs").Should().Contain("GetCloakSpeedMultiplier(pilot)");
    }

    // The decloak lock

    [Test]
    public void Decloaking_LocksFiringForFiveSeconds()
    {
        var book = new ShipCloakBook();
        book.Begin(Ship, Now, Profile(engage: 4), 21);
        var cloakedAt = Now.AddSeconds(5);

        book.End(Ship, cloakedAt).Should().Be(ShipCloakEnd.Decloaked);
        book.GetPhase(Ship, cloakedAt).Should().Be(ShipCloakPhase.Off);
        book.IsFiringLocked(Ship, cloakedAt).Should().BeTrue();
        book.GetLockSecondsLeft(Ship, cloakedAt).Should().Be(5);
        book.IsFiringLocked(Ship, cloakedAt.AddSeconds(4.9)).Should().BeTrue();
        book.IsFiringLocked(Ship, cloakedAt.AddSeconds(5)).Should().BeFalse();
        book.GetLockSecondsLeft(Ship, cloakedAt.AddSeconds(9)).Should().Be(0);
    }

    [Test]
    public void AWeaponAfterDecloaking_IsRefusedWithTheSecondsLeft_ButOtherModulesAreNot()
    {
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Off, 5, false, true).Should().Be("Your weapons are locked for 5 more seconds after decloaking.");
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Off, 1, false, true).Should().Be("Your weapons are locked for 1 more second after decloaking.");
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Off, 5, false, false).Should().BeEmpty("a repairer or hardener is not a weapon");
        ShipCloakRules.GetModuleBlock(ShipCloakPhase.Off, 0, false, true).Should().BeEmpty();
        ShipCloakRules.GetFiringLockMessage(0).Should().BeEmpty();
    }

    [Test]
    public void ACloakThatNeverFinishedEngaging_LocksNothing()
    {
        var book = new ShipCloakBook();
        book.Begin(Ship, Now, Profile(engage: 4), 21);

        book.End(Ship, Now.AddSeconds(2)).Should().Be(ShipCloakEnd.Cancelled);
        book.IsFiringLocked(Ship, Now.AddSeconds(2)).Should().BeFalse();
        book.End(Ship, Now.AddSeconds(3)).Should().Be(ShipCloakEnd.None);
    }

    [Test]
    public void AShipIsFiringLocked_FromThePressUntilTheLockEnds()
    {
        var book = new ShipCloakBook();
        book.IsFiringLocked(Ship, Now).Should().BeFalse();

        book.Begin(Ship, Now, Profile(engage: 2), 21);
        book.IsFiringLocked(Ship, Now.AddSeconds(1)).Should().BeTrue("cloaking");
        book.IsFiringLocked(Ship, Now.AddSeconds(30)).Should().BeTrue("cloaked");

        book.End(Ship, Now.AddSeconds(30));
        book.IsFiringLocked(Ship, Now.AddSeconds(33)).Should().BeTrue("the lock");
        book.IsFiringLocked(Ship, Now.AddSeconds(36)).Should().BeFalse();
    }

    [Test]
    public void StartingANewCloak_ClearsTheOldLock_AndLeavingSpaceForgetsIt()
    {
        var book = new ShipCloakBook();
        book.Begin(Ship, Now, Profile(engage: 1), 21);
        book.End(Ship, Now.AddSeconds(2));
        book.GetLockSecondsLeft(Ship, Now.AddSeconds(2)).Should().Be(5);

        book.Begin(Ship, Now.AddSeconds(3), Profile(engage: 1), 21);
        book.GetLockSecondsLeft(Ship, Now.AddSeconds(3)).Should().Be(0);

        book.Clear(Ship);
        book.Count.Should().Be(0);
        book.IsFiringLocked(Ship, Now.AddSeconds(3)).Should().BeFalse();
    }

    [Test]
    public void ABeginningCloak_KeepsTheOneAlreadyRunning()
    {
        var book = new ShipCloakBook();
        var first = book.Begin(Ship, Now, Profile(), 21);
        book.Begin(Ship, Now.AddSeconds(1), Profile(engage: 1), 22).Should().BeSameAs(first);
        book.Count.Should().Be(1);
    }

    [Test]
    public void DamageToTheShip_BreaksTheCloak()
    {
        var space = ReadSource("Service", "Space.cs");
        ExtractMethod(space, "public static (int ShieldDamage, int HullDamage) ApplyShipDamage(").Should().Contain("NotifyCloakDamaged(target)");
        ExtractMethod(space, "public static void ApplyHullDamage(").Should().Contain("NotifyCloakDamaged(target)");

        var notify = ExtractMethod(ReadSource("Service", "SpaceCloak.cs"), "private static void NotifyCloakDamaged(");
        notify.Should().Contain("DelayCommand(0f");
        notify.Should().Contain("EndCloak(ship,");
    }

    [Test]
    public void EndingACloak_RestoresTheShipsSpeedVisibilityAndNpcTargets()
    {
        var end = ExtractMethod(ReadSource("Service", "SpaceCloak.cs"), "private static void EndCloak(");
        end.Should().Contain("RestoreCloakVisibility(ship, state)");
        end.Should().Contain("ApplyShipSpeed(ship, status)");
        end.Should().Contain("Enmity.ReevaluateEnemyAttackTargets(ship)");
        end.Should().Contain("TurnOffToggle(ship, status, state.Slot)");

        var toggles = ReadSource("Service", "SpaceToggles.cs");
        ExtractMethod(toggles, "private static void TurnOffToggle(").Should().Contain("EndCloak(creature, null, false)");
        ExtractMethod(toggles, "private static void ClearShipToggleState(").Should().Contain("ClearCloakState(creature)");
        ExtractMethod(toggles, "public static void TurnOffShipToggles(").Should().Contain("ClearCloakState(creature)");
    }
    // Reveal: any ship within range, and a Sensor Sweep

    [Test]
    public void ACloakedShip_IsHiddenFromEveryoneBeyondTheRevealRange()
    {
        ShipCloakRules.IsHidden(ShipCloakPhase.Cloaked, false, 10.1, 10, false).Should().BeTrue();
        ShipCloakRules.IsHidden(ShipCloakPhase.Cloaked, false, 10.0, 10, false).Should().BeFalse("within 10 m reveals it");
        ShipCloakRules.IsHidden(ShipCloakPhase.Cloaked, false, 3, 10, false).Should().BeFalse();
        ShipCloakRules.IsHidden(ShipCloakPhase.Engaging, false, 200, 10, false).Should().BeFalse("it has not vanished yet");
        ShipCloakRules.IsHidden(ShipCloakPhase.Off, false, 200, 10, false).Should().BeFalse();
        ShipCloakRules.IsHidden(ShipCloakPhase.Cloaked, true, 200, 10, false).Should().BeFalse("the ship's own party and crew see it");
    }

    [Test]
    public void AShipWithinTheRevealRange_SeesTheCloakedShip_AtEachDeviceRank()
    {
        var book = new ShipCloakBook();
        book.Begin(Ship, Now, Profile(engage: 2, reveal: 6), 21);
        var cloaked = Now.AddSeconds(3);

        book.IsHiddenFrom(Ship, Hunter, false, 30, cloaked).Should().BeTrue();
        book.IsHiddenFrom(Ship, Hunter, false, 6.5, cloaked).Should().BeTrue();
        book.IsHiddenFrom(Ship, Hunter, false, 6, cloaked).Should().BeFalse();
        book.IsHiddenFrom(Ship, Hunter, false, 1, cloaked).Should().BeFalse();
        book.IsHiddenFrom(Ship, Hunter, false, 30, Now.AddSeconds(1)).Should().BeFalse("not vanished yet");
        book.IsHiddenFrom(Hunter, Ship, false, 30, cloaked).Should().BeFalse("a ship with no cloak is never hidden");
    }

    [Test]
    public void ABetterDevice_IsRevealedFromCloser()
    {
        var ranges = RevampModuleCatalog.Steps.Where(x => x.Family == ShipModuleFamily.CloakingDevice).Select(x => x.RevealRange).ToList();
        ranges.Should().BeInDescendingOrder();
        ranges.First().Should().Be(CloakTuning.RevealBaseMeters);
        ranges.Last().Should().BeLessThan(ranges.First());
    }

    [Test]
    public void ASensorSweep_FindsACloakedShip_AndShowsItToTheSweepersPartyForTenSeconds()
    {
        var book = new ShipCloakBook();
        book.Begin(Ship, Now, Profile(engage: 1), 21);
        var cloaked = Now.AddSeconds(2);
        book.IsHiddenFrom(Ship, Hunter, false, 80, cloaked).Should().BeTrue();

        book.Paint(Ship, new[] { Hunter, Friend }, cloaked.AddSeconds(CloakTuning.SweepRevealSeconds));
        book.IsHiddenFrom(Ship, Hunter, false, 80, cloaked).Should().BeFalse();
        book.IsHiddenFrom(Ship, Friend, false, 80, cloaked.AddSeconds(9.9)).Should().BeFalse();
        book.IsHiddenFrom(Ship, Hunter, false, 80, cloaked.AddSeconds(10)).Should().BeTrue("the reveal lapses");
        book.IsPainted(Ship, Hunter, cloaked).Should().BeTrue();
        book.IsPainted(Ship, 0x99, cloaked).Should().BeFalse();
    }

    [Test]
    public void ASweep_ListsCloakedShipsThroughTheHook_AndRevealsThem()
    {
        SensorSweepRules.Includes(50, 100, true).Should().BeTrue();
        SensorSweepRules.Includes(150, 100, true).Should().BeFalse();

        ReadSource("Service", "SpaceCloak.cs").Should().Contain("public static bool IsShipCloaked(uint ship)");
        var sweep = ReadSource("Service", "SpaceSignatureJump.cs");
        sweep.Should().NotContain("public static bool IsShipCloaked", "the hook is the cloak's now");
        var run = ExtractMethod(sweep, "public static bool RunSensorSweep(");
        run.Should().Contain("var cloaked = IsShipCloaked(ship);");
        run.Should().Contain("RevealCloakedShip(ship, player)");
        run.IndexOf("RevealCloakedShip(", StringComparison.Ordinal)
            .Should().BeLessThan(run.IndexOf("Hostile = IsShipEnemy(player, ship)", StringComparison.Ordinal), "a revealed ship is listed as hostile when it is");
    }

    [Test]
    public void ACloakedShip_CannotSweepOrJumpToASignature()
    {
        var jump = ReadSource("Service", "SpaceSignatureJump.cs");
        ExtractMethod(jump, "public static bool RunSensorSweep(").Should().Contain("ShipCloakRules.GetModuleBlock(GetCloakPhase(player), 0, false, false)");
        ExtractMethod(jump, "public static bool BeginSignatureJump(").Should().Contain("GetCloakAbilityBlock(player)");
    }

    [Test]
    public void ACloakedShip_IsOnlyHiddenFromWhoCannotRevealIt_InTheWiring()
    {
        var space = ReadSource("Service", "SpaceCloak.cs");
        var hidden = ExtractMethod(space, "public static bool IsShipHiddenFrom(");
        hidden.Should().Contain("observer == target || GetIsDM(observer)");
        hidden.Should().Contain("IsCloakAlly(target, observer)");
        hidden.Should().Contain("IsCloakHiddenFrom(target, observer,");
        ExtractMethod(space, "private static bool IsCloakHiddenFrom(").Should().Contain("_cloaks.IsHiddenFrom(ship, observer, isAlly, distance, now");
        ExtractMethod(space, "private static bool IsCloakAlly(").Should().Contain("Party.IsInParty(ship, observer)").And.Contain("SpaceCrew.GetCrew(ship)");

        var visibility = ExtractMethod(space, "private static void RefreshCloakVisibility(");
        visibility.Should().Contain("VisibilityType.Visible").And.Contain("VisibilityType.Default");
        ExtractMethod(space, "private static void CompleteCloak(").Should().Contain("VisibilityPlugin.SetVisibilityOverride(OBJECT_INVALID, ship, VisibilityType.Hidden)");
    }

    // NPCs

    [Test]
    public void NpcShips_LoseACloakedShipAsATarget_UnlessItIsWithinTheRevealRange()
    {
        var stealth = ReadSource("Service", "Stealth.cs");
        ExtractMethod(stealth, "public static bool IsHiddenFrom(uint observer, uint target)").Should().Contain("Space.IsShipHiddenFrom(observer, target)");
        ExtractMethod(stealth, "public static bool CanAcquireAggro(uint observer, uint target)").Should().Contain("Space.IsShipHiddenFrom(observer, target)");
        ReadSource("Service", "Enmity.cs").Should().Contain("!Stealth.IsHiddenFrom(enemy, creature);", "a hidden creature keeps its enmity and is skipped");

        var space = ReadSource("Service", "Space.cs");
        ExtractMethod(space, "private static void ProcessSpaceNPCAI(").Should().Contain("Enmity.GetHighestEnmityAttackTarget(creature)");
        ExtractMethod(ReadSource("Service", "SpacePvp.cs"), "public static bool IsShipEnemy(").Should().Contain("!IsShipHiddenFrom(source, target)");
        ExtractMethod(ReadSource("Service", "SpaceToggles.cs"), "private static void RunToggleCycle(").Should().Contain("!IsShipHiddenFrom(creature, target)");
        ExtractMethod(space, "public static (uint, ShipStatus) GetCurrentTarget(").Should().Contain("IsShipHiddenFrom(player, target)");
        ExtractMethod(ReadSource("Service", "SpaceAbilities.cs"), "public static uint GetSelectedTarget(").Should().Contain("Space.IsShipHiddenFrom(ship, target)");
    }

    [Test]
    public void CompletingTheCloak_MakesEnemiesChooseAgain()
    {
        ExtractMethod(ReadSource("Service", "SpaceCloak.cs"), "private static void CompleteCloak(")
            .Should().Contain("Enmity.ReevaluateEnemyAttackTargets(ship)");
    }

    // Pirate ambushes

    [Test]
    public void ACloakedShip_SlipsPastThePirateAmbushRoll()
    {
        ShipCloakRules.DodgesAmbush(ShipCloakPhase.Cloaked).Should().BeTrue();
        ShipCloakRules.DodgesAmbush(ShipCloakPhase.Engaging).Should().BeFalse("a ship still cloaking is seen");
        ShipCloakRules.DodgesAmbush(ShipCloakPhase.Off).Should().BeFalse();

        var roll = ExtractMethod(ReadSource("Service", "TradeOrbitRisk.cs"), "private static void RollPirateAmbush(");
        roll.Should().Contain("Space.DodgesPirateAmbush(player)");
        roll.IndexOf("Space.DodgesPirateAmbush(player)", StringComparison.Ordinal)
            .Should().BeLessThan(roll.IndexOf("Random.NextFloat()", StringComparison.Ordinal));
    }

    [Test]
    public void ACustomsScan_IsNotDodgedByACloak()
    {
        var orbit = ReadSource("Service", "TradeOrbitRisk.cs");
        ExtractMethod(orbit, "public static void CheckOrbitRisks(").Should().Contain("RollCustomsScan(player, area, orbit)");
        ReadSource("Service", "TradeCustoms.cs").Should().NotContain("DodgesPirateAmbush").And.NotContain("IsShipCloaked");
    }

    // Replacing the blanket stealth block

    [Test]
    public void TheBlanketStealthBlock_IsGone_AndTheStealthControlPressesTheDevice()
    {
        var space = ReadSource("Service", "Space.cs");
        space.Should().NotContain("PreventSpaceStealth");
        space.Should().NotContain("You cannot enter stealth mode in space.");

        var cloak = ReadSource("Service", "SpaceCloak.cs");
        var redirect = ExtractMethod(cloak, "public static void RedirectSpaceStealth(");
        cloak.Should().Contain("[NWNEventHandler(ScriptName.OnStealthEnterBefore)]");
        redirect.Should().Contain("SetActionMode(creature, ActionMode.Stealth, false)", "native stealth still never hides a ship");
        redirect.Should().Contain("Only a Cloaking Device hides a ship in space.");
        redirect.Should().Contain("ActivateShipModule(creature, SlotNumberToFeat(slot))");
        redirect.IndexOf("if (!IsPlayerInSpaceMode(creature))", StringComparison.Ordinal).Should().BeLessThan(redirect.IndexOf("SetActionMode(", StringComparison.Ordinal),
            "ground stealth is untouched");
    }

    [Test]
    public void TheGroundStealthGate_LeavesSpaceToTheCloak()
    {
        var gate = ExtractMethod(ReadSource("Service", "Stealth.cs"), "public static void GateStealthEntry(");
        gate.Should().Contain("if (Space.IsPlayerInSpaceMode(creature))");
        gate.IndexOf("Space.IsPlayerInSpaceMode(creature)", StringComparison.Ordinal)
            .Should().BeLessThan(gate.IndexOf("PerkType.Stealth", StringComparison.Ordinal));
    }

    // Ship HUD

    [Test]
    public void TheCloakLine_ShowsTheDeviceState_OnlyWhenOneIsFitted()
    {
        ShipCloakRules.GetStatusText(false, ShipCloakPhase.Off, 0, 0).Should().BeNull();
        ShipCloakRules.GetStatusText(true, ShipCloakPhase.Off, 0, 0).Should().Be("Off");
        ShipCloakRules.GetStatusText(true, ShipCloakPhase.Engaging, 3, 0).Should().Be("Cloaking (3 s)");
        ShipCloakRules.GetStatusText(true, ShipCloakPhase.Cloaked, 0, 0).Should().Be("Cloaked");
        ShipCloakRules.GetStatusText(true, ShipCloakPhase.Off, 0, 4).Should().Be("Weapons locked (4 s)");

        ShipHudRules.BuildStatRows(new ShipHudStats { CloakText = "Cloaked" })
            .Should().Contain(x => x.Label == "Cloak" && x.Value == "Cloaked");
        ShipHudRules.BuildStatRows(new ShipHudStats()).Should().NotContain(x => x.Label == "Cloak");
    }

    [Test]
    public void TheHudSpeed_IncludesTheCloakPenalty()
    {
        ShipCloakRules.ApplySpeed(1.2, 0.5).Should().BeApproximately(0.6, 1e-9);
        ReadSource("Service", "SpaceHud.cs").Should().Contain("ShipCloakRules.ApplySpeed(stats.SpeedMultiplier, Space.GetCloakSpeedMultiplier(player))");
    }

    // The generated files and docs

    [Test]
    public void TheCloakingNumbers_AreGeneratedFromTheBalanceTool()
    {
        var tool = File.ReadAllText(Path.Combine(Root(), "tools", "SpaceRevampBalance", "cloak.py"));
        tool.Should().Contain("def cloak_tab(Sheet):").And.Contain("ENGAGE_SECONDS = 4.0").And.Contain("DECLOAK_LOCK_SECONDS = 5");
        ReadSource("Service", "SpaceService", "CloakTuning.cs").Should().Contain("auto-generated");
        File.ReadAllText(Path.Combine(Root(), "tools", "SpaceRevampBalance", "bible_tabs.py")).Should().Contain("cloak.cloak_tab(Sheet)");
    }

    [Test]
    public void ThePlan_ListsWhatTheCloakingUpdateAdded()
    {
        var plan = ReadSource("Readmes", "SpaceRevampPlan.md");
        var section = plan.Substring(plan.IndexOf("## 32. Cloaking", StringComparison.Ordinal));
        section.Should().Contain("Added by the cloaking update");
    }
}