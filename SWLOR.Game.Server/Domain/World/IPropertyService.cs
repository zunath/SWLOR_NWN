using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The property system: creating properties, their definitions, instance loading and city governance. The members of the parts
    /// (<see cref="IPropertyCatalogService"/>, <see cref="IPropertyLoaderService"/>, <see cref="IPropertyLoadRegistryService"/>,
    /// <see cref="IPropertySpawnService"/>, <see cref="IPropertyCityGovernanceService"/>) are forwarded by <see cref="PropertyService"/>.
    /// Maintenance and deletion, access, structure placement and the terminals are <see cref="IPropertyMaintenanceService"/>,
    /// <see cref="IPropertyAccessService"/>, <see cref="IPropertyStructurePlacementService"/> and <see cref="IPropertyTerminalService"/>.
    /// </summary>
    public interface IPropertyService
    {
        void BroadcastCityUpkeepPaid(WorldProperty city);

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        void CacheData();

        /// <summary>
        /// When the module loads, clean up any deleted data, refreshes permissions and then load properties.
        /// </summary>
        void OnModuleLoad();

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
        /// Retrieves all area resrefs used by property instance templates.
        /// </summary>
        /// <returns>A list of area resrefs.</returns>
        List<string> GetAllInstanceAreaResrefs();

        /// <summary>
        /// Tells the players waiting for a property whether it is ready or could not be loaded, and stops them waiting;
        /// while the property is still loading they are told to try again shortly and keep waiting.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        void NotifyPropertyLoadWaitersForStaff(string propertyId);

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
        /// Creates a new apartment in the database for a given player.
        /// </summary>
        /// <param name="player">The player to associate the apartment with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <returns>The new world property.</returns>
        WorldProperty CreateApartment(uint player, PropertyLayoutType layout);

        /// <summary>
        /// Creates a new starship in the database for a given player and returns the world property.
        /// </summary>
        /// <param name="player">The player to associate the starship with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <param name="planetType">The planet where this starship is being created.</param>
        /// <param name="spaceLocation">Location of the space transfer point (when a player is converted to a ship)</param>
        /// <param name="landingLocation">Location of the ground transfer point (when a player is converted back to normal)</param>
        /// <returns>The new world property.</returns>
        WorldProperty CreateStarship(
            uint player,
            PropertyLayoutType layout,
            PlanetType planetType,
            Location spaceLocation,
            Location landingLocation);

        /// <summary>
        /// Creates a new city in the specified area and assigns the specified player to become the owner.
        /// A city hall structure and interior will also be spawned at the specified location.
        /// The specified item will be destroyed.
        /// </summary>
        /// <param name="player">The player who will become mayor.</param>
        /// <param name="area">The area to claim.</param>
        /// <param name="item">The item used to place the city hall</param>
        /// <param name="location">The location to spawn city hall.</param>
        void CreateCity(uint player, uint area, uint item, Location location);

        /// <summary>
        /// Creates a new structure and interior property associated with the building.
        /// </summary>
        /// <param name="player">The player to associate the building with.</param>
        /// <param name="parentCityId">The parent city Id.</param>
        /// <param name="propertyType">The type of property to create</param>
        /// <param name="layout">The layout to use.</param>
        /// <param name="item">The item used to create the building.</param>
        /// <param name="structureType">The type of structure to create.</param>
        /// <param name="location">The location to spawn the structure.</param>
        /// <returns>The new world property.</returns>
        void CreateBuilding(
            uint player,
            uint item,
            string parentCityId,
            PropertyType propertyType,
            PropertyLayoutType layout,
            StructureType structureType,
            Location location);

        /// <summary>
        /// Creates a structure inside a specific property.
        /// </summary>
        /// <param name="parentPropertyId">The parent property to associate this structure with.</param>
        /// <param name="item">The item used to spawn the structure.</param>
        /// <param name="type">The type of structure to spawn.</param>
        /// <param name="location">The location to spawn the structure at.</param>
        WorldProperty CreateStructure(
            string parentPropertyId,
            uint item,
            StructureType type,
            Location location);

        /// <summary>
        /// Retrieves a property layout by its type.
        /// </summary>
        /// <param name="type">The type of layout to retrieve.</param>
        /// <returns>A property layout</returns>
        PropertyLayout GetLayoutByType(PropertyLayoutType type);

        /// <summary>
        /// Retrieves a structure detail by its type.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        StructureAttribute GetStructureByType(StructureType structure);

        /// <summary>
        /// Retrieves all layouts for a given property type.
        /// </summary>
        /// <param name="type">The type of property to search for</param>
        /// <returns>A list of layouts for the given property type.</returns>
        List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type);

        /// <summary>
        /// Retrieves the entrance point for a given layout.
        /// </summary>
        /// <param name="type">The layout type</param>
        /// <returns>The entrance position for the layout.</returns>
        Vector4 GetEntrancePosition(PropertyLayoutType type);

        /// <summary>
        /// Retrieves the template area by a given layout type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>The instance template area associated with this type.</returns>
        uint GetInstanceTemplate(PropertyLayoutType type);

        /// <summary>
        /// Retrieves the detail for a given permission type.
        /// </summary>
        /// <param name="permission">The type of permission to retrieve.</param>
        /// <returns>A permission detail</returns>
        PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission);

        /// <summary>
        /// Retrieves the list of available permissions for a given property type.
        /// </summary>
        /// <param name="type">The type of property</param>
        /// <returns>A list of available permissions</returns>
        List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type);

        /// <summary>
        /// Retrieves the property detail for a given type of property.
        /// </summary>
        /// <param name="type">The type of property to get.</param>
        /// <returns>A property detail for the given type.</returns>
        PropertyTypeAttribute GetPropertyDetail(PropertyType type);

        /// <summary>
        /// Retrieves a placeable associated with a property Id.
        /// </summary>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A placeable or OBJECT_INVALID if not found.</returns>
        uint GetPlaceableByPropertyId(string propertyId);

        /// <summary>
        /// Retrieves a list of structures which have an interior with the specified property type.
        /// </summary>
        /// <param name="propertyType">The property type to search for.</param>
        /// <returns>A list of structure types.</returns>
        List<StructureType> GetStructuresByInteriorPropertyType(PropertyType propertyType);

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
        /// Shows a structure's player-given name on its placeable. A structure that still has its catalog name keeps its template's.
        /// </summary>
        void ApplyStructureWorldName(uint placeable, WorldProperty structure);

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        void ExitPropertyInstance();

        /// <summary>
        /// If a structure changed action is registered, this will perform the action on the specified
        /// property and placeable. If not registered, nothing will happen.
        /// </summary>
        /// <param name="structureType">The type of structure</param>
        /// <param name="changeType">The type of change.</param>
        /// <param name="property">The world property to target</param>
        /// <param name="placeable">The placeable to target</param>
        void RunStructureChangedEvent(StructureType structureType, StructureChangeType changeType, WorldProperty property, uint placeable);

        /// <summary>
        /// Retrieves the name of a particular city level.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>A string representing the city level.</returns>
        string GetCityLevelName(int level);

        /// <summary>
        /// Retrieves the number of citizens required for the next city level.
        /// If level isn't supported, -1 will be returned.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>The number of citizens required to level up the city.</returns>
        int GetCitizensRequiredForNextCityLevel(int level);

        /// <summary>
        /// Retrieves the effective upgrade level of a city, taking into account the city's overall level
        /// into the calculation.
        /// </summary>
        /// <param name="cityId">The property Id of the city.</param>
        /// <param name="upgradeType">The type of upgrade to check</param>
        /// <returns>A value ranging between 0 and 5.</returns>
        int GetEffectiveUpgradeLevel(string cityId, PropertyUpgradeType upgradeType);
    }
}
