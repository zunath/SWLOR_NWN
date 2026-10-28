using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the property system. The logic and state live in <see cref="IPropertyService"/> and the parts
    /// behind it (the definitions catalog, the instance loader and the city governance); every member here forwards to
    /// the container's instance, and the event handlers stay here so their names in the handler order manifest do not change.
    /// </summary>
    public static class Property
    {
        private static IPropertyService Instance => FacadeAccessor.Resolve<IPropertyService>();

        /// <summary>
        /// Determines the number of hours before the city will be destroyed due to
        /// lack of citizens. This starts at the time of city hall placement for the initial check.
        /// At boot time, if the number of citizens is below the required amount, the player will have 18 hours
        /// to rectify it. Failure to do so will result in the city being lost upon the next server reboot.
        /// Note: Due to the cleanup occurring on server boot, which occurs once every 24 hours,
        ///       it's possible the player will have more time than the value specified here.
        ///       This is expected.
        /// </summary>
        public const int MinimumCitizensGracePeriodHours = PropertyCityGovernance.MinimumCitizensGracePeriodHours;

        /// <summary>
        /// Determines the number of days citizens have to register for an election.
        /// </summary>
        public const int ElectionRegistrationDays = PropertyCityGovernance.ElectionRegistrationDays;

        /// <summary>
        /// Determines the number of days all citizens have to vote for an election.
        /// </summary>
        public const int ElectionVotingDays = PropertyCityGovernance.ElectionVotingDays;

        public static void BroadcastCityUpkeepPaid(WorldProperty city)
        {
            Instance.BroadcastCityUpkeepPaid(city);
        }

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        /// <summary>
        /// When the module loads, clean up any deleted data, refreshes permissions and then load properties.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void OnModuleLoad()
        {
            Instance.OnModuleLoad();
        }

        /// <summary>
        /// Assigns a property Id as a local variable to a specific object.
        /// </summary>
        /// <param name="obj">The object to assign</param>
        /// <param name="propertyId">The property Id to assign.</param>
        public static void AssignPropertyId(uint obj, string propertyId)
        {
            Instance.AssignPropertyId(obj, propertyId);
        }

        /// <summary>
        /// Retrieves the assigned property Id assigned to a specific object.
        /// Returns an empty string if not found.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>The property Id or an empty string if not found.</returns>
        public static string GetPropertyId(uint obj)
        {
            return Instance.GetPropertyId(obj);
        }

        /// <summary>
        /// Registers an area instance to a given property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        /// <param name="instance">The area instance to register</param>
        /// <param name="layoutType">The layout of the property.</param>
        public static void RegisterInstance(string propertyId, uint instance, PropertyLayoutType layoutType)
        {
            Instance.RegisterInstance(propertyId, instance, layoutType);
        }

        /// <summary>
        /// Retrieves the current load state for an instanced property.
        /// </summary>
        /// <param name="propertyId">The property Id to check.</param>
        /// <returns>The current property load state.</returns>
        public static PropertyLoadState GetPropertyLoadState(string propertyId)
        {
            return Instance.GetPropertyLoadState(propertyId);
        }

        /// <summary>
        /// Retrieves the last failure associated with a property load.
        /// </summary>
        /// <param name="propertyId">The property Id to check.</param>
        /// <returns>The failure message or an empty string.</returns>
        public static string GetPropertyLoadFailure(string propertyId)
        {
            return Instance.GetPropertyLoadFailure(propertyId);
        }

        /// <summary>
        /// Retrieves runtime load diagnostics for all instanced properties.
        /// </summary>
        /// <returns>A list of property load diagnostics.</returns>
        public static List<PropertyLoadDiagnostic> GetPropertyLoadDiagnostics()
        {
            return Instance.GetPropertyLoadDiagnostics();
        }

        /// <summary>
        /// Retrieves a loaded property instance without throwing if it is not ready.
        /// </summary>
        /// <param name="propertyId">The property Id to retrieve.</param>
        /// <param name="instance">The loaded property instance.</param>
        /// <returns>true if the instance is fully loaded; otherwise false.</returns>
        public static bool TryGetLoadedInstance(string propertyId, out PropertyInstance instance)
        {
            return Instance.TryGetLoadedInstance(propertyId, out instance);
        }

        /// <summary>
        /// Retrieves all area resrefs used by property instance templates.
        /// </summary>
        /// <returns>A list of area resrefs.</returns>
        public static List<string> GetAllInstanceAreaResrefs()
        {
            return Instance.GetAllInstanceAreaResrefs();
        }

        /// <summary>
        /// Tells the players waiting for a property whether it is ready or could not be loaded, and stops them waiting;
        /// while the property is still loading they are told to try again shortly and keep waiting.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        public static void NotifyPropertyLoadWaitersForStaff(string propertyId)
        {
            Instance.NotifyPropertyLoadWaitersForStaff(propertyId);
        }

        /// <summary>
        /// Retrieves a usable property instance or starts loading it if it is not ready.
        /// </summary>
        /// <param name="player">The player attempting to enter the property.</param>
        /// <param name="propertyId">The property Id to load.</param>
        /// <param name="instance">The loaded property instance.</param>
        /// <returns>true if the property is ready to enter; otherwise false.</returns>
        public static bool TryResolveEnterableInstance(uint player, string propertyId, out PropertyInstance instance)
        {
            return Instance.TryResolveEnterableInstance(player, propertyId, out instance);
        }

        /// <summary>
        /// Queues an instanced property for loading again at staff priority, or moves a queued load to staff priority.
        /// A property that is already loaded queues the exterior structures of its interior again.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        /// <returns>true if a load was queued or promoted; otherwise false.</returns>
        public static bool RetryPropertyLoad(string propertyId)
        {
            return Instance.RetryPropertyLoad(propertyId);
        }

        /// <summary>
        /// Cancels a queued load that has not started and marks the property as failed.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        /// <returns>true if a queued load was cancelled; otherwise false.</returns>
        public static bool AbortQueuedPropertyLoad(string propertyId)
        {
            return Instance.AbortQueuedPropertyLoad(propertyId);
        }

        /// <summary>
        /// Starships left in a ship-only location, at a player starport that no longer exists (probably destroyed
        /// by the previous cleanup) or is being retrieved, or without a dock return to the last regular starport
        /// they docked at and its orbit. The caller saves the property.
        /// </summary>
        /// <param name="property">The starship property to update.</param>
        /// <param name="retrievedStarportId">A player starport interior being retrieved. Ships docked there return even though it still exists.</param>
        /// <param name="fallbackAreaResref">An area whose planet's starport receives the ship if it has no usable safe dock, such as the retrieved starport's city.</param>
        /// <returns>What happened to the starship's dock.</returns>
        public static StarshipDockReturnResult ReturnStarshipToSafeDock(WorldProperty property, string retrievedStarportId = "", string fallbackAreaResref = "")
        {
            return Instance.ReturnStarshipToSafeDock(property, retrievedStarportId, fallbackAreaResref);
        }

        /// <summary>
        /// Deletes a property and its owned children, clears associated data and permissions,
        /// and revokes its citizens' membership and unpaid citizenship taxes.
        /// </summary>
        /// <param name="property">The property to delete.</param>
        public static void DeleteProperty(WorldProperty property)
        {
            Instance.DeleteProperty(property);
        }

        /// <summary>
        /// Creates a new apartment in the database for a given player.
        /// </summary>
        /// <param name="player">The player to associate the apartment with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <returns>The new world property.</returns>
        public static WorldProperty CreateApartment(uint player, PropertyLayoutType layout)
        {
            return Instance.CreateApartment(player, layout);
        }

        /// <summary>
        /// Creates a new starship in the database for a given player and returns the world property.
        /// </summary>
        /// <param name="player">The player to associate the starship with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <param name="planetType">The planet where this starship is being created.</param>
        /// <param name="spaceLocation">Location of the space transfer point (when a player is converted to a ship)</param>
        /// <param name="landingLocation">Location of the ground transfer point (when a player is converted back to normal)</param>
        /// <returns>The new world property.</returns>
        public static WorldProperty CreateStarship(
            uint player,
            PropertyLayoutType layout,
            PlanetType planetType,
            Location spaceLocation,
            Location landingLocation)
        {
            return Instance.CreateStarship(player, layout, planetType, spaceLocation, landingLocation);
        }

        /// <summary>
        /// Creates a new city in the specified area and assigns the specified player to become the owner.
        /// A city hall structure and interior will also be spawned at the specified location.
        /// The specified item will be destroyed.
        /// </summary>
        /// <param name="player">The player who will become mayor.</param>
        /// <param name="area">The area to claim.</param>
        /// <param name="item">The item used to place the city hall</param>
        /// <param name="location">The location to spawn city hall.</param>
        public static void CreateCity(uint player, uint area, uint item, Location location)
        {
            Instance.CreateCity(player, area, item, location);
        }

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
        public static void CreateBuilding(
            uint player,
            uint item,
            string parentCityId,
            PropertyType propertyType,
            PropertyLayoutType layout,
            StructureType structureType,
            Location location)
        {
            Instance.CreateBuilding(player, item, parentCityId, propertyType, layout, structureType, location);
        }

        /// <summary>
        /// Creates a structure inside a specific property.
        /// </summary>
        /// <param name="parentPropertyId">The parent property to associate this structure with.</param>
        /// <param name="item">The item used to spawn the structure.</param>
        /// <param name="type">The type of structure to spawn.</param>
        /// <param name="location">The location to spawn the structure at.</param>
        public static WorldProperty CreateStructure(
            string parentPropertyId,
            uint item,
            StructureType type,
            Location location)
        {
            return Instance.CreateStructure(parentPropertyId, item, type, location);
        }

        /// <summary>
        /// Retrieves a list of permissions associated with the item storage of a property for a given player.
        /// </summary>
        /// <param name="playerId">The player Id to search for</param>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A list of permissions</returns>
        public static List<WorldPropertyPermission> GetCategoryPermissions(string playerId, string propertyId)
        {
            return Instance.GetCategoryPermissions(playerId, propertyId);
        }

        /// <summary>
        /// Retrieves a property layout by its type.
        /// </summary>
        /// <param name="type">The type of layout to retrieve.</param>
        /// <returns>A property layout</returns>
        public static PropertyLayout GetLayoutByType(PropertyLayoutType type)
        {
            return Instance.GetLayoutByType(type);
        }

        /// <summary>
        /// Retrieves a structure detail by its type.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static StructureAttribute GetStructureByType(StructureType structure)
        {
            return Instance.GetStructureByType(structure);
        }

        /// <summary>
        /// Retrieves all layouts for a given property type.
        /// </summary>
        /// <param name="type">The type of property to search for</param>
        /// <returns>A list of layouts for the given property type.</returns>
        public static List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type)
        {
            return Instance.GetAllLayoutsByPropertyType(type);
        }

        /// <summary>
        /// Retrieves the entrance point for a given layout.
        /// </summary>
        /// <param name="type">The layout type</param>
        /// <returns>The entrance position for the layout.</returns>
        public static Vector4 GetEntrancePosition(PropertyLayoutType type)
        {
            return Instance.GetEntrancePosition(type);
        }

        /// <summary>
        /// Retrieves the template area by a given layout type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>The instance template area associated with this type.</returns>
        public static uint GetInstanceTemplate(PropertyLayoutType type)
        {
            return Instance.GetInstanceTemplate(type);
        }

        /// <summary>
        /// Retrieves the detail for a given permission type.
        /// </summary>
        /// <param name="permission">The type of permission to retrieve.</param>
        /// <returns>A permission detail</returns>
        public static PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission)
        {
            return Instance.GetPermissionByType(permission);
        }

        /// <summary>
        /// Retrieves the list of available permissions for a given property type.
        /// </summary>
        /// <param name="type">The type of property</param>
        /// <returns>A list of available permissions</returns>
        public static List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type)
        {
            return Instance.GetPermissionsByPropertyType(type);
        }

        /// <summary>
        /// Retrieves the property detail for a given type of property.
        /// </summary>
        /// <param name="type">The type of property to get.</param>
        /// <returns>A property detail for the given type.</returns>
        public static PropertyTypeAttribute GetPropertyDetail(PropertyType type)
        {
            return Instance.GetPropertyDetail(type);
        }

        /// <summary>
        /// Retrieves a placeable associated with a property Id.
        /// </summary>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A placeable or OBJECT_INVALID if not found.</returns>
        public static uint GetPlaceableByPropertyId(string propertyId)
        {
            return Instance.GetPlaceableByPropertyId(propertyId);
        }

        /// <summary>
        /// Retrieves a list of structures which have an interior with the specified property type.
        /// </summary>
        /// <param name="propertyType">The property type to search for.</param>
        /// <returns>A list of structure types.</returns>
        public static List<StructureType> GetStructuresByInteriorPropertyType(PropertyType propertyType)
        {
            return Instance.GetStructuresByInteriorPropertyType(propertyType);
        }

        /// <summary>
        /// When an apartment terminal is used, open the Apartment NUI
        /// </summary>
        [NWNEventHandler(ScriptName.OnApartmentTerminal)]
        public static void StartApartmentConversation()
        {
            Instance.StartApartmentConversation();
        }

        /// <summary>
        /// Determines whether a player has a specific permission.
        /// This will always return true for DMs.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="propertyId">The property Id to check.</param>
        /// <param name="permission">The type of permission to check.</param>
        /// <returns>true if player has permission, false otherwise</returns>
        public static bool HasPropertyPermission(uint player, string propertyId, PropertyPermissionType permission)
        {
            return Instance.HasPropertyPermission(player, propertyId, permission);
        }

        /// <summary>
        /// Determines whether a player can GRANT a specific permission to another player.
        /// This will always return true for DMs.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="propertyId">The property Id to check.</param>
        /// <param name="permission">The type of permission to check.</param>
        /// <returns>true if player can grant the permission, false otherwise</returns>
        public static bool CanGrantPermission(uint player, string propertyId, PropertyPermissionType permission)
        {
            return Instance.CanGrantPermission(player, propertyId, permission);
        }

        /// <summary>
        /// Sends a player to the template area of a particular layout.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="layout">The layout type to send them to.</param>
        public static void PreviewProperty(uint player, PropertyLayoutType layout)
        {
            Instance.PreviewProperty(player, layout);
        }

        /// <summary>
        /// When a player enters a property instance, add them to the list of players.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void EnterPropertyInstance()
        {
            Instance.EnterPropertyInstance();
        }

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void ExitPropertyInstance()
        {
            Instance.ExitPropertyInstance();
        }

        /// <summary>
        /// Sends a player to a specific property's instance.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="propertyId">The property Id</param>
        public static void EnterProperty(uint player, string propertyId)
        {
            Instance.EnterProperty(player, propertyId);
        }

        /// <summary>
        /// Stores the original location of a player, before being ported into a property instance.
        /// </summary>
        /// <param name="player">The player whose location will be stored.</param>
        public static void StoreOriginalLocation(uint player)
        {
            Instance.StoreOriginalLocation(player);
        }

        /// <summary>
        /// Jumps player to their original location, which is where they were before entering a property instance.
        /// This will also clear the temporary data related to the original location.
        /// </summary>
        /// <param name="player">The player who will jump.</param>
        public static void JumpToOriginalLocation(uint player)
        {
            Instance.JumpToOriginalLocation(player);
        }

        /// <summary>
        /// When the property menu feat is used, open the GUI window.
        /// </summary>
        [NWNEventHandler(ScriptName.OnFeatUseBefore)]
        public static void PropertyMenu()
        {
            Instance.PropertyMenu();
        }

        /// <summary>
        /// Retrieves the structure type from an item.
        /// Item's resref must start with 'structure_' and end with 4 numbers.
        /// I.E: 'structure_0004'
        /// Returns StructureType.Invalid on error.
        /// </summary>
        /// <param name="item">The item to retrieve from.</param>
        /// <returns>A structure type associated with the item.</returns>
        public static StructureType GetStructureTypeFromItem(uint item)
        {
            return Instance.GetStructureTypeFromItem(item);
        }

        /// <summary>
        /// Before an item is used, if it is a structure item, place it at the specified location.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemUseBefore)]
        public static void PlaceStructure()
        {
            Instance.PlaceStructure();
        }

        /// <summary>
        /// When a building entrance is used, port the player inside the instance if they have permission
        /// or display an error message saying they don't have permission to enter.
        /// </summary>
        [NWNEventHandler(ScriptName.OnEnterProperty)]
        public static void EnterBuilding()
        {
            Instance.EnterBuilding();
        }

        /// <summary>
        /// If a structure changed action is registered, this will perform the action on the specified
        /// property and placeable. If not registered, nothing will happen.
        /// </summary>
        /// <param name="structureType">The type of structure</param>
        /// <param name="changeType">The type of change.</param>
        /// <param name="property">The world property to target</param>
        /// <param name="placeable">The placeable to target</param>
        public static void RunStructureChangedEvent(StructureType structureType, StructureChangeType changeType, WorldProperty property, uint placeable)
        {
            Instance.RunStructureChangedEvent(structureType, changeType, property, placeable);
        }

        /// <summary>
        /// When the Citizenship terminal is used, open the Manage Citizenship UI.
        /// </summary>
        [NWNEventHandler(ScriptName.OnOpenCitizenship)]
        public static void OpenCitizenshipMenu()
        {
            Instance.OpenCitizenshipMenu();
        }

        /// <summary>
        /// When the City Management terminal is used, open the City Management UI.
        /// </summary>
        [NWNEventHandler(ScriptName.OnOpenCityManage)]
        public static void OpenCityManagementMenu()
        {
            Instance.OpenCityManagementMenu();
        }

        /// <summary>
        /// Retrieves the name of a particular city level.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>A string representing the city level.</returns>
        public static string GetCityLevelName(int level)
        {
            return Instance.GetCityLevelName(level);
        }

        /// <summary>
        /// Retrieves the number of citizens required for the next city level.
        /// If level isn't supported, -1 will be returned.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>The number of citizens required to level up the city.</returns>
        public static int GetCitizensRequiredForNextCityLevel(int level)
        {
            return Instance.GetCitizensRequiredForNextCityLevel(level);
        }

        /// <summary>
        /// Retrieves the effective upgrade level of a city, taking into account the city's overall level
        /// into the calculation.
        /// </summary>
        /// <param name="cityId">The property Id of the city.</param>
        /// <param name="upgradeType">The type of upgrade to check</param>
        /// <returns>A value ranging between 0 and 5.</returns>
        public static int GetEffectiveUpgradeLevel(string cityId, PropertyUpgradeType upgradeType)
        {
            return Instance.GetEffectiveUpgradeLevel(cityId, upgradeType);
        }
    }
}
