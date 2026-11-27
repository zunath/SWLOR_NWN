namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>Why an Edict could not be enacted or repealed.</summary>
    public enum EdictRefusal
    {
        None = 0,
        UnknownEdict = 1,
        CityUnavailable = 2,

        /// <summary>A planet-based Edict on a Wild Space station.</summary>
        UnavailableHere = 3,
        LevelTooLow = 4,
        AlreadyHeld = 5,

        /// <summary>Disrepair, No confidence, or Neglect.</summary>
        CityWideCondition = 6,
        NoFreeSlot = 7,
        TargetRequired = 8,
        InvalidTarget = 9,

        /// <summary>An Alliance and an Embargo against the same city.</summary>
        ExclusivePair = 10,
        InsufficientFunds = 11,
        NotHeld = 12,
        StillLocked = 13
    }
}
