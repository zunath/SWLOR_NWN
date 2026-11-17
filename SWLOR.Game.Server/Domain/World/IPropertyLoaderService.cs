using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Instance loading: the queue that loads instanced properties a few at a time, the startup load of the world properties and
    /// the load diagnostics. The registry of the instances and their load states is <see cref="IPropertyLoadRegistryService"/>, spawning a
    /// property is <see cref="IPropertySpawnService"/> and the hot reload of the loader is <see cref="IPropertyLoadReloadService"/>.
    /// </summary>
    public interface IPropertyLoaderService
    {
        /// <summary>
        /// Retrieves runtime load diagnostics for all instanced properties.
        /// </summary>
        /// <returns>A list of property load diagnostics.</returns>
        List<PropertyLoadDiagnostic> GetPropertyLoadDiagnostics();

        /// <summary>
        /// Determines whether an instanced property is loaded when a player first enters it rather than at startup.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>true if the property loads on demand; otherwise false.</returns>
        bool IsPropertyOnDemand(WorldProperty property);

        /// <summary>
        /// Retrieves a usable property instance or starts loading it if it is not ready.
        /// </summary>
        /// <param name="player">The player attempting to enter the property.</param>
        /// <param name="propertyId">The property Id to load.</param>
        /// <param name="instance">The loaded property instance.</param>
        /// <returns>true if the property is ready to enter; otherwise false.</returns>
        bool TryResolveEnterableInstance(uint player, string propertyId, out PropertyInstance instance);

        /// <summary>
        /// Queues an instanced property for loading again at staff priority, or moves a queued load to staff priority.
        /// A property that is already loaded queues the exterior structures of its interior again.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        /// <returns>true if a load was queued or promoted; otherwise false.</returns>
        bool RetryPropertyLoad(string propertyId);

        /// <summary>
        /// Cancels a queued load that has not started and marks the property as failed.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        /// <returns>true if a queued load was cancelled; otherwise false.</returns>
        bool AbortQueuedPropertyLoad(string propertyId);

        /// <summary>
        /// When the module loads, load all properties.
        /// </summary>
        void LoadProperties();

        /// <summary>
        /// Forgets everything the loader knows about a property that is being deleted: its instance, load state,
        /// failure, waiting players, queued load and placeable.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        void ForgetProperty(string propertyId);

        /// <summary>
        /// When a hot reload quiesces this build: the queued loads and world structures and the startup progress, for
        /// <see cref="RestoreQueueState"/> in the next build.
        /// </summary>
        /// <returns>The carried bookkeeping of the queue.</returns>
        JObject CarryQueueState();

        /// <summary>
        /// On a hot reload: takes back the queued loads, world structures and startup progress the old build carried.
        /// </summary>
        /// <param name="carry">The carry the reload service read.</param>
        void RestoreQueueState(JObject carry);

        /// <summary>
        /// Starts the load processor when queued loads or world structures remain after an adoption.
        /// </summary>
        /// <returns>The number of queued instance loads and of queued world structures.</returns>
        (int Jobs, int WorldStructures) ResumeQueuedLoads();
    }
}
