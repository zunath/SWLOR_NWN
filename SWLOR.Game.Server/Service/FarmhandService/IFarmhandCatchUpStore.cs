using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>
    /// The records a farmhand's catch-up reads and writes. <see cref="FarmhandCatchUpProcess"/> calls them in a fixed order, so the order
    /// can be tested without a database or an engine; <see cref="Farmhands"/> implements them over the real records.
    /// </summary>
    public interface IFarmhandCatchUpStore
    {
        /// <summary>The pending catch-up of the house or corral, or null.</summary>
        FarmhandCatchUp GetPending(string targetId);

        /// <summary>Saves the pending catch-up.</summary>
        void SavePending(FarmhandCatchUp pending);

        /// <summary>Deletes the pending catch-up. Deleting one that is gone changes nothing.</summary>
        void DeletePending(string targetId);

        /// <summary>The farmhand of the house or corral, or null.</summary>
        Farmhand GetFarmhand(string targetId);

        /// <summary>The animals standing in the corral.</summary>
        List<Livestock> GetAnimals(string corralId);

        /// <summary>The planter's crops in the house.</summary>
        List<GardenPlot> GetPlots(string houseId, string planterId);

        /// <summary>Whether the animal's record is still there. A catch-up never brings back an animal that was removed since.</summary>
        bool AnimalExists(string animalId);

        /// <summary>Whether the crop's record is still there. A catch-up never brings back a crop that was removed since.</summary>
        bool PlotExists(string plotRecordId);

        /// <summary>Saves the whole animal record.</summary>
        void SetAnimal(Livestock animal);

        /// <summary>Saves the whole crop record.</summary>
        void SetPlot(GardenPlot plot);

        /// <summary>Removes an animal that went unfed for 72 hours, and tells its owner.</summary>
        void RemoveWanderedOff(Livestock animal);

        /// <summary>Clears a crop that was left ripe for 72 hours, at the time the replay ran to. Clearing one that is gone changes nothing.</summary>
        void RemoveWithered(GardenPlot plot, DateTime untilUtc);

        /// <summary>Saves the whole farmhand record.</summary>
        void SetFarmhand(Farmhand farmhand);
    }
}
