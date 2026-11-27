namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Names an Asteroid Drill collection (keyed by its id) that may still be pending on the drill
    /// (<see cref="StationBuildingState.PendingDrillCollections"/>), so the collector's next login finds it. It is saved
    /// before the drill saves the collection as pending and deleted after the drill clears it. A new type, so no migration.
    /// </summary>
    public class DrillCollectionReservation : EntityBase
    {
        public DrillCollectionReservation()
        {
            OwnerPlayerId = string.Empty;
            BuildingId = string.Empty;
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }

        /// <summary>The drill's structure property id.</summary>
        public string BuildingId { get; set; }
    }
}
