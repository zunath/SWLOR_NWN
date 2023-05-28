using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    public static class Property
    {
        private static readonly Dictionary<StructureType, StructureAttribute> _activeStructures = new();
        private static readonly Dictionary<PropertyType, PropertyTypeAttribute> _propertyTypes = new();
        private static readonly Dictionary<PropertyLayoutType, PropertyLayout> _activeLayouts = new();
        private static readonly Dictionary<PropertyType, List<PropertyLayoutType>> _layoutsByPropertyType = new();
        private static readonly Dictionary<PropertyLayoutType, Vector4> _entrancesByLayout = new();
        private static readonly Dictionary<PropertyPermissionType, PropertyPermissionAttribute> _activePermissions = new();

        private static readonly Dictionary<string, uint> _instanceTemplates = new();
        private static readonly Dictionary<string, PropertyInstance> _propertyInstances = new();
        private static readonly Dictionary<PropertyType, List<PropertyPermissionType>> _permissionsByPropertyType = new();

        private static readonly Dictionary<string, uint> _structurePropertyIdToPlaceable = new();
        private static readonly Dictionary<StructureType, Dictionary<StructureChangeType, Action<WorldProperty, uint>>> _structureChangedActions = StructureChangedAction.BuildSpawnActions();

        private static readonly Dictionary<int, int> _citizensRequired = new()
        {
            { 1, 5 }, // Level 1 requires a minimum of 5 citizens
            { 2, 10 }, // Level 2 requires a minimum of 10 citizens
            { 3, 15 }, // Level 3 requires a minimum of 15 citizens
            { 4, 20 }, // Level 4 requires a minimum of 20 citizens
            { 5, 25 }  // Level 5 requires a minimum of 25 citizens
        };

        private static readonly Dictionary<PropertyType, List<StructureType>> _structureTypesByPropertyType = new();

        /// <summary>
        /// Determines the number of hours before the city will be destroyed due to
        /// lack of citizens. This starts at the time of city hall placement for the initial check.
        /// At boot time, if the number of citizens is below the required amount, the player will have 18 hours
        /// to rectify it. Failure to do so will result in the city being lost upon the next server reboot.
        /// Note: Due to the cleanup occurring on server boot, which occurs once every 24 hours,
        ///       it's possible the player will have more time than the value specified here.
        ///       This is expected.
        /// </summary>
        public const int MinimumCitizensGracePeriodHours = 18;

        /// <summary>
        /// Determines the number of days citizens have to register for an election.
        /// </summary>
        public const int ElectionRegistrationDays = 14;

        /// <summary>
        /// Determines the number of days all citizens have to vote for an election.
        /// </summary>
        public const int ElectionVotingDays = 7;

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CachePropertyTypes();
            CachePropertyLayoutTypes();
            CachePermissions();
            CacheStructures();
            CacheInstanceTemplates();
            CacheStructuresByPropertyType();
        }

        /// <summary>
        /// When the module loads, clean up any deleted data, refreshes permissions and then load properties.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void OnModuleLoad()
        {
            RefreshPermissions();
            ProcessCities();
            CleanUpData();
            LoadProperties();
        }

        private static void CachePropertyTypes()
        {
            var propertyTypes = Enum.GetValues(typeof(PropertyType)).Cast<PropertyType>();
            foreach (var type in propertyTypes)
            {
                var detail = type.GetAttribute<PropertyType, PropertyTypeAttribute>();
                _propertyTypes[type] = detail;
            }
        }

        private static void CachePropertyLayoutTypes()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IPropertyLayoutListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IPropertyLayoutListDefinition)Activator.CreateInstance(type);
                var layouts = instance.Build();

                foreach (var (layoutType, layout) in layouts)
                {
                    _activeLayouts[layoutType] = layout;

                    if (!_layoutsByPropertyType.ContainsKey(layout.PropertyType))
                        _layoutsByPropertyType[layout.PropertyType] = new List<PropertyLayoutType>();

                    _layoutsByPropertyType[layout.PropertyType].Add(layoutType);
                    _entrancesByLayout[layoutType] = GetEntrancePosition(layout.AreaInstanceResref);
                }
            }

            Console.WriteLine($"Loaded {_activeLayouts.Count} property layouts.");
        }

        private static void CachePermissions()
        {
            var permissionTypes = Enum.GetValues(typeof(PropertyPermissionType)).Cast<PropertyPermissionType>();
            foreach (var type in permissionTypes)
            {
                var permission = type.GetAttribute<PropertyPermissionType, PropertyPermissionAttribute>();

                if (permission.IsActive)
                {
                    _activePermissions[type] = permission;
                }
            }

            // Assign the list of permissions associated with each property type.
            _permissionsByPropertyType[PropertyType.Apartment] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ExtendLease,
                PropertyPermissionType.CancelLease,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories
            };

            _permissionsByPropertyType[PropertyType.CityHall] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription
            };

            _permissionsByPropertyType[PropertyType.Starship] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories,
                PropertyPermissionType.PilotShip,
                PropertyPermissionType.RefitShip
            };

            _permissionsByPropertyType[PropertyType.City] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditTaxes,
                PropertyPermissionType.AccessTreasury,
                PropertyPermissionType.ManageUpgrades,
                PropertyPermissionType.ManageUpkeep
            };

            _permissionsByPropertyType[PropertyType.Category] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.AccessStorage
            };

            _permissionsByPropertyType[PropertyType.Bank] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories,
            };

            _permissionsByPropertyType[PropertyType.MedicalCenter] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.Starport] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.Cantina] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.House] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.EditCategories,
            };
        }

        /// <summary>
        /// When the module loads, read all structure types and store them into the cache.
        /// </summary>
        private static void CacheStructures()
        {
            var structureTypes = Enum.GetValues(typeof(StructureType)).Cast<StructureType>();
            foreach (var structure in structureTypes)
            {
                var detail = structure.GetAttribute<StructureType, StructureAttribute>();

                if (detail.IsActive)
                {
                    _activeStructures[structure] = detail;
                }
            }
        }

        /// <summary>
        /// When the module loads, iterate over all areas and cache any that are instance templates.
        /// </summary>
        private static void CacheInstanceTemplates()
        {
            var templateResrefs = _activeLayouts
                .Select(x => x.Value.AreaInstanceResref)
                .ToList();

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var resref = GetResRef(area);
                if (templateResrefs.Contains(resref))
                    _instanceTemplates[resref] = area;
            }
        }

        /// <summary>
        /// When the module loads, link structures back to their property types.
        /// </summary>
        private static void CacheStructuresByPropertyType()
        {
            foreach (var (structureType, detail) in _activeStructures)
            {
                if (detail.LayoutType == PropertyLayoutType.Invalid)
                    continue;

                var layout = GetLayoutByType(detail.LayoutType);
                if (!_structureTypesByPropertyType.ContainsKey(layout.PropertyType))
                    _structureTypesByPropertyType[layout.PropertyType] = new List<StructureType>();

                _structureTypesByPropertyType[layout.PropertyType].Add(structureType);
            }
        }

        /// <summary>
        /// Iterates over all areas to find the matching instance assigned to the specified resref.
        /// Then, the entrance waypoint is located and its coordinates are stored into cache.
        /// </summary>
        /// <param name="areaResref">The resref of the area to look for</param>
        /// <returns>X, Y, and Z coordinates of the entrance location</returns>
        private static Vector4 GetEntrancePosition(string areaResref)
        {
            var area = Area.GetAreaByResref(areaResref);
            
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) != "PROPERTY_ENTRANCE") continue;

                var position = GetPosition(obj);
                return new Vector4(position, GetFacing(obj));
            }
            
            return new Vector4();
        }

        /// <summary>
        /// Assigns a property Id as a local variable to a specific object.
        /// </summary>
        /// <param name="obj">The object to assign</param>
        /// <param name="propertyId">The property Id to assign.</param>
        public static void AssignPropertyId(uint obj, string propertyId)
        {
            SetLocalString(obj, "PROPERTY_ID", propertyId);
        }

        /// <summary>
        /// Retrieves the assigned property Id assigned to a specific object.
        /// Returns an empty string if not found.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>The property Id or an empty string if not found.</returns>
        public static string GetPropertyId(uint obj)
        {
            return GetLocalString(obj, "PROPERTY_ID");
        }

        /// <summary>
        /// Registers an area instance to a given property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        /// <param name="instance">The area instance to register</param>
        public static void RegisterInstance(string propertyId, uint instance, PropertyLayoutType layoutType)
        {
            AssignPropertyId(instance, propertyId);
            _propertyInstances[propertyId] = new PropertyInstance(instance, layoutType);
        }

        /// <summary>
        /// Retrieves the instanced area associated with a specific property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        /// <returns>An area associated with the property Id.</returns>
        public static PropertyInstance GetRegisteredInstance(string propertyId)
        {
            return _propertyInstances[propertyId];
        }
        
        /// <summary>
        /// When the module loads, remove all data marked for deletion and any properties with expired leases.
        /// </summary>
        private static void CleanUpData()
        {
            var now = DateTime.UtcNow;

            // Mark any properties with expired leases as queued for deletion. They will be picked up on the last
            // step of this method.
            var propertyTypesWithLeases = new[]
            {
                (int)PropertyType.Apartment,
            };
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), propertyTypesWithLeases);
            var queryCount = (int)DB.SearchCount(query);
            var properties = DB.Search(query.AddPaging(queryCount, 0));

            foreach (var property in properties)
            {
                var lease = property.Dates[PropertyDateType.Lease];
                if (lease <= now)
                {
                    Log.Write(LogGroup.Property, $"Property '{property.CustomName}' has an expired lease. Expired on: {lease.ToString("G")}");

                    property.IsQueuedForDeletion = true;
                    DB.Set(property);
                }
            }

            // Remove any properties queued for deletion.
            query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), true);
            queryCount = (int)DB.SearchCount(query);
            properties = DB.Search(query.AddPaging(queryCount, 0));

            foreach (var property in properties)
            {
                Log.Write(LogGroup.Property, $"Property '{property.CustomName}' scheduled for deletion. Peforming delete now.");
                DeleteProperty(property);
            }

            // Starship properties should have their current location wiped on every boot.
            // This ensures the player's ship doesn't get lost in space when they're thrown out of an instance.
            var starshipQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Starship);
            var starshipCount = (int)DB.SearchCount(starshipQuery);
            var starshipProperties = DB.Search(starshipQuery.AddPaging(starshipCount, 0));

            foreach (var property in starshipProperties)
            {
                if (property.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
                {
                    property.Positions.Remove(PropertyLocationType.CurrentPosition);
                }

                // In the event this starship was docked at a deleted player starport,
                // it needs to be relocated to the NPC starport found on the planet
                var dockPosition = property.Positions[PropertyLocationType.DockPosition];
                if (!string.IsNullOrWhiteSpace(dockPosition.InstancePropertyId))
                {
                    var dbStarport = DB.Get<WorldProperty>(dockPosition.InstancePropertyId);
                    if (dbStarport == null)
                    {
                        // The PC starport no longer exists (probably destroyed by the previous cleanup)
                        // Luckily we track the last NPC starport they visited so we can simply replace
                        // their docked position with it.
                        property.Positions[PropertyLocationType.DockPosition] = property.Positions[PropertyLocationType.LastNPCDockPosition];

                        Log.Write(LogGroup.Property, $"Starship '{property.CustomName}' ({property.Id}) was docked at a non-existent player starport. It has been relocated to the last NPC dock position at '{property.Positions[PropertyLocationType.LastNPCDockPosition].AreaResref}'.");
                    }
                }
                
                DB.Set(property);
            }
        }

        /// <summary>
        /// Handles the processing of each individual city. The following will occur on each boot:
        /// 1.) Checking for the required number of citizens.
        ///     If this is the first reboot where they're under the requirement, 18 hours will be given to them for correction.
        ///     Otherwise if the timer has expired, the city will be destroyed.
        /// 2.) Processing elections. Moves the election process between stages and calculates votes at the end of the process.
        ///     If a new mayor is elected, the previous mayor's permissions are removed and given to the new one.
        /// </summary>
        private static void ProcessCities()
        {
            var cityQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            var queryCount = (int)DB.SearchCount(cityQuery);
            var cities = DB.Search(cityQuery.AddPaging(queryCount, 0));
            var now = DateTime.UtcNow;

            foreach (var city in cities)
            {
                ProcessCityCitizenRequirement(now, city);
                ProcessCityElections(now, city);

                if (now < city.Dates[PropertyDateType.Upkeep])
                {
                    Log.Write(LogGroup.Property, $"City '{city.CustomName}' ({city.Id}) upkeep isn't ready yet.");
                    continue;
                }
                else
                {
                    ProcessCityLevel(city);
                    ProcessUpkeep(now, city);
                    ProcessCitizenshipFees(city);

                    // Next upkeep check should be 7 days from the previous one.
                    city.Dates[PropertyDateType.Upkeep] = city.Dates[PropertyDateType.Upkeep].AddDays(7);
                }

                DB.Set(city);
            }
        }

        private static void ProcessCityCitizenRequirement(DateTime now, WorldProperty city)
        {
            var citizenQuery = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            var citizenCount = (int)DB.SearchCount(citizenQuery);
            var citizens = DB.Search(citizenQuery.AddPaging(citizenCount, 0))
                .ToList();

            // City is below the number of citizens required to maintain the city.
            if (citizenCount < _citizensRequired[1])
            {
                if (city.Dates.ContainsKey(PropertyDateType.BelowRequiredCitizens))
                {
                    // The amount of time has elapsed. It's time to delete the city.
                    if (now >= city.Dates[PropertyDateType.BelowRequiredCitizens])
                    {
                        city.IsQueuedForDeletion = true;

                        Log.Write(LogGroup.Property, $"City '{city.CustomName}' in area '{city.ParentPropertyId}' has been queued for deletion because it fell under the required {_citizensRequired[1]} citizens needed to maintain it.");
                    }
                    else
                    {
                        Log.Write(LogGroup.Property, $"City '{city.CustomName}' in area '{city.ParentPropertyId}' is below the required citizen count but time has not expired. Next check will occur on the next server reboot.");
                    }
                }
                // This is the first restart where the city is below the required amount.
                else
                {
                    city.Dates[PropertyDateType.BelowRequiredCitizens] = now.AddHours(MinimumCitizensGracePeriodHours);

                    Log.Write(LogGroup.Property, $"City '{city.CustomName}' has fallen below the required {_citizensRequired[1]} citizens required to maintain a city. An expiration has been applied");
                }
            }
            // Otherwise they're at or above the required amount. Ensure the date is removed from the property.
            else
            {
                if (city.Dates.ContainsKey(PropertyDateType.BelowRequiredCitizens))
                {
                    city.Dates.Remove(PropertyDateType.BelowRequiredCitizens);
                }
            }

            DB.Set(city);
        }

        private static void ProcessCityElections(DateTime now, WorldProperty city)
        {
            var incumbentMayorId = city.OwnerPlayerId;

            void TransferPermissions(string winnerPlayerId)
            {
                var mayorPermission = DB.Search(new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), incumbentMayorId, false)
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), city.Id, false))
                    .Single();

                DB.Delete<WorldPropertyPermission>(mayorPermission.Id);
                
                var winnerPermission = DB.Search(new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), winnerPlayerId, false)
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), city.Id, false))
                    .SingleOrDefault() ?? new WorldPropertyPermission
                {
                    PlayerId = winnerPlayerId,
                    PropertyId = city.Id
                };

                foreach (var permission in _permissionsByPropertyType[PropertyType.City])
                {
                    winnerPermission.Permissions[permission] = true;
                    winnerPermission.GrantPermissions[permission] = true;
                }

                DB.Set(winnerPermission);
            }

            // No reason to process deleted cities. Skip.
            if (city.IsQueuedForDeletion) return;

            Log.Write(LogGroup.Property, $"Election process starting for city {city.CustomName} ({city.Id})");
            var election = DB.Search(new DBQuery<Election>()
                .AddFieldSearch(nameof(Election.PropertyId), city.Id, false))
                .SingleOrDefault();

            // Election hasn't started yet.
            if (election == null)
            {
                // It's time to start a new election.
                if (now >= city.Dates[PropertyDateType.ElectionStart])
                {
                    election = new Election
                    {
                        PropertyId = city.Id,
                        Stage = ElectionStageType.Registration
                    };

                    DB.Set(election);
                }
            }
            // Election has started. See if it's time to progress to the next stage.
            else
            {
                var registrationCutOff = city.Dates[PropertyDateType.ElectionStart].AddDays(ElectionRegistrationDays);
                var votingCutOff = registrationCutOff.AddDays(ElectionVotingDays);

                // We're past the voting stage.
                // Count up the votes for each candidate and determine the winner.
                // Adjust mayors if incumbent lost.
                if (now >= votingCutOff)
                {
                    var votes = new Dictionary<string, int>();
                    foreach (var voter in election.VoterSelections.Values)
                    {
                        if (!votes.ContainsKey(voter.CandidatePlayerId))
                            votes[voter.CandidatePlayerId] = 0;

                        votes[voter.CandidatePlayerId]++;
                    }

                    var orderedVotes = votes.OrderByDescending(x => x.Value).ToList();

                    // Nobody voted at all. Incumbent stays in power.
                    if (orderedVotes.Count <= 0)
                    {
                        Log.Write(LogGroup.Property, $"No one voted. Incumbent mayor '{incumbentMayorId}' stays in power.");
                    }
                    // If top two are the same, incumbent mayor wins.
                    else if (orderedVotes.Count >= 2 && orderedVotes.ElementAt(0).Value != orderedVotes.ElementAt(1).Value)
                    {
                        Log.Write(LogGroup.Property, $"Top 2 candidates were tied. Incumbent mayor '{incumbentMayorId}' wins the election.");
                    }
                    // Otherwise, take the person with the highest votes.
                    else 
                    {
                        var winnerPlayerId = orderedVotes.ElementAt(0).Key;
                        TransferPermissions(winnerPlayerId);
                        city.OwnerPlayerId = winnerPlayerId;
                        Log.Write(LogGroup.Property, $"New mayor of {city.CustomName} is '{winnerPlayerId}'");
                    }

                    Log.Write(LogGroup.Property, $"Vote Counts:");
                    foreach (var (candidatePlayerId, voteCount) in orderedVotes)
                    {
                        Log.Write(LogGroup.Property, $"{candidatePlayerId}: {voteCount} votes");
                    }

                    // The next election should occur in 3 weeks from the end of this election.
                    // To avoid timing issues between server restarts, we use the start of this election as a reference point.
                    city.Dates[PropertyDateType.ElectionStart] = city.Dates[PropertyDateType.ElectionStart]
                        .AddDays(ElectionRegistrationDays + ElectionVotingDays) // This gets us to the end of the election
                        .AddDays(21); // Then add another 3 weeks.

                    DB.Set(city);
                    DB.Delete<Election>(election.Id);
                }
                // Registration cut-off has passed. 
                // If no one has registered, the incumbent mayor wins by default.
                // If only one person has registered, they become mayor without proceeding to the voting stage.
                //     -> If this was a different person from the incumbent, they receive mayor power immediately.
                // Otherwise, the election proceeds to the voting stage.
                else if (now >= registrationCutOff)
                {
                    // In the event that absolutely no one ran for election,
                    // the incumbent mayor stays in power and another election is scheduled
                    // three weeks from now. 
                    if (election.CandidatePlayerIds.Count <= 0)
                    {
                        city.Dates[PropertyDateType.ElectionStart] = city.Dates[PropertyDateType.ElectionStart]
                            .AddDays(ElectionRegistrationDays) // This gets us to the end of the registration period
                            .AddDays(21); // Then add another 3 weeks.

                        DB.Set(city);

                        DB.Delete<Election>(election.Id);
                        Log.Write(LogGroup.Property, $"No one ran for this election. Existing mayor '{incumbentMayorId}' wins by default.");
                    }
                    // In the event only one person ran for election, they automatically win
                    // and the power shift occurs immediately. Another election is scheduled
                    // three weeks from now.
                    else if(election.CandidatePlayerIds.Count == 1)
                    {
                        var winnerPlayerId = election.CandidatePlayerIds[0];

                        // The winner was the incumbent mayor. No changes are needed.
                        if (winnerPlayerId == incumbentMayorId)
                        {
                            Log.Write(LogGroup.Property, $"Incumbent mayor '{incumbentMayorId}' ran unopposed. They retain mayor status.");
                        }
                        // Someone new won. Transfer mayor permissions over to the new player.
                        else
                        {
                            city.OwnerPlayerId = winnerPlayerId;
                            TransferPermissions(winnerPlayerId);
                            Log.Write(LogGroup.Property, $"Only one person '{winnerPlayerId}' ran for mayor. They win by default.");
                        }

                        city.Dates[PropertyDateType.ElectionStart] = city.Dates[PropertyDateType.ElectionStart]
                            .AddDays(ElectionRegistrationDays) // This gets us to the end of the registration period
                            .AddDays(21); // Then add another 3 weeks.

                        DB.Set(city);
                        DB.Delete<Election>(election.Id);
                    }
                    // We're past the registration window. Move into the voting stage.
                    else
                    {
                        election.Stage = ElectionStageType.Voting;

                        DB.Set(election);
                        Log.Write(LogGroup.Property, $"City '{city.CustomName}' ({city.Id}) has progressed into the Voting stage of the election.");
                    }

                }
            }
        }

        private static void ProcessCityLevel(WorldProperty city)
        {
            Log.Write(LogGroup.Property, $"Processing city level for '{city.CustomName}' ({city.Id})...");

            var mayor = DB.Get<Player>(city.OwnerPlayerId);
            var citizenCount = DB.SearchCount(new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false));
            var currentLevel = city.Upgrades[PropertyUpgradeType.CityLevel];
            var mayorLevel = mayor.Perks.ContainsKey(PerkType.CityManagement)
                ? mayor.Perks[PerkType.CityManagement] + 1
                : 0;
            
            // Mayor's perk level has fallen below the city level.
            if (mayorLevel < currentLevel)
            {
                currentLevel = mayorLevel;
                Log.Write(LogGroup.Property, $"City level reduced to {currentLevel} because mayor's perk level is {mayorLevel}");
            }

            var maxLevelThisCycle = 1;
            for (var level = 1; level <= 5; level++)
            {
                // Mayor can't support a higher city level.
                if (level > mayorLevel)
                {
                    Log.Write(LogGroup.Property, $"Mayor cannot support city level {level} or higher.");
                    break;
                }

                // The citizen count requirements are met
                if (citizenCount >= _citizensRequired[level])
                {
                    maxLevelThisCycle = level;
                    Log.Write(LogGroup.Property, $"Meets citizen requirement for level {level} (Required amount: {_citizensRequired[level]})");
                }
            }

            // Current level is higher than max level this cycle. Drop it down that value.
            if (currentLevel > maxLevelThisCycle)
            {
                currentLevel = maxLevelThisCycle;
                Log.Write(LogGroup.Property, $"City level dropped to {maxLevelThisCycle}");
            }

            // Upkeep hasn't been paid. City isn't eligible to increase in level.
            if (city.Upkeep > 0)
            {
                Log.Write(LogGroup.Property, $"Unable to upgrade city because upkeep hasn't been fully paid.");
            }
            // The city can increase in level and upkeep has been paid. Perform the upgrade now.
            else if (currentLevel < maxLevelThisCycle)
            {
                currentLevel++;
                Log.Write(LogGroup.Property, $"City increased by one level this cycle.");
            }

            Log.Write(LogGroup.Property, $"City level changed to {currentLevel} from {city.Upgrades[PropertyUpgradeType.CityLevel]}");
            city.Upgrades[PropertyUpgradeType.CityLevel] = currentLevel;
            DB.Set(city);

            Log.Write(LogGroup.Property, $"Finished processing city level for '{city.CustomName}' ({city.Id})");
        }

        private static void ProcessUpkeep(DateTime now, WorldProperty city)
        {
            Log.Write(LogGroup.Property, $"Processing city '{city.CustomName}' ({city.Id}) upkeep...");
            
            // If upkeep wasn't fully paid for this week, process the destruction date
            if (city.Upkeep > 0)
            {
                Log.Write(LogGroup.Property, $"City upkeep was not paid for the past week.");

                // This is a consecutive week in which upkeep wasn't paid. Check if it's time to destroy the city.
                if (city.Dates.ContainsKey(PropertyDateType.DisrepairDestruction))
                {
                    if (now >= city.Dates[PropertyDateType.DisrepairDestruction])
                    {
                        Log.Write(LogGroup.Property, $"City upkeep was not paid for 30 days. City is marked for destruction.");
                        city.IsQueuedForDeletion = true;
                    }
                }
                else
                {
                    city.Dates[PropertyDateType.DisrepairDestruction] = now.AddDays(30);
                    Log.Write(LogGroup.Property, $"This is the first week upkeep wasn't paid. Destruction will occur on {city.Dates[PropertyDateType.DisrepairDestruction]:yyyy-MM-dd hh:mm:ss}");
                }

            }
            // Otherwise upkeep has been paid. Remove the disrepair destruction date if it exists.
            else
            {
                if (city.Dates.ContainsKey(PropertyDateType.DisrepairDestruction))
                {
                    city.Dates.Remove(PropertyDateType.DisrepairDestruction);
                    Log.Write(LogGroup.Property, $"City upkeep was paid. Removing destruction date.");
                }
            }
            
            // Calculate new upkeep price for this week.
            var dbMayor = DB.Get<Player>(city.OwnerPlayerId);
            var upkeepReductionPercent = dbMayor.Perks.ContainsKey(PerkType.Upkeep)
                ? dbMayor.Perks[PerkType.Upkeep] * 0.05f
                : 0;
            var layout = GetLayoutByType(city.Layout);
            const int UpgradeBasePrice = 10000;
            var basePrice = layout.PricePerDay * 7;
            basePrice -= (int)(basePrice * upkeepReductionPercent);

            var upgradePrice = 
                (city.Upgrades[PropertyUpgradeType.BankLevel] - 1) * UpgradeBasePrice +
                (city.Upgrades[PropertyUpgradeType.MedicalCenterLevel] - 1) * UpgradeBasePrice +
                (city.Upgrades[PropertyUpgradeType.StarportLevel] - 1) * UpgradeBasePrice +
                (city.Upgrades[PropertyUpgradeType.CantinaLevel] - 1) * UpgradeBasePrice;

            Log.Write(LogGroup.Property, $"Weekly upkeep calcuated to be: {basePrice + upgradePrice} credits.");
            city.Upkeep += basePrice + upgradePrice;
            DB.Set(city);
            Log.Write(LogGroup.Property, $"Total upkeep owed: {city.Upkeep} credits.");

            Log.Write(LogGroup.Property, $"Finished processing city upkeep for '{city.CustomName}' ({city.Id})");
        }

        private static void ProcessCitizenshipFees(WorldProperty city)
        {
            Log.Write(LogGroup.Property, $"Processing citizenship fees for '{city.CustomName}' ({city.Id})");

            var citizenQuery = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), city.Id, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            var citizenCount = (int)DB.SearchCount(citizenQuery);
            var citizens = DB.Search(citizenQuery.AddPaging(citizenCount, 0))
                .ToList();

            foreach (var citizen in citizens)
            {
                citizen.PropertyOwedTaxes += city.Taxes[PropertyTaxType.Citizenship];
                Log.Write(LogGroup.Property, $"Citizen '{citizen.Name}' owes an additional {city.Taxes[PropertyTaxType.Citizenship]} credits for a total of {citizen.PropertyOwedTaxes} credits");
                DB.Set(citizen);
            }

            Log.Write(LogGroup.Property, $"Finished processing citizenship fees for '{city.CustomName}' ({city.Id})");
        }

        private static void DeleteProperty(WorldProperty property)
        {
            // Recursively clear any children properties tied to this property.
            foreach (var (childType, propertyIds) in property.ChildPropertyIds)
            {
                if (childType == PropertyChildType.RegisteredStarport ||
                    childType == PropertyChildType.Starship)
                    continue;

                if (propertyIds.Count > 0)
                {
                    var query = new DBQuery<WorldProperty>()
                        .AddFieldSearch(nameof(WorldProperty.Id), propertyIds);
                    var queryCount = (int)DB.SearchCount(query);
                    var children = DB.Search(query.AddPaging(queryCount, 0));

                    foreach (var child in children)
                    {
                        DeleteProperty(child);
                    }
                }
            }

            // Clear permissions for the property.
            var permissionsQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), property.Id, false);
            var permissionsCount = (int)DB.SearchCount(permissionsQuery);
            var permissions = DB.Search(permissionsQuery.AddPaging(permissionsCount, 0));

            foreach (var permission in permissions)
            {
                DB.Delete<WorldPropertyPermission>(permission.Id);
                Log.Write(LogGroup.Property, $"Deleted property permission for property '{permission.PropertyId}' and player '{permission.PlayerId}'.");
            }

            // Clear item categories and their permissions
            var categoriesQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), property.Id, false);
            var categoriesCount = (int)DB.SearchCount(categoriesQuery);
            var categories = DB.Search(categoriesQuery.AddPaging(categoriesCount, 0))
                .ToList();
            var categoryPropertyIds = categories.Select(s => s.Id).ToList();

            // Clear any permissions tied to categories.
            if (categoryPropertyIds.Count > 0)
            {
                permissionsQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryPropertyIds);
                permissionsCount = (int)DB.SearchCount(permissionsQuery);
                permissions = DB.Search(permissionsQuery.AddPaging(permissionsCount, 0))
                    .ToList();

                foreach (var permission in permissions)
                {
                    DB.Delete<WorldPropertyPermission>(permission.Id);
                    Log.Write(LogGroup.Property, $"Deleted property permission for category '{permission.PropertyId}'.");
                }
            }

            // Clear the actual categories (and any associated items)
            foreach (var category in categories)
            {
                DB.Delete<WorldPropertyCategory>(category.Id);
                Log.Write(LogGroup.Property, $"Deleted property category '{category.Name}', id: '{category.Id}' from property '{category.ParentPropertyId}'");
            }

            // Clear any citizenship assignments on players who may be citizens of this property.
            var citizenQuery = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), property.Id, false);
            var citizenCount = (int)DB.SearchCount(citizenQuery);
            var citizens = DB.Search(citizenQuery.AddPaging(citizenCount, 0));

            foreach (var citizen in citizens)
            {
                Log.Write(LogGroup.Property, $"Citizenship revoked for player '{citizen.Name}' ({citizen.Id}) on property '{property.CustomName}' ({property.Id})");
                citizen.CitizenPropertyId = string.Empty;
                DB.Set(citizen);
            }

            // Clear any bank items stored within this city.
            var bankQuery = new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.StorageId), property.Id, false);
            var bankCount = (int)DB.SearchCount(bankQuery);
            var dbBankItems = DB.Search(bankQuery.AddPaging(bankCount, 0));

            foreach (var item in dbBankItems)
            {
                DB.Delete<InventoryItem>(item.Id);
                Log.Write(LogGroup.Property, $"Deleted bank item '{item.Quantity}x {item.Name}' ({item.Tag} / {item.Resref}) from property '{property.Id}' which was stored by {item.PlayerId}");
            }

            // Finally delete the entire property.
            DB.Delete<WorldProperty>(property.Id);
            Log.Write(LogGroup.Property, $"Property '{property.CustomName}' deleted.");
        }

        /// <summary>
        /// When the module loads, update the permissions list on all properties to reflect any changes.
        /// </summary>
        private static void RefreshPermissions()
        {
            foreach (var (type, permissions) in _permissionsByPropertyType)
            {
                var propertyQuery = new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)type);
                var propertyCount = (int)DB.SearchCount(propertyQuery);
                var dbProperties = DB.Search(propertyQuery.AddPaging(propertyCount, 0))
                    .ToList();

                foreach (var property in dbProperties)
                {
                    var permissionQuery = new DBQuery<WorldPropertyPermission>()
                        .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), property.Id, false);
                    var permissionCount = (int)DB.SearchCount(permissionQuery);
                    var dbPropertyPermissions = DB.Search(permissionQuery.AddPaging(permissionCount, 0))
                        .ToList();
                    
                    foreach (var propertyPermission in dbPropertyPermissions)
                    {
                        // Perform a refresh of permissions (adding/removing as needed)
                        // If changes occurred, save them.
                        if (RefreshPermissions(property.OwnerPlayerId, propertyPermission, permissions))
                        {
                            DB.Set(propertyPermission);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Modifies the permissions on an individual world property permission object.
        /// </summary>
        /// <param name="propertyOwnerId">The Id of the property owner.</param>
        /// <param name="dbPermission">The permission to modify.</param>
        /// <param name="masterList">The master list of permissions.</param>
        private static bool RefreshPermissions(string propertyOwnerId, WorldPropertyPermission dbPermission, List<PropertyPermissionType> masterList)
        {
            var hasChanges = false;
            // Remove any permissions that have been removed from the master list.
            for (var index = dbPermission.Permissions.Count - 1; index >= 0; index--)
            {
                // Permission no longer exists in master list. Remove it from this instance.
                var permission = dbPermission.Permissions.ElementAt(index).Key;
                if (!masterList.Contains(permission))
                {
                    dbPermission.Permissions.Remove(permission);
                    Log.Write(LogGroup.Property, $"Removing permission {permission} from property {dbPermission.PropertyId} for player Id {dbPermission.PlayerId}.");
                    hasChanges = true;
                }
            }
            for (var index = dbPermission.GrantPermissions.Count - 1; index >= 0; index--)
            {
                var grantPermission = dbPermission.GrantPermissions.ElementAt(index).Key;
                if (!masterList.Contains(grantPermission))
                {
                    dbPermission.Permissions.Remove(grantPermission);
                    Log.Write(LogGroup.Property, $"Removing grant permission {grantPermission} from property {dbPermission.PropertyId} for player Id {dbPermission.PlayerId}.");
                    hasChanges = true;
                }
            }

            // Now add any new permissions
            var hasAccess = propertyOwnerId == dbPermission.PlayerId;
            foreach (var masterPermission in masterList)
            {
                if (!dbPermission.Permissions.ContainsKey(masterPermission))
                {
                    dbPermission.Permissions[masterPermission] = hasAccess;
                    Log.Write(LogGroup.Property, $"Adding permission {dbPermission.Permissions[masterPermission]} to property {dbPermission.PropertyId} for player Id {dbPermission.PlayerId}.");
                    hasChanges = true;
                }

                if (!dbPermission.GrantPermissions.ContainsKey(masterPermission))
                {
                    dbPermission.GrantPermissions[masterPermission] = hasAccess;
                    Log.Write(LogGroup.Property, $"Adding permission {dbPermission.Permissions[masterPermission]} to property {dbPermission.PropertyId} for player Id {dbPermission.PlayerId}.");
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// When the module loads, load all properties.
        /// </summary>
        private static void LoadProperties()
        {
            var instanceTypes = _propertyTypes
                .Where(x => x.Value.SpawnType == PropertySpawnType.Instance)
                .Select(s => (int)s.Key)
                .ToList();
            var worldTypes = _propertyTypes
                .Where(x => x.Value.SpawnType == PropertySpawnType.World)
                .Select(s => (int)s.Key)
                .ToList();
            var areaTypes = _propertyTypes
                .Where(x => x.Value.SpawnType == PropertySpawnType.Area)
                .Select(s => (int)s.Key)
                .ToList();

            var instanceQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), instanceTypes);
            var instancePropertiesCount = DB.SearchCount(instanceQuery);
            var instanceProperties = DB.Search(instanceQuery
                .AddPaging((int)instancePropertiesCount, 0))
                .ToList();

            var worldQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), worldTypes);
            var worldPropertiesCount = DB.SearchCount(worldQuery);
            var worldProperties = DB.Search(worldQuery
                .AddPaging((int)worldPropertiesCount, 0))
                .ToList();

            var areaQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), areaTypes);
            var areaPropertiesCount = DB.SearchCount(areaQuery);
            var areaProperties = DB.Search(areaQuery
                .AddPaging((int)areaPropertiesCount, 0))
                .ToList();

            foreach (var property in instanceProperties)
            {
                SpawnIntoWorld(property, OBJECT_INVALID);
            }
            
            foreach (var property in worldProperties)
            {
                // If the parent is contained in the instance list, this world property needs to 
                // be spawned inside the instance.
                if (_propertyInstances.ContainsKey(property.ParentPropertyId))
                {
                    var instance = _propertyInstances[property.ParentPropertyId];
                    SpawnIntoWorld(property, instance.Area);
                }
                // Otherwise the parent exists within a pre-existing area (non-instance).
                // We need to find out which area it is by looking at the parent's parent Id,
                // which will be the resref of the area.
                else
                {
                    var parent = DB.Get<WorldProperty>(property.ParentPropertyId);
                    var areaResref = parent.ParentPropertyId;
                    var area = Area.GetAreaByResref(areaResref);

                    SpawnIntoWorld(property, area);
                }
            }

            foreach (var property in areaProperties)
            {
                var area = Area.GetAreaByResref(property.ParentPropertyId);
                SpawnIntoWorld(property, area);
            }


            Log.Write(LogGroup.Property, $"Loaded {instanceProperties.Count} instanced properties.", true);
            Log.Write(LogGroup.Property, $"Loaded {worldProperties.Count} world properties.", true);
            Log.Write(LogGroup.Property, $"Loaded {areaProperties.Count} area properties.", true);
        }

        /// <summary>
        /// Creates a list of default item categories for use in freshly made properties.
        /// </summary>
        /// <returns>A list of categories</returns>
        private static List<WorldPropertyCategory> CreateDefaultCategories(string parentPropertyId)
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

        private static WorldProperty CreateProperty(
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
            var propertyDetail = _propertyTypes[type];

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

            foreach (var permission in _permissionsByPropertyType[type])
            {
                ownerPermissions.Permissions[permission] = true;
                ownerPermissions.GrantPermissions[permission] = true;

                if (creatorPermissions != null)
                {
                    creatorPermissions.Permissions[permission] = true;
                    creatorPermissions.GrantPermissions[permission] = true;
                }
            }

            DB.Set(property);
            DB.Set(ownerPermissions);
            if(creatorPermissions != null)
                DB.Set(creatorPermissions);

            if (propertyDetail.HasStorage)
            {
                // Create the default item storage categories and give permission to the owner for all categories.
                foreach (var category in CreateDefaultCategories(property.Id))
                {
                    DB.Set(category);

                    var categoryPermission = new WorldPropertyPermission
                    {
                        PropertyId = category.Id,
                        PlayerId = ownerPlayerId
                    };

                    foreach (var permission in _permissionsByPropertyType[PropertyType.Category])
                    {
                        categoryPermission.Permissions[permission] = true;
                        categoryPermission.GrantPermissions[permission] = true;
                    }

                    DB.Set(categoryPermission);
                }
            }
            
            SpawnIntoWorld(property, targetArea);

            Log.Write(LogGroup.Property, $"{GetName(creatorPlayer)} ({GetPCPlayerName(creatorPlayer)} / {GetPCPublicCDKey(creatorPlayer)}) placed {propertyDetail.Name}.");

            return property;
        }

        /// <summary>
        /// Creates a new apartment in the database for a given player.
        /// </summary>
        /// <param name="player">The player to associate the apartment with.</param>
        /// <param name="layout">The layout to use.</param>
        /// <returns>The new world property.</returns>
        public static WorldProperty CreateApartment(uint player, PropertyLayoutType layout)
        {
            var playerId = GetObjectUUID(player);
            var propertyName = $"{GetName(player)}'s Apartment";
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
        public static WorldProperty CreateStarship(
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
            var planet = Planet.GetPlanetByType(planetType);
            var npcLandingWaypoint = GetWaypointByTag(planet.LandingWaypointTag);
            var npcLandingPosition = GetPosition(npcLandingWaypoint);
            var npcLandingOrientation = GetFacing(npcLandingWaypoint);
            var npcLandingArea = GetArea(npcLandingWaypoint);
            var npcLandingResref = GetResRef(npcLandingArea);

            var playerId = GetObjectUUID(player);
            var propertyName = $"{GetName(player)}'s Starship";

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
        public static void CreateCity(uint player, uint area, uint item, Location location)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var propertyName = $"{GetName(player)}'s City";
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
                property.Upgrades[PropertyUpgradeType.BankLevel] = 1;
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
            DB.Set(dbPlayer);

            Log.Write(LogGroup.Property, $"{GetName(player)} ({GetPCPlayerName(player)} / {GetPCPublicCDKey(player)}) founded a new city in {GetName(area)}.");
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
            var layoutDetail = GetLayoutByType(layout);
            var propertyName = $"{GetName(player)}'s {layoutDetail.Name}";
            var city = DB.Get<WorldProperty>(parentCityId);

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
            DB.Set(buildingStructure);
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
            var structureDetail = GetStructureByType(type);
            var area = GetAreaFromLocation(location);
            var areaResref = GetResRef(area);
            var position = GetPositionFromLocation(location);
            var parentProperty = DB.Get<WorldProperty>(parentPropertyId);
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

            DB.Set(structure);
            DB.Set(parentProperty);

            // Now spawn it within the game world.
            var placeable = CreateObject(ObjectType.Placeable, structureDetail.Resref, location);
            SetPlotFlag(placeable, true);
            AssignPropertyId(placeable, structure.Id);

            _structurePropertyIdToPlaceable[structure.Id] = placeable;

            DestroyObject(item);
            RunStructureChangedEvent(type, StructureChangeType.PositionChanged, structure, placeable);

            return structure;
        }

        /// <summary>
        /// Retrieves a list of permissions associated with the item storage of a property for a given player.
        /// </summary>
        /// <param name="playerId">The player Id to search for</param>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A list of permissions</returns>
        public static List<WorldPropertyPermission> GetCategoryPermissions(string playerId, string propertyId)
        {
            var categoriesQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categories = DB.Search(categoriesQuery).ToList();
            var categoryIds = categories.Select(s => s.Id).ToList();

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryIds)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = DB.Search(permissionQuery);

            return permissions.ToList();
        }

        /// <summary>
        /// Retrieves a property layout by its type.
        /// </summary>
        /// <param name="type">The type of layout to retrieve.</param>
        /// <returns>A property layout</returns>
        public static PropertyLayout GetLayoutByType(PropertyLayoutType type)
        {
            return _activeLayouts[type];
        }

        /// <summary>
        /// Retrieves a structure detail by its type.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static StructureAttribute GetStructureByType(StructureType structure)
        {
            return _activeStructures[structure];
        }

        /// <summary>
        /// Retrieves all layouts for a given property type.
        /// </summary>
        /// <param name="type">The type of property to search for</param>
        /// <returns>A list of layouts for the given property type.</returns>
        public static List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type)
        {
            return _layoutsByPropertyType[type].ToList();
        }

        /// <summary>
        /// Retrieves the entrance point for a given layout.
        /// </summary>
        /// <param name="type">The layout type</param>
        /// <returns>The entrance position for the layout.</returns>
        public static Vector4 GetEntrancePosition(PropertyLayoutType type)
        {
            return _entrancesByLayout[type];
        }

        /// <summary>
        /// Retrieves the template area by a given layout type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>The instance template area associated with this type.</returns>
        public static uint GetInstanceTemplate(PropertyLayoutType type)
        {
            var layout = _activeLayouts[type];
            return _instanceTemplates[layout.AreaInstanceResref];
        }

        /// <summary>
        /// Retrieves the detail for a given permission type.
        /// </summary>
        /// <param name="permission">The type of permission to retrieve.</param>
        /// <returns>A permission detail</returns>
        public static PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission)
        {
            return _activePermissions[permission];
        }

        /// <summary>
        /// Retrieves the list of available permissions for a given property type.
        /// </summary>
        /// <param name="type">The type of property</param>
        /// <returns>A list of available permissions</returns>
        public static List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type)
        {
            return _permissionsByPropertyType[type];
        }

        /// <summary>
        /// Retrieves the property detail for a given type of property.
        /// </summary>
        /// <param name="type">The type of property to get.</param>
        /// <returns>A property detail for the given type.</returns>
        public static PropertyTypeAttribute GetPropertyDetail(PropertyType type)
        {
            return _propertyTypes[type];
        }

        /// <summary>
        /// Retrieves a placeable associated with a property Id.
        /// </summary>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A placeable or OBJECT_INVALID if not found.</returns>
        public static uint GetPlaceableByPropertyId(string propertyId)
        {
            return !_structurePropertyIdToPlaceable.ContainsKey(propertyId) 
                ? OBJECT_INVALID 
                : _structurePropertyIdToPlaceable[propertyId];
        }

        /// <summary>
        /// Retrieves a list of structures which have an interior with the specified property type.
        /// </summary>
        /// <param name="propertyType">The property type to search for.</param>
        /// <returns>A list of structure types.</returns>
        public static List<StructureType> GetStructuresByInteriorPropertyType(PropertyType propertyType)
        {
            if (!_structureTypesByPropertyType.ContainsKey(propertyType))
                return new List<StructureType>();

            return _structureTypesByPropertyType[propertyType].ToList();
        }

        /// <summary>
        /// When an apartment terminal is used, open the Apartment NUI
        /// </summary>
        [NWNEventHandler("apartment_term")]
        public static void StartApartmentConversation()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;
            
            Gui.TogglePlayerWindow(player, GuiWindowType.ManageApartment, null, terminal);
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
            // DMs always have permission.
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;
            
            if (!GetIsPC(player))
                return false;

            var playerId = GetObjectUUID(player);
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = DB.Search(query).FirstOrDefault();

            // Player doesn't exist in the permissions list. No permission.
            if (permissions == null)
                return false;

            // Player exists, check their permission.
            return permissions.Permissions[permission];
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
            // DMs always have permission.
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            if (!GetIsPC(player))
                return false;

            var playerId = GetObjectUUID(player);
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permissions = DB.Search(query).FirstOrDefault();

            // Player doesn't exist in the permissions list. No permission.
            if (permissions == null)
                return false;

            // Player exists, check their permission.
            return permissions.GrantPermissions[permission];
        }

        /// <summary>
        /// Sends a player to the template area of a particular layout.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="layout">The layout type to send them to.</param>
        public static void PreviewProperty(uint player, PropertyLayoutType layout)
        {
            var entrance = GetEntrancePosition(layout);
            var area = GetInstanceTemplate(layout);
            var position = new Vector3(entrance.X, entrance.Y, entrance.Z);
            var location = Location(area, position, entrance.W);

            StoreOriginalLocation(player);
            AssignCommand(player, () =>
            {
                JumpToLocation(location);
            });
        }

        /// <summary>
        /// When a player enters a property instance, add them to the list of players.
        /// </summary>
        [NWNEventHandler("area_enter")]
        public static void EnterPropertyInstance()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var propertyId = GetPropertyId(OBJECT_SELF);

            if (!_propertyInstances.ContainsKey(propertyId))
                return;

            if (!_propertyInstances[propertyId].Players.Contains(player))
                _propertyInstances[propertyId].Players.Add(player);
        }

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        [NWNEventHandler("area_exit")]
        public static void ExitPropertyInstance()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var propertyId = GetPropertyId(OBJECT_SELF);

            if (!_propertyInstances.ContainsKey(propertyId))
                return;

            if (_propertyInstances[propertyId].Players.Contains(player))
                _propertyInstances[propertyId].Players.Remove(player);
        }


        /// <summary>
        /// Sends a player to a specific property's instance.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="propertyId">The property Id</param>
        public static void EnterProperty(uint player, string propertyId)
        {
            if (!HasPropertyPermission(player, propertyId, PropertyPermissionType.EnterProperty))
            {
                FloatingTextStringOnCreature("You do not have permission to access that property.", player, false);
                return;
            }

            var property = DB.Get<WorldProperty>(propertyId);
            var entrance = _entrancesByLayout[property.Layout];
            var instance = GetRegisteredInstance(property.Id);
            var position = new Vector3(entrance.X, entrance.Y, entrance.Z);
            var location = Location(instance.Area, position, entrance.W);

            StoreOriginalLocation(player);
            AssignCommand(player, () =>
            {
                JumpToLocation(location);
            });
        }

        /// <summary>
        /// Stores the original location of a player, before being ported into a property instance.
        /// </summary>
        /// <param name="player">The player whose location will be stored.</param>
        public static void StoreOriginalLocation(uint player)
        {
            var position = GetPosition(player);
            var facing = GetFacing(player);
            var area = GetArea(player);

            SetLocalFloat(player, "PROPERTY_STORED_LOCATION_X", position.X);
            SetLocalFloat(player, "PROPERTY_STORED_LOCATION_Y", position.Y);
            SetLocalFloat(player, "PROPERTY_STORED_LOCATION_Z", position.Z);
            SetLocalFloat(player, "PROPERTY_STORED_LOCATION_FACING", facing);
            SetLocalObject(player, "PROPERTY_STORED_LOCATION_AREA", area);
        }

        /// <summary>
        /// Jumps player to their original location, which is where they were before entering a property instance.
        /// This will also clear the temporary data related to the original location.
        /// </summary>
        /// <param name="player">The player who will jump.</param>
        public static void JumpToOriginalLocation(uint player)
        {
            var position = Vector3(
                GetLocalFloat(player, "PROPERTY_STORED_LOCATION_X"),
                GetLocalFloat(player, "PROPERTY_STORED_LOCATION_Y"),
                GetLocalFloat(player, "PROPERTY_STORED_LOCATION_Z"));
            var facing = GetLocalFloat(player, "PROPERTY_STORED_LOCATION_FACING");
            var area = GetLocalObject(player, "PROPERTY_STORED_LOCATION_AREA");
            var location = Location(area, position, facing);

            AssignCommand(player, () => ActionJumpToLocation(location));

            DeleteLocalFloat(player, "PROPERTY_STORED_LOCATION_X");
            DeleteLocalFloat(player, "PROPERTY_STORED_LOCATION_Y");
            DeleteLocalFloat(player, "PROPERTY_STORED_LOCATION_Z");
            DeleteLocalFloat(player, "PROPERTY_STORED_LOCATION_FACING");
            DeleteLocalObject(player, "PROPERTY_STORED_LOCATION_AREA");
        }

        /// <summary>
        /// When the property menu feat is used, open the GUI window.
        /// </summary>
        [NWNEventHandler("feat_use_bef")]
        public static void PropertyMenu()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));

            if (feat != FeatType.PropertyMenu) return;

            var player = OBJECT_SELF;

            if (Gui.IsWindowOpen(player, GuiWindowType.ManageStructures))
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.ManageStructures);
                return;
            }

            var area = GetArea(player);
            var propertyId = GetPropertyId(area);

            if (string.IsNullOrWhiteSpace(propertyId))
            {
                FloatingTextStringOnCreature($"This menu can only be accessed within player properties.", player, false);
                return;
            }

            var playerId = GetObjectUUID(player);
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            var permission = DB.Search(permissionQuery).FirstOrDefault();

            var categoryQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categoryIds = DB.Search(categoryQuery).Select(s => s.Id).ToList();
            long categoryPermissionCount = 0;

            if (categoryIds.Count > 0)
            {
                permissionQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryIds);
                categoryPermissionCount = DB.SearchCount(permissionQuery);
            }

            // Player can access this menu only if they have permission to manipulate structures,
            // retrieve structures, or access the property's item storage.
            if (permission == null ||
                !permission.Permissions[PropertyPermissionType.RetrieveStructures] &&
                !permission.Permissions[PropertyPermissionType.EditStructures] &&
                categoryPermissionCount <= 0)
            {
                FloatingTextStringOnCreature($"You do not have permission to access this property.", player, false);
                return;
            }
            
            Gui.TogglePlayerWindow(player, GuiWindowType.ManageStructures);
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
            var resref = GetResRef(item);
            if (!resref.StartsWith("structure_")) return StructureType.Invalid;

            var id = resref.Substring(resref.Length - 4, 4);

            if (!int.TryParse(id, out var structureId))
            {
                return StructureType.Invalid;
            }

            return (StructureType)structureId;
        }

        /// <summary>
        /// Before an item is used, if it is a structure item, place it at the specified location.
        /// </summary>
        [NWNEventHandler("item_use_bef")]
        public static void PlaceStructure()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM_OBJECT_ID"));
            if (!GetResRef(item).StartsWith("structure_"))
                return;

            EventsPlugin.SkipEvent();

            var player = OBJECT_SELF;
            var area = GetArea(player);
            var propertyId = GetPropertyId(area);
            var playerId = GetObjectUUID(player);
            var structureType = GetStructureTypeFromItem(item);
            var position = Vector3(
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_X")),
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Y")),
                (float)Convert.ToDouble(EventsPlugin.GetEventData("TARGET_POSITION_Z")));

            // Special case: City Hall pulls up a menu with details about the land and an option to place it down, claiming the land.
            if (structureType == StructureType.CityHall)
            {
                if (!GetLocalBool(area, "IS_BUILDABLE"))
                {
                    FloatingTextStringOnCreature("Cities cannot be founded here.", player, false);
                    return;
                }

                SetLocalObject(player, "PROPERTY_CITY_HALL_ITEM", item);
                SetLocalFloat(player, "PROPERTY_CITY_HALL_X", position.X);
                SetLocalFloat(player, "PROPERTY_CITY_HALL_Y", position.Y);
                SetLocalFloat(player, "PROPERTY_CITY_HALL_Z", position.Z);
                Dialog.StartConversation(player, player, nameof(PlaceCityHallDialog));
                return;
            }
            
            if (structureType == StructureType.Invalid) return;

            // Must be in a player property.
            if (string.IsNullOrWhiteSpace(propertyId))
            {
                FloatingTextStringOnCreature($"Structures may only be placed within player properties.", player, false);
                return;
            }

            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            var permission = DB.Search(query).FirstOrDefault();

            // Player must have permission to edit structures.
            if (permission == null ||
                !permission.Permissions[PropertyPermissionType.EditStructures])
            {
                FloatingTextStringOnCreature($"You do not have permission to place structures within this property.", player, false);
                return;
            }
            
            var property = DB.Get<WorldProperty>(propertyId);
            var layout = GetLayoutByType(property.Layout);
            int structureLimit;
            var structureDetail = GetStructureByType(structureType);
            var structureQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure);
            var structureQueryCount = (int)DB.SearchCount(structureQuery);
            var structures = DB.Search(structureQuery.AddPaging(structureQueryCount, 0));
            int structureCount;
            string fixtureName;

            // Special Case: Cities differentiate between structures and buildings.
            // They use the BuildingLimit value for buildings and StructureLimit for everything else.
            // Buildings are determined by structures whose restrictions are solely to "City" property types.
            if (property.PropertyType == PropertyType.City &&
                structureDetail.RestrictedPropertyTypes == PropertyType.City)
            {
                structureCount = structures.Count(x => GetStructureByType(x.StructureType).LayoutType != PropertyLayoutType.Invalid);
                structureLimit = layout.BuildingLimit;
                fixtureName = "Building";
            }
            else
            {
                structureCount = structures.Count(x => GetStructureByType(x.StructureType).LayoutType == PropertyLayoutType.Invalid);
                structureLimit = layout.StructureLimit;
                fixtureName = "Structure";
            }

            // Over the structure limit.
            if (structureCount >= structureLimit)
            {
                FloatingTextStringOnCreature($"{fixtureName} limit reached for this property.", player, false);
                return;
            }

            // Structure can't be placed within this type of property.
            if (!structureDetail.RestrictedPropertyTypes.HasFlag(property.PropertyType))
            {
                FloatingTextStringOnCreature($"This {fixtureName} cannot be placed within this type of property.", player, false);
                return;
            }

            var location = Location(area, position, 0.0f);

            // If no interior layout is defined, this is a basic structure.
            if (structureDetail.LayoutType == PropertyLayoutType.Invalid)
            {
                CreateStructure(propertyId, item, structureType, location);
            }
            // Otherwise we have a layout which means an interior must be spawned.
            else
            {
                var structureLayout = GetLayoutByType(structureDetail.LayoutType);
                CreateBuilding(
                    player, 
                    item, 
                    propertyId, 
                    structureLayout.PropertyType, 
                    structureDetail.LayoutType, 
                    structureType, location);
            }

            SendMessageToPC(player, $"{fixtureName} Limit: {structureCount+1} / {structureLimit}");
        }

        /// <summary>
        /// Spawns the property into the game world.
        /// For structures, this means spawning a placeable at the location.
        /// For cities, starships, apartments, and buildings this means spawning area instances.
        /// </summary>
        /// <param name="property">The property to spawn into the world.</param>
        /// <param name="area">The area to spawn the property into. Leave OBJECT_INVALID if spawning an instance.</param>
        private static void SpawnIntoWorld(WorldProperty property, uint area)
        {
            var propertyDetail = _propertyTypes[property.PropertyType];

            // World spawns represent placeables within the game world such as furniture and buildings
            if (propertyDetail.SpawnType == PropertySpawnType.World)
            {
                var furniture = GetStructureByType(property.StructureType);

                var staticPosition = property.Positions[PropertyLocationType.StaticPosition];
                var position = Vector3(staticPosition.X, staticPosition.Y, staticPosition.Z);
                var location = Location(area, position, staticPosition.Orientation);

                var placeable = CreateObject(ObjectType.Placeable, furniture.Resref, location);
                SetPlotFlag(placeable, true);
                AssignPropertyId(placeable, property.Id);

                _structurePropertyIdToPlaceable[property.Id] = placeable;

                // Some structures have custom spawn-in actions which also need to be run
                // when brought into the world. 
                RunStructureChangedEvent(property.StructureType, StructureChangeType.PositionChanged, property, placeable);
            }
            // Instance spawns are instanced areas that are spawned dynamically into the game world.
            else if(propertyDetail.SpawnType == PropertySpawnType.Instance)
            {
                // If no interior layout is defined, the provided area will be used.
                var layout = GetLayoutByType(property.Layout);
                var targetArea = CreateArea(layout.AreaInstanceResref);
                RegisterInstance(property.Id, targetArea, property.Layout);
                
                SetName(targetArea, "{PC} " + property.CustomName);

                if (layout.OnSpawnAction != null)
                {
                    layout.OnSpawnAction(targetArea);
                }
            }
            // Area spawns exist in a pre-built area.
            else if(propertyDetail.SpawnType == PropertySpawnType.Area)
            {
                AssignPropertyId(area, property.Id);
            }
        }

        /// <summary>
        /// When a building entrance is used, port the player inside the instance if they have permission
        /// or display an error message saying they don't have permission to enter.
        /// </summary>
        [NWNEventHandler("enter_property")]
        public static void EnterBuilding()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var door = OBJECT_SELF;
            
            // Buildings only ever have one child which is the interior area instance
            var buildingId = GetPropertyId(door);
            var building = DB.Get<WorldProperty>(buildingId);
            var interiorId = building.ChildPropertyIds[PropertyChildType.Interior].Single();
            var interior = DB.Get<WorldProperty>(interiorId);

            var instance = GetRegisteredInstance(interior.Id);
            var entrance = GetEntrancePosition(interior.Layout);
            var position = Vector3(entrance.X, entrance.Y, entrance.Z);
            var location = Location(instance.Area, position, entrance.W);
            var permission = DB.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), interiorId, false))
                .SingleOrDefault();

            // Building is publicly accessible or the player has permission to enter.
            if (interior.IsPubliclyAccessible ||
                !interior.IsPubliclyAccessible && permission != null && permission.Permissions[PropertyPermissionType.EnterProperty])
            {
                StoreOriginalLocation(player);
                AssignCommand(player, () => ActionJumpToLocation(location));
            }
            else
            {
                SendMessageToPC(player, "You do not have permission to enter.");
            }
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
            if (!_structureChangedActions.ContainsKey(structureType))
                return;

            if (!_structureChangedActions[structureType].ContainsKey(changeType))
                return;

            _structureChangedActions[structureType][changeType](property, placeable);
        }

        /// <summary>
        /// When the Citizenship terminal is used, open the Manage Citizenship UI.
        /// </summary>
        [NWNEventHandler("open_citizenship")]
        public static void OpenCitizenshipMenu()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, "Only players may use this terminal.");
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var terminal = OBJECT_SELF;
            var area = GetArea(terminal);
            var propertyId = GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);

            // Player is a citizen of another city. Their citizenship needs to be revoked before
            // they're able to access this terminal.
            if (!string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId) &&
                dbPlayer.CitizenPropertyId != Guid.Empty.ToString() &&
                dbBuilding.ParentPropertyId != dbPlayer.CitizenPropertyId)
            {
                SendMessageToPC(player, ColorToken.Red("You are a citizen of another city. You cannot access this terminal unless you revoke your citizenship first."));
            }
            else
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.ManageCitizenship, null, terminal);
            }
        }

        /// <summary>
        /// When the City Management terminal is used, open the City Management UI.
        /// </summary>
        [NWNEventHandler("open_city_manage")]
        public static void OpenCityManagementMenu()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var terminal = OBJECT_SELF;
            var area = GetArea(terminal);
            var propertyId = GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);

            var permission = DB.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), dbBuilding.ParentPropertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false))
                .SingleOrDefault();

            // Player has at least one permission. Display the window.
            if (permission != null && (permission.Permissions[PropertyPermissionType.RenameProperty] ||
                                       permission.Permissions[PropertyPermissionType.EditTaxes] ||
                                       permission.Permissions[PropertyPermissionType.AccessTreasury] ||
                                       permission.Permissions[PropertyPermissionType.ManageUpgrades] ||
                                       permission.Permissions[PropertyPermissionType.ManageUpkeep]))
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.ManageCity, null, terminal);
            }
            else
            {
                SendMessageToPC(player, ColorToken.Red("You do not have permission to access this terminal."));
            }
        }

        /// <summary>
        /// Retrieves the name of a particular city level.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>A string representing the city level.</returns>
        public static string GetCityLevelName(int level)
        {
            switch (level)
            {
                case 1:
                    return "Outpost";
                case 2:
                    return "Village";
                case 3:
                    return "Township";
                case 4:
                    return "City";
                case 5:
                    return "Metropolis";
            }

            return string.Empty;
        }

        /// <summary>
        /// Retrieves the number of citizens required for the next city level.
        /// If level isn't supported, -1 will be returned.
        /// </summary>
        /// <param name="level">The level to retrieve</param>
        /// <returns>The number of citizens required to level up the city.</returns>
        public static int GetCitizensRequiredForNextCityLevel(int level)
        {
            return _citizensRequired.ContainsKey(level)
                ? _citizensRequired[level]
                : -1;
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
            if (string.IsNullOrWhiteSpace(cityId))
                return 0;

            var property = DB.Get<WorldProperty>(cityId);
            if (property == null)
                return 0;

            if (!property.Upgrades.ContainsKey(upgradeType))
                return 0;

            var cityLevel = property.Upgrades[PropertyUpgradeType.CityLevel];
            var upgradeLevel = property.Upgrades[upgradeType];
            var effectiveLevel = cityLevel < upgradeLevel ? cityLevel : upgradeLevel;
            var structureType = StructureType.Invalid;

            switch (upgradeType)
            {
                case PropertyUpgradeType.BankLevel:
                    structureType = StructureType.Bank;
                    break;
                case PropertyUpgradeType.MedicalCenterLevel:
                    structureType = StructureType.MedicalCenter;
                    break;
                case PropertyUpgradeType.StarportLevel:
                    structureType = StructureType.Starport;
                    break;
                case PropertyUpgradeType.CantinaLevel:
                    structureType = StructureType.Cantina;
                    break;
            }

            if (structureType == StructureType.Invalid)
                return 0;

            // At least one building property of the given type must exist within the city.
            var buildingCount = DB.SearchCount(new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)structureType)
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), cityId, false));

            if (buildingCount <= 0)
                return 0;

            return effectiveLevel;
        }
    }
}
