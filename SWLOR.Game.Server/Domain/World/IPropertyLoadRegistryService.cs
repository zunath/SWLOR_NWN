using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The registry of property instances: the area instance of each property and the players in it, the load state and failure
    /// of each property, the players waiting for a load, the instance spawn actions that have run and the placeable of each
    /// structure.
    /// </summary>
    public interface IPropertyLoadRegistryService
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
        /// Shows a property's description to a player arriving from outside it. Coming back to the property's area, such as after a
        /// reload, does not repeat it.
        /// </summary>
        void ShowPropertyDescription();

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        void ExitPropertyInstance();

        /// <summary>
        /// Remembers the placeable that was spawned for a structure property.
        /// </summary>
        /// <param name="propertyId">The structure's property Id.</param>
        /// <param name="placeable">The placeable in the game world.</param>
        void RegisterStructurePlaceable(string propertyId, uint placeable);

        /// <summary>Records why a property failed to load.</summary>
        /// <param name="propertyId">The property Id.</param>
        /// <param name="failure">The failure text.</param>
        void SetPropertyLoadFailure(string propertyId, string failure);

        /// <summary>Forgets the failure of a property.</summary>
        /// <param name="propertyId">The property Id.</param>
        void ClearPropertyLoadFailure(string propertyId);

        /// <summary>Finds the instance registered to a property whether or not its area is still valid.</summary>
        /// <param name="propertyId">The property Id.</param>
        /// <param name="instance">The registered instance.</param>
        /// <returns>true if an instance is registered; otherwise false.</returns>
        bool TryGetRegisteredInstance(string propertyId, out PropertyInstance instance);

        /// <summary>On a hot reload: registers the instance found in the world and marks its spawn action as run.</summary>
        /// <param name="propertyId">The property Id.</param>
        /// <param name="instance">The instance adopted from the world.</param>
        void AdoptInstance(string propertyId, PropertyInstance instance);

        /// <summary>Determines whether the spawn action of a property's layout has run for its instance.</summary>
        /// <param name="propertyId">The property Id.</param>
        /// <returns>true if it has run; otherwise false.</returns>
        bool IsSpawnActionCompleted(string propertyId);

        /// <summary>Records that the spawn action of a property's layout has run for its instance.</summary>
        /// <param name="propertyId">The property Id.</param>
        void MarkSpawnActionCompleted(string propertyId);

        /// <summary>Counts the players waiting for a property to load.</summary>
        /// <param name="propertyId">The property Id.</param>
        /// <returns>The number of waiting players.</returns>
        int GetWaiterCount(string propertyId);

        /// <summary>Adds a player to those waiting for a property to load. Nothing happens for a non-player or a blank property Id.</summary>
        /// <param name="player">The player.</param>
        /// <param name="propertyId">The property Id.</param>
        void AddPropertyLoadWaiter(uint player, string propertyId);

        /// <summary>Tells the players waiting for a property whether it loaded or failed, and stops them waiting.</summary>
        /// <param name="propertyId">The property Id.</param>
        /// <param name="loaded">true if the property loaded; false if it failed.</param>
        void NotifyPropertyLoadWaiters(string propertyId, bool loaded);

        /// <summary>Tells a player that the property is still loading.</summary>
        /// <param name="player">The player to tell.</param>
        void SendPropertyLoadingMessage(uint player);

        /// <summary>Finds the placeable remembered for a structure property.</summary>
        /// <param name="propertyId">The structure's property Id.</param>
        /// <param name="placeable">The placeable.</param>
        /// <returns>true if one is remembered; otherwise false.</returns>
        bool TryGetStructurePlaceable(string propertyId, out uint placeable);

        /// <summary>
        /// Forgets the instance, load state, failure, waiting players, spawn action and placeable of a property that is being deleted.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        void ForgetProperty(string propertyId);

        /// <summary>When the module loads: forgets the load states, failures, waiting players and spawn actions.</summary>
        void ResetLoadBookkeeping();

        /// <summary>When a hot reload quiesces this build: the load states, failures and waiting players, for the loader's carry.</summary>
        /// <returns>The carried registry state.</returns>
        JObject CarryState();

        /// <summary>On a hot reload: takes back the load states, failures and waiting players the old build carried.</summary>
        /// <param name="carry">The carry the loader read, which <see cref="CarryState"/> wrote.</param>
        void RestoreState(JObject carry);
    }
}
