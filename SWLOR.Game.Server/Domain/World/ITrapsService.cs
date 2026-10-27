using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Player-placed traps: per-owner capacity, minimum spacing between any two live traps, an arming delay, a
    /// proximity trigger, and a lifetime cap. The static facade <c>Service.Traps</c> forwards to the container's
    /// instance of this interface and keeps the event handler.
    /// </summary>
    public interface ITrapsService
    {
        /// <summary>
        /// The number of live traps an owner may have at once: the base capacity plus the Additional Trap Capacity stat.
        /// </summary>
        int GetTrapCapacity(uint owner);

        /// <summary>
        /// Places a trap at the target location. Placement fails when another live trap - anyone's -
        /// sits within the minimum spacing. Exceeding the owner's capacity despawns their oldest trap.
        /// </summary>
        bool TryPlaceTrap(
            uint owner,
            Location location,
            int baseDamage,
            CombatDamageType damageType,
            Type statusEffect,
            int statusDurationSeconds,
            VisualEffect triggerVisualEffect,
            VisualEffect markerVisualEffect);

        /// <summary>
        /// Deploys a concealed trap from a crafted kit. Unlike the perk-active traps, the placement
        /// is hidden from everyone but the owner until an observer with the matching Trapcraft
        /// training spots it, at which point they may attempt to disarm it.
        /// </summary>
        bool TryPlaceKitTrap(uint owner, Location location, int tier);

        /// <summary>
        /// Deactivates every trap an owner has placed and forgets them.
        /// </summary>
        void ClearTraps(uint owner);

        /// <summary>
        /// Disarm attempt, fired when a player uses a revealed concealed trap. Success clears the
        /// trap and awards Espionage XP; failure sets it off on the would-be disarmer.
        /// </summary>
        void DisarmTrap();

        /// <summary>
        /// Whether a player with the given Trapcraft rank (and Master Saboteur, for tier 5) can use a trap of a tier.
        /// </summary>
        bool CanUseTrapTier(int trapcraftRank, bool hasMasterSaboteur, int tier);
    }
}
