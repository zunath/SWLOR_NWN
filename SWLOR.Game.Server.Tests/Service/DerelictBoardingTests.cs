using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Derelict boarding (plan section 28, W15c): which derelicts can be boarded or are contested, the boarding and salvage
/// gates, the instanced interiors and their tier, safety and returning, contested waves, the loot step-up,
/// reinforcements, droid salvage teams and the payout. Numbers come from tools/SpaceRevampBalance/derelicts.py
/// (DerelictTuning).
/// </summary>
public class DerelictBoardingTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static string RepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return Path.Combine(new[] { directory!.FullName }.Concat(parts).ToArray());
    }

    private static DerelictRequest Request(ShipSize size = ShipSize.Small, float distance = 10f, DerelictMode underway = DerelictMode.None,
        bool sameParty = false, int docked = 0, int droids = 1) => new()
    {
        IsDerelict = true,
        Boardable = true,
        PartyLocated = true,
        ShipSize = size,
        Distance = distance,
        Underway = underway,
        SameParty = sameParty,
        DockedCount = docked,
        PostedDroids = droids,
    };

    private static DerelictBoarding Boarding(bool contested = true, DerelictMode mode = DerelictMode.Boarding, int tierIndex = 3,
        int salvageSeconds = 0)
    {
        var boarding = new DerelictBoarding
        {
            SignatureId = "sig-1",
            Mode = mode,
            TierIndex = tierIndex,
            SpaceTier = Math.Min(tierIndex, 6),
            Contested = contested,
            AreaResref = "hutlar_orbit",
            SalvageSecondsNeeded = salvageSeconds,
        };
        DerelictRules.Begin(boarding, Now);
        return boarding;
    }

    // Finding: boardable and contested derelicts

    [Test]
    public void Boardable_HalfOfDerelicts_AndContested_FortyPercentOfThose()
    {
        DerelictTuning.BoardablePercent.Should().Be(50);
        DerelictTuning.ContestedPercent.Should().Be(40);
        Enumerable.Range(0, 100).Count(DerelictRules.RollsBoardable).Should().Be(50);
        Enumerable.Range(0, 100).Count(DerelictRules.RollsContested).Should().Be(40);
    }

    [Test]
    public void Boardable_OnlyDerelictSignatures_AndContestedOnlyWhenBoardable()
    {
        var derelict = SignatureSiteRules.Create("d", "hutlar_orbit", 3, SignatureType.Derelict, 0, 0, 0, Now, boardable: true, contested: true);
        derelict.Boardable.Should().BeTrue();
        derelict.Contested.Should().BeTrue();

        SignatureSiteRules.Create("c", "hutlar_orbit", 3, SignatureType.CombatSite, 0, 0, 0, Now, boardable: true, contested: true)
            .Boardable.Should().BeFalse();
        var plain = SignatureSiteRules.Create("p", "hutlar_orbit", 3, SignatureType.Derelict, 0, 0, 0, Now, boardable: false, contested: true);
        plain.Boardable.Should().BeFalse();
        plain.Contested.Should().BeFalse();

        SignatureSiteRules.Create("w", "space_wspulsar", 5, SignatureType.Derelict, 0, 0, 0, Now, wildSpace: true, boardable: true)
            .Boardable.Should().BeTrue("Wild Space derelicts can be boarded too");
    }

    [Test]
    public void Scanner_ShowsBoardableAndContested_FromTheThreatLevel()
    {
        var derelict = SignatureSiteRules.Create("d", "hutlar_orbit", 3, SignatureType.Derelict, 50, 50, 0, Now, boardable: true, contested: true);
        ScanningRules.RowTooltip(derelict, ScanningTuning.InfoThreatPercent, 320, 320).Should().Contain("contested").And.Contain("/board");
        ScanningRules.RowTooltip(derelict, ScanningTuning.InfoTierPercent, 320, 320).Should().NotContain("/board");
    }

    [Test]
    public void Spawner_RollsTheBoardableAndContestedShares()
    {
        var source = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Service", "SpaceScanning.cs"));
        source.Should().Contain("DerelictRules.RollsBoardable(Random.Next(100))");
        source.Should().Contain("boardable && DerelictRules.RollsContested(Random.Next(100))");
    }

    [Test]
    public void ScannerTab_OffersBoardAndSalvageOnAReachableBoardableDerelict()
    {
        var derelict = SignatureSiteRules.Create("sig-b", "hutlar_orbit", 3, SignatureType.Derelict, 10f, 10f, 0f, Now, boardable: true);
        ScanningRules.AddSignal(derelict, "me", derelict.SignalRequired);
        ScanningRules.CanBoard(derelict, new[] { "me" }).Should().BeFalse("its site is not open yet");
        derelict.RevealedAt = Now;
        ScanningRules.CanBoard(derelict, new[] { "partner", "me" }).Should().BeTrue();
        ScanningRules.CanBoard(derelict, new[] { "outsider" }).Should().BeFalse("a party that did not scan it must scan it");

        var plain = SignatureSiteRules.Create("sig-p", "hutlar_orbit", 3, SignatureType.Derelict, 10f, 10f, 0f, Now);
        ScanningRules.AddSignal(plain, "me", plain.SignalRequired);
        plain.RevealedAt = Now;
        ScanningRules.CanBoard(plain, new[] { "me" }).Should().BeFalse("a derelict that cannot be boarded");

        var combat = SignatureSiteRules.Create("sig-c", "hutlar_orbit", 3, SignatureType.CombatSite, 10f, 10f, 0f, Now);
        ScanningRules.AddSignal(combat, "me", combat.SignalRequired);
        combat.RevealedAt = Now;
        ScanningRules.CanBoard(combat, new[] { "me" }).Should().BeFalse();

        var rows = ScanningRules.BuildRows(new[] { derelict, plain, combat }, new[] { "me" }, false, 320f, 320f);
        rows.Single(x => x.SignatureId == "sig-b").CanBoard.Should().BeTrue();
        rows.Where(x => x.SignatureId != "sig-b").Should().OnlyContain(x => !x.CanBoard && x.CanJump);

        derelict.DerelictLooted = true;
        ScanningRules.CanBoard(derelict, new[] { "me" }).Should().BeFalse("a stripped derelict");

        var hud = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Feature", "GuiDefinition", "ShipHudDefinition.cs"));
        hud.Should().Contain(".SetText(\"Board\")").And.Contain(".SetText(\"Salvage\")").And.Contain("model => model.SignatureCanBoard");
        var model = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "ShipHudViewModel.cs"))
            .Replace("\r\n", "\n");
        model.Should().Contain("if (Space.TargetDerelictSignature(Player, _scannerRows[index].SignatureId))\n                Space.BoardDerelict(Player);");
        model.Should().Contain("if (Space.TargetDerelictSignature(Player, _scannerRows[index].SignatureId))\n                Space.SendSalvageTeam(Player);");
        File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Service", "SpaceDerelicts.cs"))
            .Should().Contain("SetCurrentTarget(player, objective);");
    }

    // Docking and boarding gates

    [Test]
    public void Board_SmallAndMediumShipsDock_InRangeOutOfCombat()
    {
        DerelictRules.GetBoardRefusal(Request()).Should().Be(DerelictRefusal.None);
        DerelictRules.GetBoardRefusal(Request(ShipSize.Medium)).Should().Be(DerelictRefusal.None);
        DerelictRules.GetBoardRefusal(Request(ShipSize.Large)).Should().Be(DerelictRefusal.VisitorTooLarge);
        DerelictRules.GetBoardRefusal(Request(ShipSize.Capital)).Should().Be(DerelictRefusal.VisitorTooLarge);
        DerelictRules.GetBoardRefusal(Request(distance: ShipDockingRules.DockRange + 1)).Should().Be(DerelictRefusal.OutOfRange);
        DerelictRules.GetBoardRefusal(new DerelictRequest { IsDerelict = true, Boardable = true, PartyLocated = true, ShipSize = ShipSize.Small, InCombat = true })
            .Should().Be(DerelictRefusal.InCombat);
        DerelictRules.GetBoardRefusal(new DerelictRequest { IsDerelict = true, Boardable = true, PartyLocated = true, ShipSize = ShipSize.Small, DefensesLeft = true })
            .Should().Be(DerelictRefusal.DefensesLeft);
    }

    [Test]
    public void Board_RefusesNonDerelicts_UnboardableLootedAndUnscanned()
    {
        DerelictRules.GetBoardRefusal(new DerelictRequest()).Should().Be(DerelictRefusal.NotADerelict);
        DerelictRules.GetBoardRefusal(new DerelictRequest { IsDerelict = true }).Should().Be(DerelictRefusal.NotBoardable);
        DerelictRules.GetBoardRefusal(new DerelictRequest { IsDerelict = true, Boardable = true, Looted = true }).Should().Be(DerelictRefusal.Looted);
        DerelictRules.GetBoardRefusal(new DerelictRequest { IsDerelict = true, Boardable = true }).Should().Be(DerelictRefusal.NotLocated);
    }

    [Test]
    public void Board_OneParty_TwoShips_NoBoardingDuringASalvageTeam()
    {
        DerelictRules.GetBoardRefusal(Request(underway: DerelictMode.Boarding, sameParty: true, docked: 1)).Should().Be(DerelictRefusal.None);
        DerelictRules.GetBoardRefusal(Request(underway: DerelictMode.Boarding, sameParty: true, docked: DerelictTuning.DockCapacity))
            .Should().Be(DerelictRefusal.Full);
        DerelictRules.GetBoardRefusal(Request(underway: DerelictMode.Boarding, sameParty: false, docked: 1)).Should().Be(DerelictRefusal.OtherParty);
        DerelictRules.GetBoardRefusal(Request(underway: DerelictMode.SalvageTeam)).Should().Be(DerelictRefusal.SalvageUnderway);
        DerelictTuning.DockCapacity.Should().Be(2);
    }

    [Test]
    public void Salvage_AnyHullWithPostedDroids_NotWhileBoarded()
    {
        DerelictRules.GetSalvageRefusal(Request(ShipSize.Large)).Should().Be(DerelictRefusal.None, "the ship holds beside the derelict, it does not dock");
        DerelictRules.GetSalvageRefusal(Request(droids: 0)).Should().Be(DerelictRefusal.NoDroids);
        DerelictRules.GetSalvageRefusal(Request(underway: DerelictMode.Boarding, sameParty: true)).Should().Be(DerelictRefusal.BoardingUnderway);
        DerelictRules.GetSalvageRefusal(Request(underway: DerelictMode.SalvageTeam)).Should().Be(DerelictRefusal.SalvageUnderway);
        DerelictRules.GetSalvageRefusal(Request(distance: 100f)).Should().Be(DerelictRefusal.OutOfRange);
    }

    [Test]
    public void Refusals_AllHaveMessages()
    {
        foreach (var refusal in Enum.GetValues<DerelictRefusal>().Where(x => x != DerelictRefusal.None))
            DerelictRules.DescribeRefusal(refusal).Should().NotBeNullOrWhiteSpace(refusal.ToString());
    }

    [Test]
    public void Commands_BoardAndSalvage_AreOpenToEveryone()
    {
        var commands = new SpaceChatCommand().BuildChatCommands();
        commands.Should().ContainKey("board");
        commands.Should().ContainKey("salvage");
        commands["board"].Authorization.Should().Be(SWLOR.Game.Server.Enumeration.AuthorizationLevel.All);
        commands["salvage"].Authorization.Should().Be(SWLOR.Game.Server.Enumeration.AuthorizationLevel.All);
    }

    // Instances and tier

    [Test]
    public void Tier_SpaceTiersMapToContentTiers_WildSpaceIsTierFivePointFive()
    {
        DerelictRules.TierIndex(1, false).Should().Be(1);
        DerelictRules.TierIndex(6, false).Should().Be(6);
        DerelictRules.TierIndex(5, true).Should().Be(DerelictTuning.WildSpaceTierIndex);
        DerelictRules.SlicingTier(6).Should().Be(5);
        DerelictRules.SlicingTier(DerelictTuning.WildSpaceTierIndex).Should().Be(5);
        DerelictRules.TerminalResref(2).Should().Be("slice_term_2");
        DerelictRules.LockboxResref(DerelictTuning.WildSpaceTierIndex).Should().Be("lockbox_t5");
        DerelictRules.ScrapResref(6).Should().Be("ship_scrap_5");
        DerelictRules.ScrapResref(DerelictTuning.WildSpaceTierIndex).Should().Be("ship_scrap_5");
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(7)]
    public void Interior_HoldsTheTiersGroundEnemiesTerminalsAndAStrongbox(int tierIndex)
    {
        var plan = DerelictRules.PlanInterior(tierIndex, max => max - 1);
        DerelictTuning.TemplateResrefs.Should().Contain(plan.TemplateResref);
        plan.Enemies.Should().HaveCount(DerelictTuning.InteriorEnemies).And.OnlyContain(x => DerelictRules.Roster(tierIndex).Contains(x));
        plan.Terminals.Should().HaveCount(DerelictTuning.InteriorTerminals).And.OnlyContain(x => x == DerelictRules.TerminalResref(tierIndex));
        plan.Dressing.Should().HaveCount(DerelictTuning.DressingCount);
        plan.StrongboxResref.Should().Be(DerelictTuning.StrongboxResref);
    }

    [Test]
    public void Templates_AreExistingStarshipInteriors_WithAnEntranceAndAnExit()
    {
        var layouts = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Feature", "PropertyLayoutDefinition", "StarshipLayoutDefinition.cs"));
        DerelictTuning.TemplateResrefs.Should().HaveCount(7).And.HaveSameCount(DerelictTuning.TemplateNames);
        DerelictTuning.TemplateResrefs.Should().Contain(new[] { "ship_mule_z", "ship_falchion_z", "ship_saber_z", "ship_corv_v" }, "W11o added the roomy hull layouts");
        foreach (var resref in DerelictTuning.TemplateResrefs)
        {
            layouts.Should().Contain($".AreaInstance(\"{resref}\")");
            var git = File.ReadAllText(RepositoryFile("Module", "git", resref + ".git.json"));
            git.Should().Contain("\"PROPERTY_ENTRANCE\"").And.Contain("\"building_exit\"");
        }
    }

    [Test]
    public void Blueprints_EveryResrefExists()
    {
        foreach (var roster in DerelictTuning.Rosters.Skip(1))
        {
            roster.Should().NotBeEmpty();
            foreach (var resref in roster)
            {
                var utc = JObject.Parse(File.ReadAllText(RepositoryFile("Module", "utc", resref + ".utc.json")));
                ((JArray)utc["VarTable"]!["value"]!).Should().Contain(x => ((string)x["Name"]!["value"]!).StartsWith("LOOT_TABLE_"),
                    $"{resref} drops its ground loot");
            }
        }

        foreach (var resref in DerelictTuning.DressingResrefs.Append(DerelictTuning.StrongboxResref)
                     .Concat(Enumerable.Range(1, 5).Select(t => $"slice_term_{t}")))
        {
            File.Exists(RepositoryFile("Module", "utp", resref + ".utp.json")).Should().BeTrue(resref);
        }

        foreach (var resref in Enumerable.Range(1, 5).SelectMany(t => new[] { $"lockbox_t{t}", $"ship_scrap_{t}" }))
            File.Exists(RepositoryFile("Module", "uti", resref + ".uti.json")).Should().BeTrue(resref);
    }

    [Test]
    public void Rosters_RiseWithTheTier_AndWildSpaceUsesTheTopBand()
    {
        DerelictTuning.Rosters.Should().HaveCount(8);
        DerelictRules.Roster(1).Should().Contain("mynock");
        DerelictRules.Roster(3).Should().OnlyContain(x => x.StartsWith("abs_"));
        DerelictRules.Roster(5).Should().OnlyContain(x => x.StartsWith("fdy_"));
        DerelictRules.Roster(DerelictTuning.WildSpaceTierIndex).Should().Equal(DerelictRules.Roster(6));
        DerelictRules.Roster(DerelictTuning.WildSpaceTierIndex).Should().NotIntersectWith(DerelictRules.Roster(5));
    }

    // Safety and returning

    [Test]
    public void Hold_ABoardedSiteStaysOpen_UntilItsHoldEnds()
    {
        var signature = SignatureSiteRules.Create("d", "hutlar_orbit", 3, SignatureType.Derelict, 0, 0, 0, Now, boardable: true);
        signature.RevealedAt = Now;
        var late = Now.AddSeconds(ScanningTuning.SiteLifetimeSeconds + 60);
        SignatureSiteRules.IsOver(signature, late).Should().BeTrue();

        signature.HeldUntil = DerelictRules.HoldUntil(Now);
        SignatureSiteRules.IsOver(signature, late).Should().BeFalse("boarders are aboard");
        SignatureSiteRules.IsOver(signature, Now.AddMinutes(DerelictTuning.BoardingHoldMinutes)).Should().BeTrue();

        var boarding = Boarding();
        DerelictRules.IsExpired(boarding, Now.AddMinutes(DerelictTuning.BoardingHoldMinutes - 1)).Should().BeFalse();
        DerelictRules.IsExpired(boarding, Now.AddMinutes(DerelictTuning.BoardingHoldMinutes)).Should().BeTrue();
    }

    [Test]
    public void Safety_TheDockedShipLeavesSpace_SoNothingCanAttackIt()
    {
        var source = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Service", "SpaceDerelicts.cs"));
        source.Should().Contain("property.Positions.Remove(PropertyLocationType.CurrentPosition)");
        source.Should().Contain("ExitSpaceMode(player)");
        new PlayerShip().DockedDerelictId.Should().BeEmpty("older records and new ships are not docked at a derelict");
    }

    [Test]
    public void Returning_TheCrewShipFirst_ThenAnOwnedShip_ThenTheFirstDocked()
    {
        var boarding = Boarding();
        boarding.DockedShipIds.AddRange(new[] { "ship-a", "ship-b" });
        boarding.CrewByShipId["ship-a"] = new List<string> { "pilot-a" };
        boarding.CrewByShipId["ship-b"] = new List<string> { "pilot-b", "gunner-b" };
        var owners = new Dictionary<string, string> { ["ship-a"] = "owner-a", ["ship-b"] = "owner-b" };

        DerelictRules.ChooseShip(boarding, "gunner-b", owners).Should().Be("ship-b");
        DerelictRules.ChooseShip(boarding, "owner-b", owners).Should().Be("ship-b");
        DerelictRules.ChooseShip(boarding, "stranger", owners).Should().Be("ship-a");
        DerelictRules.ChooseShip(Boarding(), "stranger", owners).Should().BeNull();
    }

    [Test]
    public void Returning_ToTheShipInterior_ElseItsDock_ElseTheStart()
    {
        DerelictRules.ChooseReturn(true, true).Should().Be(DerelictReturn.ShipInterior);
        DerelictRules.ChooseReturn(true, false).Should().Be(DerelictReturn.ShipInterior);
        DerelictRules.ChooseReturn(false, true).Should().Be(DerelictReturn.ShipDock);
        DerelictRules.ChooseReturn(false, false).Should().Be(DerelictReturn.Start);
    }

    [Test]
    public void Abandoned_OnceNoShipIsDockedAndNobodyIsAboard()
    {
        var boarding = Boarding();
        DerelictRules.IsBoardingAbandoned(boarding, 0).Should().BeTrue();
        boarding.DockedShipIds.Add("ship-a");
        DerelictRules.IsBoardingAbandoned(boarding, 0).Should().BeFalse("the crew walked back to their docked ship");
        boarding.DockedShipIds.Clear();
        DerelictRules.IsBoardingAbandoned(boarding, 1).Should().BeFalse();
        DerelictRules.IsBoardingAbandoned(Boarding(mode: DerelictMode.SalvageTeam), 0).Should().BeFalse();
    }

    [Test]
    public void Wiring_UndockLoginAndTheShipsHatch()
    {
        var space = File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Service", "Space.cs"));
        space.Should().Contain("UndockFromDerelict(player, dbShip)").And.Contain("CompleteDerelictUndock(player, launch)");
        File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Feature", "PersistentLocation.cs")).Should().Contain("TryGetDerelictLoginRedirect");
        File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Feature", "DialogDefinition", "PropertyExitDialog.cs")).Should().Contain("CrossToDerelict");
        File.ReadAllText(RepositoryFile("SWLOR.Game.Server", "Service", "SpaceDocking.cs")).Should().Contain("IsDockedAtDerelict(ship, out var derelictName)");
        ScriptName.OnDerelictAirlockUsed.Length.Should().BeLessThanOrEqualTo(16);
        ScriptName.OnDerelictStrongboxUsed.Length.Should().BeLessThanOrEqualTo(16);
    }

    // Contested waves, the loot step-up and reinforcements

    [Test]
    public void Waves_FirstAfterBoarding_NextAfterEachIsCleared_UpToTheMaximum()
    {
        var boarding = Boarding();
        DerelictRules.WaveDue(boarding, Now).Should().BeFalse();
        var time = Now.AddSeconds(DerelictTuning.WaveFirstSeconds);
        for (var wave = 1; wave <= DerelictTuning.MaximumWaves; wave++)
        {
            DerelictRules.WaveDue(boarding, time).Should().BeTrue($"wave {wave}");
            DerelictRules.RecordWaveSpawned(boarding);
            DerelictRules.WaveDue(boarding, time.AddMinutes(10)).Should().BeFalse("a wave is out");
            time = time.AddMinutes(3);
            DerelictRules.RecordWaveCleared(boarding, time);
            boarding.WavesCleared.Should().Be(wave);
            DerelictRules.WaveDue(boarding, time.AddSeconds(DerelictTuning.WaveGapSeconds - 1)).Should().BeFalse();
            time = time.AddSeconds(DerelictTuning.WaveGapSeconds);
        }

        DerelictRules.WaveDue(boarding, time.AddHours(1)).Should().BeFalse("no more than the maximum");
        DerelictRules.WaveDue(Boarding(contested: false), Now.AddHours(1)).Should().BeFalse("only contested derelicts draw waves");
    }

    [Test]
    public void Waves_StopOnceTheStrongboxIsOpened_AndLaterClearsDoNotCount()
    {
        var boarding = Boarding();
        DerelictRules.RecordWaveSpawned(boarding);
        boarding.Looted = true;
        DerelictRules.RecordWaveCleared(boarding, Now);
        boarding.WavesCleared.Should().Be(0);
        DerelictRules.WaveDue(boarding, Now.AddHours(1)).Should().BeFalse();
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void LootStep_EachWaveClearedRaisesTheStrongbox(int tierIndex)
    {
        var step = DerelictRules.WaveStepPercent(tierIndex);
        step.Should().BeInRange(5, 60);
        var baseCredits = DerelictTuning.StrongboxCredits[tierIndex];
        DerelictRules.StrongboxCredits(tierIndex, 0).Should().Be(baseCredits);
        for (var waves = 1; waves <= DerelictTuning.MaximumWaves; waves++)
        {
            DerelictRules.StrongboxCredits(tierIndex, waves).Should().BeGreaterThan(DerelictRules.StrongboxCredits(tierIndex, waves - 1));
            DerelictRules.StrongboxCredits(tierIndex, waves).Should().Be((int)Math.Round(baseCredits * (1 + step * waves / 100.0), MidpointRounding.AwayFromZero));
        }

        DerelictRules.StrongboxCredits(tierIndex, 10).Should().Be(DerelictRules.StrongboxCredits(tierIndex, DerelictTuning.MaximumWaves));
        DerelictRules.IsPaidBoarder(new HashSet<string> { "fighter" }, "fighter", false).Should().BeTrue();
        DerelictRules.IsPaidBoarder(new HashSet<string> { "fighter" }, "passenger", false).Should().BeFalse("an idle passenger is not paid");
        DerelictRules.IsPaidBoarder(new HashSet<string>(), "opener", true).Should().BeTrue("the opener is paid");
        DerelictRules.StrongboxLockboxes(0).Should().Be(1);
        DerelictRules.StrongboxLockboxes(2).Should().Be(3);
        DerelictRules.DefenderCredits(tierIndex, 2).Should().Be((int)Math.Round(DerelictRules.StrongboxCredits(tierIndex, 2) * 0.5, MidpointRounding.AwayFromZero));
    }

    [Test]
    public void Strongbox_OpensOnlyOnceTheInteriorIsClear()
    {
        DerelictRules.CanOpenStrongbox(0).Should().BeTrue();
        DerelictRules.CanOpenStrongbox(1).Should().BeFalse();
    }

    [Test]
    public void Reinforcements_OnlyWhenAWaveFindsNoDefender_WhileSomeoneIsAboard()
    {
        var boarding = Boarding();
        DerelictRules.RecordWaveSpawned(boarding);
        DerelictRules.ShouldReinforce(boarding, 0, 2, Now).Should().BeTrue();
        DerelictRules.ShouldReinforce(boarding, 1, 2, Now).Should().BeFalse("a ship defends the derelict");
        DerelictRules.ShouldReinforce(boarding, 0, 0, Now).Should().BeFalse("nobody is aboard");
        DerelictRules.ShouldReinforce(Boarding(contested: false), 0, 2, Now).Should().BeFalse();

        boarding.Reinforcements = DerelictTuning.ReinforcementCount;
        boarding.LastReinforcementAt = Now;
        DerelictRules.ShouldReinforce(boarding, 0, 2, Now.AddSeconds(DerelictTuning.ReinforcementSeconds - 1)).Should().BeFalse();
        DerelictRules.ShouldReinforce(boarding, 0, 2, Now.AddSeconds(DerelictTuning.ReinforcementSeconds)).Should().BeTrue();

        boarding.Reinforcements = DerelictTuning.MaximumReinforcements - 1;
        DerelictRules.ReinforcementSize(boarding).Should().Be(1);
        boarding.Reinforcements = DerelictTuning.MaximumReinforcements;
        DerelictRules.ShouldReinforce(boarding, 0, 2, Now.AddHours(1)).Should().BeFalse();

        var calm = Boarding();
        DerelictRules.ShouldReinforce(calm, 0, 2, Now).Should().BeFalse("no wave is out");
    }

    [Test]
    public void Defense_AndTether_Radii()
    {
        DerelictRules.IsDefending(DerelictTuning.DefenseRadius).Should().BeTrue();
        DerelictRules.IsDefending(DerelictTuning.DefenseRadius + 1).Should().BeFalse();
        DerelictRules.IsTethered(DerelictTuning.SalvageTetherMeters).Should().BeTrue();
        DerelictRules.IsTethered(DerelictTuning.SalvageTetherMeters + 1).Should().BeFalse();
    }

    // Droid salvage teams

    [Test]
    public void SalvageTeam_IndustrySetsTheTimer_ThroughTheDroidHook()
    {
        DerelictRules.TeamIndustry(new[] { 40, 30 }).Should().Be(70);
        DerelictRules.TeamIndustry(new[] { 100, 60 }).Should().Be(DerelictTuning.SalvageIndustryCap, "a team is capped at one best droid");
        DerelictRules.TeamIndustry(Array.Empty<int>()).Should().Be(0);

        DerelictRules.SalvageSeconds(0).Should().Be(DerelictTuning.SalvageBaseSeconds);
        DerelictRules.SalvageSeconds(80).Should().Be(387);
        DerelictRules.SalvageSeconds(100).Should().Be(369);
        DroidIndustryRules.SalvageTeamSpeedPercent(100).Should().Be(30);
    }

    [Test]
    public void SalvageTeam_IndustryRaisesTheYield()
    {
        DroidIndustryRules.SalvageTeamYieldPercent(100).Should().Be(25);
        DerelictRules.SalvageCredits(3, 0).Should().Be(DerelictTuning.SalvageCredits[3]);
        DerelictRules.SalvageCredits(3, 100).Should().Be((int)Math.Round(DerelictTuning.SalvageCredits[3] * 1.25, MidpointRounding.AwayFromZero));
        DerelictRules.SalvageScrap(0).Should().Be(DerelictTuning.SalvageScrap);
        DerelictRules.SalvageScrap(100).Should().Be(8);
    }

    [Test]
    public void SalvageTeam_WorksOnTheTimer_AndAWaveOutPausesIt()
    {
        var team = Boarding(mode: DerelictMode.SalvageTeam, salvageSeconds: 10);
        DerelictRules.AdvanceSalvage(team, 4);
        team.SalvageProgress.Should().Be(4);
        DerelictRules.RecordWaveSpawned(team);
        DerelictRules.AdvanceSalvage(team, 4);
        team.SalvageProgress.Should().Be(4, "the droids take cover while a wave is out");
        DerelictRules.RecordWaveCleared(team, Now);
        DerelictRules.AdvanceSalvage(team, 20);
        team.SalvageProgress.Should().Be(10);
        DerelictRules.IsSalvageDone(team).Should().BeTrue();
        DerelictRules.IsSalvageDone(Boarding()).Should().BeFalse();
    }

    // Payout

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public void Payout_BoardingContestedAndSalvage_AgainstTheirTargets(int tierIndex)
    {
        var combat = (double)DerelictTuning.CombatCreditsPerHour[tierIndex];
        var boarding = DerelictTuning.BoardingCreditsPerHour[tierIndex];
        (boarding / combat).Should().BeInRange(1.10, 1.20, "boarding pays 110-120% of the tier's rate");
        DerelictTuning.ContestedGroupCreditsPerHour[tierIndex].Should().BeGreaterThan(boarding, "contested derelicts pay the mixed group more");
        ((double)DerelictTuning.SalvageCreditsPerHour[tierIndex] / boarding).Should().BeInRange(0.60, 0.70, "a salvage team pays 60-70% of boarding");
        DerelictTuning.StrongboxCredits[tierIndex].Should().BePositive();
        DerelictTuning.SalvageCredits[tierIndex].Should().BePositive();
    }
}
