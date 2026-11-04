namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The acknowledgement that a deed was saved as a ship, keyed by the ship id the registering player's deed is marked
    /// with. It is written once the ship is saved and stays until that player's next login has settled their marked deeds
    /// and saved the character file, so a login that finds a deed still marked as registering removes it whatever has
    /// happened to the ship since (sold and unregistered by its buyer). A new type, so no migration.
    /// </summary>
    public class ShipRegistrationReceipt : EntityBase
    {
        public ShipRegistrationReceipt()
        {
            OwnerPlayerId = string.Empty;
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }
    }
}
