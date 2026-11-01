namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Plain inputs for deciding whether a toggled module may fire its next cycle.
    /// </summary>
    public class ShipToggleCheck
    {
        /// <summary>The ship is still in space and alive.</summary>
        public bool ShipInSpace { get; set; } = true;

        /// <summary>The ship is cloaking or cloaked and the module is not the Cloaking Device itself.</summary>
        public bool ShipCloaked { get; set; }

        /// <summary>False for modules that work around the ship and ignore their target.</summary>
        public bool NeedsTarget { get; set; } = true;

        public bool TargetValid { get; set; }
        public bool TargetDead { get; set; }

        /// <summary>The target passes the module's valid object types and ship checks.</summary>
        public bool TargetTypeAllowed { get; set; } = true;

        /// <summary>The target is another player ship that the ground PvP rules no longer let this ship attack.</summary>
        public bool PvpForbidden { get; set; }

        public bool CheckRange { get; set; }
        public float Distance { get; set; }
        public float MaxDistance { get; set; }

        /// <summary>The module's own validation message (no ammo, asteroid used up). Empty when valid.</summary>
        public string ValidationError { get; set; }

        public int Capacitor { get; set; }
        public int RequiredCapacitor { get; set; }
    }
}
