using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The records of a clerk collection, butchering or crating: the mark lives on the animal's <see cref="Livestock"/> record. A
    /// butchered or crated animal is deleted with its mark; a collected-from animal records the collection at the time the clerk took
    /// it and clears the mark. The steps and their order are <see cref="ClerkRecoveryProcess"/>'s.
    /// </summary>
    internal sealed class AnimalRecoveryStore : IClerkRecoveryStore
    {
        private readonly string _animalId;

        public AnimalRecoveryStore(string animalId)
        {
            _animalId = animalId;
        }

        public ClerkRecoveryMark ReadMark() => DB.Get<Livestock>(_animalId)?.Recovery;

        public void WriteMark(ClerkRecoveryMark mark)
        {
            var animal = DB.Get<Livestock>(_animalId) ?? throw new InvalidOperationException($"Animal '{_animalId}' is gone, so its action cannot be marked.");
            animal.Recovery = mark;
            DB.Set(animal);
        }

        public void WriteOverflowItems(string playerId, string recordPrefix, string resref, int count) =>
            ClerkRecovery.WriteOverflowItems(playerId, recordPrefix, resref, count);

        public void WriteOverflowCrate(string playerId, string recordPrefix, LivestockCrate crate) =>
            ClerkRecovery.WriteOverflowCrate(playerId, recordPrefix, crate);

        public void FinishSource(ClerkRecoveryMark mark)
        {
            var animal = DB.Get<Livestock>(_animalId);
            if (animal == null)
                return;

            if (mark.Action != ClerkRecoveryAction.Collect)
            {
                Corrals.DeleteAnimal(_animalId);
                Corrals.DestroyCreature(_animalId);
                return;
            }

            LivestockRules.ApplyCollection(animal, mark.AtUtc);
            if (animal.LastProcessedUtc < mark.AtUtc)
                animal.LastProcessedUtc = mark.AtUtc;
            animal.Recovery = null;
            DB.Set(animal);
        }
    }
}
