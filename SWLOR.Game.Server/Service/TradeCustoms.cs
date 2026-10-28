using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Customs scans (plan section 13). A patrol of the system's lawful authority hails a ship carrying goods it bans:
    /// the pilot pays the fine and surrenders the goods, or runs and the patrol turns hostile. Either way lawful
    /// standing drops. No answer in time counts as running.
    /// </summary>
    public static partial class Trade
    {
        private static readonly Dictionary<uint, CustomsScan> _customsScans = new();

        /// <summary>The customs scan waiting for this pilot's answer, or null.</summary>
        public static CustomsScan GetCustomsScan(uint player)
        {
            return _customsScans.TryGetValue(player, out var scan) ? scan : null;
        }

        private const float CustomsSpoofCheckSeconds = 10f;

        /// <summary>
        /// Whether a slicer aboard can spoof the scan instead (plan section 24, "Slicing in space"): the pilot or the
        /// Sensors crew member with the Slicing rank for the patrol's tier, out of combat. Success returns a clean
        /// scan; failure, or giving up, plays out as being caught.
        /// </summary>
        public static bool CanSliceCustomsScan(uint player) => string.IsNullOrEmpty(GetCustomsSliceError(player, out _, out _, out _));

        /// <summary>Why the scan cannot be spoofed now, or empty, with the slicer, the patrol ship and the slicing tier.</summary>
        public static string GetCustomsSliceError(uint player, out uint slicer, out uint patrolShip, out int tier)
        {
            slicer = OBJECT_INVALID;
            patrolShip = OBJECT_INVALID;
            tier = 0;
            var scan = GetCustomsScan(player);
            if (scan == null)
                return "The customs patrol has no further business with you.";
            if (scan.IsBeingSliced)
                return "The scan is already being sliced.";

            patrolShip = _lawfulGroups.TryGetValue(scan.PatrolGroup, out var patrol)
                ? patrol.FirstOrDefault(x => GetIsObjectValid(x) && !GetIsDead(x))
                : OBJECT_INVALID;
            if (!GetIsObjectValid(patrolShip))
                return "The customs patrol is out of reach.";

            tier = SpaceSlicingRules.TargetTier(scan.Tier, false);
            slicer = Space.PickSlicer(player, tier);
            return SlicingSession.ValidateStart(slicer, patrolShip, SlicingSourceType.CustomsScan, tier);
        }

        /// <summary>The pilot answers by slicing the scan: whoever slices aboard gets the mini-game at the patrol's tier.</summary>
        public static void SliceCustomsScan(uint player)
        {
            var error = GetCustomsSliceError(player, out var slicer, out var patrolShip, out var tier);
            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(player, ColorToken.Red(error));
                return;
            }

            var scan = GetCustomsScan(player);
            if (!Space.StartSlicing(slicer, patrolShip, SlicingSourceType.CustomsScan, tier))
                return;

            scan.Slicer = slicer;
            Gui.ClosePlayerWindow(player, GuiWindowType.CustomsScan);
            if (slicer != player)
                SendMessageToPC(player, ColorToken.Green($"{GetName(slicer)} slices the patrol's scan from the Sensors station."));
            Log.Write(LogGroup.Space, $"CUSTOMS SPOOF - {PlayerName.GetAuditName(slicer)} - slicing {scan.Authority}'s scan of {PlayerName.GetAuditName(player)} at tier {tier}");
            WatchCustomsSpoof(player, scan.Id, slicer);
        }

        /// <summary>A spoof whose slicing ended without an outcome (the patrol left, the session was lost) counts as caught.</summary>
        private static void WatchCustomsSpoof(uint player, string scanId, uint slicer)
        {
            AssignCommand(GetModule(), () => DelayCommand(CustomsSpoofCheckSeconds, () =>
            {
                if (!_customsScans.TryGetValue(player, out var scan) || scan.Id != scanId || scan.Slicer != slicer)
                    return;

                var session = SlicingSession.Get(slicer);
                if (session != null && session.Source == SlicingSourceType.CustomsScan)
                {
                    WatchCustomsSpoof(player, scanId, slicer);
                    return;
                }

                ResolveCustomsSpoof(slicer, false);
            }));
        }

        /// <summary>
        /// The end of a customs spoof, for the slicer who ran it. Success: a clean scan, no fine, no confiscation and no
        /// standing lost. Failure: caught, with the fine (or what the pilot carries), the confiscation and the standing loss.
        /// </summary>
        public static void ResolveCustomsSpoof(uint slicer, bool success)
        {
            var (player, scan) = _customsScans.FirstOrDefault(x => x.Value.Slicer == slicer);
            if (scan == null)
                return;

            _customsScans.Remove(player);
            var outcome = SpaceSlicingRules.SpoofOutcome(success, scan.Fine, GetGold(player));
            if (outcome.Clean)
            {
                SendMessageToPC(player, ColorToken.Green($"The {CustomsRules.GetAuthorityName(scan.Authority)} patrol's scan comes back clean. It lets you go."));
                if (slicer != player)
                    SendMessageToPC(slicer, ColorToken.Green("The patrol's scan comes back clean."));
                Log.Write(LogGroup.Space, $"CUSTOMS SPOOFED - {PlayerName.GetAuditName(slicer)} - {scan.Authority} scan of {PlayerName.GetAuditName(player)} came back clean");
                DismissLawfulGroup(scan.PatrolGroup, 5f);
                return;
            }

            var fine = outcome.Fine;
            if (fine > 0)
                TakeGoldFromCreature(fine, player, true);

            var ship = GetActiveShip(player);
            var taken = ship == null || !outcome.ConfiscatesGoods
                ? new Dictionary<CommodityType, int>()
                : CustomsRules.Confiscate(ship.Cargo, scan.Authority);
            if (ship != null)
                DB.Set(ship);

            if (outcome.Standing is { } standing)
                RecordStanding(player, scan.Authority, standing);
            var message = taken.Count > 0
                ? $"The spoof is detected. The patrol fines you {fine} credits, confiscates {CustomsRules.Describe(taken)} and lets you go."
                : $"The spoof is detected. The patrol fines you {fine} credits and lets you go.";
            SendMessageToPC(player, ColorToken.Red(message));
            if (slicer != player)
                SendMessageToPC(slicer, ColorToken.Red("The spoof is detected."));
            Log.Write(LogGroup.Space, $"CUSTOMS SPOOF FAILED - {PlayerName.GetAuditName(slicer)} - {scan.Authority} fined {PlayerName.GetAuditName(player)} {fine}, confiscated {CustomsRules.Describe(taken)}");
            DismissLawfulGroup(scan.PatrolGroup, 5f);
        }

        private static void RollCustomsScan(uint player, uint area, TradeOrbitDetail orbit)
        {
            if (!IsFlyingIn(player, area) || _customsScans.ContainsKey(player))
                return;

            var ship = GetActiveShip(player);
            var authority = CustomsRules.PickPatrol(orbit, Random.NextFloat());
            var bannedValue = CustomsRules.GetBannedValue(ship?.Cargo, authority);
            if (ship == null || bannedValue <= 0)
                return;

            // A pilot the authority is hostile to has already been met by its patrol (RollHostilePatrols); standing with it
            // otherwise cuts the chance of a scan.
            var tier = GetStandingTier(player, TradeStandingRules.GetFaction(authority));
            if (tier == TradeStandingTier.Hostile)
                return;

            var chance = CustomsRules.GetScanChance(
                bannedValue,
                CustomsRules.GetCompartmentPercent(GetFittedModuleTags(ship)),
                TradeStandingRules.GetScanCutPercent(tier));
            if (Random.NextFloat() >= chance)
                return;

            PruneLawfulGroups();
            var group = Guid.NewGuid().ToString();
            var patrol = SpawnAround(player, CustomsRules.GetPatrolTags(authority, orbit.Tier), PatrolRangeMeters);
            foreach (var patrolShip in patrol)
            {
                MarkLawful(patrolShip, group, authority, LawfulShipKind.CustomsPatrol);
                AssignCommand(patrolShip, () => ActionMoveToObject(player, true, 10f));
                DespawnWhenIdle(patrolShip, TradeTuning.PatrolSeconds);
            }

            _lawfulGroups[group] = patrol;
            var scan = new CustomsScan
            {
                Id = group,
                Area = area,
                Authority = authority,
                PatrolGroup = group,
                Goods = CustomsRules.GetBannedUnits(ship.Cargo, authority),
                Fine = CustomsRules.GetFine(bannedValue),
                AnswerBy = DateTime.UtcNow.AddSeconds(TradeTuning.CustomsAnswerSeconds),
                Tier = orbit.Tier,
            };
            _customsScans[player] = scan;

            SendMessageToPC(player, ColorToken.Red(
                $"A {CustomsRules.GetAuthorityName(authority)} customs patrol scans your ship and finds {CustomsRules.Describe(scan.Goods)}. " +
                $"Pay the fine of {scan.Fine} credits and surrender the goods, or run."));
            Log.Write(LogGroup.Space, $"CUSTOMS SCAN - {PlayerName.GetAuditName(player)} - {authority} in {GetResRef(area)} found {CustomsRules.Describe(scan.Goods)} - fine {scan.Fine}");

            if (Gui.IsWindowOpen(player, GuiWindowType.CustomsScan))
                Gui.ClosePlayerWindow(player, GuiWindowType.CustomsScan);
            Gui.TogglePlayerWindow(player, GuiWindowType.CustomsScan, new CustomsScanPayload(scan.Id));

            AssignCommand(GetModule(), () => DelayCommand(TradeTuning.CustomsAnswerSeconds, () =>
            {
                if (_customsScans.TryGetValue(player, out var pending) && pending.Id == scan.Id && !pending.IsBeingSliced)
                    RunFromCustoms(player, true);
            }));
        }

        /// <summary>The pilot pays the fine and the patrol confiscates the goods it bans.</summary>
        public static void PayCustomsFine(uint player)
        {
            var scan = GetCustomsScan(player);
            if (scan == null || scan.IsBeingSliced)
                return;

            if (GetGold(player) < scan.Fine)
            {
                SendMessageToPC(player, ColorToken.Red($"You need {scan.Fine} credits to pay the fine."));
                return;
            }

            _customsScans.Remove(player);
            TakeGoldFromCreature(scan.Fine, player, true);

            var ship = GetActiveShip(player);
            var taken = ship == null ? new Dictionary<CommodityType, int>() : CustomsRules.Confiscate(ship.Cargo, scan.Authority);
            if (ship != null)
                DB.Set(ship);

            RecordStanding(player, scan.Authority, TradeStandingEvent.CaughtSmuggling);
            SendMessageToPC(player, ColorToken.Green(taken.Count > 0
                ? $"You pay {scan.Fine} credits. The patrol confiscates {CustomsRules.Describe(taken)} and lets you go."
                : $"You pay {scan.Fine} credits and the patrol lets you go."));
            Log.Write(LogGroup.Space, $"CUSTOMS FINE - {PlayerName.GetAuditName(player)} - paid {scan.Fine} to {scan.Authority}, confiscated {CustomsRules.Describe(taken)}");

            DismissLawfulGroup(scan.PatrolGroup, 5f);
            Gui.ClosePlayerWindow(player, GuiWindowType.CustomsScan);
        }

        /// <summary>The pilot runs (or did not answer in time): the patrol turns hostile.</summary>
        public static void RunFromCustoms(uint player, bool timedOut)
        {
            var scan = GetCustomsScan(player);
            if (scan == null || scan.IsBeingSliced)
                return;

            _customsScans.Remove(player);
            _lawfulOffenders.Add((scan.PatrolGroup, player));
            TurnLawfulGroupHostile(scan.PatrolGroup, player);
            RecordStanding(player, scan.Authority, TradeStandingEvent.RanFromCustoms);

            SendMessageToPC(player, ColorToken.Red(timedOut
                ? "You did not answer the customs patrol. It moves to stop you!"
                : "You run from the customs patrol. It moves to stop you!"));
            Log.Write(LogGroup.Space, $"CUSTOMS RUN - {PlayerName.GetAuditName(player)} - ran from {scan.Authority}{(timedOut ? " (no answer)" : string.Empty)}");

            Gui.ClosePlayerWindow(player, GuiWindowType.CustomsScan);
        }
    }
}
