using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Core;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the per-player object visibility. The logic and state live in
    /// <see cref="IObjectVisibilityService"/>; every member here forwards to the container's instance, and the event
    /// handlers stay here so their names in the handler order manifest do not change.
    /// </summary>
    public static class ObjectVisibility
    {
        private static IObjectVisibilityService Instance => FacadeAccessor.Resolve<IObjectVisibilityService>();

        /// <summary>
        /// When the module loads, cycle through every area and every object to identify the visibility objects.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadVisibilityObjects()
        {
            Instance.LoadVisibilityObjects();
        }

        /// <summary>
        /// When a player enters the server, toggle visibility on all objects
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void LoadPlayerVisibilityObjects()
        {
            Instance.LoadPlayerVisibilityObjects();
        }

        /// <summary>
        /// Modifies the visibility of an object for a specific player.
        /// </summary>
        /// <param name="player">The player to adjust.</param>
        /// <param name="target">The target object to adjust.</param>
        /// <param name="type">The new type of visibility to use.</param>
        public static void AdjustVisibility(uint player, uint target, VisibilityType type)
        {
            Instance.AdjustVisibility(player, target, type);
        }

        /// <summary>
        /// Adjusts the visibility of a given object for a given player.
        /// </summary>
        /// <param name="player">The player to adjust.</param>
        /// <param name="visibilityObjectId">The visibility object Id of the object to adjust.</param>
        /// <param name="type">The new visibility type to adjust to.</param>
        public static void AdjustVisibilityByObjectId(uint player, string visibilityObjectId, VisibilityType type)
        {
            Instance.AdjustVisibilityByObjectId(player, visibilityObjectId, type);
        }
    }
}
