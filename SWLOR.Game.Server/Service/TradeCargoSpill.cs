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
    /// A destroyed ship's cargo spills into cargo pods anyone can scoop for five minutes, in PvE and PvP (plan section
    /// 13). A pod is a placeable whose locals hold commodity units; a pilot uses it to scoop what fits.
    /// </summary>
    public static partial class Trade
    {
        /// <summary>The cargo pod blueprint.</summary>
        public const string CargoPodResref = "spc_cargo_pod";

        private const string PodUnitsVariablePrefix = "CARGO_POD_UNITS_";
        private const float PodSpacing = 4f;

        /// <summary>
        /// Spills the ship's whole cargo at the wreck and empties its bay; sealed freight is lost with the ship and its
        /// contracts fail. Called by Space.ApplyDeath.
        /// </summary>
        public static void SpillCargo(PlayerShip ship, Location location)
        {
            if (ship == null)
                return;

            FailSealedFreight(ship);
            if (ship.Cargo == null || ship.Cargo.Count == 0)
                return;

            var lots = ship.Cargo.ToDictionary(x => x.Key, x => x.Value);
            ship.Cargo.Clear();
            var kept = new Dictionary<CommodityType, int>();
            var spilled = SpillUnits(lots.ToDictionary(x => x.Key, x => x.Value.Units), location, kept);

            // Units no pod could be created for stay in the bay, at their share of the lot's cost, rather than being lost.
            foreach (var (commodity, units) in kept)
            {
                var lot = lots[commodity];
                var cost = lot.Units <= 0 ? 0 : (int)((long)lot.CostBasis * units / lot.Units);
                CargoHoldRules.AddUnits(ship.Cargo, commodity, units, cost);
            }

            Log.Write(LogGroup.Space, $"CARGO SPILL - ship {ship.Id} spilled {spilled} units" +
                                      (kept.Count > 0 ? $", kept {kept.Values.Sum()} units no pod could hold." : "."));
        }

        /// <summary>
        /// Spills commodity units into cargo pods around a location and returns the units spilled. Units whose pod could
        /// not be created are added to <paramref name="kept"/>, when given.
        /// </summary>
        public static int SpillUnits(IReadOnlyDictionary<CommodityType, int> units, Location location, Dictionary<CommodityType, int> kept = null)
        {
            var cargo = units
                .Where(x => x.Value > 0)
                .ToDictionary(x => x.Key, x => new CargoLot { Units = x.Value });
            if (cargo.Count == 0)
                return 0;

            var pods = CargoSpillRules.Split(cargo, TradeTuning.SpillMaximumContainers);
            var area = GetAreaFromLocation(location);
            var center = GetPositionFromLocation(location);
            for (var index = 0; index < pods.Count; index++)
            {
                var angle = 360f / pods.Count * index;
                var position = Vector3(
                    center.X + PodSpacing * (float)Math.Cos(angle * Math.PI / 180),
                    center.Y + PodSpacing * (float)Math.Sin(angle * Math.PI / 180),
                    center.Z);
                var pod = CreateObject(ObjectType.Placeable, CargoPodResref, Location(area, position, angle));
                if (!GetIsObjectValid(pod))
                {
                    if (kept != null)
                    {
                        foreach (var (commodity, count) in pods[index].Units)
                        {
                            kept.TryGetValue(commodity, out var current);
                            kept[commodity] = current + count;
                        }
                    }

                    continue;
                }

                WritePod(pod, pods[index].Units);
                SetEventScript(pod, EventScript.Placeable_OnUsed, ScriptName.OnCargoPodUsed);
                AssignCommand(pod, () => DelayCommand(TradeTuning.SpillSeconds, () => DestroyObject(pod)));
            }

            return pods.Sum(x => x.Units.Values.Sum()) - (kept?.Values.Sum() ?? 0);
        }

        /// <summary>A pilot uses a cargo pod.</summary>
        [NWNEventHandler(ScriptName.OnCargoPodUsed)]
        public static void ScoopCargoPod()
        {
            ScoopPod(OBJECT_SELF, GetLastUsedBy());
        }

        /// <summary>The units a cargo pod holds, by commodity.</summary>
        public static Dictionary<CommodityType, int> GetPodContents(uint pod) => ReadPod(pod);

        /// <summary>What fits goes into the user's ship's cargo bay, and the pod keeps the rest.</summary>
        public static void ScoopPod(uint pod, uint user)
        {
            if (!GetIsPC(user) || !GetIsObjectValid(pod))
                return;

            if (!Space.IsPlayerInSpaceMode(user))
            {
                SendMessageToPC(user, ColorToken.Red("Only a ship can scoop a cargo pod."));
                return;
            }

            var ship = GetActiveShip(user);
            var capacity = GetCargoCapacity(ship);
            if (ship == null || capacity <= 0)
            {
                SendMessageToPC(user, ColorToken.Red(TradeRules.GetRefusalMessage(TradeRefusal.NoCargoBay)));
                return;
            }

            var contents = ReadPod(pod);
            var taken = CargoSpillRules.Scoop(contents, GetFreeVolume(ship));
            if (taken.Count == 0)
            {
                SendMessageToPC(user, ColorToken.Red(TradeRules.GetRefusalMessage(TradeRefusal.CargoBayFull)));
                return;
            }

            // Scooped goods count at their base price, so selling them gives XP only for a real margin.
            foreach (var (commodity, units) in taken)
            {
                var basePrice = TradeLookup.GetCommodity(commodity)?.BasePrice ?? 0;
                CargoHoldRules.AddUnits(ship.Cargo, commodity, units, basePrice * units);
            }

            DB.Set(ship);
            var text = string.Join(", ", taken.Select(x => $"{x.Value} {TradeLookup.GetCommodity(x.Key)?.Name}"));
            SendMessageToPC(user, ColorToken.Green($"Scooped into the cargo bay: {text}."));

            if (contents.Count == 0)
            {
                DestroyObject(pod);
                return;
            }

            WritePod(pod, contents);
            SendMessageToPC(user, ColorToken.Red("The cargo bay is full. The rest stays in the pod."));
        }

        private static void WritePod(uint pod, IReadOnlyDictionary<CommodityType, int> units)
        {
            WriteUnits(pod, units);
            SetName(pod, CargoSpillRules.GetPodName(units));
        }

        private static Dictionary<CommodityType, int> ReadPod(uint pod) => ReadUnits(pod);

        /// <summary>Stores commodity units on an object's locals (a cargo pod or a convoy hauler).</summary>
        private static void WriteUnits(uint holder, IReadOnlyDictionary<CommodityType, int> units)
        {
            foreach (var commodity in Enum.GetValues<CommodityType>())
            {
                if (units.TryGetValue(commodity, out var count) && count > 0)
                    SetLocalInt(holder, PodUnitsVariablePrefix + commodity, count);
                else
                    DeleteLocalInt(holder, PodUnitsVariablePrefix + commodity);
            }
        }

        private static Dictionary<CommodityType, int> ReadUnits(uint holder)
        {
            var units = new Dictionary<CommodityType, int>();
            foreach (var commodity in Enum.GetValues<CommodityType>())
            {
                var count = GetLocalInt(holder, PodUnitsVariablePrefix + commodity);
                if (count > 0)
                    units[commodity] = count;
            }

            return units;
        }
    }
}
