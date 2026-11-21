namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A module being taken out of a ship, keyed by the module's instance id. It is saved before the ship saves the module
    /// as removed and deleted once the remover's character file holds the module, so the database, not the character
    /// file, says whether a removal happened: a login after a restart gives the module again only when the ship no longer
    /// has it and the character file does not either. A new type, so no migration.
    /// </summary>
    public class ShipModuleRemoval : EntityBase
    {
        public ShipModuleRemoval()
        {
            OwnerPlayerId = string.Empty;
            ShipId = string.Empty;
            SerializedItem = string.Empty;
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }

        public string ShipId { get; set; }

        public string SerializedItem { get; set; }
    }
}
