using System.Collections.Generic;
namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The outcome of one weapon shot. The roll fills the hit, critical and damage fields; applying the damage
    /// fills the shield and hull fields.
    /// </summary>
    public class ShipShotResult
    {
        public bool IsHit { get; set; }
        public bool IsCritical { get; set; }

        /// <summary>The shot missed on the target's evasion. A blinded attacker's miss is not an evaded attack.</summary>
        public bool Evaded { get; set; }

        /// <summary>The hit chance the roll was made against.</summary>
        public int ChanceToHit { get; set; }

        /// <summary>The damage after every stage, before it meets the shield. Zero on a miss.</summary>
        public int Damage { get; set; }

        /// <summary>The threat the shot adds: its damage, or a nominal amount for a miss.</summary>
        public int Enmity { get; set; }

        /// <summary>Damage actually removed from the shield once applied.</summary>
        public int ShieldDamage { get; set; }

        /// <summary>Damage actually removed from the hull once applied.</summary>
        public int HullDamage { get; set; }

        /// <summary>Riders this landed shot consumed. Their extra damage and handlers run once the damage lands.</summary>
        public List<SpaceShotRider> ConsumedRiders { get; } = new();
    }
}
