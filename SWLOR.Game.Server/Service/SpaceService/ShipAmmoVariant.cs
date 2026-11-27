using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// An ammo variant a missile or torpedo launcher fires in place of its plain ammo (plan sections 8 and 14). Rows
    /// come from the generated RevampRewardCatalog.
    /// </summary>
    public class ShipAmmoVariant
    {
        public string Resref { get; set; }
        public string Name { get; set; }

        /// <summary>The launcher family that fires it.</summary>
        public ShipModuleFamily Launcher { get; set; }

        /// <summary>The damage type the shot deals in place of the launcher's.</summary>
        public CombatDamageType DamageType { get; set; }

        /// <summary>The shot's DMG as a percent of the launcher's.</summary>
        public int DmgPercent { get; set; } = 100;

        /// <summary>Added to the shot's Accuracy.</summary>
        public int Accuracy { get; set; }

        /// <summary>The shield loses this many points per point of damage it absorbs.</summary>
        public double ShieldDamageMultiplier { get; set; } = 1.0;

        public ShipAmmoRider Rider { get; set; }

        /// <summary>The slow percent, or the burn's percent of the shot's DMG per tick.</summary>
        public int RiderPercent { get; set; }

        public int RiderSeconds { get; set; }

        /// <summary>Engineering crafts it; otherwise it only comes from salvage.</summary>
        public bool Crafted { get; set; }
    }
}
