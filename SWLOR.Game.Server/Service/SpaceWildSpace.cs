using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Wild Space (plan section 23): six tier 5.5 systems, each with an environment that changes the stats of every
    /// ship inside it while it stays there, and the Rift Raiders, whose wrecks spill stolen cargo. The systems are
    /// reached only through rifts (SpaceRifts).
    /// </summary>
    public static partial class Space
    {
        /// <summary>The temporary stat modifier group the environment's changes live in on a ship.</summary>
        private const string WildSpaceEnvironmentGroup = "WILD_SPACE_ENVIRONMENT";

        /// <summary>A player's environment is applied this long after arriving, once the launch or jump has finished.</summary>
        private const float WildSpaceEnvironmentDelaySeconds = 1f;

        /// <summary>The environment of an area, or Invalid when it is not a Wild Space system.</summary>
        public static WildSpaceEnvironment GetWildSpaceEnvironment(uint area)
        {
            return IsWildSpace(area)
                ? WildSpaceRules.ParseEnvironment(GetLocalInt(area, WildSpaceTuning.EnvironmentVariable))
                : WildSpaceEnvironment.Invalid;
        }

        /// <summary>A ship entering a Wild Space system takes its environment.</summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ApplyWildSpaceEnvironmentOnEnter()
        {
            var area = OBJECT_SELF;
            var ship = GetEnteringObject();
            if (GetObjectType(ship) != ObjectType.Creature || GetIsDM(ship) || GetIsDMPossessed(ship))
                return;

            var environment = GetWildSpaceEnvironment(area);
            if (environment == WildSpaceEnvironment.Invalid)
                return;

            if (!GetIsPC(ship))
            {
                ApplyWildSpaceEnvironment(ship, environment, false);
                return;
            }

            // A pilot's hull is known once the launch or jump has finished.
            DelayCommand(WildSpaceEnvironmentDelaySeconds, () =>
            {
                if (GetIsObjectValid(ship) && GetArea(ship) == area)
                    ApplyWildSpaceEnvironment(ship, environment, true);
            });
        }

        /// <summary>A ship leaving a Wild Space system loses its environment.</summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void RemoveWildSpaceEnvironmentOnExit()
        {
            var ship = GetExitingObject();
            if (GetObjectType(ship) == ObjectType.Creature && IsWildSpace(OBJECT_SELF))
                RemoveWildSpaceEnvironment(ship);
        }

        /// <summary>
        /// Puts an environment's stat changes on a ship (replacing any it had) and tells a pilot what the system does. The
        /// Raider Prowler takes only the helpful changes.
        /// </summary>
        public static void ApplyWildSpaceEnvironment(uint ship, WildSpaceEnvironment environment, bool notify)
        {
            RemoveWildSpaceEnvironment(ship);
            var ignorePenalties = IsProwlerShip(ship);
            var effects = WildSpaceRules.Effects(environment, ignorePenalties);
            foreach (var effect in effects)
                TemporaryStatModifier.Add(ship, effect.Stat, effect.Amount, WildSpaceTuning.EnvironmentSeconds, WildSpaceEnvironmentGroup);

            if (effects.Any(x => x.Stat == StatType.MovementSpeedPercentAdjustment))
                Stat.ApplyCreatureMovementRate(ship);

            if (notify && GetIsPC(ship))
            {
                var system = WildSpaceRules.GetSystem(GetResRef(GetArea(ship)));
                SendMessageToPC(ship, ColorToken.Cyan(WildSpaceRules.EntryMessage(system?.Name ?? GetName(GetArea(ship)), environment, ignorePenalties)));
            }
        }

        /// <summary>Removes every environment stat change from a ship.</summary>
        public static void RemoveWildSpaceEnvironment(uint ship)
        {
            var hadSpeed = TemporaryStatModifier.GetStatAdjustment(ship, StatType.MovementSpeedPercentAdjustment, WildSpaceEnvironmentGroup) != 0;
            var stats = WildSpaceTuning.Effects.Values.SelectMany(x => x).Select(x => x.Stat).Distinct();
            foreach (var stat in stats)
                TemporaryStatModifier.Consume(ship, stat, WildSpaceEnvironmentGroup);

            if (hadSpeed)
                Stat.ApplyCreatureMovementRate(ship);
        }

        /// <summary>Whether a player flies the Raider Prowler, which ignores environment penalties.</summary>
        private static bool IsProwlerShip(uint ship)
        {
            if (!GetIsPC(ship))
                return false;

            var status = GetShipStatus(ship);
            return status != null && WildSpaceRules.IsProwler(FindShipDetail(status.ItemTag)?.Hull?.Tag);
        }

        /// <summary>A destroyed Rift Raider sometimes spills stolen cargo (plan section 13) beside its wreck.</summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void SpillRiftRaiderCargo()
        {
            var creature = OBJECT_SELF;
            if (GetIsPC(creature) || !WildSpaceRules.IsRiftRaider(GetTag(creature)) || GetLocalBool(creature, NoWreckVariable))
                return;

            var cargo = WildSpaceRules.RollStolenCargo(max => Random.Next(max));
            if (cargo.Count == 0)
                return;

            var kept = new Dictionary<CommodityType, int>();
            var spilled = Trade.SpillUnits(cargo, GetLocation(creature), kept);
            Log.Write(LogGroup.Space, $"STOLEN CARGO - {GetTag(creature)} in {GetResRef(GetArea(creature))} spilled {spilled} units ({string.Join(", ", cargo.Select(x => $"{x.Value} {x.Key}"))})" +
                                      (kept.Count > 0 ? $", {kept.Values.Sum()} units had no pod" : string.Empty));
        }
    }
}
