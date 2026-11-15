#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Tests.Feature.SpaceItemFixtures;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Fixes from the 2026-11-14 full review of the space and city revamp: targets in another system, extraction holds,
/// agent leads, downed mission holders, DM signatures, the sensor sweep, docking saves, ship management on stale
/// windows, login notices and the DB cache's shared ids.
/// </summary>
public class SpaceRevampReviewFixTests
{
    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(RepositoryRoot(), "SWLOR.Game.Server"), Path.Combine)).Replace("\r\n", "\n");

    private static string Between(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        from.Should().BeGreaterThanOrEqualTo(0, $"'{start}' is in the source");
        return source[from..source.IndexOf(end, from + start.Length, StringComparison.Ordinal)];
    }

    [Test]
    public void AModule_TreatsATargetInAnotherSystemAsNoTarget()
    {
        var activate = Between(ReadSource("Service", "Space.cs"), "private static void ActivateShipModuleCore(", "// Check to ensure activator is within maximum distance.");
        var pick = activate.IndexOf("var (target, targetShipStatus) = GetCurrentTarget(isDroid ? activator : user);", StringComparison.Ordinal);
        var guard = activate.IndexOf("if (GetIsObjectValid(target) && GetArea(target) != GetArea(activator))", StringComparison.Ordinal);
        pick.Should().BeGreaterThanOrEqualTo(0);
        guard.Should().BeGreaterThan(pick, "distance across areas reads 0, so a ship that jumped away must not stay a target");
    }

    [Test]
    public void AShipDeath_RemovesTheShipWideFeatsAndSensorState()
    {
        var death = ReadSource("Service", "SpaceDeath.cs");
        death.Should().Contain("CreaturePlugin.RemoveFeat(creature, FeatType.ShipAllWeapons);")
            .And.Contain("CreaturePlugin.RemoveFeat(creature, FeatType.ShipJumpToSignature);")
            .And.Contain("ClearScanningState(creature);");
    }

    [Test]
    public void AnExtractionCycle_EndsOnTheShip_SoACrewLogoutCannotLeaveTheTargetHeld()
    {
        foreach (var file in new[] { "ShipModuleActions.cs", "ShipSalvageActions.cs" })
        {
            var source = ReadSource("Feature", "ShipModuleDefinition", file);
            source.Should().NotMatchRegex(@"\n            DelayCommand\((cycleSeconds \+ 0\.1f|seconds), \(\) =>\n            \{\n                ReleaseExtraction",
                $"{file}: a bare delay dies with the crew member who started it");
            source.Should().Contain("AssignCommand(activator, () => DelayCommand(");
        }
    }

    [Test]
    public void AnAgentLead_OpensOnlyWithItsHolderInTheParty()
    {
        ReadSource("Service", "FactionAgentEvents.cs").Should().Contain("public static string GetLeadOpenRefusal(ScanSignature signature, uint opener)");

        var cache = Between(ReadSource("Service", "SpaceScanSites.cs"), "public static void OpenSiteCache(", "FactionAgents.OnSiteCompleted(signature, user, cache);");
        cache.IndexOf("FactionAgents.GetLeadOpenRefusal(signature, user);", StringComparison.Ordinal)
            .Should().BeLessThan(cache.LastIndexOf("signature.Completed = true;", StringComparison.Ordinal),
                "the cache pays only when the mission is credited, or one mission could be looted over and over");

        ReadSource("Service", "SpaceSlicing.cs").Should().Contain("return FactionAgents.GetLeadOpenRefusal(signature, slicer);");
    }

    [Test]
    public void AMissionCreditedWhileItsHolderIsDown_FinishesOnceTheyStand()
    {
        var agents = ReadSource("Service", "FactionAgents.cs");
        var credit = Between(agents, "private static void Credit(uint player, string missionId, uint source)", "// Agent intel");
        credit.Should().Contain("if (GetIsDead(player) || GetCurrentHitPoints(player) <= 0)\n                RetryMissionAdvance(player, missionId);");
        credit.Should().Contain("AssignCommand(GetModule(), () => DelayCommand(MissionAdvanceRetrySeconds,",
            "the credit's source (a cache or a target can) is destroyed, which would end a delay scheduled on it");
        credit.Should().Contain("Quest.GetQuestByIdOrDefault(missionId)?.Advance(player, OBJECT_INVALID);");

        Between(agents, "public static void RegisterHeldMissions()", "// The board").Should().Contain("RetryMissionAdvance(player, mission.Id);");
    }

    [Test]
    public void ADmSignature_IsAnExtra_AndIsNeverRespawned()
    {
        ReadSource("Service", "SpaceDmTools.cs").Should().Contain("signature.DmPlaced = true;");
        var scanning = ReadSource("Service", "SpaceScanning.cs");
        scanning.Should().Contain("if (!signature.IsAgentLead && !signature.DmPlaced)");
        scanning.Should().Contain("signatures.Count(x => x.Type != SignatureType.Rift && !x.IsAgentLead && !x.DmPlaced)");
        new ScanSignature().DmPlaced.Should().BeFalse();
    }

    [Test]
    public void TheSensorSweepCooldown_SurvivesLeavingTheControls_AndEndsAtLogout()
    {
        var jump = ReadSource("Service", "SpaceSignatureJump.cs");
        Between(jump, "private static void ClearScanningState(uint player)", "private static void ForgetSensorSweep(")
            .Should().NotContain("_lastSensorSweep.Remove(player);", "retaking the controls must not reset a sweep that reveals cloaked ships");
        jump.Should().Contain("private static void ForgetSensorSweep(uint player) => _lastSensorSweep.Remove(player);");
        Between(ReadSource("Service", "Space.cs"), "public static void ExitServer()", "MarkRiftRunPilotAway(player);")
            .Should().Contain("ForgetSensorSweep(player);");
    }

    [Test]
    public void Docking_BuildsOnTheCrewsSave_AndADetachNeverPicksASecondPilot()
    {
        var docking = ReadSource("Service", "SpaceDocking.cs");
        docking.IndexOf("LeaveCrewStation(watcher, returnToConsole: false);", StringComparison.Ordinal)
            .Should().BeLessThan(docking.IndexOf("visitor = DB.Get<PlayerShip>(visitor.Id) ?? visitor;", StringComparison.Ordinal));
        docking.IndexOf("visitor = DB.Get<PlayerShip>(visitor.Id) ?? visitor;", StringComparison.Ordinal)
            .Should().BeLessThan(docking.IndexOf("visitor.DockedToShipId = host.Id;", StringComparison.Ordinal),
                "releasing the crew saves their station hotbars to the ship first");

        docking.Should().Contain("var launchPending = IsShipLaunchPending(visitor.Id);")
            .And.Contain("ShipDockingRules.ChooseDetachPilot(entries[index], playerId =>\n                    !launchPending &&");
    }

    [Test]
    public void ShipManagement_ActsOnTheShipAsSavedNow()
    {
        var window = ReadSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs");
        window.Should().Contain("private PlayerShip GetSelectedShip()");
        window.Should().Contain("FloatingTextStringOnCreature(\"That ship is no longer registered.\", Player, false);");
        window.Should().Contain("DB.Get<WorldProperty>(ship.PropertyId)?.CustomName ?? \"(Unregistered)\"");

        var rename = Between(window, "public Action OnClickSaveShipName()", "private bool ValidateModuleEquip(");
        rename.IndexOf("permission.Permissions.GetValueOrDefault(PropertyPermissionType.RenameProperty)", StringComparison.Ordinal)
            .Should().BeLessThan(rename.IndexOf("dbProperty.CustomName = ShipName;", StringComparison.Ordinal),
                "a window opened before a transfer must not rename the new owner's ship");
        rename.Should().Contain("ShipTransfer.IsListedForSale(dbShip.Id)");

        foreach (var handler in new[] { "public Action OnClickPermissions()", "public Action OnClickRepair()", "private void ProcessConfiguration(int slot)" })
            Between(window, handler, "\n        }").Should().Contain("var dbShip = GetSelectedShip();");
    }

    [Test]
    public void ACargoPod_IsScoopedOnlyByAPilotFlyingTheirOwnShip()
    {
        Between(ReadSource("Service", "TradeCargoSpill.cs"), "public static void ScoopPod(uint pod, uint user)", "Only a ship can scoop a cargo pod.")
            .Should().Contain("if (!Space.IsPlayerInSpaceMode(user) || !Space.IsShipInSpace(user))",
                "crew keep an active ship they are not flying, which would receive the goods remotely");
    }

    [Test]
    public void CustomsAndPirates_RollOnlyForPilotsFlyingTheirOwnShips()
    {
        ReadSource("Service", "TradeOrbitRisk.cs").Should().Contain(
            "GetArea(player) == area && Space.IsPlayerInSpaceMode(player) && Space.IsShipInSpace(player);");
    }

    [Test]
    public void TheLastShipInAnOpenRiftRun_CannotLeaveTheControls()
    {
        RiftRunRules.LeavingWouldAbandon(RiftRunPhase.Fighting, 0).Should().BeTrue("a solo exit would wreck the ship");
        RiftRunRules.LeavingWouldAbandon(RiftRunPhase.Waiting, 0).Should().BeTrue();
        RiftRunRules.LeavingWouldAbandon(RiftRunPhase.Advancing, 0).Should().BeTrue();
        RiftRunRules.LeavingWouldAbandon(RiftRunPhase.Fighting, 1).Should().BeFalse("a group's ship waits for its pilot to rejoin");
        RiftRunRules.LeavingWouldAbandon(RiftRunPhase.Returning, 0).Should().BeFalse("a cleared run is already going home");
        RiftRunRules.LeavingWouldAbandon(RiftRunPhase.Closing, 0).Should().BeFalse();

        var exit = Between(ReadSource("Feature", "ChatCommandDefinition", "SpaceChatCommand.cs"), "private void ExitSpaceCommand()", "private void DockCommand()");
        exit.IndexOf("if (Space.RefuseLeavingLastRiftRunShip(user))", StringComparison.Ordinal)
            .Should().BeGreaterThan(0).And.BeLessThan(exit.IndexOf("Space.WarpPlayerInsideShip(user);", StringComparison.Ordinal));
        ReadSource("Service", "SpaceRiftRuns.cs").Should().Contain(
            "RiftRunRules.LeavingWouldAbandon(run.Phase, run.Pilots.Count(x => x != pilot && GetIsObjectValid(x)))");
    }

    [Test]
    public void AShipAtADerelict_CannotChangeHands()
    {
        ReadSource("Service", "ShipTransfer.cs").Should().Contain("DockedAway = Space.IsDockedAboardHost(ship, out _) || Space.IsDockedAtDerelict(ship, out _),");
    }

    [Test]
    public void LivestockNotices_AreSentOnceTheClientShowsChat_AndDeletedOnlyWhenSent()
    {
        var notices = Between(ReadSource("Service", "Corrals.cs"), "public static void DeliverNotices()", "private const float NoticeDelaySeconds");
        var delay = notices.IndexOf("DelayCommand(NoticeDelaySeconds, () =>", StringComparison.Ordinal);
        delay.Should().BeGreaterThanOrEqualTo(0);
        notices.IndexOf("DB.Delete<LivestockNotice>(notice.Id);", StringComparison.Ordinal).Should().BeGreaterThan(delay);
    }

    [Test]
    public void RecordsKeyedByACdKeyOrShipId_HaveTheirOwnIds()
    {
        // The DB cache holds one entity per id for the frame, whatever the type: a bare CD key is the account's id and a
        // bare ship id the ship's, so a second type under it throws an InvalidCastException on the next Get.
        new VoterRewardLedger("cd").Id.Should().NotBe("cd");
        new CivicExchangeAllowance("cd").Id.Should().NotBe("cd");
        ShipRegistrationReceipt.KeyFor("ship").Should().NotBe("ship");

        ReadSource("Service", "CivicExchange.cs").Should().NotContain("DB.Get<CivicExchangeAllowance>(GetAllowanceKey(player))")
            .And.NotContain("DB.Get<CivicExchangeAllowance>(allowanceKey)")
            .And.NotContain("DB.Get<CivicExchangeAllowance>(parts[1])");
        ReadSource("Service", "ElectionBallot.cs").Should().Contain("DB.Get<VoterRewardLedger>(VoterRewardLedger.KeyFor(cdKey))");
        ReadSource("Service", "SpaceShipItemMoves.cs").Should().Contain("new ShipRegistrationReceipt { Id = ShipRegistrationReceipt.KeyFor(shipId),");
    }

    [Test]
    public void BuyingTheCivicBuff_SavesOverTheFoodRemovalsSave_NotAStaleCopy()
    {
        var buy = Between(ReadSource("Service", "CivicExchange.cs"), "private static string BuyBuff(", "SendMessageToPC(player");
        var apply = buy.IndexOf("StatusEffect.ApplyStatusEffect<CivicProvisionsStatusEffect>", StringComparison.Ordinal);
        var reread = buy.IndexOf("dbPlayer = DB.Get<Player>(dbPlayer.Id) ?? dbPlayer;", StringComparison.Ordinal);
        apply.Should().BeGreaterThanOrEqualTo(0);
        reread.Should().BeGreaterThan(apply, "ending the food buff saves the character, so the XP is taken from that save");
        buy.IndexOf("dbPlayer.UnallocatedXP -= item.PriceXp;", StringComparison.Ordinal).Should().BeGreaterThan(reread);
    }

    [Test]
    public void TheFertilizerPicked_IsTheOnePlanted_WhateverTiersAreCarried()
    {
        var load = Between(ReadSource("Feature", "GuiDefinition", "ViewModel", "GardenPlotViewModel.cs"), "private void LoadFertilizers(", "private void RefreshPlantButton(");
        load.Should().Contain("options.Add(new GuiComboEntry(FertilizerRules.DescribeOption(tier, count), _fertilizerTiers.Count));",
                "the combo selects an entry's value, which GetSelectedFertilizer reads as a place in the tiers carried")
            .And.Contain("var index = SelectedFertilizerIndex - 1;");
    }

    [Test]
    public void ABounty_LandsOnlyOnTheCharacterPicked()
    {
        ReadSource("Service", "CityBounties.cs").Should().Contain("if (dbTarget == null || dbTarget.Id != expectedPlayerId)");
        ReadSource("Feature", "GuiDefinition", "ViewModel", "CityAffairsViewModel.Bounties.cs")
            .Should().Contain("CityBounties.Post(Player, _cityId, target.Player, target.PlayerId, target.DisguiseId, reward)");
    }

    [Test]
    public void APassengerDeliveredAfterTheirFlightLanded_GetsTheHomePortPass()
    {
        var confirm = Between(ReadSource("Service", "Shuttle.cs"), "private static void ConfirmDelivery(", "private static bool ShouldLogRetry(");
        var pass = confirm.IndexOf("HomePorts.OnArrived(ride.PlayerId, GetRideRoute(ride));", StringComparison.Ordinal);
        pass.Should().BeGreaterThanOrEqualTo(0);
        confirm.IndexOf("DB.Delete<ShuttleRide>(ride.Id);", StringComparison.Ordinal).Should().BeGreaterThan(pass);
    }

    [Test]
    public void OnlyAPilotWhoseShipIsInSpace_FliesAShip_ForDockingBoardingAndSalvage()
    {
        var flown = Between(ReadSource("Service", "SpaceDocking.cs"), "private static bool TryGetFlownShip(", "public static List<PlayerShip> GetDockedShips(");
        flown.Should().Contain("|| !IsShipInSpace(creature))", "a crew member keeps their own ship as active while it waits elsewhere");
    }

    [Test]
    public void AHuntThatRanOutWhileTheServerWasDown_GetsItsTimeAgain_InsteadOfEndingWithNoOutcome()
    {
        var hunts = ReadSource("Service", "LivestockHunts.cs");
        var start = hunts[hunts.IndexOf("public static void ProcessAtServerStart()", StringComparison.Ordinal)..];
        start.Should().Contain("hunt.EndsUtc = LivestockRules.GetHuntEndUtc(now);")
            .And.Contain("Begin(hunt, city);")
            .And.NotContain("The predators moved on", "calling a hunt before a restart must not dodge both the fight and the penalty");
    }

    [Test]
    public void TheWeeklyCityCheck_BillsTheWeekInOneSave_AndChargesEachCitizenOnce()
    {
        var property = ReadSource("Service", "Property.cs");
        var cities = Between(property, "private static void ProcessCities()", "private static void ProcessCityCitizenRequirement(");
        var level = cities.IndexOf("var levelChanged = ProcessCityLevel(city);", StringComparison.Ordinal);
        var upkeep = cities.IndexOf("ProcessUpkeep(now, city);", StringComparison.Ordinal);
        var date = cities.IndexOf("city.Dates[PropertyDateType.Upkeep] = week.AddDays(7);", StringComparison.Ordinal);
        var journal = cities.IndexOf("city.Dates[PropertyDateType.CitizenshipFeesDue] = week;", StringComparison.Ordinal);
        var save = cities.IndexOf("DB.Set(city);", journal, StringComparison.Ordinal);
        var fees = cities.LastIndexOf("ProcessCitizenshipFees(city);", StringComparison.Ordinal);
        level.Should().BeGreaterThanOrEqualTo(0);
        new[] { level, upkeep, date, journal, save, fees }.Should().BeInAscendingOrder("level, upkeep and next date are saved together, then the fees");
        cities.IndexOf("ProcessCitizenshipFees(city);", StringComparison.Ordinal).Should().BeLessThan(level, "an interrupted week's fees are finished first");

        Between(property, "private static bool ProcessCityLevel(", "private static void OnCityLevelSaved(").Should().NotContain("DB.Set(city)");
        Between(property, "private static void ProcessUpkeep(", "private static void ProcessCitizenshipFees(").Should().NotContain("DB.Set(city)");
        var feesBody = Between(property, "private static void ProcessCitizenshipFees(", "public static void DeleteProperty(");
        feesBody.Should().Contain("if (citizen.CitizenshipFeeChargedFor == charge)")
            .And.Contain("citizen.CitizenshipFeeChargedFor = charge;")
            .And.Contain("city.Dates.Remove(PropertyDateType.CitizenshipFeesDue);");

        var week = new DateTime(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc);
        SWLOR.Game.Server.Service.PropertyService.CityUpkeepRules.GetCitizenshipFeeMark("city-a", week)
            .Should().NotBe(SWLOR.Game.Server.Service.PropertyService.CityUpkeepRules.GetCitizenshipFeeMark("city-a", week.AddDays(7)))
            .And.NotBe(SWLOR.Game.Server.Service.PropertyService.CityUpkeepRules.GetCitizenshipFeeMark("city-b", week));
    }

    [Test]
    public void TheCityChecks_SurviveAMissingMayorRecord_AndARepeatedPermissionTransfer()
    {
        var property = ReadSource("Service", "Property.cs");
        Between(property, "private static bool ProcessCityLevel(", "private static void OnCityLevelSaved(").Should().Contain("mayor != null &&");
        Between(property, "private static void ProcessUpkeep(", "private static void ProcessCitizenshipFees(").Should().Contain("dbMayor != null &&");
        var transfer = Between(property, "void TransferPermissions(string winnerPlayerId)", "CityOfficers.EndAllOnNewMayor(city, winnerPlayerId);");
        transfer.Should().NotContain(".Single();", "a restart after the delete finds the old mayor's permission already gone");
    }

    [Test]
    public void AShipListedForSale_CannotTakeAJumpBeacon()
    {
        var buildings = ReadSource("Service", "StationBuildings.cs");
        Between(buildings, "private static bool IsDockedAt(", "public static List<(WorldProperty Ship, PlayerShip Record)> GetJumpableShips(")
            .Should().Contain("!ShipTransfer.IsListedForSale(record.Id)", "a buyer pays for a ship at the station its listing shows");
        Between(buildings, "public static List<(WorldProperty Ship, PlayerShip Record)> GetJumpableShips(", "return")
            .Should().Contain("IsDockedAt(ship, record, interiors)", "beacon selection offers only ships at the dock");
        buildings.Should().Contain("ShipDocked = ship != null && IsDockedAt(ship, record, GetStarportInteriorIds(origin.Id)),", "the jump itself checks the same");
    }
}
