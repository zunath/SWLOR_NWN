using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Hutt Cartel enforcers on the Smuggler's Moon lanes (plan section 11): spawn-table ships on the Neutral faction
    /// that join their orbit's enforcer group as they spawn. A pilot Hostile to the Hutt Cartel is attacked on sight, when
    /// they enter the orbit and by each enforcer that spawns while they fly there; firing on one turns the group on the
    /// attacker and costs Hutt Cartel standing (<see cref="OnShipAttacked"/>).
    /// </summary>
    public static partial class Trade
    {
        /// <summary>An enforcer joins its orbit's group and turns on the pilots the Cartel hunts there.</summary>
        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        public static void InitializeHuttEnforcerOnSpawn()
        {
            var ship = OBJECT_SELF;
            if (!HuttEnforcerRules.IsEnforcer(GetTag(ship)) || !string.IsNullOrEmpty(GetLocalString(ship, LawfulGroupVariable)))
                return;

            var area = GetArea(ship);
            if (!GetIsObjectValid(area))
                return;

            PruneLawfulGroups();
            var group = HuttEnforcerRules.GetGroup(GetObjectUUID(area));
            MarkLawful(ship, group, TradeAuthority.None, LawfulShipKind.HuttEnforcer);
            if (!_lawfulGroups.TryGetValue(group, out var members))
            {
                members = new List<uint>();
                _lawfulGroups[group] = members;
            }

            members.RemoveAll(x => !GetIsObjectValid(x) || GetIsDead(x));
            members.Add(ship);

            var offenders = _lawfulOffenders.Where(x => x.Group == group).Select(x => x.Player).ToHashSet();
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!IsFlyingIn(player, area))
                    continue;

                if (offenders.Contains(player) ||
                    HuttEnforcerRules.AttacksOnSight(GetStandingTier(player, HuttEnforcerRules.Faction)))
                {
                    _lawfulOffenders.Add((group, player));
                    TurnLawfulShipHostile(ship, player);
                }
            }
        }

        /// <summary>
        /// A pilot Hostile to the Hutt Cartel who enters an orbit the enforcers patrol is attacked on sight by every
        /// enforcer there. Their defence costs no further standing.
        /// </summary>
        private static void RollHuttEnforcers(uint player, uint area)
        {
            if (!IsFlyingIn(player, area) || !HuttEnforcerRules.PatrolsOrbit(GetResRef(area)))
                return;

            if (!HuttEnforcerRules.AttacksOnSight(GetStandingTier(player, HuttEnforcerRules.Faction)))
                return;

            PruneLawfulGroups();
            var group = HuttEnforcerRules.GetGroup(GetObjectUUID(area));
            if (!_lawfulGroups.TryGetValue(group, out var members) || !members.Any(x => GetIsObjectValid(x) && !GetIsDead(x)))
                return;

            _lawfulOffenders.Add((group, player));
            TurnLawfulGroupHostile(group, player);

            SendMessageToPC(player, ColorToken.Red("Hutt Cartel enforcers recognize your ship and move to attack on sight!"));
            Log.Write(LogGroup.Space, $"HOSTILE ENFORCERS - {PlayerName.GetAuditName(player)} - Hutt Cartel enforcers attack in {GetResRef(area)}");
        }
    }
}
