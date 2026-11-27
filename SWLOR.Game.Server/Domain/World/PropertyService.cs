using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Domain.World;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The property system: creating properties and the boot sequence of the module load. The parts are the definitions
    /// (<see cref="IPropertyCatalogService"/>), instance loading (<see cref="IPropertyLoaderService"/>, <see cref="IPropertyLoadRegistryService"/>,
    /// <see cref="IPropertySpawnService"/>, <see cref="IPropertyLoadReloadService"/>), city governance (<see cref="IPropertyCityGovernanceService"/>),
    /// maintenance and deletion (<see cref="IPropertyMaintenanceService"/>), access (<see cref="IPropertyAccessService"/>), structure placement
    /// (<see cref="IPropertyStructurePlacementService"/>) and the terminals (<see cref="IPropertyTerminalService"/>). The logic and state that
    /// used to be part of the static property class moved to these classes unchanged; the members of the definitions, loading and
    /// city parts are forwarded from here, the others are reached through their own interfaces. The area handlers run on this
    /// instance; static callers reach it through <c>GameServices.Property</c>.
    /// </summary>
    public class PropertyService : IPropertyService
    {
        private readonly IDBService _db;
        private readonly IPlanetService _planet;
        private readonly IPropertyCatalogService _catalog;
        private readonly IPropertyLoaderService _loader;
        private readonly IPropertyLoadRegistryService _registry;
        private readonly IPropertySpawnService _spawn;
        private readonly IPropertyLoadReloadService _reload;
        private readonly IPropertyCityGovernanceService _city;
        private readonly IPropertyMaintenanceService _maintenance;

        public PropertyService(
            IDBService db,
            IPlanetService planet,
            IPropertyCatalogService catalog,
            IPropertyLoaderService loader,
            IPropertyLoadRegistryService registry,
            IPropertySpawnService spawn,
            IPropertyLoadReloadService reload,
            IPropertyCityGovernanceService city,
            IPropertyMaintenanceService maintenance)
        {
            _db = db;
            _planet = planet;
            _catalog = catalog;
            _loader = loader;
            _registry = registry;
            _spawn = spawn;
            _reload = reload;
            _city = city;
            _maintenance = maintenance;
        }

        /// <inheritdoc />
        public void BroadcastCityUpkeepPaid(WorldProperty city)
        {
            _city.BroadcastCityUpkeepPaid(city);
        }

        /// <inheritdoc />
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            _catalog.CacheData();
        }

        /// <summary>
        /// When the module loads, clean up any deleted data, refreshes permissions and then load properties. Boot only: a
        /// hot reload runs no upkeep, election or lease clean-up and spawns nothing; it adopts the properties already in the
        /// world (<see cref="CarryProperties"/>, <see cref="AdoptProperties"/>).
        /// </summary>
        [BootPhase(BootReload.BootOnly, AdoptedBy = nameof(AdoptProperties), CarriedBy = nameof(CarryProperties))]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void OnModuleLoad()
        {
            _maintenance.RefreshPermissions();
            _city.ProcessCities();
            _maintenance.CleanUpData();
            _loader.LoadProperties();
        }

        /// <summary>When a hot reload quiesces this build: <see cref="IPropertyLoadReloadService.CarryLoadState"/>.</summary>
        public void CarryProperties()
        {
            _reload.CarryLoadState();
        }

        /// <summary>On a hot reload, in place of <see cref="OnModuleLoad"/>: <see cref="IPropertyLoadReloadService.AdoptLiveProperties"/>.</summary>
        public void AdoptProperties()
        {
            _reload.AdoptLiveProperties();
        }

        /// <inheritdoc />
        public void AssignPropertyId(uint obj, string propertyId)
        {
            _registry.AssignPropertyId(obj, propertyId);
        }

        /// <inheritdoc />
        public string GetPropertyId(uint obj)
        {
            return _registry.GetPropertyId(obj);
        }

        /// <inheritdoc />
        public void RegisterInstance(string propertyId, uint instance, PropertyLayoutType layoutType)
        {
            _registry.RegisterInstance(propertyId, instance, layoutType);
        }

        /// <inheritdoc />
        public PropertyLoadState GetPropertyLoadState(string propertyId)
        {
            return _registry.GetPropertyLoadState(propertyId);
        }

        /// <inheritdoc />
        public string GetPropertyLoadFailure(string propertyId)
        {
            return _registry.GetPropertyLoadFailure(propertyId);
        }

        /// <inheritdoc />
        public List<PropertyLoadDiagnostic> GetPropertyLoadDiagnostics()
        {
            return _loader.GetPropertyLoadDiagnostics();
        }

        /// <inheritdoc />
        public bool TryGetLoadedInstance(string propertyId, out PropertyInstance instance)
        {
            return _registry.TryGetLoadedInstance(propertyId, out instance);
        }

        /// <inheritdoc />
        public List<string> GetAllInstanceAreaResrefs()
        {
            return _catalog.GetAllInstanceAreaResrefs();
        }

        /// <inheritdoc />
        public void NotifyPropertyLoadWaitersForStaff(string propertyId)
        {
            _registry.NotifyPropertyLoadWaitersForStaff(propertyId);
        }

        /// <inheritdoc />
        public bool TryResolveEnterableInstance(uint player, string propertyId, out PropertyInstance instance)
        {
            return _loader.TryResolveEnterableInstance(player, propertyId, out instance);
        }

        /// <inheritdoc />
        public bool RetryPropertyLoad(string propertyId)
        {
            return _loader.RetryPropertyLoad(propertyId);
        }

        /// <inheritdoc />
        public bool AbortQueuedPropertyLoad(string propertyId)
        {
            return _loader.AbortQueuedPropertyLoad(propertyId);
        }

        /// <summary>
        /// Creates a list of default item categories for use in freshly made properties.
        /// </summary>
        /// <returns>A list of categories</returns>
        private List<WorldPropertyCategory> CreateDefaultCategories(string parentPropertyId)
        {
            return new List<WorldPropertyCategory>
            {
                new WorldPropertyCategory
                {
                    ParentPropertyId = parentPropertyId,
                    Name = "Weapons"
                },
                new WorldPropertyCategory
                {
                    ParentPropertyId = parentPropertyId,
                    Name = "Armor"
                },
                new WorldPropertyCategory
                {
                    ParentPropertyId = parentPropertyId,
                    Name = "Crafting"
                },
                new WorldPropertyCategory
                {
                    ParentPropertyId = parentPropertyId,
                    Name = "Miscellaneous"
                },
            };
        }

        private WorldProperty CreateProperty(
            uint creatorPlayer,
            string ownerPlayerId,
            string propertyName,
            PropertyType type,
            PropertyLayoutType layout,
            uint targetArea = OBJECT_INVALID,
            Action<WorldProperty> constructionAction = null)
        {
            var creatorPlayerId = GetObjectUUID(creatorPlayer);
            var layoutDetail = GetLayoutByType(layout);
            var propertyDetail = _catalog.GetPropertyDetail(type);

            var property = new WorldProperty
            {
                CustomName = propertyName,
                IsPubliclyAccessible = propertyDetail.PublicSetting == PropertyPublicType.AlwaysPublic,
                PropertyType = type,
                OwnerPlayerId = ownerPlayerId,
                Layout = layout,
                ItemStorageCount = layoutDetail.ItemStorageLimit
            };

            constructionAction?.Invoke(property);

            var ownerPermissions = new WorldPropertyPermission
            {
                PropertyId = property.Id,
                PlayerId = ownerPlayerId
            };

            var creatorPermissions = creatorPlayerId == ownerPlayerId
                ? null
                : new WorldPropertyPermission
                {
                    PropertyId = property.Id,
                    PlayerId = creatorPlayerId
                };

            foreach (var permission in _catalog.GetPermissionsByPropertyType(type))
            {
                ownerPermissions.Permissions[permission] = true;
                ownerPermissions.GrantPermissions[permission] = true;

                if (creatorPermissions != null)
                {
                    creatorPermissions.Permissions[permission] = true;
                    creatorPermissions.GrantPermissions[permission] = true;
                }
            }

            _db.Set(property);
            _db.Set(ownerPermissions);
            if(creatorPermissions != null)
                _db.Set(creatorPermissions);

            if (propertyDetail.HasStorage)
            {
                // Create the default item storage categories and give permission to the owner for all categories.
                foreach (var category in CreateDefaultCategories(property.Id))
                {
                    _db.Set(category);

                    var categoryPermission = new WorldPropertyPermission
                    {
                        PropertyId = category.Id,
                        PlayerId = ownerPlayerId
                    };

                    foreach (var permission in _catalog.GetPermissionsByPropertyType(PropertyType.Category))
                    {
                        categoryPermission.Permissions[permission] = true;
                        categoryPermission.GrantPermissions[permission] = true;
                    }

                    _db.Set(categoryPermission);
                }
            }

            if (_loader.IsPropertyOnDemand(property))
            {
                _registry.SetPropertyLoadState(property.Id, PropertyLoadState.Unloaded);
            }
            else
            {
                _spawn.SpawnIntoWorld(property, targetArea);
            }

            Log.Write(LogGroup.Property, $"{GetName(creatorPlayer)} ({GetPCPlayerName(creatorPlayer)} / {GetPCPublicCDKey(creatorPlayer)}) placed {propertyDetail.Name}.");

            return property;
        }

        /// <summary>
        /// Creates a new apartment in the database for a given player.
        /// </summary>
        /// <param name="player">The player to associate the apartment with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <returns>The new world property.</returns>
        public WorldProperty CreateApartment(uint player, PropertyLayoutType layout)
        {
            var playerId = GetObjectUUID(player);
            var propertyName = "Apartment";
            return CreateProperty(player, playerId, propertyName, PropertyType.Apartment, layout, OBJECT_INVALID, property =>
            {
                property.Dates[PropertyDateType.Lease] = DateTime.UtcNow.AddDays(7);
            });
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
        public WorldProperty CreateStarship(
            uint player,
            PropertyLayoutType layout,
            PlanetType planetType,
            Location spaceLocation,
            Location landingLocation)
        {
            var spacePosition = GetPositionFromLocation(spaceLocation);
            var spaceOrientation = GetFacingFromLocation(spaceLocation);
            var spaceArea = GetAreaFromLocation(spaceLocation);
            var spaceAreaResref = GetResRef(spaceArea);

            var landingPosition = GetPositionFromLocation(landingLocation);
            var landingOrientation = GetFacingFromLocation(landingLocation);
            var landingArea = GetAreaFromLocation(landingLocation);
            var landingAreaResref = GetResRef(landingArea);
            var landingPropertyId = GetPropertyId(landingArea);

            // In the event the starport a ship is located at is destroyed or otherwise disappears,
            // we need to know the location of the planet's NPC starport so the ship can be returned there.
            // If we don't capture this correctly, the ship will be lost in limbo and the players won't be
            // able to access it.
            var planet = _planet.GetPlanetByType(planetType);
            var npcLandingWaypoint = GetWaypointByTag(planet.LandingWaypointTag);
            var npcLandingPosition = GetPosition(npcLandingWaypoint);
            var npcLandingOrientation = GetFacing(npcLandingWaypoint);
            var npcLandingArea = GetArea(npcLandingWaypoint);
            var npcLandingResref = GetResRef(npcLandingArea);
            var npcOrbitWaypoint = GetWaypointByTag(planet.SpaceOrbitWaypointTag);
            var npcOrbitPosition = GetPosition(npcOrbitWaypoint);
            var npcOrbitOrientation = GetFacing(npcOrbitWaypoint);
            var npcOrbitResref = GetResRef(GetArea(npcOrbitWaypoint));

            var playerId = GetObjectUUID(player);
            var propertyName = "Starship";

            return CreateProperty(player, playerId, propertyName, PropertyType.Starship, layout, OBJECT_INVALID, property =>
            {
                property.Positions[PropertyLocationType.LastNPCDockPosition] = new PropertyLocation
                {
                    X = npcLandingPosition.X,
                    Y = npcLandingPosition.Y,
                    Z = npcLandingPosition.Z,
                    Orientation = npcLandingOrientation,
                    AreaResref = npcLandingResref
                };

                property.Positions[PropertyLocationType.LastNPCSpacePosition] = new PropertyLocation
                {
                    X = npcOrbitPosition.X,
                    Y = npcOrbitPosition.Y,
                    Z = npcOrbitPosition.Z,
                    Orientation = npcOrbitOrientation,
                    AreaResref = npcOrbitResref
                };

                property.Positions[PropertyLocationType.DockPosition] = new PropertyLocation
                {
                    X = landingPosition.X,
                    Y = landingPosition.Y,
                    Z = landingPosition.Z,
                    Orientation = landingOrientation,
                    AreaResref = string.IsNullOrWhiteSpace(landingPropertyId) ? landingAreaResref : string.Empty,
                    InstancePropertyId = !string.IsNullOrWhiteSpace(landingPropertyId) ? landingPropertyId : string.Empty
                };

                property.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation
                {
                    X = spacePosition.X,
                    Y = spacePosition.Y,
                    Z = spacePosition.Z,
                    Orientation = spaceOrientation,
                    AreaResref = spaceAreaResref
                };
            });
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
        public void CreateCity(uint player, uint area, uint item, Location location)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var propertyName = "Player City";
            var now = DateTime.UtcNow;
            var city = CreateProperty(player, playerId, propertyName, PropertyType.City, PropertyLayoutType.City, area, property =>
            {
                property.ParentPropertyId = GetResRef(area);
                AssignPropertyId(area, property.Id);

                // Maintenance fees will be collected one week from now.
                property.Dates[PropertyDateType.Upkeep] = now.AddDays(7);

                // The first voting cycle will begin three weeks from now.
                property.Dates[PropertyDateType.ElectionStart] = now.AddDays(21);

                // The property will be cleaned up on the first server reset after 18 hours have passed
                // if there are less than the required number of citizens registered.
                property.Dates[PropertyDateType.BelowRequiredCitizens] = now.AddHours(18);

                // Initialize taxes at zero.
                property.Taxes[PropertyTaxType.Citizenship] = 0;
                property.Taxes[PropertyTaxType.Transportation] = 0;

                // Upgrades
                property.Upgrades[PropertyUpgradeType.CityLevel] = 1;
                property.Upgrades[PropertyUpgradeType.MedicalCenterLevel] = 1;
                property.Upgrades[PropertyUpgradeType.StarportLevel] = 1;
                property.Upgrades[PropertyUpgradeType.CantinaLevel] = 1;
            });

            CreateBuilding(
                player,
                item,
                city.Id,
                PropertyType.CityHall,
                PropertyLayoutType.CityHallStyle1,
                StructureType.CityHall,
                location);

            dbPlayer.CitizenPropertyId = city.Id;
            // A new citizenship must not inherit taxes from a previously deleted city.
            dbPlayer.PropertyOwedTaxes = 0;
            _db.Set(dbPlayer);

            Log.Write(LogGroup.Property, $"{GetName(player)} ({GetPCPlayerName(player)} / {GetPCPublicCDKey(player)}) founded a new city in {GetName(area)}.");
            _city.BroadcastPropertyEvent(
                city,
                "New Player City Founded",
                $"A new player city has been founded.\n" +
                $"**First Upkeep Check**: {_city.FormatUtcDate(city.Dates[PropertyDateType.Upkeep])}\n" +
                $"**First Election Opens**: {_city.FormatUtcDate(city.Dates[PropertyDateType.ElectionStart])}\n" +
                $"**Citizen Requirement Grace Ends**: {_city.FormatUtcDate(city.Dates[PropertyDateType.BelowRequiredCitizens])}");
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
        public void CreateBuilding(
            uint player,
            uint item,
            string parentCityId,
            PropertyType propertyType,
            PropertyLayoutType layout,
            StructureType structureType,
            Location location)
        {
            var layoutDetail = GetLayoutByType(layout);
            var propertyName = layoutDetail.Name;
            var city = _db.Get<WorldProperty>(parentCityId);

            // Hierarchy goes:
            //      City  (Top Level)
            //          -> Contains: Structure (buildings)
            //              -> Contains: Building interiors
            var buildingStructure = CreateStructure(parentCityId, item, structureType, location);

            var interior = CreateProperty(
                player,
                city.OwnerPlayerId,
                propertyName,
                propertyType,
                layout,
                OBJECT_INVALID,
                interiorProperty =>
            {
                interiorProperty.ParentPropertyId = buildingStructure.Id;
                interiorProperty.CustomName = buildingStructure.CustomName;
            });

            if (!buildingStructure.ChildPropertyIds.ContainsKey(PropertyChildType.Interior))
                buildingStructure.ChildPropertyIds[PropertyChildType.Interior] = new List<string>();

            buildingStructure.ChildPropertyIds[PropertyChildType.Interior].Add(interior.Id);
            _db.Set(buildingStructure);

            var buildingPlaceable = GetPlaceableByPropertyId(buildingStructure.Id);
            if (GetIsObjectValid(buildingPlaceable))
            {
                RunStructureChangedEvent(structureType, StructureChangeType.PositionChanged, buildingStructure, buildingPlaceable);
            }
        }

        /// <summary>
        /// Creates a structure inside a specific property.
        /// </summary>
        /// <param name="parentPropertyId">The parent property to associate this structure with.</param>
        /// <param name="item">The item used to spawn the structure.</param>
        /// <param name="type">The type of structure to spawn.</param>
        /// <param name="location">The location to spawn the structure at.</param>
        public WorldProperty CreateStructure(
            string parentPropertyId,
            uint item,
            StructureType type,
            Location location)
        {
            var structureDetail = GetStructureByType(type);
            var area = GetAreaFromLocation(location);
            var areaResref = GetResRef(area);
            var position = GetPositionFromLocation(location);
            var parentProperty = _db.Get<WorldProperty>(parentPropertyId);
            var structureItemStorage = structureDetail.ItemStorage;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.StructureBonus)
                    continue;

                structureItemStorage += GetItemPropertyCostTableValue(ip);
            }

            var structure = new WorldProperty
            {
                CustomName = structureDetail.Name,
                PropertyType = PropertyType.Structure,
                SerializedItem = ObjectPlugin.Serialize(item),
                OwnerPlayerId = parentProperty.OwnerPlayerId,
                ParentPropertyId = parentPropertyId,
                StructureType = type,
                ItemStorageCount = structureItemStorage
            };

            structure.Positions[PropertyLocationType.StaticPosition] = new PropertyLocation
            {
                AreaResref = areaResref,
                Orientation = 0.0f,
                X = position.X,
                Y = position.Y,
                Z = position.Z
            };

            if (!parentProperty.ChildPropertyIds.ContainsKey(PropertyChildType.Structure))
                parentProperty.ChildPropertyIds[PropertyChildType.Structure] = new List<string>();

            parentProperty.ChildPropertyIds[PropertyChildType.Structure].Add(structure.Id);
            parentProperty.ItemStorageCount += structureItemStorage;

            _db.Set(structure);
            _db.Set(parentProperty);

            // Now spawn it within the game world.
            var placeable = CreateObject(ObjectType.Placeable, structureDetail.Resref, location);
            SetPlotFlag(placeable, true);
            AssignPropertyId(placeable, structure.Id);

            _registry.RegisterStructurePlaceable(structure.Id, placeable);

            DestroyObject(item);
            RunStructureChangedEvent(type, StructureChangeType.PositionChanged, structure, placeable);

            return structure;
        }

        /// <inheritdoc />
        public PropertyLayout GetLayoutByType(PropertyLayoutType type)
        {
            return _catalog.GetLayoutByType(type);
        }

        /// <inheritdoc />
        public StructureAttribute GetStructureByType(StructureType structure)
        {
            return _catalog.GetStructureByType(structure);
        }

        /// <inheritdoc />
        public List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type)
        {
            return _catalog.GetAllLayoutsByPropertyType(type);
        }

        /// <inheritdoc />
        public Vector4 GetEntrancePosition(PropertyLayoutType type)
        {
            return _catalog.GetEntrancePosition(type);
        }

        /// <inheritdoc />
        public uint GetInstanceTemplate(PropertyLayoutType type)
        {
            return _catalog.GetInstanceTemplate(type);
        }

        /// <inheritdoc />
        public PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission)
        {
            return _catalog.GetPermissionByType(permission);
        }

        /// <inheritdoc />
        public List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type)
        {
            return _catalog.GetPermissionsByPropertyType(type);
        }

        /// <inheritdoc />
        public PropertyTypeAttribute GetPropertyDetail(PropertyType type)
        {
            return _catalog.GetPropertyDetail(type);
        }

        /// <inheritdoc />
        public uint GetPlaceableByPropertyId(string propertyId)
        {
            return _registry.GetPlaceableByPropertyId(propertyId);
        }

        /// <inheritdoc />
        public List<StructureType> GetStructuresByInteriorPropertyType(PropertyType propertyType)
        {
            return _catalog.GetStructuresByInteriorPropertyType(propertyType);
        }

        /// <inheritdoc />
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public void EnterPropertyInstance()
        {
            _registry.EnterPropertyInstance();
        }

        /// <inheritdoc />
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public void ShowPropertyDescription()
        {
            _registry.ShowPropertyDescription();
        }

        /// <inheritdoc />
        public void ApplyStructureWorldName(uint placeable, WorldProperty structure)
        {
            _spawn.ApplyStructureWorldName(placeable, structure);
        }

        /// <inheritdoc />
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void ExitPropertyInstance()
        {
            _registry.ExitPropertyInstance();
        }

        /// <inheritdoc />
        public void RunStructureChangedEvent(StructureType structureType, StructureChangeType changeType, WorldProperty property, uint placeable)
        {
            _catalog.RunStructureChangedEvent(structureType, changeType, property, placeable);
        }

        /// <inheritdoc />
        public string GetCityLevelName(int level)
        {
            return _city.GetCityLevelName(level);
        }

        /// <inheritdoc />
        public int GetCitizensRequiredForNextCityLevel(int level)
        {
            return _city.GetCitizensRequiredForNextCityLevel(level);
        }

        /// <inheritdoc />
        public int GetEffectiveUpgradeLevel(string cityId, PropertyUpgradeType upgradeType)
        {
            return _city.GetEffectiveUpgradeLevel(cityId, upgradeType);
        }

    }
}
