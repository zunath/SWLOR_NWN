using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// A destroyed ship's cargo (plan section 13): the whole bay spills into cargo pods anyone can scoop for five
    /// minutes. A pod holds commodity units, not items; scooping moves what fits into the scooping ship's cargo bay.
    /// </summary>
    public static class CargoSpillRules
    {
        /// <summary>
        /// Splits the cargo into pods: one commodity per pod, largest lots first. Past the pod limit the remaining
        /// commodities share the pods in turn.
        /// </summary>
        public static List<CargoSpillLot> Split(IReadOnlyDictionary<CommodityType, CargoLot> cargo, int maximumPods)
        {
            var lots = (cargo ?? new Dictionary<CommodityType, CargoLot>())
                .Where(x => x.Value.Units > 0)
                .OrderByDescending(x => x.Value.Units)
                .ThenBy(x => x.Key)
                .ToList();

            var pods = new List<CargoSpillLot>();
            if (lots.Count == 0 || maximumPods <= 0)
                return pods;

            var count = Math.Min(maximumPods, lots.Count);
            for (var pod = 0; pod < count; pod++)
            {
                pods.Add(new CargoSpillLot());
            }

            for (var index = 0; index < lots.Count; index++)
            {
                pods[index % count].Units[lots[index].Key] = lots[index].Value.Units;
            }

            return pods;
        }

        /// <summary>
        /// What a ship with this much free volume scoops from a pod, largest lots first. The pod keeps the rest.
        /// </summary>
        public static Dictionary<CommodityType, int> Scoop(Dictionary<CommodityType, int> pod, int freeVolume)
        {
            var taken = new Dictionary<CommodityType, int>();
            foreach (var (commodity, units) in pod.OrderByDescending(x => x.Value).ThenBy(x => x.Key).ToList())
            {
                var fits = Math.Min(units, CargoHoldRules.GetUnitsThatFit(freeVolume, commodity));
                if (fits <= 0)
                    continue;

                taken[commodity] = fits;
                freeVolume -= fits * TradeLookup.GetVolume(commodity);
                if (fits >= units)
                    pod.Remove(commodity);
                else
                    pod[commodity] = units - fits;
            }

            return taken;
        }

        /// <summary>A pod's name: "Cargo Pod: 40 Grain, 12 Seafood".</summary>
        public static string GetPodName(IReadOnlyDictionary<CommodityType, int> pod)
        {
            var contents = pod
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key)
                .Select(x => $"{x.Value} {TradeLookup.GetCommodity(x.Key)?.Name ?? x.Key.ToString()}");
            return $"Cargo Pod: {string.Join(", ", contents)}";
        }
    }
}
