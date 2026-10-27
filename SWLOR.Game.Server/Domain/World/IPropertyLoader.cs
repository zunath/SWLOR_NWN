using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Instance loading: the registry of property area instances, their load states, failures and waiting players, the
    /// queue that loads them, and spawning a property into the game world.
    /// </summary>
    public interface IPropertyLoader
    {
        /// <summary>
        /// Assigns a property Id as a local variable to a specific object.
        /// </summary>
        /// <param name="obj">The object to assign</param>
        /// <param name="propertyId">The property Id to assign.</param>
        void AssignPropertyId(uint obj, string propertyId);

        /// <summary>
        /// Retrieves the assigned property Id assigned to a specific object.
        /// Returns an empty string if not found.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>The property Id or an empty string if not found.</returns>
        string GetPropertyId(uint obj);

        /// <summary>
        /// Registers an area instance to a given property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        /// <param name="instance">The area instance to register</param>
        /// <param name="layoutType">The layout of the property.</param>
        void RegisterInstance(string propertyId, uint instance, PropertyLayoutType layoutType);

        /// <summary>
        /// Retrieves the current load state for an instanced property.
        /// </summary>
        /// <param name="propertyId">The property Id to check.</param>
        /// <returns>The current property load state.</returns>
        PropertyLoadState GetPropertyLoadState(string propertyId);

        /// <summary>
        /// Retrieves the last failure associated with a property load.
        /// </summary>
        /// <param name="propertyId">The property Id to check.</param>
        /// <returns>The failure message or an empty string.</returns>
        string GetPropertyLoadFailure(string propertyId);

        /// <summary>
        /// Retrieves runtime load diagnostics for all instanced properties.
        /// </summary>
        /// <returns>A list of property load diagnostics.</returns>
        List<PropertyLoadDiagnostic> GetPropertyLoadDiagnostics();

        /// <summary>
        /// Retrieves a loaded property instance without throwing if it is not ready.
        /// </summary>
        /// <param name="propertyId">The property Id to retrieve.</param>
        /// <param name="instance">The loaded property instance.</param>
        /// <returns>true if the instance is fully loaded; otherwise false.</returns>
        bool TryGetLoadedInstance(string propertyId, out PropertyInstance instance);

        /// <summary>
        /// Records the load state of a property. Nothing happens for a blank property Id.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        /// <param name="state">The new load state.</param>
        void SetPropertyLoadState(string propertyId, PropertyLoadState state);

        /// <summary>
        /// Determines whether an instanced property is loaded when a player first enters it rather than at startup.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>true if the property loads on demand; otherwise false.</returns>
        bool IsPropertyOnDemand(WorldProperty property);

        /// <summary>
        /// Tells the players waiting for a property whether it is ready or could not be loaded, and stops them waiting;
        /// while the property is still loading they are told to try again shortly and keep waiting.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        void NotifyPropertyLoadWaitersForStaff(string propertyId);

        /// <summary>
        /// Tells a player that the property could not be loaded.
        /// </summary>
        /// <param name="player">The player to tell.</param>
        void SendPropertyLoadFailedMessage(uint player);

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
        /// Retrieves a placeable associated with a property Id.
        /// </summary>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A placeable or OBJECT_INVALID if not found.</returns>
        uint GetPlaceableByPropertyId(string propertyId);

        /// <summary>
        /// When a player enters a property instance, add them to the list of players.
        /// </summary>
        void EnterPropertyInstance();

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        void ExitPropertyInstance();

        /// <summary>
        /// Spawns the property into the game world.
        /// For structures, this means spawning a placeable at the location.
        /// For cities, starships, apartments, and buildings this means spawning area instances.
        /// </summary>
        /// <param name="property">The property to spawn into the world.</param>
        /// <param name="area">The area to spawn the property into. Leave OBJECT_INVALID if spawning an instance.</param>
        void SpawnIntoWorld(WorldProperty property, uint area);

        /// <summary>
        /// Remembers the placeable that was spawned for a structure property.
        /// </summary>
        /// <param name="propertyId">The structure's property Id.</param>
        /// <param name="placeable">The placeable in the game world.</param>
        void RegisterStructurePlaceable(string propertyId, uint placeable);

        /// <summary>
        /// Forgets everything the loader knows about a property that is being deleted: its instance, load state,
        /// failure, waiting players, queued load and placeable.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        void ForgetProperty(string propertyId);
    }
}
