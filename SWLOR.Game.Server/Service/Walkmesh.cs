using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the baked walkmesh locations. The logic and state live in <see cref="IWalkmeshService"/>;
    /// every member here forwards to the container's instance, and the event handlers stay here so their names in the
    /// handler order manifest do not change.
    /// </summary>
    public static class Walkmesh
    {
        private static IWalkmeshService Instance => FacadeAccessor.Resolve<IWalkmeshService>();

        /// <summary>
        /// When the module content changes, rerun the baking process.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleContentChange)]
        public static void LoadWalkmeshes()
        {
            Instance.LoadWalkmeshes();
        }

        /// <summary>
        /// When the module loads, retrieve the list of walkable locations from the database.
        /// These locations can be used to spawn objects randomly throughout an area.
        /// This only runs if the module content has NOT changed since the last run.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RetrieveWalkmeshes()
        {
            Instance.RetrieveWalkmeshes();
        }

        /// <summary>
        /// Retrieves a random location from the walkmeshes in an area.
        /// </summary>
        /// <param name="area">The area to retrieve a random location for.</param>
        /// <returns>A random location within an area.</returns>
        public static Location GetRandomLocation(uint area)
        {
            return Instance.GetRandomLocation(area);
        }
    }
}
