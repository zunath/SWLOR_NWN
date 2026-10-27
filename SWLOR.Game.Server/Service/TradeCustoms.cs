using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
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

        /// <summary>
        /// Whether a slicer aboard can spoof the scan instead (plan section 24, "Slicing in space"). Space slicing (W12)
        /// starts the slicing mini-game at the patrol's tier here: success returns a clean scan, failure plays out as
        /// being caught. Until then no one can.
        /// </summary>
        public static bool CanSliceCustomsScan(uint player) => false;

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
                if (_customsScans.TryGetValue(player, out var pending) && pending.Id == scan.Id)
                    RunFromCustoms(player, true);
            }));
        }

        /// <summary>The pilot pays the fine and the patrol confiscates the goods it bans.</summary>
        public static void PayCustomsFine(uint player)
        {
            var scan = GetCustomsScan(player);
            if (scan == null)
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
            if (scan == null)
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
