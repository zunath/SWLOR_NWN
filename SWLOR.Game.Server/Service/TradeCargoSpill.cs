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

        /// <summary>Spills the ship's whole cargo at the wreck and empties its bay. Called by Space.ApplyDeath.</summary>
        public static void SpillCargo(PlayerShip ship, Location location)
        {
            if (ship?.Cargo == null || ship.Cargo.Count == 0)
                return;

            var pods = CargoSpillRules.Split(ship.Cargo, TradeTuning.SpillMaximumContainers);
            ship.Cargo.Clear();

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
                    continue;

                WritePod(pod, pods[index].Units);
                SetEventScript(pod, EventScript.Placeable_OnUsed, ScriptName.OnCargoPodUsed);
                AssignCommand(pod, () => DelayCommand(TradeTuning.SpillSeconds, () => DestroyObject(pod)));
            }

            Log.Write(LogGroup.Space, $"CARGO SPILL - ship {ship.Id} spilled {pods.Sum(x => x.Units.Values.Sum())} units into {pods.Count} pods.");
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
            var taken = CargoSpillRules.Scoop(contents, CargoHoldRules.GetFreeVolume(capacity, ship.Cargo));
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
            foreach (var commodity in Enum.GetValues<CommodityType>())
            {
                if (units.TryGetValue(commodity, out var count) && count > 0)
                    SetLocalInt(pod, PodUnitsVariablePrefix + commodity, count);
                else
                    DeleteLocalInt(pod, PodUnitsVariablePrefix + commodity);
            }

            SetName(pod, CargoSpillRules.GetPodName(units));
        }

        private static Dictionary<CommodityType, int> ReadPod(uint pod)
        {
            var units = new Dictionary<CommodityType, int>();
            foreach (var commodity in Enum.GetValues<CommodityType>())
            {
                var count = GetLocalInt(pod, PodUnitsVariablePrefix + commodity);
                if (count > 0)
                    units[commodity] = count;
            }

            return units;
        }
    }
}
