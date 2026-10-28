using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Capital battle events (plan section 11, encounter types). Every few hours, while no incursion runs, a Sith or
    /// Mandalorian Armada battle group (the incursion command group of the orbit's tier) drops into one open tier 5-6
    /// orbit for a limited time. Its ships drop their normal loot. Held in memory: a restart starts the clock again.
    /// </summary>
    public static partial class Space
    {
        private const string CapitalBattleVariable = "CAPITAL_BATTLE";

        private static DateTime _capitalBattleNextAt;
        private static DateTime _capitalBattleEndsAt;
        private static IncursionArea _capitalBattleArea;
        private static IncursionInvader _capitalBattleInvader;
        private static readonly List<uint> _capitalBattleShips = new();

        /// <summary>Run from the incursion tick: starts, tracks and ends the capital battle.</summary>
        private static void ProcessCapitalBattle(DateTime now)
        {
            if (_capitalBattleNextAt == default)
                _capitalBattleNextAt = now.AddMinutes(IncursionTuning.CapitalBattleFirstMinutes);

            if (_capitalBattleArea != null)
            {
                _capitalBattleShips.RemoveAll(x => !GetIsObjectValid(x) || GetIsDead(x));
                var name = IncursionRules.GetInvader(_capitalBattleInvader)?.Name;
                switch (CapitalBattleRules.GetStep(_capitalBattleShips.Count, now, _capitalBattleEndsAt))
                {
                    case CapitalBattleStep.Destroyed:
                        BroadcastIncursion(ColorToken.Green($"CAPITAL BATTLE: the {name} battle group in {_capitalBattleArea.Name} is destroyed."));
                        EndCapitalBattle(now);
                        break;
                    case CapitalBattleStep.Withdraw:
                        BroadcastIncursion(ColorToken.Orange($"CAPITAL BATTLE: the {name} battle group withdraws from {_capitalBattleArea.Name}."));
                        EndCapitalBattle(now);
                        break;
                }

                return;
            }

            if (now < _capitalBattleNextAt)
                return;

            _capitalBattleNextAt = CapitalBattleRules.NextStart(now);
            if (GetActiveIncursion() != null)
                return;

            var area = CapitalBattleRules.PickArea(max => Random.Next(max));
            if (area != null)
                StartCapitalBattle(area, IncursionRules.PickInvader(max => Random.Next(max)), now);
        }

        private static void StartCapitalBattle(IncursionArea area, IncursionInvader invader, DateTime now)
        {
            var areaObject = Area.GetAreaByResref(area.Resref);
            if (!GetIsObjectValid(areaObject))
                return;

            var center = GetPositionFromLocation(Walkmesh.GetRandomLocation(areaObject));
            var ships = CapitalBattleRules.Ships(area, invader);
            for (var index = 0; index < ships.Count; index++)
            {
                var (x, y) = SignatureSiteRules.RingOffset(index, ships.Count, IncursionTuning.CapitalBattleRingMeters);
                var ship = CreateObject(ObjectType.Creature, ships[index].Tag,
                    Location(areaObject, Vector3(center.X + x, center.Y + y, center.Z), Random.Next(360)));
                if (!GetIsObjectValid(ship))
                    continue;

                SetLocalInt(ship, CapitalBattleVariable, 1);
                _capitalBattleShips.Add(ship);
            }

            if (_capitalBattleShips.Count == 0)
                return;

            _capitalBattleArea = area;
            _capitalBattleInvader = invader;
            _capitalBattleEndsAt = now.AddMinutes(IncursionTuning.CapitalBattleMinutes);
            var name = IncursionRules.GetInvader(invader)?.Name;
            BroadcastIncursion(ColorToken.Red(
                $"CAPITAL BATTLE: a {name} battle group has dropped out of hyperspace in {area.Name}. It withdraws in {IncursionTuning.CapitalBattleMinutes:0} minutes."));
            Log.Write(LogGroup.Space, $"CAPITAL BATTLE - {invader} in {area.Resref}");
        }

        private static void EndCapitalBattle(DateTime now)
        {
            foreach (var ship in _capitalBattleShips.Where(GetIsObjectValid).ToList())
                DestroyObject(ship);

            _capitalBattleShips.Clear();
            _capitalBattleArea = null;
            _capitalBattleNextAt = CapitalBattleRules.NextStart(now);
        }
    }
}
