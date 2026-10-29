using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Capital battle events (plan section 11, encounter types). Every few hours, while no incursion runs, a Sith or
    /// Mandalorian Armada battle group (the incursion command group of the orbit's tier) drops into one open tier 5-6
    /// orbit for a limited time. Its ships drop their normal loot, and destroying the whole group also pays the pilots
    /// who damaged it (W11p, <see cref="CapitalBattleRules.BuildPayout"/>). Held in memory: a restart starts the clock
    /// again and drops an unfinished battle; payouts owed to offline pilots are saved with the incursion record.
    /// </summary>
    public static partial class Space
    {
        private const string CapitalBattleVariable = "CAPITAL_BATTLE";

        private static DateTime _capitalBattleNextAt;
        private static DateTime _capitalBattleEndsAt;
        private static IncursionArea _capitalBattleArea;
        private static IncursionInvader _capitalBattleInvader;
        private static readonly List<uint> _capitalBattleShips = new();
        // The contribution book of the running battle, by persistent player id as an incursion's is, so a pilot who logs out
        // before the group falls is still paid (at the next login). Damage is tracked per ship until it is destroyed.
        private static readonly Dictionary<string, IncursionContribution> _capitalBattleContributions = new();
        private static readonly Dictionary<uint, Dictionary<string, double>> _capitalBattleDamage = new();
        private static readonly Dictionary<uint, string> _capitalBattleLastHit = new();
        private static readonly Dictionary<string, string> _capitalBattlePilotNames = new();

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
                        var paid = PayCapitalBattle();
                        BroadcastIncursion(ColorToken.Green(
                            $"CAPITAL BATTLE: the {name} battle group in {_capitalBattleArea.Name} is destroyed. {CapitalBattleRules.DescribeSpoilsShare(paid)}"));
                        EndCapitalBattle(now);
                        break;
                    case CapitalBattleStep.Withdraw:
                        BroadcastIncursion(ColorToken.Orange(
                            $"CAPITAL BATTLE: the {name} battle group withdraws from {_capitalBattleArea.Name}. No spoils are paid."));
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

        /// <summary>
        /// Starts a capital battle now, from the DM tools: in the picked orbit, or a random one when
        /// <paramref name="areaResref"/> is empty. Refused while a battle or an incursion runs. The schedule is left as it is.
        /// </summary>
        public static (bool Started, string Message) StartCapitalBattleAt(string areaResref)
        {
            if (_capitalBattleArea != null)
                return (false, $"A capital battle is already under way in {_capitalBattleArea.Name}.");

            var incursion = GetActiveIncursion();
            if (incursion != null)
                return (false, $"An incursion is running in the {IncursionRules.FindRegion(incursion.Center)?.Name} region. A capital battle waits for it to end.");

            var area = string.IsNullOrWhiteSpace(areaResref)
                ? CapitalBattleRules.PickArea(max => Random.Next(max))
                : IncursionTuning.CapitalBattleAreas.FirstOrDefault(x => x.Resref == areaResref);
            if (area == null)
                return (false, "That orbit does not host capital battles.");

            var invader = IncursionRules.PickInvader(max => Random.Next(max));
            if (!StartCapitalBattle(area, invader, DateTime.UtcNow))
                return (false, "The capital battle could not be started.");

            return (true, $"Started a {IncursionRules.GetInvader(invader)?.Name} capital battle in {area.Name}.");
        }

        private static bool StartCapitalBattle(IncursionArea area, IncursionInvader invader, DateTime now)
        {
            var areaObject = Area.GetAreaByResref(area.Resref);
            if (!GetIsObjectValid(areaObject))
                return false;

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
                return false;

            _capitalBattleArea = area;
            _capitalBattleInvader = invader;
            _capitalBattleEndsAt = now.AddMinutes(IncursionTuning.CapitalBattleMinutes);
            var name = IncursionRules.GetInvader(invader)?.Name;
            BroadcastIncursion(ColorToken.Red(
                $"CAPITAL BATTLE: a {name} battle group has dropped out of hyperspace in {area.Name}. It withdraws in {IncursionTuning.CapitalBattleMinutes:0} minutes."));
            Log.Write(LogGroup.Space, $"CAPITAL BATTLE - {invader} in {area.Resref}");
            return true;
        }

        /// <summary>A destroyed battle ship's points go to the ships that damaged it, shared by the players aboard each.</summary>
        private static void CreditCapitalBattleKill(uint ship)
        {
            _capitalBattleDamage.Remove(ship, out var damage);
            _capitalBattleLastHit.Remove(ship, out var killer);
            if (damage == null || _capitalBattleArea == null)
                return;

            var points = IncursionRules.KillPoints(_npcPresets.TryGetValue(GetTag(ship), out var preset) ? preset.Difficulty : "Normal");
            IncursionRules.CreditKill(_capitalBattleContributions, _capitalBattleArea.Tier, damage, points, killer,
                pilotId => GetPilotCrew(pilotId, _capitalBattlePilotNames));
        }

        /// <summary>
        /// Pays every contributor of a destroyed battle group: at once to a pilot online, or kept in the incursion record
        /// for the next login. Returns how many pilots were paid.
        /// </summary>
        private static int PayCapitalBattle()
        {
            var state = IncursionRecord;
            state.PendingBattlePayouts ??= new Dictionary<string, IncursionPayout>();
            var online = GetOnlinePlayersById();
            var paid = 0;
            var pending = false;
            foreach (var (playerId, contribution) in _capitalBattleContributions)
            {
                var payout = CapitalBattleRules.BuildPayout(contribution, _capitalBattleArea);
                if (payout.Points <= 0)
                    continue;

                paid++;
                if (online.TryGetValue(playerId, out var player))
                {
                    DeliverPayout(player, payout, "Capital battle spoils at {0}", "CAPITAL BATTLE PAYOUT");
                }
                else
                {
                    CapitalBattleRules.AddPending(state.PendingBattlePayouts, playerId, payout);
                    pending = true;
                }
            }

            if (pending)
                SaveIncursions();

            Log.Write(LogGroup.Space, $"CAPITAL BATTLE DESTROYED - {_capitalBattleInvader} in {_capitalBattleArea.Resref}, {paid} contributors paid");
            return paid;
        }

        /// <summary>A pilot who was offline when a capital battle group fell collects the spoils at login.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void DeliverPendingCapitalBattlePayout()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            if (IncursionRecord.PendingBattlePayouts == null || !IncursionRecord.PendingBattlePayouts.ContainsKey(playerId))
                return;

            // The payout stays pending until delivery: a player who disconnects within the delay collects it at the next login.
            DelayCommand(6f, () =>
            {
                var payout = IncursionRules.ClaimPending(IncursionRecord.PendingBattlePayouts, playerId, GetIsObjectValid(player));
                if (payout == null)
                    return;

                SaveIncursions();
                DeliverPayout(player, payout, "Capital battle spoils at {0}", "CAPITAL BATTLE PAYOUT");
            });
        }

        private static void EndCapitalBattle(DateTime now)
        {
            foreach (var ship in _capitalBattleShips.Where(GetIsObjectValid).ToList())
                DestroyObject(ship);

            _capitalBattleShips.Clear();
            _capitalBattleContributions.Clear();
            _capitalBattleDamage.Clear();
            _capitalBattleLastHit.Clear();
            _capitalBattlePilotNames.Clear();
            _capitalBattleArea = null;
            _capitalBattleNextAt = CapitalBattleRules.NextStart(now);
        }
    }
}
