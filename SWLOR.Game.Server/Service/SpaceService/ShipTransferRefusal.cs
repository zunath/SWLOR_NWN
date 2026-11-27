namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Why a registered ship cannot change owner right now (plan section 7, "Ship transfers and sales").</summary>
    public enum ShipTransferRefusal
    {
        None = 0,
        MissingShip = 1,
        SameOwner = 2,
        InSpace = 3,
        DockedAway = 4,
        PlayersAboard = 5,
        ListedForSale = 6,
        CargoAboard = 7,
        SealedFreight = 8,
        UntradeableItem = 9,
        RecipientAtLimit = 10,
    }
}
