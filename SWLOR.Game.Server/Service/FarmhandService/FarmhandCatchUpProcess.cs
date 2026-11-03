using System.Collections.Generic;
using Newtonsoft.Json;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>
    /// The steps of a farmhand's catch-up, in the order that makes a stop safe. A replay changes several records: the animals and plots
    /// get their new timers, and the products the droid collected for them exist only in the farmhand's output hopper. Saving the
    /// animals and plots first and the farmhand last would lose the products when the server stopped between, and saving the farmhand
    /// first would collect them again. So the replay runs on copies, the full new state of everything it changed is saved as one
    /// pending record (<see cref="FarmhandCatchUp"/>), each record is written whole (the animals and plots, the removals, and the
    /// farmhand last), and the pending record is deleted. A stop at any point leaves the pending record, and the next catch-up or
    /// server start (<see cref="Recover"/>) writes every record in it again: whole records, so doing it twice changes nothing.
    /// </summary>
    public static class FarmhandCatchUpProcess
    {
        /// <summary>
        /// Brings the farmhand up to date at <paramref name="now"/>, after finishing a catch-up that a stop left. Does nothing where no
        /// droid works, and nothing new when the farmhand was just processed.
        /// </summary>
        /// <returns>What the replay did, with the records as they were saved, or null when it ran nothing.</returns>
        public static FarmhandReplayResult CatchUp(
            IFarmhandCatchUpStore store,
            string targetId,
            DateTime now,
            Func<LivestockType, LivestockDefinition> getAnimal,
            Func<CropType, CropDefinition> getCrop)
        {
            Recover(store, targetId);

            var stored = store.GetFarmhand(targetId);
            if (stored == null || now <= stored.LastProcessedUtc)
                return null;

            // The replay runs on copies, so a stored record is never changed before it is written: a write that fails leaves the stored
            // records as they were, for the pending record to finish.
            var farmhand = Copy(stored);
            var animals = farmhand.TargetType == FarmhandTargetType.Corral ? Copy(store.GetAnimals(targetId)) : new List<Livestock>();
            var plots = farmhand.TargetType == FarmhandTargetType.House ? Copy(store.GetPlots(targetId, farmhand.OwnerPlayerId)) : new List<GardenPlot>();
            var result = FarmhandRules.Replay(farmhand, animals, plots, getAnimal, getCrop, now);
            if (!result.Ran)
                return null;

            var pending = FarmhandCatchUpRules.Build(farmhand, stored.LastProcessedUtc, result);
            store.SavePending(pending);
            Apply(store, pending);
            return result;
        }

        /// <summary>
        /// Finishes the catch-up a stop left for the house or corral, if there is one: every record in it is written again, farmhand last,
        /// and it is deleted. A catch-up the farmhand already holds, or that belongs to a farmhand that is gone, is only deleted.
        /// </summary>
        /// <returns>True when records were written.</returns>
        public static bool Recover(IFarmhandCatchUpStore store, string targetId)
        {
            var pending = store.GetPending(targetId);
            if (pending == null)
                return false;

            if (FarmhandCatchUpRules.Settle(pending, store.GetFarmhand(targetId)) != FarmhandCatchUpSettlement.Apply)
            {
                store.DeletePending(targetId);
                return false;
            }

            Apply(store, pending);
            return true;
        }

        /// <summary>
        /// Writes the records of a pending catch-up whole, the farmhand after all the others, and deletes the pending record. An animal
        /// or crop that was removed since the replay read it stays removed.
        /// </summary>
        private static void Apply(IFarmhandCatchUpStore store, FarmhandCatchUp pending)
        {
            foreach (var animal in pending.Animals)
            {
                if (store.AnimalExists(animal.Id))
                    store.SetAnimal(animal);
            }

            foreach (var plot in pending.Plots)
            {
                if (store.PlotExists(plot.Id))
                    store.SetPlot(plot);
            }

            foreach (var animal in pending.WanderedOff)
            {
                if (store.AnimalExists(animal.Id))
                    store.RemoveWanderedOff(animal);
            }

            foreach (var plot in pending.Withered)
            {
                store.RemoveWithered(plot, pending.UntilUtc);
            }

            store.SetFarmhand(pending.Farmhand);
            store.DeletePending(pending.TargetId);
        }

        private static T Copy<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));
    }
}
