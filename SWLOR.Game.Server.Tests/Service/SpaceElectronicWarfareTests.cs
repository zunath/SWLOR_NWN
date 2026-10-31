using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Ion Web, Interdictor, Jammer, Target Painter and Afterburner (W11k, plan Appendix B): the catalog rows from the
/// Ship Modules (Revamp) draft, the hull bonuses, how a web, hold, jam or paint lasts and combines, the Afterburner's
/// speed, and the wiring of the Interdictor's hold into jumps, docking, boarding, Rift Keys, cloaks and Afterburners.
/// </summary>
public class SpaceElectronicWarfareTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static readonly ShipModuleFamily[] Families =
    {
        ShipModuleFamily.IonWeb, ShipModuleFamily.Interdictor, ShipModuleFamily.Jammer, ShipModuleFamily.TargetPainter,
        ShipModuleFamily.Afterburner,
    };

    private static List<ShipModuleStep> Steps(ShipModuleFamily family) =>
        RevampModuleCatalog.Steps.Where(x => x.Family == family).OrderBy(x => x.Rank).ToList();

    [Test]
    public void EachFamily_IsAMidSlotPilotingToggleLadder_WithItsOwnActionIcon()
    {
        ((int)ShipModuleFamily.IonWeb).Should().Be(47);
        ((int)ShipModuleFamily.Afterburner).Should().Be(51);
        ((int)ShipModuleType.IonWeb).Should().Be(27);
        ((int)ShipModuleType.Afterburner).Should().Be(31);

        var expected = new Dictionary<ShipModuleFamily, (string Prefix, double Cycle, int Capacitor, string Texture)>
        {
            [ShipModuleFamily.IonWeb] = ("sm_web", 3.0, 3, "iit_smionweb"),
            [ShipModuleFamily.Interdictor] = ("sm_intd", 4.0, 5, "iit_sminterdict"),
            [ShipModuleFamily.Jammer] = ("sm_jam", 4.0, 4, "iit_smjammer"),
            [ShipModuleFamily.TargetPainter] = ("sm_paint", 4.0, 4, "iit_smtgtpaint"),
            [ShipModuleFamily.Afterburner] = ("sm_aburn", 2.0, 3, "iit_smafterburn"),
        };
        foreach (var (family, (prefix, cycle, capacitor, texture)) in expected)
        {
            var steps = Steps(family);
            // The Target Painter starts at rank 10 (review decision 2026-10-30).
            steps.Select(x => x.Rank).Should().Equal(family == ShipModuleFamily.TargetPainter
                ? new[] { 10, 15, 20, 25, 30, 35, 40, 45 }
                : new[] { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45 });
            steps.Select(x => x.Tag).Should().Equal(steps.Select(x => $"{prefix}_{x.Rank:00}"));
            steps.Should().OnlyContain(x => x.Row == ShipModulePowerType.Mid && x.Skill == SkillType.Piloting &&
                                            x.Activation == ShipModuleActivation.Toggle && !x.Capital &&
                                            x.CycleSeconds == cycle && x.CapacitorCost == capacitor && x.Texture == texture);
            steps.Should().OnlyContain(x => x.ShortName.Length <= 14 && !x.Description.Any(c => c > 127));
        }
    }

    [Test]
    public void Numbers_AreTheDraftRows()
    {
        Steps(ShipModuleFamily.IonWeb).Select(x => x.WebPercent).Should().Equal(20, 21, 23, 25, 26, 28, 30, 31, 33, 35);
        Steps(ShipModuleFamily.IonWeb).Should().OnlyContain(x => x.Range == 20);
        Steps(ShipModuleFamily.Interdictor).Select(x => x.InterdictRange).Should().Equal(15, 16, 17, 18, 19, 20, 21, 22, 23, 24);
        Steps(ShipModuleFamily.Jammer).Select(x => x.TargetAccuracy).Should().Equal(-4, -6, -8, -10, -12, -14, -16, -18, -20, -22);
        Steps(ShipModuleFamily.TargetPainter).Select(x => x.PaintPercent).Should().Equal(6, 6, 7, 7, 8, 8, 9, 10);
        Steps(ShipModuleFamily.Afterburner).Select(x => x.AfterburnerPercent).Should().Equal(25, 26, 28, 30, 31, 33, 35, 36, 38, 40);
        Steps(ShipModuleFamily.Jammer).Concat(Steps(ShipModuleFamily.TargetPainter)).Should().OnlyContain(x => x.Range == 30,
            "the draft gives them no range, so they take the E-War Array's 30 m");
        Steps(ShipModuleFamily.Afterburner).Should().OnlyContain(x => x.Range == 0);
    }

    [Test]
    public void Recipes_FollowTheCargoExpander_SoNoValueMoves()
    {
        var recipes = Families.SelectMany(Steps).Select(x => (int)x.RecipeType).ToList();
        // The Target Painter starts at rank 10, so the block holds 48 ranks.
        recipes.Should().Equal(Enumerable.Range(7743, 48));
        Enum.GetValues<RecipeType>().Select(x => (int)x).Where(recipes.Contains).Should().HaveCount(48);
        ((int)RecipeType.ShipCargoExpander45).Should().Be(7742);
        Families.SelectMany(Steps).Should().OnlyContain(x => x.RecipeLevel == Math.Max(2, x.Rank));
        Steps(ShipModuleFamily.Jammer).First().Components.Should().Contain(("sc_sensor_1", 1));
        Steps(ShipModuleFamily.Afterburner).Last().Components.Should().Contain(("sc_thruster_5", 1));
    }

    [Test]
    public void TargetModes_HostileModulesFollowTheTarget_TheAfterburnerIsSelfCentered()
    {
        foreach (var type in new[] { ShipModuleType.IonWeb, ShipModuleType.Interdictor, ShipModuleType.Jammer, ShipModuleType.TargetPainter })
        {
            ShipToggleRules.GetTargetMode(type).Should().Be(ShipToggleTargetMode.Follow);
            ShipToggleRules.TargetsEnemy(type).Should().BeTrue("PvP rules gate it like a weapon");
            ShipToggleRules.IsOffensive(type).Should().BeFalse("it is no weapon: power routing and All Weapons leave it alone");
            DroidCrewRules.GetTask(type, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.Weapon, "a droid runs it at the pilot's target");
        }

        ShipToggleRules.GetTargetMode(ShipModuleType.Afterburner).Should().Be(ShipToggleTargetMode.SelfCentered);
        ShipToggleRules.TargetsEnemy(ShipModuleType.Afterburner).Should().BeFalse();
        DroidCrewRules.GetTask(ShipModuleType.Afterburner, ShipModuleActivation.Toggle).Should().Be(DroidCrewTask.None);
        ShipToggleRules.TargetsEnemy(ShipModuleType.CombatLaser).Should().BeTrue();
        ShipToggleRules.TargetsEnemy(ShipModuleType.ShieldRepairer).Should().BeFalse();
    }

    [Test]
    public void HullBonuses_InterceptorWebsHarderAndFarther_ScoutFarther_BarracudaIsImmune()
    {
        ShipElectronicWarfareRules.WebPercent(35, ShipRole.Interceptor).Should().Be(44);
        ShipElectronicWarfareRules.WebPercent(20, ShipRole.Interceptor).Should().Be(25);
        ShipElectronicWarfareRules.WebPercent(35, ShipRole.Fighter).Should().Be(35);
        ShipElectronicWarfareRules.WebPercent(90, ShipRole.Fighter).Should().Be(ShipElectronicWarfareTuning.MaximumWebPercent);

        ShipElectronicWarfareRules.WebRange(20, ShipRole.Fighter, "sd_striker").Should().Be(20f);
        ShipElectronicWarfareRules.WebRange(20, ShipRole.Interceptor, "sd_condor").Should().Be(30f);
        ShipElectronicWarfareRules.WebRange(20, ShipRole.Interceptor, ShipElectronicWarfareTuning.RepublicScoutTag).Should().Be(34f);
        ShipElectronicWarfareRules.InterdictorRange(24, ShipRole.Interceptor).Should().Be(36f);
        ShipElectronicWarfareRules.InterdictorRange(24, ShipRole.Gunship).Should().Be(24f);

        ShipElectronicWarfareRules.IsWebImmune(ShipElectronicWarfareTuning.BarracudaTag).Should().BeTrue();
        ShipElectronicWarfareRules.IsWebImmune("sd_striker").Should().BeFalse();
        ShipElectronicWarfareTuning.BarracudaTag.Should().Be(CloakTuning.BarracudaTag);

        var scout = RevampShipCatalog.Hulls.Single(x => x.Tag == ShipElectronicWarfareTuning.RepublicScoutTag);
        scout.Role.Should().Be(ShipRole.Interceptor);
        scout.Signature.Should().Contain("Ion Web range +20%");
        RevampShipCatalog.Hulls.Single(x => x.Tag == ShipElectronicWarfareTuning.BarracudaTag).Signature.Should().Contain("Immune to Ion Webs");
    }

    [Test]
    public void AHold_LastsACyclePlusPadding_AndALongerHoldIsKept()
    {
        var status = new ShipStatus();
        status.IsInterdicted(Now).Should().BeFalse("saved data without a hold reads as never held");

        ShipElectronicWarfareRules.Interdict(status, ShipElectronicWarfareRules.HoldSeconds(4.0), Now);
        status.IsInterdicted(Now.AddSeconds(4.4)).Should().BeTrue();
        status.IsInterdicted(Now.AddSeconds(4.5)).Should().BeFalse("the hold lapses a moment after the module stops");

        ShipElectronicWarfareRules.Interdict(status, 10, Now);
        ShipElectronicWarfareRules.Interdict(status, 1, Now);
        status.InterdictedUntil.Should().Be(Now.AddSeconds(10));
    }

    [Test]
    public void AJam_TheStrongerPenaltyCounts_AndAFreshJamReplacesALapsedOne()
    {
        var status = new ShipStatus();
        ShipElectronicWarfareRules.Jam(status, -12, 4.5, Now);
        ShipElectronicWarfareRules.Jam(status, -6, 4.5, Now.AddSeconds(1));
        status.GetAccuracyPenalty(Now.AddSeconds(2)).Should().Be(-12);

        ShipElectronicWarfareRules.Jam(status, -6, 4.5, Now.AddSeconds(10));
        status.GetAccuracyPenalty(Now.AddSeconds(11)).Should().Be(-6);
        status.GetAccuracyPenalty(Now.AddSeconds(15)).Should().Be(0);
    }

    [Test]
    public void APaint_RaisesDamageTaken_TheStrongerPaintCounts_AndLapses()
    {
        var status = new ShipStatus();
        status.GetPaintPercent(Now).Should().Be(0);

        ShipElectronicWarfareRules.Paint(status, 10, 4.5, Now);
        ShipElectronicWarfareRules.Paint(status, 5, 4.5, Now.AddSeconds(1));
        status.GetPaintPercent(Now.AddSeconds(2)).Should().Be(10);
        status.GetPaintPercent(Now.AddSeconds(6)).Should().Be(0);
    }

    [Test]
    public void Afterburner_AddsItsPercent_AndAHoldShutsItDown()
    {
        ShipAfterburnerRules.ApplySpeed(1.0, 40).Should().BeApproximately(1.4, 1e-9);
        ShipAfterburnerRules.ApplySpeed(0.5, 25).Should().BeApproximately(0.625, 1e-9, "it multiplies with the cloak and routing");
        ShipAfterburnerRules.ApplySpeed(1.1, 0).Should().Be(1.1);
        ShipAfterburnerRules.GetError(true).Should().Be(ShipElectronicWarfareRules.InterdictedMessage);
        ShipAfterburnerRules.GetError(false).Should().BeEmpty();
    }

    [Test]
    public void TheModules_AreBuilt_AndAWebOrHoldNeedsAnotherShip()
    {
        var modules = new RevampModuleDefinition().BuildShipModules();
        modules["sm_web_45"].Type.Should().Be(ShipModuleType.IonWeb);
        modules["sm_intd_45"].Type.Should().Be(ShipModuleType.Interdictor);
        modules["sm_jam_45"].Type.Should().Be(ShipModuleType.Jammer);
        modules["sm_paint_45"].Type.Should().Be(ShipModuleType.TargetPainter);
        modules["sm_aburn_45"].Type.Should().Be(ShipModuleType.Afterburner);
        modules["sm_aburn_45"].CanTargetSelf.Should().BeTrue();
        foreach (var tag in new[] { "sm_web_45", "sm_intd_45", "sm_jam_45", "sm_paint_45" })
        {
            modules[tag].CanTargetSelf.Should().BeFalse(tag);
            modules[tag].ModuleValidationAction.Should().NotBeNull(tag);
            modules[tag].ModuleActivatedAction.Should().NotBeNull(tag);
        }
    }

    [Test]
    public void TheHold_IsWiredIntoEveryWayOutOfAFight()
    {
        ReadSource("Service", "SpaceSignatureJump.cs").Should().NotContain("IsShipInterdicted(uint ship) => false")
            .And.Contain("if (IsShipInterdicted(player))");
        ReadSource("Service", "SpaceElectronicWarfare.cs").Should()
            .Contain("GetShipStatus(ship)?.IsInterdicted(DateTime.UtcNow) == true")
            .And.Contain("_signatureJumps.Remove(target)")
            .And.Contain("ShutDownAfterburners(target, targetStatus);")
            .And.Contain("BreakCloakByElectronicWarfare(target, \"An Interdictor broke your cloak.\");")
            .And.Contain("BreakCloakByElectronicWarfare(target, \"An Ion Web broke your cloak.\");");
        ReadSource("Feature", "PlaceableScripts.cs").Should().Contain("Space.RefuseWhileInterdicted(user)");
        ReadSource("Feature", "DialogDefinition", "StarportDockDialog.cs").Should().Contain("Space.RefuseWhileInterdicted(player)");
        ReadSource("Service", "SpaceDocking.cs").Should().Contain("if (RefuseWhileInterdicted(player))");
        ReadSource("Service", "SpaceDerelicts.cs").Should().Contain("if (RefuseWhileInterdicted(player))");
        ReadSource("Service", "SpaceRiftRuns.cs").Should().Contain("var hold = GetInterdictionRefusal(user);");
        ReadSource("Service", "SpaceCloak.cs").Should().Contain("if (status.IsInterdicted(DateTime.UtcNow))");
    }

    [Test]
    public void PaintSpeedAndPvp_AreWiredIntoCombat()
    {
        ReadSource("Service", "Space.cs").Should()
            .Contain("targetStatus.GetPaintPercent(DateTime.UtcNow);")
            .And.Contain("if (ShipToggleRules.TargetsEnemy(shipModuleDetails.Type) &&")
            .And.Contain("if (!ShipToggleRules.TargetsEnemy(shipModuleDetail.Type) || targetShipStatus == null)");
        ReadSource("Service", "SpaceToggles.cs").Should()
            .Contain("ShipToggleRules.TargetsEnemy(detail.Type) && !CanShipAttack(creature, target)")
            .And.Contain("if (IsAfterburnerSlot(status, slotNumber))");
        ReadSource("Service", "SpacePowerRouting.cs").Should().Contain("GetAfterburnerPercent(status)));");
        ReadSource("Service", "SpaceHud.cs").Should().Contain("Space.GetAfterburnerPercent(status));");
    }

    private static string ReadSource(params string[] relativePath)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = relativePath.Aggregate(Path.Combine(directory!.FullName, "SWLOR.Game.Server"), Path.Combine);
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
