namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>Why a holding action did not happen.</summary>
    public enum HoldingRefusal
    {
        None = 0,
        NotSoldHere = 1,
        AtLimit = 2,
        NotEnoughCredits = 3,
        MaximumLevel = 4,
        MissingGoods = 5,
        PlantBuilt = 6,
        NotAtDock = 7,
        ShipNotDocked = 8,
        NothingToCollect = 9,
        CargoBayFull = 10,
        UpkeepOwed = 11,
        NothingNeeded = 12,
        NoneAboard = 13,
        DroidAlreadyPosted = 14,
        NoDroid = 15,
        NoFine = 16,
        NotOwner = 17,
    }
}
