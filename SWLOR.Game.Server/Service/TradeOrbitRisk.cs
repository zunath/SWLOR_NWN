using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The risks of flying cargo (plan section 13): when a pilot enters a space system, by launching or jumping a
    /// starlane, a lawful system's customs patrol may scan the ship and pirates may ambush a valuable cargo.
    /// </summary>
    public static partial class Trade
    {
        /// <summary>Pirate raiders appear this far from their prey.</summary>
        private const float AmbushRangeMeters = 35f;

        /// <summary>Lawful ships (customs patrols and convoys) appear this far from the pilot they come for.</summary>
        private const float PatrolRangeMeters = 25f;

        /// <summary>A pilot entering a system rolls its customs scan and then its pirate ambush.</summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void CheckOrbitRisks()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var area = OBJECT_SELF;
            var orbit = TradeLookup.GetOrbit(GetResRef(area));
            if (orbit == null)
                return;

            DelayCommand(TradeTuning.ScanDelaySeconds, () => RollCustomsScan(player, area, orbit));
            DelayCommand(TradeTuning.AmbushDelaySeconds, () => RollPirateAmbush(player, area, orbit));
        }

        /// <summary>Whether the player still flies a ship in this area.</summary>
        private static bool IsFlyingIn(uint player, uint area)
        {
            return GetIsObjectValid(player) && GetArea(player) == area && Space.IsPlayerInSpaceMode(player);
        }

        /// <summary>The module tags fitted to a ship, in every row.</summary>
        private static IEnumerable<string> GetFittedModuleTags(PlayerShip ship)
        {
            var status = ship?.Status;
            if (status == null)
                return Enumerable.Empty<string>();

            return status.HighPowerModules.Values
                .Concat(status.MidPowerModules.Values)
                .Concat(status.LowPowerModules.Values)
                .Select(x => x.ItemTag);
        }

        private static void RollPirateAmbush(uint player, uint area, TradeOrbitDetail orbit)
        {
            if (!IsFlyingIn(player, area) || _customsScans.ContainsKey(player))
                return;

            var ship = GetActiveShip(player);
            if (ship == null)
                return;

            var value = PirateAmbushRules.GetCargoValue(ship.Cargo, ship.SealedFreight);
            var chance = PirateAmbushRules.GetAmbushChance(value, orbit.Tier);
            if (chance <= 0 || Random.NextFloat() >= chance)
                return;

            var squad = PirateAmbushRules.GetSquad(orbit.Tier, PirateAmbushRules.GetSquadSize(value, orbit.Tier));
            var raiders = SpawnAround(player, squad, AmbushRangeMeters);
            foreach (var raider in raiders)
            {
                Enmity.ModifyEnmity(player, raider, 100);
                DespawnWhenIdle(raider, TradeTuning.AmbushSeconds);
            }

            SendMessageToPC(player, ColorToken.Red("Pirate raiders drop out of hyperspace, hunting your cargo!"));
            Log.Write(LogGroup.Space, $"PIRATE AMBUSH - {PlayerName.GetAuditName(player)} - {raiders.Count} raiders in {GetResRef(area)} - cargo value {value}");
        }

        /// <summary>Creates ships on a ring around the player, facing them, and returns the ones created.</summary>
        private static List<uint> SpawnAround(uint player, IReadOnlyList<string> tags, float range)
        {
            var created = new List<uint>();
            var area = GetArea(player);
            var center = GetPosition(player);
            var start = Random.NextFloat() * 360f;
            for (var index = 0; index < tags.Count; index++)
            {
                var angle = start + 360f / Math.Max(1, tags.Count) * index;
                var position = Vector3(
                    center.X + range * (float)Math.Cos(angle * Math.PI / 180),
                    center.Y + range * (float)Math.Sin(angle * Math.PI / 180),
                    center.Z);
                var ship = CreateObject(ObjectType.Creature, tags[index], Location(area, position, (angle + 180f) % 360f));
                if (GetIsObjectValid(ship))
                    created.Add(ship);
            }

            return created;
        }

        /// <summary>Removes a ship once nobody is fighting it, checking again every minute.</summary>
        private static void DespawnWhenIdle(uint ship, float seconds)
        {
            AssignCommand(ship, () => DelayCommand(seconds, () =>
            {
                if (!GetIsObjectValid(ship) || GetIsDead(ship))
                    return;

                if (Enmity.GetEnmityTable(ship).Count > 0)
                {
                    DespawnWhenIdle(ship, 60f);
                    return;
                }

                DestroyObject(ship);
            }));
        }
    }
}
