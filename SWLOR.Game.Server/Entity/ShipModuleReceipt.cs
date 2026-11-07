namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// The acknowledgement that a ship took a fitted module, keyed by the module's instance id the fitter's item is marked
    /// with. It is written once the ship saved the module and stays until the fitter's next login has settled their marked
    /// modules and saved the character file, so a login that finds a module still marked as being fitted removes it
    /// whatever has happened to the ship since (removed by crew, lost with the ship). A new type, so no migration.
    /// </summary>
    public class ShipModuleReceipt : EntityBase
    {
        public ShipModuleReceipt()
        {
            OwnerPlayerId = string.Empty;
            ShipId = string.Empty;
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }

        public string ShipId { get; set; }
    }
}
