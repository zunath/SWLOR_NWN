using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>Hot reload of the property loader.</summary>
    public interface IPropertyLoadReloadService
    {
        /// <summary>
        /// When a hot reload quiesces this build: leaves the loader's bookkeeping on the module (load states, failures, the
        /// players waiting for a load, the queued loads and world structures, the startup progress), which the world cannot
        /// tell, for the next build's <see cref="AdoptLiveProperties"/>. It changes nothing.
        /// </summary>
        void CarryLoadState();

        /// <summary>
        /// On a hot reload, in place of the boot's <see cref="LoadProperties"/>: registers the property instances, structure
        /// placeables and starport docks that are in the world, takes the carried bookkeeping and resumes the queued loads where
        /// the old build left them. It runs no upkeep, election or lease clean-up, spawns nothing that is already in the world and
        /// writes nothing to Redis.
        /// </summary>
        void AdoptLiveProperties();
    }
}
