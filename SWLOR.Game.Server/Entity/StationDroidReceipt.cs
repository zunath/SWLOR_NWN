namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The acknowledgement that a station building took a droid's controller (an assignment), keyed by the assignment id
    /// the owner's controller is marked with. It is written once the building saved the droid, and again when the
    /// droid is sent to its owner's overflow store (recalled by a manager, or the building deleted), and it stays until
    /// the owner's next login has settled their marked controllers and saved the character file. A login that finds a
    /// controller still marked as being assigned removes it when the receipt exists, whatever has happened to the
    /// building since. A new type, so no migration.
    /// </summary>
    public class StationDroidReceipt : EntityBase
    {
        public StationDroidReceipt()
        {
            OwnerPlayerId = string.Empty;
            BuildingId = string.Empty;
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }

        public string BuildingId { get; set; }
    }
}
