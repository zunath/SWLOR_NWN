namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What an ammo variant does to its target once a hit lands (plan section 14).</summary>
    public enum ShipAmmoRider
    {
        None = 0,

        /// <summary>Slows the target by the variant's percent for its seconds.</summary>
        Slow = 1,

        /// <summary>Burns the target for the variant's percent of the shot's DMG every tick for its seconds.</summary>
        Burn = 2,
    }
}
