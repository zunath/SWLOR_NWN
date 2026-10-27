using System;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the player-placed traps. The logic and state live in <see cref="ITrapsService"/>; every member
    /// here forwards to the container's instance, and the event handler stays here so its name in the handler order
    /// manifest does not change.
    /// </summary>
    public static class Traps
    {
        private static ITrapsService Instance => FacadeAccessor.Resolve<ITrapsService>();

        public static int GetTrapCapacity(uint owner)
        {
            return Instance.GetTrapCapacity(owner);
        }

        /// <summary>
        /// Places a trap at the target location. Placement fails when another live trap - anyone's -
        /// sits within the minimum spacing. Exceeding the owner's capacity despawns their oldest trap.
        /// </summary>
        public static bool TryPlaceTrap(
            uint owner,
            Location location,
            int baseDamage,
            CombatDamageType damageType,
            Type statusEffect,
            int statusDurationSeconds,
            VisualEffect triggerVisualEffect,
            VisualEffect markerVisualEffect)
        {
            return Instance.TryPlaceTrap(
                owner,
                location,
                baseDamage,
                damageType,
                statusEffect,
                statusDurationSeconds,
                triggerVisualEffect,
                markerVisualEffect);
        }

        /// <summary>
        /// Deploys a concealed trap from a crafted kit. Unlike the perk-active traps, the placement
        /// is hidden from everyone but the owner until an observer with the matching Trapcraft
        /// training spots it, at which point they may attempt to disarm it.
        /// </summary>
        public static bool TryPlaceKitTrap(uint owner, Location location, int tier)
        {
            return Instance.TryPlaceKitTrap(owner, location, tier);
        }

        public static void ClearTraps(uint owner)
        {
            Instance.ClearTraps(owner);
        }

        /// <summary>
        /// Disarm attempt, fired when a player uses a revealed concealed trap. Success clears the
        /// trap and awards Espionage XP; failure sets it off on the would-be disarmer.
        /// </summary>
        [NWNEventHandler(ScriptName.OnEspionageTrapUsed)]
        public static void DisarmTrap()
        {
            Instance.DisarmTrap();
        }

        public static bool CanUseTrapTier(int trapcraftRank, bool hasMasterSaboteur, int tier)
        {
            return Instance.CanUseTrapTier(trapcraftRank, hasMasterSaboteur, tier);
        }
    }
}
