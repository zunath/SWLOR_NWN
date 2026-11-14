namespace SWLOR.Game.Server.Service.TransferService
{
    /// <summary>Which way a journaled transfer moves value between a character file and the database.</summary>
    public enum TransferDirection
    {
        /// <summary>
        /// Items leave the character's inventory and the database gains them (feed into a hopper, an animal into a corral, a starter
        /// enzyme into a vat). The character file is saved without them before the database changes.
        /// </summary>
        Deposit = 1,

        /// <summary>
        /// The database gives up something and the character's inventory gains items (a hopper emptied, an animal butchered, a crop
        /// harvested, a batch collected). The database changes first, with a receipt, and the character file is saved with the items
        /// before the entry goes.
        /// </summary>
        Withdraw = 2
    }
}
