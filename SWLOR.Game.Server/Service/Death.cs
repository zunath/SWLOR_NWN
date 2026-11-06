using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Player death, respawn, the XP debt penalty and where a player respawns. The logic lives in
    /// <see cref="IDeathService"/>; the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static class Death
    {
        private static IDeathService Instance => FacadeAccessor.Resolve<IDeathService>();

        /// <summary>
        /// When a player starts dying, instantly kill them.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDying)]
        public static void OnPlayerDying() => Instance.OnPlayerDying();

        /// <summary>
        /// Handles resetting a player's standard faction reputations and displaying the respawn pop-up menu.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath() => Instance.OnPlayerDeath();

        /// <summary>
        /// Handles setting player's HP, FP, and STM to half of maximum,
        /// applies penalties for death, and teleports him or her to their home point.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleRespawn)]
        public static void OnPlayerRespawn() => Instance.OnPlayerRespawn();

        /// <summary>
        /// Handles setting a player's respawn point if they don't have one set already.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void InitializeRespawnPoint() => Instance.InitializeRespawnPoint();

        /// <summary>
        /// Returns where a player respawns: their registered medical center, or the default respawn
        /// point (DTH_DEFAULT_RESPAWN_POINT) when no medical center is saved, its area no longer exists,
        /// or the saved point is not on walkable ground. Never returns the place the player died.
        /// </summary>
        /// <param name="dbPlayer">The player's record. May be null.</param>
        /// <returns>The location to respawn at.</returns>
        public static Location GetRespawnLocation(Player dbPlayer) => Instance.GetRespawnLocation(dbPlayer);

        /// <summary>
        /// Whether a player record names a respawn area at all.
        /// </summary>
        /// <param name="dbPlayer">The player's record. May be null.</param>
        /// <returns>true if a respawn area resref is saved, false otherwise.</returns>
        public static bool HasRespawnArea(Player dbPlayer) => Instance.HasRespawnArea(dbPlayer);
    }
}
