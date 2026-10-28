using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        public const int MaxRegisteredShips = 10;

        private static readonly Dictionary<string, ShipDetail> _shipTypes = new();
        private static readonly Dictionary<string, ShipModuleDetail> _shipModules = new();
        private static readonly Dictionary<string, SpaceObjectDetail> _spaceObjects = new();

        private static readonly Dictionary<uint, ShipStatus> _shipNPCs = new();
        private static readonly Dictionary<string, ShipNpcEntry> _npcPresets = RevampNpcShipCatalog.Ships.ToDictionary(x => x.CreatureTag);
        private static readonly Dictionary<uint, ShipStatus> _spaceObjectInstances = new();

        public static Dictionary<FeatType, ShipModuleFeat> ShipModuleFeats { get; } = ShipModuleFeat.GetAll();

        private static readonly HashSet<string> _shipItemResrefs = new();
        private static readonly HashSet<string> _shipModuleItemTags = new();
        private static readonly HashSet<AppearanceType> _shipAppearances = new();

        private static readonly Dictionary<string, uint> _shipClones = new();
        private static readonly Dictionary<uint, PendingShipLaunch> _pendingShipLaunches = new();

        private static readonly Dictionary<PlanetType, Dictionary<string, ShipDockPoint>> _dockPoints = new();

        private static readonly HashSet<uint> _playersInSpace = new();

        private const float NPCShipKeepDistance = 15f;
        private const float ShipDamageMessageRange = 10f;

        /// <summary>
        /// When the module loads, cache all space data into memory.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadSpaceSystem()
        {
            LoadShips();
            LoadShipModules();
            LoadShipEnemies();

            Console.WriteLine($"Loaded {_shipTypes.Count} ships.");
            Console.WriteLine($"Loaded {_shipModules.Count} ship modules.");
            Console.WriteLine($"Loaded {_spaceObjects.Count} space objects.");

            Scheduler.ScheduleRepeating(ProcessSpaceNPCAI, TimeSpan.FromSeconds(1));
            Scheduler.ScheduleRepeating(PlayerShipRecovery, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100d));
            ScheduleShipToggleLoop();
            ScheduleSpaceMines();
            SpaceAuras.ScheduleRefresh();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void EnterServer()
        {
            var player = GetEnteringObject();
            ReloadPlayerTlkStrings();
            RestoreCrewMemberAfterLogin(player);
            WarpPlayerInsideShip(player);
            RestoreDockedPassengerAfterLogin(player);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ExitServer()
        {
            var player = GetExitingObject();
            _pendingShipLaunches.Remove(player);
            _shipCombatAt.Remove(player);
            CrewMemberDisconnected(player);
            if (!IsPlayerInSpaceMode(player))
                return;

            MarkRiftRunPilotAway(player);

            // The pilot's crew go back to their consoles; the ship stays in space without a pilot, and its droids
            // stop until someone flies it again.
            ReleaseShipCrew(player, "The ship's pilot has left the controls, so you leave your station.");
            UnloadDroidCrew(player);
            UnregisterShipPilot(player);
            TurnOffShipToggles(player);
            CloneShip(player);

            if (_playersInSpace.Contains(player))
                _playersInSpace.Remove(player);
        }

        /// <summary>
        /// When the module loads,
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadLandingPoints()
        {
            var count = 0;
            var waypoint = GetObjectByTag("STARSHIP_DOCKPOINT", count);

            while (GetIsObjectValid(waypoint))
            {
                var area = GetArea(waypoint);
                RegisterLandingPoint(waypoint, area, true, string.Empty);

                count++;
                waypoint = GetObjectByTag("STARSHIP_DOCKPOINT", count);
            }
        }

        /// <summary>
        /// Registers a waypoint as a landing point.
        /// Once added, this location will become available for players to land at.
        /// </summary>
        /// <param name="waypoint">The waypoint to register.</param>
        /// <param name="area">The area to use for registration.</param>
        /// <param name="isNPC">If true, will be marked as an NPC dock. Otherwise will be marked as a PC dock.</param>
        /// <param name="propertyId">If specified, references the world property Id of this landing point.</param>
        public static void RegisterLandingPoint(uint waypoint, uint area, bool isNPC, string propertyId)
        {
            var dockPointId = GetLocalString(waypoint, "STARSHIP_DOCKPOINT_ID");
            var planet = Planet.GetPlanetType(area);

            // Only waypoints in recognized planets are tracked.
            if (planet == PlanetType.Invalid)
                return;

            if (!_dockPoints.ContainsKey(planet))
                _dockPoints[planet] = new Dictionary<string, ShipDockPoint>();

            if (!string.IsNullOrWhiteSpace(dockPointId))
            {
                if (_dockPoints[planet].ContainsKey(dockPointId))
                    return;

                DeleteLocalString(waypoint, "STARSHIP_DOCKPOINT_ID");
            }

            if (!isNPC && !string.IsNullOrWhiteSpace(propertyId))
                RemoveLandingPointByPropertyId(propertyId);

            dockPointId = Guid.NewGuid().ToString();
            var dockPoint = new ShipDockPoint
            {
                Location = GetLocation(waypoint),
                Name = string.IsNullOrWhiteSpace(propertyId) ? GetName(waypoint) : string.Empty,
                PropertyId = propertyId,
                IsNPC = isNPC
            };

            _dockPoints[planet][dockPointId] = dockPoint;

            SetLocalString(waypoint, "STARSHIP_DOCKPOINT_ID", dockPointId);
        }

        /// <summary>
        /// Removes a waypoint from the landing point registration.
        /// Once removed, this location will no longer be available to land at.
        /// </summary>
        /// <param name="waypoint">The waypoint to remove.</param>
        /// <param name="cityArea">The area to remove from.</param>
        public static void RemoveLandingPoint(uint waypoint, uint cityArea)
        {
            var planet = Planet.GetPlanetType(cityArea);

            // Only waypoints in recognized planets are tracked.
            if (planet == PlanetType.Invalid)
                return;

            var dockPointId = GetLocalString(waypoint, "STARSHIP_DOCKPOINT_ID");
            if (_dockPoints.ContainsKey(planet) &&
                _dockPoints[planet].ContainsKey(dockPointId))
            {
                _dockPoints[planet].Remove(dockPointId);
            }
        }

        /// <summary>
        /// Removes a player starport landing point by its property Id.
        /// </summary>
        /// <param name="propertyId">The property Id to remove.</param>
        public static void RemoveLandingPointByPropertyId(string propertyId)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return;

            foreach (var planet in _dockPoints.Keys.ToList())
            {
                var dockPointIds = _dockPoints[planet]
                    .Where(x => x.Value.PropertyId == propertyId)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var dockPointId in dockPointIds)
                {
                    _dockPoints[planet].Remove(dockPointId);
                }
            }
        }

        /// <summary>
        /// Retrieves all of the registered dock points for a given planet.
        /// </summary>
        /// <param name="planetType">The planet to search.</param>
        /// <returns>A dictionary of dock points.</returns>
        public static Dictionary<string, ShipDockPoint> GetDockPointsByPlanet(PlanetType planetType)
        {
            if (!_dockPoints.ContainsKey(planetType))
                return new Dictionary<string, ShipDockPoint>();

            return _dockPoints[planetType].ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Loads all of the implementations of IShipListDefinition into the cache.
        /// </summary>
        private static void LoadShips()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IShipListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IShipListDefinition)Activator.CreateInstance(type);
                var ships = instance.BuildShips();

                foreach (var (shipType, shipDetail) in ships)
                {
                    _shipTypes.Add(shipType, shipDetail);
                    _shipAppearances.Add(shipDetail.Appearance);

                    // Hull Designs give ships other appearances; they are ship models too.
                    foreach (var design in HullDesignRules.DesignsFor(shipType))
                    {
                        _shipAppearances.Add(design.Appearance);
                    }

                    if (!_shipItemResrefs.Contains(shipDetail.ItemResref))
                        _shipItemResrefs.Add(shipDetail.ItemResref);
                }
            }
        }

        /// <summary>
        /// Loads all of the implementations of IShipModuleListDefinition into the cache.
        /// </summary>
        private static void LoadShipModules()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IShipModuleListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IShipModuleListDefinition)Activator.CreateInstance(type);
                var shipModules = instance.BuildShipModules();

                foreach (var (moduleType, moduleDetail) in shipModules)
                {
                    // Give warning if ship module is active and short name is longer than GUI will allow.
                    if (moduleDetail.ShortName.Length > 14 &&
                        moduleDetail.Type != ShipModuleType.Passive)
                    {
                        Log.Write(LogGroup.Space, $"Ship module with short name {moduleDetail.ShortName} is longer than 14 characters. Short names should be no more than 14 characters so they display on the UI properly.", true);
                    }

                    _shipModules.Add(moduleType, moduleDetail);

                    if (!_shipModuleItemTags.Contains(moduleType))
                        _shipModuleItemTags.Add(moduleType);
                }
            }
        }

        /// <summary>
        /// Loads all of the implementations of ISpaceObjectListDefinition into the cache.
        /// </summary>
        private static void LoadShipEnemies()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ISpaceObjectListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (ISpaceObjectListDefinition)Activator.CreateInstance(type);
                var ships = instance.BuildSpaceObjects();

                foreach (var (creatureTag, enemy) in ships)
                {
                    _spaceObjects.Add(creatureTag, enemy);
                }
            }
        }

        /// <summary>
        /// Retrieves a ship's detail by type.
        /// </summary>
        /// <param name="itemTag">The item tag to search for.</param>
        /// <returns>A ship detail matching the type.</returns>
        public static ShipDetail GetShipDetailByItemTag(string itemTag)
        {
            return _shipTypes[itemTag];
        }

        /// <summary>
        /// Retrieves a ship module's detail by type.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship module to search for.</param>
        /// <returns>A ship module detail matching the type.</returns>
        public static ShipModuleDetail GetShipModuleDetailByItemTag(string itemTag)
        {
            return _shipModules[itemTag];
        }

        /// <summary>
        /// Determines whether an item's tag is registered to a ship module.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship to search for.</param>
        /// <returns>true if registered, false otherwise</returns>
        public static bool IsRegisteredShip(string itemTag)
        {
            return _shipTypes.ContainsKey(itemTag);
        }

        /// <summary>
        /// Determines whether an item's tag is registered to a ship module.
        /// </summary>
        /// <param name="itemTag">The item tag of the ship module to search for.</param>
        /// <returns>true if registered, false otherwise</returns>
        public static bool IsRegisteredShipModule(string itemTag)
        {
            return _shipModules.ContainsKey(itemTag);
        }

        /// <summary>
        /// Determines whether an item is a ship deed.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a ship deed, false otherwise</returns>
        public static bool IsItemShip(uint item)
        {
            var resref = GetResRef(item);
            return _shipItemResrefs.Contains(resref);
        }

        /// <summary>
        /// Determines whether an item is a ship module.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if item is a ship module, false otherwise</returns>
        public static bool IsItemShipModule(uint item)
        {
            var tag = GetTag(item);
            return _shipModuleItemTags.Contains(tag);
        }

        /// <summary>
        /// Determines whether an item is starship ammo, its launcher variants included.
        /// </summary>
        public static bool IsStarshipAmmo(uint item)
        {
            var resref = GetResRef(item);
            if (resref == "ship_missile" ||
                resref == "ship_fuelcapsule" ||
                resref == "proton_bomb" ||
                resref == "acm_ammo")
            {
                return true;
            }
            return ShipAmmoRules.Variant(resref) != null;
        }

        /// <summary>The appearance a player ship shows: its Hull Design from a Refit, else its model's own.</summary>
        public static AppearanceType GetShipAppearance(PlayerShip ship, ShipDetail shipDetail)
        {
            return HullDesignRules.ShipAppearance(ship.Status.ItemTag, shipDetail.Appearance, ship.HullDesignAppearance);
        }

        /// <summary>
        /// Sets a player's current target.
        /// </summary>
        /// <param name="creature">The creature whose target will be set.</param>
        /// <param name="target">The target to set.</param>
        private static void SetCurrentTarget(uint creature, uint target)
        {
            // Set the VFX to the new target if creature is a player.
            if (GetIsObjectValid(target) &&
                GetIsPC(creature))
            {
                PlayerPlugin.ApplyLoopingVisualEffectToObject(creature, target, VisualEffect.Vfx_Target_Marker);
            }
            SetLocalObject(creature, "SPACE_TARGET", target);
            SpaceCrew.ForgetDroidEngagements(creature);

            if(GetIsPC(creature) && !Gui.IsWindowOpen(creature, GuiWindowType.TargetStatus) && GetShipStatus(target) != null)
                Gui.TogglePlayerWindow(creature, GuiWindowType.TargetStatus);
        }

        /// <summary>
        /// Retrieves the currently selected target of a player. Returns OBJECT_INVALID if not set.
        /// </summary>
        /// <param name="player">The player whose target to retrieve.</param>
        /// <returns>The selected target or OBJECT_INVALID.</returns>
        public static (uint, ShipStatus) GetCurrentTarget(uint player)
        {
            var target = GetLocalObject(player, "SPACE_TARGET");

            // A target that cloaked is out of sight: the selection stays but nothing can act on it.
            if (IsShipHiddenFrom(player, target))
                return (OBJECT_INVALID, null);

            return (target, GetShipStatus(target));
        }

        /// <summary>
        /// Clears a creature's current target.
        /// </summary>
        /// <param name="creature">The creature whose target will be cleared.</param>
        private static void ClearCurrentTarget(uint creature)
        {
            // Remove the VFX from the current target if it exists.
            var (target, _) = GetCurrentTarget(creature);
            if (GetIsObjectValid(target) &&
                GetIsPC(creature))
            {
                PlayerPlugin.ApplyLoopingVisualEffectToObject(creature, target, VisualEffect.None);
            }

            DeleteLocalObject(creature, "SPACE_TARGET");

            if(GetIsPC(creature) && Gui.IsWindowOpen(creature, GuiWindowType.TargetStatus))
                Gui.TogglePlayerWindow(creature, GuiWindowType.TargetStatus);
        }

        /// <summary>
        /// Handles swapping a player's target to the object they attempted to attack using NWN's combat system.
        /// </summary>
        [NWNEventHandler(ScriptName.OnInputAttackObjectBefore)]
        public static void SelectTarget()
        {
            var player = OBJECT_SELF;
            var position = GetPosition(player);

            // A crew member picks their own target. A viewport watcher has no controls, so a click does nothing.
            var isCrew = TryGetCrewStation(player, out _, out _);
            if (!isCrew && IsAtCrewStation(player))
            {
                EventsPlugin.SkipEvent();
                return;
            }

            if (!isCrew && !IsPlayerInSpaceMode(player)) return;
            EventsPlugin.SkipEvent();

            // Clicking enemies will cause this flag to be true (and we should proceed with target selection)
            // Being attacked by enemies will have this flag set to false (and we should exit without doing anything else)
            var clearAllActions = Convert.ToInt32(EventsPlugin.GetEventData("CLEAR_ALL_ACTIONS"));
            if (clearAllActions == 0) return;

            var target = StringToObject(EventsPlugin.GetEventData("TARGET"));
            var (currentTarget, _) = GetCurrentTarget(player);

            // Targeted the same object - remove it.
            if (currentTarget == target)
            {
                PlayerPlugin.ShowVisualEffect(player, (int)VisualEffect.Vfx_UI_Cancel, 1f, position, Vector3.Zero, Vector3.Zero);
                ClearCurrentTarget(player);
            }
            // Targeted something new. Remove existing target and pick the new one.
            else
            {
                ClearCurrentTarget(player);
                SetCurrentTarget(player, target);
                PlayerPlugin.ShowVisualEffect(player, (int)VisualEffect.Vfx_UI_Select, 1f, position, Vector3.Zero, Vector3.Zero);
            }
        }

        /// <summary>
        /// When a player enters a space area, update the property's space position.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void UpdateSpacePosition()
        {
            var player = GetEnteringObject();
            TryCompleteShipLaunch(player);
            UpdateSpacePosition(player);
        }

        /// <summary>Persists the actual ship position after launch completion or a later area change.</summary>
        private static void UpdateSpacePosition(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (!IsPlayerInSpaceMode(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
            var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
            var position = GetPosition(player);
            var areaResref = GetResRef(GetArea(player));
            var orientation = GetFacing(player);

            dbProperty.Positions[PropertyLocationType.CurrentPosition] = new PropertyLocation
            {
                X = position.X,
                Y = position.Y,
                Z = position.Z,
                Orientation = orientation,
                AreaResref = areaResref
            };

            DB.Set(dbProperty);
        }

        /// <summary>
        /// When a creature leaves an area, their current target is cleared.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void ClearTargetOnAreaExit()
        {
            var player = GetExitingObject();
            if (GetIsDM(player)) return;

            if (_pendingShipLaunches.TryGetValue(player, out var launch) &&
                OBJECT_SELF == launch.DepartureArea)
                launch.HasDeparted = true;

            // Leaving a space area (a starlane jump) switches every toggle off.
            TurnOffShipToggles(player);
            ClearCurrentTarget(player);
        }

        /// <summary>
        /// When the ship computer is used, check the user's permissions.
        /// If player has permission and the ship isn't currently being controlled by another player,
        /// send the player into space mode.
        /// </summary>
        [NWNEventHandler(ScriptName.OnShipComputer)]
        public static void UseShipComputer()
        {
            var computer = OBJECT_SELF;
            var player = GetLastUsedBy();
            if (IsPlayerInSpaceMode(player))
                return;

            SetLocalBool(computer, ShipComputerVariable, true);

            // A click while a launch is pending goes on with the launch.
            if (_pendingShipLaunches.ContainsKey(player))
            {
                TakeShipControls(player, computer);
                return;
            }

            // While someone flies the ship, its computer is the crew console: take a station or watch from a viewport.
            var propertyId = Property.GetPropertyId(GetArea(computer));
            var dbShip = DB.Search(new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false)).FirstOrDefault();
            if (dbShip != null && GetIsObjectValid(GetShipPilot(dbShip.Id)))
            {
                if (!ConversationMenu.TryStart(player, computer, "CrewStationDialog"))
                    SendMessageToPC(player, ColorToken.Red("This ship's controls are in use."));
                return;
            }

            // A docked ship with crew stations or a droid bay offers its droid crew first to a captain who has
            // droids to post or recall, and a ship carrying docked ships offers undocking to their pilots; the
            // menu's first choice takes the controls.
            if (dbShip != null &&
                (ShouldOfferDroidCrewMenu(player, dbShip, propertyId) || GetUndockableShips(player, dbShip).Count > 0) &&
                ConversationMenu.TryStart(player, computer, "CrewStationDialog"))
            {
                return;
            }

            TakeShipControls(player, computer);
        }

        /// <summary>Whether a placeable is a ship computer (it marks itself when first used).</summary>
        public static bool IsShipComputer(uint placeable) => GetLocalBool(placeable, ShipComputerVariable);

        /// <summary>
        /// Puts the player at the controls of the ship whose computer they use: checks their permission and the
        /// ship, then sends them to its space position.
        /// </summary>
        public static void TakeShipControls(uint player, uint computer)
        {
            var area = GetArea(computer);
            if (IsPlayerInSpaceMode(player))
                return;

            // A second click may replace a canceled jump while still inside. Once the
            // area transition starts, keep the original request and return location.
            if (_pendingShipLaunches.TryGetValue(player, out var previousLaunch))
            {
                if (previousLaunch.HasDeparted || GetArea(player) != previousLaunch.DepartureArea)
                    return;
                _pendingShipLaunches.Remove(player);
            }

            var playerId = GetObjectUUID(player);
            var propertyId = Property.GetPropertyId(area);

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            var permission = DB.Search(permissionQuery).FirstOrDefault();

            if (permission == null || !permission.Permissions[PropertyPermissionType.PilotShip])
            {
                SendMessageToPC(player, ColorToken.Red("You do not have permission to pilot this starship."));
                return;
            }

            var shipQuery = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false);
            var dbShip = DB.Search(shipQuery).FirstOrDefault();

            if (dbShip == null)
            {
                SendMessageToPC(player, ColorToken.Red("ERROR: Could not locate ship. Notify an admin."));
                return;
            }

            // A ship docked at a derelict undocks beside it (plan section 28).
            if (IsDockedAtDerelict(dbShip, out _))
            {
                UndockFromDerelict(player, dbShip);
                return;
            }

            if (IsDockedAboardHost(dbShip, out var hostName))
            {
                SendMessageToPC(player, ColorToken.Red($"This ship is docked aboard {hostName}. Undock it from that ship's computer or with /undock."));
                return;
            }

            // A ship listed on the market is locked until it sells or the listing ends (plan section 7).
            if (ShipTransfer.IsListedForSale(dbShip.Id))
            {
                SendMessageToPC(player, ColorToken.Red("This ship is listed for sale and cannot fly until the listing ends."));
                return;
            }

            if ((_shipClones.ContainsKey(dbShip.Id) && !GetIsObjectValid(_shipClones[dbShip.Id])) ||
                _pendingShipLaunches.Values.Any(launch => launch.ShipId == dbShip.Id))
            {
                SendMessageToPC(player, ColorToken.Red("This ship's controls are in use."));
                return;
            }

            if (!CanPlayerUseShip(player, dbShip.Status))
            {
                var skillError = GetShipSkillRequirementError(player, dbShip.Status);
                SendMessageToPC(player, ColorToken.Red(string.IsNullOrEmpty(skillError)
                    ? "You do not have the ability to pilot this ship."
                    : skillError));
                return;
            }

            // A ship waiting in an open Rift Run while its pilot was logged out launches into the run's current chamber
            // (W11k).
            var rejoinLocation = GetRiftRunRejoinLocation(dbShip.Id);
            if (rejoinLocation != null)
            {
                BeginShipLaunch(player, dbShip.Id, rejoinLocation, GetLocation(player));
                return;
            }

            var dbProperty = DB.Get<WorldProperty>(propertyId);

            // The existence of a current location means the ship is currently in space.
            // Warp the player to the ship's location.
            // Otherwise the player is docked. Warp the player to the space location of this dock.
            if (dbProperty == null ||
                (!dbProperty.Positions.TryGetValue(PropertyLocationType.CurrentPosition, out var propertyLocation) &&
                 !dbProperty.Positions.TryGetValue(PropertyLocationType.SpacePosition, out propertyLocation)) ||
                propertyLocation == null || string.IsNullOrWhiteSpace(propertyLocation.AreaResref))
            {
                SendMessageToPC(player, ColorToken.Red("Unable to locate this starship's space destination. Notify an admin."));
                Log.WriteStructured(LogGroup.Space, "Ship launch destination missing: PlayerId={PlayerId} ShipId={ShipId} PropertyId={PropertyId}", playerId, dbShip.Id, propertyId);
                return;
            }

            var spaceArea = Area.GetAreaByResref(propertyLocation.AreaResref);
            if (!GetIsObjectValid(spaceArea) || spaceArea == area)
            {
                SendMessageToPC(player, ColorToken.Red("This starship's space destination is unavailable. Notify an admin."));
                Log.WriteStructured(LogGroup.Space, "Ship launch area unavailable: PlayerId={PlayerId} ShipId={ShipId} AreaResref={AreaResref}", playerId, dbShip.Id, propertyLocation.AreaResref);
                return;
            }

            // A launch from a dock inside a Small/Medium-only area needs a hull that fits.
            if (!CanShipEnterArea(player, dbShip, spaceArea))
                return;

            var spacePosition = Vector3(propertyLocation.X, propertyLocation.Y, propertyLocation.Z);
            var location = Location(spaceArea, spacePosition, propertyLocation.Orientation);

            BeginShipLaunch(player, dbShip.Id, location, GetLocation(player));
        }

        /// <summary>
        /// Reserves the ship and sends the player to its space location. The launch completes when they arrive
        /// (<see cref="TryCompleteShipLaunch"/>); <paramref name="pilotLocation"/> is where their copy sits in the
        /// ship's interior, and <paramref name="undockFromShipId"/> names a host the ship leaves on arrival.
        /// </summary>
        private static void BeginShipLaunch(uint player, string shipId, Location location, Location pilotLocation, string undockFromShipId = "",
            string undockFromDerelictId = "")
        {
            var launch = new PendingShipLaunch
            {
                PlayerId = GetObjectUUID(player),
                ShipId = shipId,
                SpaceArea = GetAreaFromLocation(location),
                PilotLocation = pilotLocation,
                DepartureArea = GetArea(player),
                UndockFromShipId = undockFromShipId ?? string.Empty,
                UndockFromDerelictId = undockFromDerelictId ?? string.Empty,
                ExpiresAt = DateTime.UtcNow.AddSeconds(30)
            };
            _pendingShipLaunches[player] = launch;
            Scheduler.Schedule(() => CheckShipLaunch(player, launch), TimeSpan.FromMilliseconds(100));

            // Only movement belongs in the action queue. Area entry and the scheduler
            // complete the launch independently of client input and area-load timing.
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(location);
            });
        }

        /// <summary>Commits ship state only after the player reaches the requested destination area.</summary>
        private static bool TryCompleteShipLaunch(uint player)
        {
            if (!_pendingShipLaunches.TryGetValue(player, out var launch) || GetArea(player) != launch.SpaceArea)
                return false;

            _pendingShipLaunches.Remove(player);
            if (GetObjectUUID(player) != launch.PlayerId || IsPlayerInSpaceMode(player) || GetIsDead(player))
                return false;

            if (!string.IsNullOrEmpty(launch.UndockFromShipId))
                CompleteUndockLaunch(player, launch);
            if (!string.IsNullOrEmpty(launch.UndockFromDerelictId))
                CompleteDerelictUndock(player, launch);

            SetLocalLocation(player, "SPACE_INSTANCE_LOCATION", launch.PilotLocation);
            SetLocalBool(player, "SPACE_INSTANCE_LOCATION_SET", true);
            EnterSpaceMode(player, launch.ShipId, launch.PilotLocation);
            RejoinRiftRun(player, launch.ShipId);
            return true;
        }

        /// <summary>Polls independently of creature actions, preserving loading requests and expiring unstarted jumps.</summary>
        private static void CheckShipLaunch(uint player, PendingShipLaunch launch)
        {
            // A canceled request may have been replaced by a later click on the controls.
            if (!_pendingShipLaunches.TryGetValue(player, out var current) || current != launch)
                return;

            if (!GetIsObjectValid(player) || GetObjectUUID(player) != launch.PlayerId ||
                GetIsDead(player) || !GetIsObjectValid(launch.SpaceArea))
            {
                _pendingShipLaunches.Remove(player);
                return;
            }

            if (TryCompleteShipLaunch(player))
            {
                UpdateSpacePosition(player);
                return;
            }

            var area = GetArea(player);
            var interior = launch.DepartureArea;
            if (GetIsObjectValid(area) && area != interior && area != launch.SpaceArea)
            {
                _pendingShipLaunches.Remove(player);
                return;
            }

            // Cancel an unstarted jump after a bounded wait. A player already loading
            // the new area keeps their reservation until arrival, death, or disconnect.
            if (!launch.HasDeparted && area == interior && DateTime.UtcNow >= launch.ExpiresAt)
            {
                _pendingShipLaunches.Remove(player);
                SendMessageToPC(player, ColorToken.Red("Unable to reach this starship's space destination. Please try again or notify an admin."));
                Log.WriteStructured(LogGroup.Space, "Ship launch did not arrive: PlayerId={PlayerId} ShipId={ShipId} AreaResref={AreaResref}",
                    launch.PlayerId, launch.ShipId, GetResRef(launch.SpaceArea));
                return;
            }

            Scheduler.Schedule(() => CheckShipLaunch(player, launch), TimeSpan.FromMilliseconds(100));
        }

        /// <summary>
        /// Retrieves the slot number (1-30) of the ship module feat.
        /// </summary>
        /// <param name="feat">The feat to check</param>
        /// <returns>The slot number (1-30) of the ship module feat.</returns>
        public static int GetFeatSlotNumber(FeatType feat)
        {
            var slotNumber = (int)feat - (int)FeatType.ShipModule1 + 1;
            return slotNumber;
        }

        /// <summary>
        /// Retrieves the associated feat given a high slot number.
        /// Must be in the range of 1-10
        /// </summary>
        /// <param name="slot">The slot number. Range is 1-10</param>
        /// <returns>The feat associated with the high slot number</returns>
        public static FeatType HighSlotToFeat(int slot)
        {
            var featId = (int)(FeatType.ShipModule1) - 1 + slot;
            return (FeatType)featId;
        }

        /// <summary>
        /// Retrieves the associated feat given a low slot number.
        /// Must be in the range of 1-10
        /// </summary>
        /// <param name="slot">The slot number. Range is 1-10</param>
        /// <returns>The feat associated with the low slot number.</returns>
        public static FeatType LowSlotToFeat(int slot)
        {
            slot += 10; // Offset by 10 for low modules.
            var featId = (int)(FeatType.ShipModule1) - 1 + slot;
            return (FeatType)featId;
        }

        /// <summary>
        /// Retrieves the associated feat given a mid slot number.
        /// Must be in the range of 1-10
        /// </summary>
        /// <param name="slot">The slot number. Range is 1-10</param>
        /// <returns>The feat associated with the mid slot number.</returns>
        public static FeatType MidSlotToFeat(int slot)
        {
            slot += 20; // Offset by 20 for mid modules.
            var featId = (int)(FeatType.ShipModule1) - 1 + slot;
            return (FeatType)featId;
        }

        /// <summary>
        /// Converts a high slot feat to its slot number.
        /// </summary>
        /// <param name="feat">The feat to convert</param>
        /// <returns>The slot number associated with the feat.</returns>
        public static int HighFeatToSlot(FeatType feat)
        {
            var offset = (int)FeatType.ShipModule1 - 1;
            var slot = (int)feat - offset;

            return slot;
        }

        /// <summary>
        /// Converts a low slot feat to its slot number.
        /// </summary>
        /// <param name="feat">The feat to convert</param>
        /// <returns>The slot number associated with the feat.</returns>
        public static int LowFeatToSlot(FeatType feat)
        {
            var offset = (int)FeatType.ShipModule1 - 1;
            var slot = (int)feat - offset;
            slot -= 10; // Offset by 10 for low modules.

            return slot;
        }

        /// <summary>
        /// Converts a mid slot feat to its slot number.
        /// </summary>
        /// <param name="feat">The feat to convert</param>
        /// <returns>The slot number associated with the feat.</returns>
        public static int MidFeatToSlot(FeatType feat)
        {
            var offset = (int)FeatType.ShipModule1 - 1;
            var slot = (int)feat - offset;
            slot -= 20; // Offset by 20 for mid modules.

            return slot;
        }

        /// <summary>
        /// When a player enters the game, reapply any custom TLK strings related to ship module feats.
        /// </summary>
        private static void ReloadPlayerTlkStrings()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (!IsPlayerInSpaceMode(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbPlayerShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);

            foreach (var (slot, shipModule) in dbPlayerShip.Status.HighPowerModules)
            {
                var shipModuleDetail = _shipModules[shipModule.ItemTag];
                var feat = HighSlotToFeat(slot);
                ApplyShipModuleFeat(player, shipModuleDetail, feat);
            }
            foreach (var (slot, shipModule) in dbPlayerShip.Status.LowPowerModules)
                ApplyShipModuleFeat(player, _shipModules[shipModule.ItemTag], LowSlotToFeat(slot));
            foreach (var (slot, shipModule) in dbPlayerShip.Status.MidPowerModules)
                ApplyShipModuleFeat(player, _shipModules[shipModule.ItemTag], MidSlotToFeat(slot));
            RestoreShipModuleRecasts(player);
        }

        /// <summary>
        /// Checks whether a player is in Space mode.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <returns>true if player is in space mode, false otherwise</returns>
        public static bool IsPlayerInSpaceMode(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);
            return dbPlayer.ActiveShipId != Guid.Empty.ToString();
        }

        /// <summary>
        /// Makes the player enter space mode which changes the player's appearance, loads the ship's hot bar, etc.
        /// </summary>
        /// <param name="player">The player entering space mode.</param>
        /// <param name="shipId">The Id of the ship to enter space with.</param>
        /// <param name="pilotLocation">The interior location where the Pilot clone should remain.</param>
        public static void EnterSpaceMode(uint player, string shipId, Location pilotLocation)
        {
            // Ground effects must be removed when entering space mode.
            // Otherwise players could buff on the ground, then get those same bonuses while in space.
            StatusEffect.RemoveAllStatusEffects(player);
            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                RemoveEffect(player, effect);
            }

            ClonePlayerAndSit(player, pilotLocation);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbPlayerShip = DB.Get<PlayerShip>(shipId);
            var shipDetail = _shipTypes[dbPlayerShip.Status.ItemTag];
            ClearShipToggleState(player, dbPlayerShip.Status);
            dbPlayerShip.Status.PowerRouting = ShipPowerRouting.Balanced;

            // The droid crew takes its stations with the pilot, so the stations they work count from the start.
            var droidCrew = SeatDroidCrew(player, dbPlayerShip);
            var launchStatus = dbPlayerShip.Status;
            RecalculateShipStats(launchStatus, GetAbilityScore(player, AbilityType.Willpower), false,
                slot => GetStationSlotEffectiveness(player, launchStatus, slot));
            SetLocalFloat(player, Stat.ShipMovementSpeedVariable, (float)dbPlayerShip.Status.SpeedMultiplier);

            // Snapshot the character's appearance at the moment it gets overwritten so exiting
            // space restores exactly what they looked like, even if the value captured at
            // character initialization is stale or corrupted.
            var currentAppearance = GetAppearanceType(player);
            if (currentAppearance != AppearanceType.Invalid && !IsShipAppearance(currentAppearance))
            {
                dbPlayer.OriginalAppearanceType = currentAppearance;
            }

            // Update player appearance to match that of the ship and its Hull Design.
            SetCreatureAppearanceType(player, GetShipAppearance(dbPlayerShip, shipDetail));
            Stat.ApplyCreatureMovementRate(player);

            // Set active ship Id and serialize the player's hot bar.
            dbPlayer.SerializedHotBar = CreaturePlugin.SerializeQuickbar(player);
            dbPlayer.ActiveShipId = shipId;

            // Only the pilot's own slots become feats and hotbar buttons; a crew seat's slots live on its station hotbar.
            var pilotSlots = PilotHotBarRules.GetPilotSlots(dbPlayerShip.Status,
                tag => _shipModules.TryGetValue(tag, out var detail) && detail.Type != ShipModuleType.Passive);
            foreach (var slot in pilotSlots)
            {
                var feat = SlotNumberToFeat(slot);
                TryGetFittedModule(dbPlayerShip.Status, slot, out var shipModule);

                // Convert current ship module to feat.
                CreaturePlugin.AddFeat(player, feat);

                // Rename the feat to match the configured name on the ship module.
                ApplyShipModuleFeat(player, _shipModules[shipModule.ItemTag], feat);
            }

            // The All Weapons feat is owned before the hot bar loads, so a saved slot for it survives.
            CreaturePlugin.AddFeat(player, FeatType.ShipAllWeapons);
            CreaturePlugin.AddFeat(player, FeatType.ShipJumpToSignature);

            // Load the player's ship hot bar.
            if (!dbPlayerShip.PlayerHotBars.ContainsKey(playerId) ||
                !CreaturePlugin.DeserializeQuickbar(player, dbPlayerShip.PlayerHotBars[playerId]))
            {
                const int MaxSlots = 35;

                // Deserialization failed. Clear out the player's hot bar and start fresh.
                for (var slot = 0; slot <= MaxSlots; slot++)
                {
                    PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }

                var currentSlot = 0;
                foreach (var slot in pilotSlots)
                {
                    PlayerPlugin.SetQuickBarSlot(player, currentSlot, PlayerQuickBarSlot.UseFeat(SlotNumberToFeat(slot)));
                    currentSlot++;
                }

                dbPlayerShip.PlayerHotBars[playerId] = CreaturePlugin.SerializeQuickbar(player);
            }

            // A saved bar from before the crew stations (or a refit) may still hold a crew seat's slots.
            if (RemoveCrewSlotsFromHotBar(player, dbPlayerShip.Status))
                dbPlayerShip.PlayerHotBars[playerId] = CreaturePlugin.SerializeQuickbar(player);

            SpaceHud.EnsureAllWeaponsOnHotBar(player);
            EnsureJumpToSignatureOnHotBar(player, dbPlayerShip.Status);

            DB.Set(dbPlayer);
            DB.Set(dbPlayerShip);

            // Replace the unpiloted NPC with the player and mark the controls occupied
            // immediately; DestroyObject may leave the NPC valid until the frame ends.
            if (_shipClones.TryGetValue(dbPlayerShip.Id, out var clone) && GetIsObjectValid(clone))
            {
                DestroyObject(clone);
            }
            _shipClones[dbPlayerShip.Id] = OBJECT_INVALID;

            if(!_playersInSpace.Contains(player))
                _playersInSpace.Add(player);

            RegisterShipPilot(shipId, player);
            AnnounceDroidCrew(player, dbPlayerShip, droidCrew);
            LoadDockedShipInteriors(shipId);
            RestoreShipModuleRecasts(player);
            ExecuteScript("space_enter", player);
        }

        /// <summary>
        /// When a player enters the module, if they were piloting a ship, send them to the instance.
        /// Note that if the server rebooted since they logged off, the normal persistent locations script
        /// will take over and send them to the last dock they were at.
        /// </summary>
        public static void WarpPlayerInsideShip(uint player)
        {
            ExitSpaceMode(player);
            Ability.ReapplyPlayerAuraAOE(player);

            if (!GetLocalBool(player, "SPACE_INSTANCE_LOCATION_SET"))
                return;

            var location = GetLocalLocation(player, "SPACE_INSTANCE_LOCATION");

            AssignCommand(player, () => ClearAllActions());
            AssignCommand(player, () => ActionJumpToLocation(location));

            DeleteLocalLocation(player, "SPACE_INSTANCE_LOCATION");
            DeleteLocalBool(player, "SPACE_INSTANCE_LOCATION_SET");
        }

        private static void ClonePlayerAndSit(uint player, Location pilotLocation)
        {
            var copy = CopyObject(player, pilotLocation, OBJECT_INVALID, "spaceship_copy");
            var chair = GetNearestObjectByTag("pilot_chair", copy);
            SetName(copy, "Pilot");
            ChangeToStandardFaction(copy, StandardFaction.Defender);
            TakeGoldFromCreature(GetGold(copy), copy, true);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(copy)), copy);

            for (var item = GetFirstItemInInventory(copy); GetIsObjectValid(item); item = GetNextItemInInventory(copy))
            {
                SetDroppableFlag(item, false);
                DestroyObject(item);
            }

            AssignCommand(copy, () =>
            {
                ClearAllActions();
            });

            DelayCommand(1f, () =>
            {
                AssignCommand(copy, () =>
                {
                    ActionSit(chair);
                });
            });

            SetPlotFlag(copy, true);
            SetLocalObject(player, "SPACE_PILOT_CLONE", copy);
        }

        /// <summary>
        /// Applies the custom TLKs related to a ship module feat.
        /// Also applies an override of the ship module onto the ship feat's texture.
        /// </summary>
        /// <param name="player">The player who will receive the overrides.</param>
        /// <param name="shipModuleDetail">The ship module detail whose name, description, and graphic will be loaded.</param>
        /// <param name="feat">The ship module feat</param>
        private static void ApplyShipModuleFeat(uint player, ShipModuleDetail shipModuleDetail, FeatType feat)
        {
            if (!GetIsObjectValid(player)) return;
            if (!ShipModuleFeats.ContainsKey(feat)) return;

            var shipModuleFeat = ShipModuleFeats[feat];

            PlayerPlugin.SetTlkOverride(player, shipModuleFeat.NameTlkId, shipModuleFeat.SlotName + ":" + shipModuleDetail.Name);
            PlayerPlugin.SetTlkOverride(player, shipModuleFeat.DescriptionTlkId, shipModuleDetail.Description);

            SetTextureOverride(shipModuleFeat.TextureName, shipModuleDetail.Texture, player);
        }

        /// <summary>
        /// Determines whether an appearance type belongs to a registered ship.
        /// </summary>
        /// <param name="appearance">The appearance type to check.</param>
        /// <returns>true if the appearance is used by a ship, false otherwise</returns>
        private static bool IsShipAppearance(AppearanceType appearance)
        {
            return _shipAppearances.Contains(appearance);
        }

        /// <summary>
        /// Restores a player's character appearance after leaving space mode.
        /// If the stored appearance is missing or was corrupted to a ship appearance,
        /// it is repaired from the racial default and saved onto the provided entity.
        /// </summary>
        /// <param name="player">The player whose appearance will be restored.</param>
        /// <param name="dbPlayer">The player entity. Mutated if the stored appearance needs repair; the caller is responsible for persisting it.</param>
        private static void RestoreCharacterAppearance(uint player, Player dbPlayer)
        {
            var appearance = dbPlayer.OriginalAppearanceType;

            if (appearance == AppearanceType.Invalid || IsShipAppearance(appearance))
            {
                appearance = Race.GetDefaultAppearance(GetRacialType(player), GetGender(player)).AppearanceType;
                Log.Write(LogGroup.Space, $"Stored appearance of player {GetName(player)} ({dbPlayer.Id}) was unusable ({dbPlayer.OriginalAppearanceType}). Repaired to racial default {appearance}.");
                dbPlayer.OriginalAppearanceType = appearance;
            }

            SetCreatureAppearanceType(player, appearance);

            // The ship model leaves the previous visual transform behind; restore the character's own height.
            var scale = dbPlayer.AppearanceScale <= 0f ? 1.0f : dbPlayer.AppearanceScale;
            SetObjectVisualTransform(player, ObjectVisualTransform.Scale, scale);
            var headScale = dbPlayer.HeadAppearanceScale <= 0f ? 1.0f : dbPlayer.HeadAppearanceScale;
            HelmetModelRenderer.SetHeadScale(player, headScale);

            // Reapply material colors after the client rebuilds the character model.
            TintMapService.QueueRefresh(player);
        }

        /// <summary>
        /// Makes the player exit space mode which reverts the player's appearance, loads the character's hot bar, etc.
        /// </summary>
        /// <param name="player">The player exiting space mode.</param>
        public static void ExitSpaceMode(uint player)
        {
            if (!IsPlayerInSpaceMode(player))
                return;

            // Leaving space or docking returns the crew to their consoles (plan section 10). Posted droids stay posted.
            ReleaseShipCrew(player, "The ship's pilot has left the controls, so you leave your station.");
            UnloadDroidCrew(player);
            UnregisterShipPilot(player);
            CloneShip(player);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var shipId = dbPlayer.ActiveShipId;
            var dbShip = DB.Get<PlayerShip>(shipId);
            ClearShipToggleState(player, dbShip.Status);

            // Space auras end with the flight, and so do the statuses the ship picked up (a burn or a daze must not
            // follow the pilot onto the ground, as ground effects do not follow them into space).
            SpaceAuras.RemoveAuras(player);
            SpaceHostileAbilityRewards.Clear(player);
            SpaceSustainedFire.Clear(player);
            SpaceOrdnance.Clear(player);
            SpaceBulwark.Clear(player);
            SpaceCommand.Clear(player);
            StatusEffect.RemoveAllStatusEffects(player);
            dbShip.Status.PendingRiders?.Clear();

            ClearCurrentTarget(player);
            RestoreCharacterAppearance(player, dbPlayer);
            DeleteLocalFloat(player, Stat.ShipMovementSpeedVariable);
            Stat.ApplyCreatureMovementRate(player);
            Enmity.RemoveCreatureEnmity(player);

            ClearShipModuleRecasts(player, dbPlayer);
            // Save the ship's hot bar and unassign the active ship Id.
            dbShip.PlayerHotBars[playerId] = CreaturePlugin.SerializeQuickbar(player);
            dbPlayer.ActiveShipId = Guid.Empty.ToString();

            // Remove all module feats from the player.
            foreach (var (feat, _) in ShipModuleFeats)
            {
                CreaturePlugin.RemoveFeat(player, feat);
            }
            CreaturePlugin.RemoveFeat(player, FeatType.ShipAllWeapons);
            CreaturePlugin.RemoveFeat(player, FeatType.ShipJumpToSignature);
            ClearScanningState(player);

            // Load the player's hot bar.
            if (string.IsNullOrWhiteSpace(dbPlayer.SerializedHotBar) ||
                !CreaturePlugin.DeserializeQuickbar(player, dbPlayer.SerializedHotBar))
            {
                // Deserialization failed. Clear out the player's hot bar and start fresh.
                for (var slot = 0; slot <= 35; slot++)
                {
                    PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }

                dbPlayer.SerializedHotBar = CreaturePlugin.SerializeQuickbar(player);
            }

            DB.Set(dbPlayer);
            DB.Set(dbShip);

            // Destroy the NPC clone.
            DestroyPilotClone(player);

            if (_playersInSpace.Contains(player))
                _playersInSpace.Remove(player);

            ExecuteScript("space_exit", player);
        }

        private static void CloneShip(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var shipId = dbPlayer.ActiveShipId;
            var dbShip = DB.Get<PlayerShip>(shipId);
            var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
            var shipDetail = GetShipDetailByItemTag(dbShip.Status.ItemTag);

            // The existence of a current location on a ship property indicates it is currently in space.
            // Spawn an NPC representing the ship at the location of the player.
            if (dbProperty.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
            {
                if (_shipClones.ContainsKey(shipId) &&
                    !GetIsObjectValid(_shipClones[shipId]))
                {
                    var location = GetLocation(player);
                    var position = GetPositionFromLocation(location);
                    dbProperty.Positions[PropertyLocationType.CurrentPosition] = new PropertyLocation
                    {
                        AreaResref = GetResRef(GetAreaFromLocation(location)),
                        X = position.X,
                        Y = position.Y,
                        Z = position.Z,
                        Orientation = GetFacingFromLocation(location)
                    };
                    DB.Set(dbProperty);

                    var clone = CreateObject(ObjectType.Creature, "player_starship", location);
                    SetCreatureAppearanceType(clone, GetShipAppearance(dbShip, shipDetail));
                    SetName(clone, dbProperty.CustomName);

                    _shipClones[dbShip.Id] = clone;
                }
            }
            // Otherwise the assumption is the ship is docked. A clone isn't needed and the ship should be removed
            // from the cache.
            else
            {
                _shipClones.Remove(dbShip.Id);
            }
        }

        private static void DestroyPilotClone(uint player)
        {
            var copy = GetLocalObject(player, "SPACE_PILOT_CLONE");
            if (GetIsObjectValid(copy))
            {
                DestroyObject(copy);
            }

            DeleteLocalObject(player, "SPACE_PILOT_CLONE");
        }

        /// <summary>
        /// Determines if player can use the specified ship.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="playerShip">The player ship status to check</param>
        /// <returns>true if all requirements are met, false otherwise</returns>
        public static bool CanPlayerUseShip(uint player, ShipStatus playerShip)
        {
            if (!_shipTypes.ContainsKey(playerShip.ItemTag ?? string.Empty))
                return false;

            // Crew-station modules are the crew's to work, so the pilot needs only the Helm's (plan section 10).
            if (GetPilotModules(playerShip).Any(shipModule => !_shipModules.ContainsKey(shipModule.ItemTag ?? string.Empty)))
                return false;

            return string.IsNullOrEmpty(GetShipSkillRequirementError(player, playerShip));
        }

        /// <summary>
        /// Empties every quickbar button that holds a crew seat's module feat, so the pilot's bar keeps only
        /// pilot-seat abilities. Returns whether any button was cleared.
        /// </summary>
        private static bool RemoveCrewSlotsFromHotBar(uint player, ShipStatus status)
        {
            const int MaxSlots = 35;
            var changed = false;
            for (var index = 0; index <= MaxSlots; index++)
            {
                var button = PlayerPlugin.GetQuickBarSlot(player, index);
                if (button.ObjectType != QuickBarSlotType.Feat || !ShipModuleFeats.ContainsKey((FeatType)button.INTParam1))
                    continue;

                if (!PilotHotBarRules.IsCrewSlot(status, GetFeatSlotNumber((FeatType)button.INTParam1)))
                    continue;

                PlayerPlugin.SetQuickBarSlot(player, index, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                changed = true;
            }

            return changed;
        }

        /// <summary>The high, low and mid modules the pilot works: every one outside a crew-station slot.</summary>
        private static IEnumerable<ShipStatus.ShipStatusModule> GetPilotModules(ShipStatus playerShip)
        {
            foreach (var (slot, shipModule) in playerShip.HighPowerModules)
            {
                if (!IsStationSlot(playerShip, ShipStatCalculator.GetHotbarSlot(ShipModulePowerType.High, slot)))
                    yield return shipModule;
            }

            foreach (var (slot, shipModule) in playerShip.LowPowerModules)
            {
                if (!IsStationSlot(playerShip, ShipStatCalculator.GetHotbarSlot(ShipModulePowerType.Low, slot)))
                    yield return shipModule;
            }

            foreach (var (slot, shipModule) in playerShip.MidPowerModules)
            {
                if (!IsStationSlot(playerShip, ShipStatCalculator.GetHotbarSlot(ShipModulePowerType.Mid, slot)))
                    yield return shipModule;
            }
        }

        /// <summary>
        /// Returns the use error for the first unmet RequiresSkill rank on the ship's deed or any module the pilot
        /// works, or an empty string if the player meets them all. Crew-station modules are checked against their
        /// crew instead. Requirements are stored when the deed is registered and when a module is installed, so no
        /// item has to be deserialized.
        /// </summary>
        public static string GetShipSkillRequirementError(uint player, ShipStatus playerShip)
        {
            var error = Item.GetSkillRequirementsError(player, playerShip.RequiredSkills);
            if (!string.IsNullOrEmpty(error)) return error;

            foreach (var shipModule in GetPilotModules(playerShip))
            {
                error = Item.GetSkillRequirementsError(player, shipModule.RequiredSkills);
                if (!string.IsNullOrEmpty(error)) return error;
            }

            return string.Empty;
        }

        /// <summary>
        /// When a ship module item is examined, append the configured description to the item's description.
        /// </summary>
        [NWNEventHandler(ScriptName.OnExamineObjectBefore)]
        public static void ExamineShipModuleItem()
        {
            ApplyShipModuleExamineDetails(StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID")));
        }

        /// <summary>
        /// Applies ship module examine details to an item.
        /// </summary>
        public static void ApplyExamineDetails(uint item)
        {
            ApplyShipModuleExamineDetails(item);
        }

        /// <summary>
        /// Combines an item's authored description with its ship module description. An item with no
        /// authored description reports its base item's placeholder text, which is dropped.
        /// </summary>
        public static string BuildShipModuleDescription(string originalDescription, string baseItemDescription, string moduleDescription)
        {
            var original = originalDescription?.Trim() ?? string.Empty;
            if (original.Length == 0 || original == (baseItemDescription?.Trim() ?? string.Empty))
                return moduleDescription;

            return $"{original}\n{moduleDescription}";
        }

        private static void ApplyShipModuleExamineDetails(uint item)
        {
            // Must be an item
            if (GetObjectType(item) != ObjectType.Item) return;

            // Tag must be registered with the system.
            var itemTag = GetTag(item);
            if (!_shipModules.ContainsKey(itemTag)) return;

            var moduleDetail = GetShipModuleDetailByItemTag(itemTag);

            // A description must have been set.
            if (string.IsNullOrWhiteSpace(moduleDetail.Description)) return;

            // Append the ship module's description to the item's description.
            var baseItemDescription = GetStringByStrRef(StringToInt(Get2DAString("baseitems", "Description", (int)GetBaseItemType(item))));
            SetDescription(item, ShipComponents.BuildDescription(
                BuildShipModuleDescription(GetDescription(item, true), baseItemDescription, moduleDetail.Description),
                ShipComponents.GetTraits(item)));
        }

        /// <summary>
        /// When a ship module's feat is used, execute the currently equipped module's custom code.
        /// </summary>
        [NWNEventHandler(ScriptName.OnFeatUseBefore)]
        public static void HandleShipModuleFeats()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));
            ActivateShipModule(OBJECT_SELF, feat);
        }

        /// <summary>
        /// Validates and activates the fitted module. A toggle module is switched on (firing its first cycle
        /// at once) or, when already on, switched off. A one-press module fires once and recasts.
        /// </summary>
        public static void ActivateShipModule(uint activator, FeatType feat)
        {
            // A crew member's station module fires from the ship (plan section 10).
            if (TryGetCrewStation(activator, out var ship, out var seat))
            {
                ActivateShipModuleCore(ship, feat, false, activator, seat);
                return;
            }

            ActivateShipModuleCore(activator, feat, false);
        }

        /// <summary>
        /// Shared activation. With <paramref name="onlyTurnOn"/>, a toggle that is already on is left alone
        /// instead of being switched off (ship AI uses this). With a <paramref name="crewMember"/>, the ship fires a
        /// module of the crew member's <paramref name="crewSeat"/> at the crew member's own target, with the crew
        /// member's rank, stats and effectiveness. With a <paramref name="droid"/>, the ship fires a module of the
        /// droid's seat at the pilot's target (repairers at the ship itself), with the droid's tier and
        /// effectiveness, and nobody is told of a refusal.
        /// </summary>
        private static void ActivateShipModuleCore(
            uint activator,
            FeatType feat,
            bool onlyTurnOn,
            uint crewMember = OBJECT_INVALID,
            ShipCrewSeat crewSeat = ShipCrewSeat.Invalid,
            ShipDroidCrew droid = null)
        {
            if (!ShipModuleFeats.ContainsKey(feat)) return;
            var activatorShipStatus = GetShipStatus(activator);
            if (activatorShipStatus == null) return;
            var slotNumber = GetFeatSlotNumber(feat);
            var isDroid = droid != null;
            var isCrew = isDroid || (crewMember != OBJECT_INVALID && crewMember != activator);
            if (isDroid)
            {
                crewMember = OBJECT_INVALID;
                crewSeat = droid.Seat;
            }

            // Feedback goes to whoever pressed the module. A droid has no one to tell.
            var user = isDroid ? OBJECT_INVALID : isCrew ? crewMember : activator;

            if (slotNumber < 1 || slotNumber > 30)
            {
                Log.Write(LogGroup.Error, $"Failed to locate matching ship module by its feat for player {GetName(user)}");
                SendActivationMessage(user, "Unable to use that module.");
                return;
            }

            // Slots 1-10 are high powered, 11-20 low powered and 21-30 mid powered.
            if (!TryGetFittedModule(activatorShipStatus, slotNumber, out var shipModule)) return;

            // A crew-station slot works only for the crew member at its seat; the pilot fires Helm slots only.
            var slotSeat = ShipCrewSeats.GetSeatForSlot(activatorShipStatus.Size, activatorShipStatus.SlotTags, slotNumber);
            if (isCrew)
            {
                if (slotSeat != crewSeat)
                {
                    SendActivationMessage(user, $"That module belongs to the {ShipCrewSeats.GetSeatName(slotSeat)}.");
                    return;
                }

                if (isDroid)
                {
                    if (!DroidCrewRules.MeetsRank(shipModule.RequiredSkills, droid.Tier))
                        return;
                }
                else
                {
                    var rankError = GetStationRankError(crewMember, crewSeat, shipModule);
                    if (!string.IsNullOrEmpty(rankError))
                    {
                        SendActivationMessage(user, rankError);
                        return;
                    }
                }
            }
            else if (slotSeat != ShipCrewSeat.Pilot)
            {
                SendActivationMessage(activator, SpaceCrew.IsSeatCrewed(activator, activatorShipStatus, slotNumber)
                    ? $"This module belongs to the {ShipCrewSeats.GetSeatName(slotSeat)} station."
                    : $"This slot needs a crew member at the {ShipCrewSeats.GetSeatName(slotSeat)} station.");
                return;
            }

            // Found the ship module. Run validation checks.
            var shipModuleDetails = _shipModules[shipModule.ItemTag];
            var isToggle = shipModuleDetails.Activation == ShipModuleActivation.Toggle;

            // A toggle that is on is switched off by its next press.
            if (isToggle && GetActiveToggle(activatorShipStatus, slotNumber) != null)
            {
                if (!onlyTurnOn)
                    TurnOffToggle(activator, activatorShipStatus, slotNumber);
                return;
            }

            // A dazed ship cannot switch a module on or fire one (a toggle that is on can still be switched off above).
            if (IsShipIncapacitated(activator))
            {
                SendActivationMessage(user, "You cannot use modules while incapacitated.");
                return;
            }

            // A cloaking or cloaked ship uses nothing but its Cloaking Device, and its weapons stay locked after it decloaks.
            var cloakBlock = GetCloakModuleBlock(activator, shipModuleDetails);
            if (!string.IsNullOrEmpty(cloakBlock))
            {
                SendActivationMessage(user, cloakBlock);
                return;
            }

            // Check capacitor requirements
            var requiredCapacitor = GetModuleCapacitorCost(activator, activatorShipStatus, shipModule, shipModuleDetails);

            if (activatorShipStatus.Capacitor < requiredCapacitor)
            {
                SendActivationMessage(user, $"Your ship does not have enough capacitor to use that module. (Required: {requiredCapacitor})");
                return;
            }

            // Check recast requirements
            var now = DateTime.UtcNow;
            if (shipModule.RecastTime > now)
            {
                SendActivationMessage(user, "That module is not ready.");
                return;
            }

            // Check global recast requirements. Toggles run on their own cycle and ignore it. Each crew member has
            // their own (plan section 9).
            _crewAssignments.TryGetValue(crewMember, out var crewAssignment);
            var globalRecast = isCrew ? crewAssignment?.GlobalRecast ?? DateTime.MinValue : activatorShipStatus.GlobalRecast;
            if (!isToggle && globalRecast > now)
            {
                return;
            }

            // A crew member fires at the target they picked; a droid at the pilot's.
            var (target, targetShipStatus) = GetCurrentTarget(isDroid ? activator : user);

            // A droid's repairers work on its own ship.
            if (isDroid && ShipToggleRules.GetTargetMode(_shipModules[shipModule.ItemTag].Type) == ShipToggleTargetMode.Locked)
                target = activator;

            // Targeting your own ship uses the one status, so the module's effect and its cost are saved together.
            if (target == activator)
                targetShipStatus = activatorShipStatus;

            // Check for valid object type if the ship module requires it.
            if (shipModuleDetails.ValidTargetTypes.Count > 0 &&
                !shipModuleDetails.ValidTargetTypes.Contains(GetObjectType(target)) &&
                !(shipModuleDetails.CanTargetSelf && !GetIsObjectValid(target)))
            {
                SendActivationMessage(user, "This module cannot be used on that target type.");
                return;
            }

            // Check for a selected target that doesn't have a ship status.
            if (GetObjectType(target) != ObjectType.Placeable && targetShipStatus == null && !shipModuleDetails.CanTargetSelf)
            {
                SendActivationMessage(user, "Invalid target.");
                return;
            }

            // Player ships attack each other only where the ground rules allow it (plan section 9, PvP); the hostile
            // E-War modules follow the same rule (W11k). Modules that work around the ship pick their own enemies and
            // skip the ones the rules protect.
            if (ShipToggleRules.TargetsEnemy(shipModuleDetails.Type) &&
                !shipModuleDetails.CanTargetSelf &&
                GetIsObjectValid(target) &&
                target != activator)
            {
                var pvpRefusal = GetPvpRefusal(activator, target);
                if (!string.IsNullOrEmpty(pvpRefusal))
                {
                    SendActivationMessage(user, pvpRefusal);
                    return;
                }
            }

            // A toggle that fires at the ship's target needs one to follow.
            var targetMode = ShipToggleRules.GetTargetMode(shipModuleDetails.Type);
            if (isToggle && targetMode == ShipToggleTargetMode.Follow && !GetIsObjectValid(target))
            {
                SendActivationMessage(user, "Invalid target.");
                return;
            }

            // Check to ensure activator is within maximum distance.
            var maxDistance = shipModuleDetails.ModuleMaxDistanceAction == null ? 10f : shipModuleDetails.ModuleMaxDistanceAction(activator, activatorShipStatus, target, targetShipStatus, shipModule.ModuleBonus);
            if (GetIsPC(activator) && GetDistanceBetween(activator, target) > maxDistance)
            {
                SendActivationMessage(user, $"Target is too far away. Maximum distance: {maxDistance} meters.");
                return;
            }

            if (!GetIsPC(activator) &&
                IsOffensiveShipModule(shipModuleDetails.Type) &&
                !shipModuleDetails.CanTargetSelf &&
                GetDistanceBetween(activator, target) > maxDistance)
            {
                return;
            }

            // A crew member's module reads their stats and works at the station's effectiveness while it validates
            // and fires (a droid at the Sensors seat is validated as a droid, not as the pilot).
            using var crewFire = SpaceCrew.BeginStationFire(isDroid
                ? BuildDroidFireContext(activator, droid)
                : isCrew ? BuildCrewFireContext(activator, crewMember, crewSeat) : null);

            // Run any custom validation specific to the ship module.
            if (shipModuleDetails.ModuleValidationAction != null)
            {
                var result = shipModuleDetails.ModuleValidationAction(activator, activatorShipStatus, target, targetShipStatus, shipModule.ModuleBonus);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    SendActivationMessage(user, result);
                    return;
                }
            }

            var recastSeconds = shipModuleDetails.CalculateRecastAction?.Invoke(activator, activatorShipStatus, shipModule.ModuleBonus);

            if (isToggle)
            {
                var cycle = recastSeconds is > 0f ? recastSeconds.Value : (float)ShipToggleRules.LoopSeconds;
                var lockedTarget = OBJECT_INVALID;

                // Support modules lock onto the target that was selected when they were turned on, or the ship itself.
                if (targetMode == ShipToggleTargetMode.Locked)
                {
                    var usable = GetIsObjectValid(target) && targetShipStatus != null && !IsShipHostile(activator, target);
                    lockedTarget = ShipToggleRules.ChooseLockTarget(usable, target, activator);
                }

                var fireTarget = ShipToggleRules.ChooseTarget(targetMode, target, lockedTarget, activator);
                var fireTargetStatus = fireTarget == activator
                    ? activatorShipStatus
                    : fireTarget == target ? targetShipStatus : GetShipStatus(fireTarget);

                Toggles(activatorShipStatus).Add(new ShipStatus.ShipActiveModule
                {
                    Slot = slotNumber,
                    NextCycleTime = now.AddSeconds(cycle),
                    TargetId = lockedTarget
                });

                // The first cycle fires at once; the auto-fire loop takes over when it ends.
                FireShipModule(activator, activatorShipStatus, slotNumber, shipModule, shipModuleDetails,
                    fireTarget, fireTargetStatus, requiredCapacitor, true, now, now.AddSeconds(cycle), false);
                UpdateToggleWake(activator, activatorShipStatus);
                return;
            }

            // One press: run the module-specific code now, then start the recast and the global recast. A crew
            // member's global recast is their own and leaves the pilot's alone.
            FireShipModule(activator, activatorShipStatus, slotNumber, shipModule, shipModuleDetails,
                target, targetShipStatus, requiredCapacitor, recastSeconds != null, now,
                now.AddSeconds(recastSeconds ?? 0f), !isCrew);

            if (isCrew && crewAssignment != null && recastSeconds != null)
                crewAssignment.GlobalRecast = now.AddSeconds(2f);
        }

        /// <summary>
        /// Regenerates a ship's shield and capacitor for one second (plan section 5). Both rates are fractional;
        /// the remainder carries to the next tick in the ship status. ShieldRechargeRate no longer applies.
        /// </summary>
        private static void ApplyAutoShipRecovery(uint player, ShipStatus shipStatus)
        {
            // A Bulwark Shield Generator raises the cap while it runs; shield above the cap is lost when it lapses.
            var maxShield = shipStatus.GetEffectiveMaxShield(DateTime.UtcNow);
            if (shipStatus.Shield > maxShield)
                shipStatus.Shield = maxShield;

            // Shield recovery
            if (shipStatus.Shield >= maxShield)
            {
                shipStatus.ShieldRegenRemainder = 0;
            }
            else
            {
                shipStatus.ShieldRegenRemainder += GetShipShieldRegenPerSecond(player, shipStatus);

                var wholeShield = (int)shipStatus.ShieldRegenRemainder;
                if (wholeShield > 0)
                {
                    shipStatus.ShieldRegenRemainder -= wholeShield;
                    RestoreShield(player, shipStatus, wholeShield);
                }
            }

            // Capacitor recovery
            if (shipStatus.Capacitor >= shipStatus.MaxCapacitor)
            {
                shipStatus.CapacitorRegenRemainder = 0;
            }
            else
            {
                shipStatus.CapacitorRegenRemainder += GetShipCapacitorRegenPerSecond(player, shipStatus);

                var wholeCapacitor = (int)shipStatus.CapacitorRegenRemainder;
                if (wholeCapacitor > 0)
                {
                    shipStatus.CapacitorRegenRemainder -= wholeCapacitor;
                    RestoreCapacitor(player, shipStatus, wholeCapacitor);
                }
            }

            if(GetIsPC(player))
                ExecuteScript("pc_target_upd", player);
        }

        /// <summary>
        /// Recover player ships every second.
        /// </summary>
        private static void PlayerShipRecovery()
        {
            foreach (var player in _playersInSpace)
            {
                // Not in space mode, skip.
                if (!IsPlayerInSpaceMode(player))
                    continue;

                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);

                ApplyAutoShipRecovery(player, dbShip.Status);

                // Update changes
                DB.Set(dbShip);
            }
        }

        public static void RestoreShield(uint creature, ShipStatus shipStatus, int amount)
        {
            var maxShield = shipStatus.GetEffectiveMaxShield(DateTime.UtcNow);
            shipStatus.Shield += amount;
            if (shipStatus.Shield > maxShield)
                shipStatus.Shield = maxShield;

            ExecuteScript("pc_shld_adjusted", creature);
        }

        public static void ReduceShield(uint creature, ShipStatus shipStatus, int amount)
        {
            shipStatus.Shield -= amount;
            if (shipStatus.Shield < 0)
                shipStatus.Shield = 0;

            ExecuteScript("pc_shld_adjusted", creature);
        }

        public static void RestoreHull(uint creature, ShipStatus shipStatus, int amount)
        {
            shipStatus.Hull += amount;
            if (shipStatus.Hull > shipStatus.MaxHull)
                shipStatus.Hull = shipStatus.MaxHull;

            ExecuteScript("pc_hull_adjusted", creature);
        }

        public static void ReduceHull(uint creature, ShipStatus shipStatus, int amount)
        {
            shipStatus.Hull -= amount;
            if (shipStatus.Hull < 0)
                shipStatus.Hull = 0;

            if (shipStatus.Hull <= 0)
            {
                AssignCommand(OBJECT_SELF, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), creature));
            }

            ExecuteScript("pc_hull_adjusted", creature);
        }

        public static void RestoreCapacitor(uint creature, ShipStatus shipStatus, int amount)
        {
            shipStatus.Capacitor += amount;
            if (shipStatus.Capacitor > shipStatus.MaxCapacitor)
                shipStatus.Capacitor = shipStatus.MaxCapacitor;

            ExecuteScript("pc_cap_adjusted", creature);
        }

        public static void ReduceCapacitor(uint creature, ShipStatus shipStatus, int amount)
        {
            shipStatus.Capacitor -= amount;
            if (shipStatus.Capacitor < 0)
                shipStatus.Capacitor = 0;

            ExecuteScript("pc_cap_adjusted", creature);
        }

        /// <summary>
        /// When a creature spawns, track it in the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpawnBefore)]
        public static void CreatureSpawn()
        {
            var creature = OBJECT_SELF;
            var creatureTag = GetTag(creature);

            // Not registered with the space system. Exit early.
            if (!_spaceObjects.ContainsKey(creatureTag)) return;

            var registeredEnemyType = _spaceObjects[creatureTag];
            if (!_shipTypes.TryGetValue(registeredEnemyType.ShipItemTag ?? string.Empty, out var shipDetail))
            {
                // Placeable space objects (asteroids, gas clouds, wrecks) carry an unused item tag (plan section 16, bug 18).
                Log.Write(LogGroup.Error, $"Space object '{creatureTag}' points at '{registeredEnemyType.ShipItemTag}', which is not a registered ship.");
                return;
            }

            // Every NPC ship takes its hull, weapons and support modules from its preset (RevampNpcShipCatalog).
            if (!_npcPresets.TryGetValue(creatureTag, out var preset))
            {
                Log.Write(LogGroup.Error, $"Space object '{creatureTag}' has no NPC ship preset and was not given a ship.");
                return;
            }

            _shipNPCs[creature] = ShipNpcRules.BuildStatus(preset, registeredEnemyType.ShipItemTag, shipDetail.CapitalShip, DateTime.UtcNow);
        }

        /// <summary>
        /// When a creature dies, remove it from the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void CreatureDeath()
        {
            var creature = OBJECT_SELF;

            _toggleWake.Remove(creature);
            _shipCombatAt.Remove(creature);
            if (_shipNPCs.ContainsKey(creature))
            {
                _shipNPCs.Remove(creature);
            }
        }

        /// <summary>
        /// Retrieves the ship status of a given creature.
        /// If creature is an NPC, it will be retrieved from the cache.
        /// If creature is a PC, it will be retrieved from the database.
        /// </summary>
        /// <param name="creature">The creature to get the status of</param>
        /// <returns>A ship status containing current statistics about a creature's ship.</returns>
        public static ShipStatus GetShipStatus(uint creature)
        {
            if (!GetIsObjectValid(creature))
            {
                return null;
            }

            // Player ship statuses must retrieved from the database.
            if (GetIsPC(creature))
            {
                var targetPlayerId = GetObjectUUID(creature);
                var dbTargetPlayer = DB.Get<Player>(targetPlayerId);

                if (dbTargetPlayer.ActiveShipId == Guid.Empty.ToString())
                    return null;

                var dbPlayerShip = DB.Get<PlayerShip>(dbTargetPlayer.ActiveShipId);

                return dbPlayerShip?.Status;
            }
            // NPC ship statuses are stored directly in cache so we can return them immediately.
            else
            {
                return _shipNPCs.ContainsKey(creature)
                    ? _shipNPCs[creature]
                    : null;
            }
        }

        /// <summary>
        /// True for a real player's ship. Their ship values come from the hull, modules and attributes alone;
        /// NPCs also add a level term (plan section 9) until the space enemy presets replace it.
        /// </summary>
        private static bool IsPlayerPilot(uint creature)
        {
            return GetIsPC(creature) && !GetIsDM(creature);
        }

        private static int GetNpcLevel(uint creature)
        {
            return Stat.GetNPCStats(creature).Level;
        }

        /// <summary>
        /// A pilot attribute for the ship math: a preset NPC ship's own, a droid's while one fires a station, otherwise
        /// the creature's ability score.
        /// </summary>
        private static int GetPilotScore(uint creature, ShipStatus status, AbilityType ability)
        {
            if (SpaceCrew.TryGetDroidAttribute(creature, ability, out var droidScore))
                return droidScore;

            return ShipNpcRules.PilotScore(status, ability, type => GetAbilityScore(creature, type));
        }

        /// <summary>
        /// The level term a legacy NPC ship adds to a value; zero for players, preset NPC ships and droids at a
        /// player's station.
        /// </summary>
        private static int GetNpcLevelBonus(uint creature, ShipStatus status, Func<int, int> bonusForLevel)
        {
            if (SpaceCrew.IsDroidStatSource(creature))
                return 0;

            return ShipNpcRules.UsesLevelTerm(IsPlayerPilot(creature), status)
                ? bonusForLevel(GetNpcLevel(creature))
                : 0;
        }

        private static ShipSize NormalizeShipSize(ShipSize size)
        {
            return size == ShipSize.Invalid ? ShipSize.Small : size;
        }

        /// <summary>
        /// Calculates the accuracy of a ship firing the given shot: ship and module Accuracy plus the pilot's
        /// AGI (energy) or PER (launcher) x 3. A legacy NPC adds 8 + 2 x level; a preset NPC's values include it.
        /// </summary>
        public static int GetShipAccuracy(uint attacker, ShipStatus attackerStatus, SpaceWeaponShot shot)
        {
            var accuracy = SpaceCombatMath.Accuracy(
                attackerStatus.Accuracy + attackerStatus.GetAccuracyPenalty(DateTime.UtcNow),
                shot.Accuracy,
                GetPilotScore(attacker, attackerStatus, AbilityType.Agility),
                GetPilotScore(attacker, attackerStatus, AbilityType.Perception),
                shot.Family);

            accuracy += GetNpcLevelBonus(attacker, attackerStatus, level => 8 + 2 * level);

            // The pilot's Accuracy and Accuracy % stats apply as they do on the ground.
            return SpaceCombatMath.AdjustRating(
                accuracy,
                Stat.GetStatAdjustment(attacker, StatType.Accuracy),
                Stat.GetStatAdjustment(attacker, StatType.AccuracyPercentAdjustment) +
                Stat.GetStatAdjustment(attacker, StatType.SpaceAccuracyPercentAdjustment));
        }

        /// <summary>
        /// Calculates the evasion of a ship: ship Evasion plus the pilot's AGI x 3. A legacy NPC adds 8 + 2 x level.
        /// The pilot's Evasion, Evasion % and Ranged Evasion % stats apply (every space weapon is ranged).
        /// </summary>
        public static int GetShipEvasion(uint defender, ShipStatus defenderStatus)
        {
            var evasion = SpaceCombatMath.Evasion(
                defenderStatus.Evasion,
                GetPilotScore(defender, defenderStatus, AbilityType.Agility));

            evasion += GetNpcLevelBonus(defender, defenderStatus, level => 8 + 2 * level);

            return SpaceCombatMath.AdjustRating(
                evasion,
                Stat.GetStatAdjustment(defender, StatType.Evasion),
                Stat.GetStatAdjustment(defender, StatType.EvasionPercentAdjustment) +
                Stat.GetStatAdjustment(defender, StatType.RangedEvasionPercentAdjustment));
        }

        /// <summary>
        /// Calculates the attack of a ship firing the given shot: ship and module Attack plus the pilot's damage
        /// attribute (PER for energy, MGT for launchers). A legacy NPC adds 8 + 2 x level.
        /// </summary>
        public static int GetShipAttack(uint attacker, ShipStatus attackerStatus, SpaceWeaponShot shot)
        {
            var damageAttribute = GetDamageAttribute(attacker, attackerStatus, shot.Family);
            var attack = SpaceCombatMath.Attack(attackerStatus.Attack, shot.Attack, damageAttribute);

            attack += GetNpcLevelBonus(attacker, attackerStatus, level => 8 + 2 * level);

            return SpaceCombatMath.AdjustRating(
                attack,
                Stat.GetStatAdjustment(attacker, StatType.Attack),
                Stat.GetStatAdjustment(attacker, StatType.AttackPercentAdjustment));
        }

        /// <summary>
        /// Calculates the defense of a ship: ship Defense plus the pilot's VIT. A legacy NPC adds 8 + 1.2 x level.
        /// The pilot's Defense and Defense % stats apply; the damage-type stats (Physical, Force) do not.
        /// </summary>
        public static int GetShipDefense(uint defender, ShipStatus defenderStatus)
        {
            var defense = SpaceCombatMath.Defense(
                defenderStatus.Defense,
                GetPilotScore(defender, defenderStatus, AbilityType.Vitality));

            defense += GetNpcLevelBonus(defender, defenderStatus, level => 8 + (int)(1.2f * level));

            return SpaceCombatMath.AdjustRating(
                defense,
                Stat.GetStatAdjustment(defender, StatType.Defense),
                Stat.GetStatAdjustment(defender, StatType.DefensePercentAdjustment) +
                Stat.GetStatAdjustment(defender, StatType.SpaceDefensePercentAdjustment));
        }

        /// <summary>
        /// Shield regained per second: a preset NPC ship's own rate, otherwise the size and WIL formula, changed by
        /// the Engineer's power routing.
        /// </summary>
        public static double GetShipShieldRegenPerSecond(uint ship, ShipStatus shipStatus)
        {
            var wil = GetPilotScore(ship, shipStatus, AbilityType.Willpower);
            return PowerRoutingRules.ApplyShieldRegen(
                ShipNpcRules.ShieldRegenPerSecond(shipStatus, shipStatus.GetEffectiveMaxShield(DateTime.UtcNow), wil),
                shipStatus.PowerRouting);
        }

        /// <summary>
        /// Capacitor regained per second: the hull's regen plus 0.05 per point of WIL, changed by the capacitor regen
        /// stat (a Wild Space environment).
        /// </summary>
        public static double GetShipCapacitorRegenPerSecond(uint ship, ShipStatus shipStatus)
        {
            var wil = GetPilotScore(ship, shipStatus, AbilityType.Willpower);
            return WildSpaceRules.AdjustCapacitorRegen(
                SpaceCombatMath.CapacitorRegenPerSecond(shipStatus.CapacitorRegenPerSecond, wil),
                Stat.GetStatAdjustment(ship, StatType.SpaceCapacitorRegenPercentAdjustment));
        }

        /// <summary>
        /// A ship's critical rate against a target of equal Vitality, before weapon rank: the critical rate and
        /// weapon critical rate stats on top of the base rate and the pilot's PER.
        /// </summary>
        public static int GetShipCriticalRate(uint attacker, ShipStatus attackerStatus)
        {
            var per = GetPilotScore(attacker, attackerStatus, AbilityType.Perception);
            var modifier = Stat.GetStatAdjustment(attacker, StatType.CriticalRatePercentAdjustment) +
                           Stat.GetStatAdjustment(attacker, StatType.WeaponCriticalRatePercentAdjustment);
            return SpaceCombatMath.CriticalRate(per, per, 0, modifier);
        }

        /// <summary>
        /// Gets the ability score a weapon family's damage scales with: PER for energy weapons, MGT for launchers.
        /// </summary>
        public static int GetDamageAttribute(uint attacker, ShipStatus attackerStatus, SpaceWeaponFamily family)
        {
            return SpaceCombatMath.DamageAttribute(
                family,
                GetPilotScore(attacker, attackerStatus, AbilityType.Perception),
                GetPilotScore(attacker, attackerStatus, AbilityType.Might));
        }

        private static ResistanceType GetShipResistanceType(CombatDamageType damageType)
        {
            return damageType switch
            {
                CombatDamageType.Thermal => ResistanceType.Thermal,
                CombatDamageType.EM => ResistanceType.EM,
                CombatDamageType.Explosive => ResistanceType.Explosive,
                _ => ResistanceType.Invalid
            };
        }

        /// <summary>
        /// Rolls one weapon shot without applying it: hit, critical, then damage with the pilot's damage stats,
        /// the size penalty, the attacker's damage bonus, the target's resistance and its damage-taken stat. A miss
        /// draws only the hit roll; a blinded attacker's hit also has to pass the blindness miss roll.
        /// A landed shot that consumes riders takes their bonus DMG, removes them from the ship status and lists
        /// them on the result; a miss leaves them waiting.
        /// </summary>
        public static ShipShotResult RollWeaponShot(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot)
        {
            var riders = shot.ConsumesRiders
                ? FindPendingRiders(activatorStatus, shot.Family, DateTime.UtcNow)
                : new List<SpaceShotRider>();
            if (riders.Count > 0)
                shot = shot.WithBonusDmg(riders.Sum(x => x.BonusDmg));

            var weaponSize = shot.Size ?? NormalizeShipSize(activatorStatus.Size);
            var targetSize = NormalizeShipSize(targetStatus.Size);

            // A crew member's station shot reads their attributes and stats and works at the station's
            // effectiveness; the ship stays the attacker (plan section 10).
            var source = SpaceCrew.GetStatSource(activator);
            var stationDmg = ShipCrewSeats.ScaleByEffectiveness(shot.Dmg, SpaceCrew.GetFireEffectiveness(activator));
            if (stationDmg != shot.Dmg)
                shot = shot.WithBonusDmg(stationDmg - shot.Dmg);

            var chanceToHit = SpaceCombatMath.HitChance(
                GetShipAccuracy(source, activatorStatus, shot),
                GetShipEvasion(target, targetStatus),
                weaponSize,
                targetSize,
                shot.IgnoreSizePenalty);
            var result = new ShipShotResult
            {
                ChanceToHit = chanceToHit,
                CreditedPlayer = SpaceCrew.GetCreditedPlayer(activator),
                CreditedSkill = SpaceCrew.GetCreditedSkill(activator, SkillType.Piloting),
            };

            var per = GetPilotScore(source, activatorStatus, AbilityType.Perception);
            var targetVit = GetPilotScore(target, targetStatus, AbilityType.Vitality);
            var attack = GetShipAttack(source, activatorStatus, shot);
            var defense = GetShipDefense(target, targetStatus);
            var damageAttribute = GetDamageAttribute(source, activatorStatus, shot.Family);

            var hitRolled = Random.D100(1) <= chanceToHit;
            var blindMiss = hitRolled && IsBlinded(activator) && Random.D100(1) <= SpaceCombatMath.BlindMissChance;
            if (!hitRolled || blindMiss)
            {
                // A miss still draws the target's attention, as it always has. It does not roll any damage.
                // Only a miss on the target's evasion counts as an evaded attack.
                result.Evaded = !hitRolled;
                var (_, missMaximum) = SpaceCombatMath.DamageRange(attack, shot.Dmg, damageAttribute, defense, targetVit, false, shot.Rank);
                result.Enmity = missMaximum;
                return result;
            }

            result.IsHit = true;

            // The weapon critical rate stat belongs to the weapon's own cycles, as "weapon cycles have +N% Critical
            // Rate" reads; an ability shot takes only the general critical rate stat.
            var criticalRate = Stat.GetStatAdjustment(source, StatType.CriticalRatePercentAdjustment);
            if (!shot.IsAbility)
                criticalRate += Stat.GetStatAdjustment(source, StatType.WeaponCriticalRatePercentAdjustment);
            result.IsCritical = Random.D100(1) <= SpaceCombatMath.CriticalRate(per, targetVit, shot.Rank, criticalRate);

            // A warded ship takes a critical hit as a normal hit that rolls minimum damage.
            var criticalWarded = result.IsCritical && SpaceBulwark.TryWardCriticalHit(target);
            if (criticalWarded)
            {
                result.IsCritical = false;
                Combat.SendIncomingCriticalHitDowngradeFeedback(activator, target);
            }

            // Consecutive energy weapon hits against one target add their stacks' DMG to this same shot.
            var shotDmg = shot.Dmg + SpaceSustainedFire.RegisterHit(activator, target, shot.Family);

            var (minimum, maximum) = SpaceCombatMath.DamageRange(
                attack, shotDmg, damageAttribute, defense, targetVit, result.IsCritical, shot.Rank);
            var rolled = criticalWarded ? minimum : (int)Random.NextFloat(minimum, maximum);

            if (result.IsCritical)
            {
                var criticalPercent = Stat.GetStatAdjustment(source, StatType.CriticalDamagePercentAdjustment) +
                                      SpaceCombatMath.HighHullCriticalDamageAdjustment(
                                          targetStatus.Hull,
                                          targetStatus.MaxHull,
                                          Stat.GetStatAdjustment(source, StatType.CriticalDamageHighHPTargetThresholdPercent),
                                          Stat.GetStatAdjustment(source, StatType.CriticalDamageHighHPTargetPercentAdjustment));
                rolled = SpaceCombatMath.CriticalDamage(rolled, true, criticalPercent);
            }

            var shapePercent = SpaceCombatMath.ShapeDamagePercent(
                shot.IsAbility,
                shot.IsArea,
                Stat.GetStatAdjustment(source, StatType.SingleTargetDamagePercentAdjustment),
                Stat.GetStatAdjustment(source, StatType.AreaAbilityDamagePercentAdjustment));

            // A launcher's shot also takes the launcher shape stats (the Ordnance Stance).
            if (shot.Family == SpaceWeaponFamily.Launcher)
            {
                shapePercent += SpaceCombatMath.ShapeDamagePercent(
                    shot.IsAbility,
                    shot.IsArea,
                    Stat.GetStatAdjustment(source, StatType.SpaceLauncherSingleTargetDamagePercentAdjustment),
                    Stat.GetStatAdjustment(source, StatType.SpaceLauncherAreaAbilityDamagePercentAdjustment));
            }

            rolled = SpaceCombatMath.ApplyDamageDealt(
                rolled,
                Stat.GetStatAdjustment(source, StatType.DamageDealtPercentAdjustment),
                shapePercent);

            var bonusPercent = 0;
            activatorStatus.DamageBonusPercent?.TryGetValue(shot.DamageType, out bonusPercent);
            bonusPercent += PowerRoutingRules.WeaponDamagePercent(activatorStatus.PowerRouting);
            bonusPercent += WildSpaceRules.EnvironmentDamagePercent(
                shot.DamageType,
                shot.IsAbility,
                Stat.GetStatAdjustment(source, StatType.SpaceEmDamagePercentAdjustment),
                Stat.GetStatAdjustment(source, StatType.SpaceAbilityDamagePercentAdjustment));

            var resistance = 0;
            targetStatus.Resistances?.TryGetValue(GetShipResistanceType(shot.DamageType), out resistance);
            resistance = SpaceCombatMath.HardenedResistance(resistance, targetStatus.HardenedAmount, targetStatus.HardenedUntil, DateTime.UtcNow);

            var damage = rolled
                         * SpaceCombatMath.SizeDamageMultiplier(weaponSize, targetSize, shot.IgnoreSizePenalty)
                         * (1.0 + bonusPercent / 100.0);

            // Ship damage is the ground's "other" damage, so the target's typed Leadership reduction applies
            // before resistance, then its damage-taken stat after it, in the ground's order.
            var leadershipPercent = Math.Min(
                Stat.GetStatAdjustment(target, StatType.LeadershipOtherDamageTakenPercentAdjustment),
                Stat.GetStatAdjustment(target, StatType.SpaceLeadershipDamageTakenPercentAdjustment));
            if (leadershipPercent != 0)
                damage = SpaceCombatMath.ApplyLeadershipReduction((int)Math.Round(damage, MidpointRounding.AwayFromZero), leadershipPercent);

            damage *= SpaceCombatMath.ResistanceMultiplier(resistance);

            result.Damage = (int)Math.Round(damage, MidpointRounding.AwayFromZero);
            // A shot that isn't fully resisted always does something.
            if (result.Damage < 1 && damage > 0)
                result.Damage = 1;

            // A Target Painter's paint adds to the target's damage taken (W11k).
            var takenPercent = Stat.GetStatAdjustment(target, StatType.DamageTakenPercentAdjustment) +
                               Stat.GetStatAdjustment(target, StatType.SpaceDamageTakenPercentAdjustment) +
                               targetStatus.GetPaintPercent(DateTime.UtcNow);
            if (takenPercent != 0)
                result.Damage = SpaceCombatMath.ApplyDamageTaken(result.Damage, takenPercent);

            result.Enmity = result.Damage;

            foreach (var rider in riders)
            {
                activatorStatus.PendingRiders.Remove(rider);
                result.ConsumedRiders.Add(rider);
            }

            return result;
        }

        /// <summary>
        /// Finishes a rolled shot: the combat log, enmity and combat point happen now, and the damage is applied
        /// after <paramref name="damageDelay"/> seconds. <paramref name="afterDamage"/> runs once the damage has
        /// landed, with the shield and hull points it removed.
        ///
        /// Ground hooks run as they do for a weapon attack: an attempt is attack activity, an evaded shot calls
        /// <c>Combat.TrackAvoidedAttack</c> for the target, and a landed critical calls
        /// <c>Combat.ApplyCriticalHitEffects</c> for the attacker. <see cref="SpaceCombatEvents"/> carries the
        /// space-only events. Threat for the shot's damage includes the attacker's damage enmity stat.
        /// </summary>
        public static void CompleteWeaponShot(
            uint activator,
            uint target,
            SpaceWeaponShot shot,
            ShipShotResult result,
            float damageDelay = 0f,
            Action<ShipShotResult> afterDamage = null)
        {
            if (!GetIsObjectValid(target))
                return;

            var attackId = result.IsHit ? (result.IsCritical ? 3 : 1) : 4;
            Messaging.SendMessageNearbyToPlayers(
                target,
                receiver => Combat.BuildCombatLogMessage(receiver, activator, target, attackId, result.ChanceToHit),
                60f);

            Enmity.ModifyEnmity(activator, target, Enmity.CalculateDamageEnmity(
                result.Enmity,
                Stat.GetStatAdjustment(activator, StatType.DamageEnmityPercentAdjustment)));

            // A crew member's shot earns them the combat point, in their seat's skill. The shot was rolled while
            // they worked the station, so the credit travels with the result (a missile lands later).
            var creditedPlayer = result.CreditedPlayer == OBJECT_INVALID || !GetIsObjectValid(result.CreditedPlayer)
                ? activator
                : result.CreditedPlayer;
            CombatPoint.AddCombatPoint(creditedPlayer, target,
                result.CreditedSkill == SkillType.Invalid ? SkillType.Piloting : result.CreditedSkill);
            Combat.TrackAttackActivity(activator);

            var shotEvent = new SpaceShotEvent { Attacker = activator, Target = target, Shot = shot, Result = result };

            if (!result.IsHit)
            {
                if (result.Evaded)
                {
                    Combat.TrackAvoidedAttack(target, activator);
                    SpaceCombatEvents.RaiseShotEvaded(shotEvent);
                }

                return;
            }

            void Apply()
            {
                if (!GetIsObjectValid(target))
                    return;

                AssignCommand(activator, () =>
                {
                    var (shieldDamage, hullDamage) = ApplyShipDamage(
                        activator,
                        target,
                        result.Damage,
                        shot.ShieldBypass,
                        shot.ShieldDamageMultiplier);
                    result.ShieldDamage = shieldDamage;
                    result.HullDamage = hullDamage;

                    ApplyConsumedRiders(activator, target, shot, result);
                    afterDamage?.Invoke(result);

                    if (result.IsCritical)
                    {
                        Combat.ApplyCriticalHitEffects(
                            activator,
                            target,
                            result.Damage,
                            Combat.StandardCriticalRating,
                            shot.IsAbility && !shot.IsArea,
                            SkillType.Piloting);
                    }

                    SpaceCombatEvents.RaiseShotLanded(shotEvent);
                });
            }

            if (damageDelay > 0f)
                DelayCommand(damageDelay, Apply);
            else
                Apply();
        }

        /// <summary>
        /// Runs what the riders a landed shot consumed add: their extra damage instance, their extra threat and
        /// the handler registered under their key.
        /// </summary>
        private static void ApplyConsumedRiders(uint activator, uint target, SpaceWeaponShot shot, ShipShotResult result)
        {
            foreach (var rider in result.ConsumedRiders)
            {
                if (rider.ExtraDamage > 0)
                {
                    var (shieldDamage, hullDamage) = ApplyShipDamage(
                        activator, target, rider.ExtraDamage, shot.ShieldBypass, shot.ShieldDamageMultiplier);
                    result.ShieldDamage += shieldDamage;
                    result.HullDamage += hullDamage;
                }

                if (rider.ExtraEnmity > 0)
                    Enmity.ModifyEnmity(activator, target, rider.ExtraEnmity);

                SpaceAbilities.RunRiderHandler(new SpaceRiderContext
                {
                    Attacker = activator,
                    Target = target,
                    Shot = shot,
                    Result = result,
                    Rider = rider
                });
            }
        }

        /// <summary>The riders on a ship that a shot of this family can consume now, oldest first.</summary>
        private static List<SpaceShotRider> FindPendingRiders(ShipStatus status, SpaceWeaponFamily family, DateTime now)
        {
            if (status?.PendingRiders == null || status.PendingRiders.Count == 0)
                return new List<SpaceShotRider>();

            status.PendingRiders.RemoveAll(x => now >= x.ExpiresAt);
            return status.PendingRiders.Where(x => x.Matches(family, now)).ToList();
        }

        /// <summary>
        /// Rolls a weapon shot and finishes it at once (see <see cref="RollWeaponShot"/> and
        /// <see cref="CompleteWeaponShot"/>).
        /// </summary>
        public static ShipShotResult ResolveWeaponShot(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot,
            float damageDelay = 0f,
            Action<ShipShotResult> afterDamage = null)
        {
            var result = RollWeaponShot(activator, activatorStatus, target, targetStatus, shot);
            CompleteWeaponShot(activator, target, shot, result, damageDelay, afterDamage);
            return result;
        }

        /// <summary>
        /// Applies damage to a ship target. Damage will first be taken to the shields.
        /// When shields reaches zero, damage will be taken on the hull.
        /// When hull reaches zero, the ship will explode.
        /// </summary>
        /// <param name="attacker">The attacking ship</param>
        /// <param name="target">The defending, targeted ship</param>
        /// <param name="amount">The amount of damage to apply to the target.</param>
        public static void ApplyShipDamage(uint attacker, uint target, int amount)
        {
            ApplyShipDamage(attacker, target, amount, false, 1.0);
        }

        /// <summary>
        /// Applies damage to a ship target through <see cref="SpaceCombatMath.ApplyToPools"/>.
        /// When hull reaches zero, the ship will explode.
        /// </summary>
        /// <param name="attacker">The attacking ship</param>
        /// <param name="target">The defending, targeted ship</param>
        /// <param name="amount">The amount of damage to apply to the target.</param>
        /// <param name="shieldBypass">When true, all of the damage goes to the hull.</param>
        /// <param name="shieldDamageMultiplier">The shield loses this many points per point of damage it absorbs.</param>
        /// <returns>The shield and hull points removed.</returns>
        public static (int ShieldDamage, int HullDamage) ApplyShipDamage(
            uint attacker,
            uint target,
            int amount,
            bool shieldBypass,
            double shieldDamageMultiplier)
        {
            if (amount < 0) return (0, 0);

            var targetShipStatus = GetShipStatus(target);

            // Leashed ships neither take nor deal damage while they return home, like plot-flagged ground enemies.
            if (targetShipStatus == null || AI.IsLeashEvading(target) || AI.IsLeashEvading(attacker))
                return (0, 0);

            // Player ships hurt each other only where the ground rules allow it, and the PvP damage ramp applies.
            if (!CanShipAttack(attacker, target))
                return (0, 0);

            amount = ApplyPvpDamageRamp(attacker, target, amount);

            // A Wild Space environment makes shields stronger or weaker.
            shieldDamageMultiplier = WildSpaceRules.AdjustShieldDamageMultiplier(
                shieldDamageMultiplier, Stat.GetStatAdjustment(target, StatType.SpaceShieldDamageTakenPercentAdjustment));

            // Firing on a convoy or customs patrol turns its group hostile and costs lawful standing.
            Trade.OnShipAttacked(attacker, target);

            var shieldBefore = targetShipStatus.Shield;
            var hullBefore = targetShipStatus.Hull;

            double shield = shieldBefore;
            double hull = hullBefore;
            SpaceCombatMath.ApplyToPools(ref shield, ref hull, amount, shieldBypass, shieldDamageMultiplier);

            // The pools are whole numbers. The shield rounds to the nearest point; the hull rounds down, so damage
            // that spills over a shield always costs at least a point of hull.
            targetShipStatus.Shield = (int)Math.Round(shield, MidpointRounding.AwayFromZero);
            targetShipStatus.Hull = (int)Math.Floor(hull);

            // Safety clamping
            if (targetShipStatus.Shield < 0)
                targetShipStatus.Shield = 0;
            if (targetShipStatus.Hull < 0)
                targetShipStatus.Hull = 0;

            var shieldDamage = shieldBefore - targetShipStatus.Shield;
            var hullDamage = hullBefore - targetShipStatus.Hull;
            RecordIncursionDamage(attacker, target, shieldDamage + hullDamage);
            if (shieldDamage > 0 || hullDamage > 0)
            {
                MarkShipCombat(attacker, target);
                InterruptSignatureJump(target);
                NotifyCloakDamaged(target);
            }

            if (hullDamage > 0)
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Explosion), target);
            }
            else if (shieldDamage > 0)
            {
                // Shields absorbed the whole hit.
                ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), target, 1.0f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Deflect), target);
            }

            // Apply death once the hull has reached zero. A shield-bypassing hit can leave shield behind; it goes too.
            if (targetShipStatus.Hull <= 0)
            {
                targetShipStatus.Shield = 0;
                RecordShipKiller(target, attacker);

                // Lexicon Note regarding GetFirstObjectInShape/GetNextObjectInShape
                // Do not apply EffectDamage without a DelayCommand (do DelayCommand(0.0, Apply...) at minimum).
                // If you do fire it the OnDamaged or OnDeath OnPlayerDeath script may fire, causing this loop to reset
                // and start from scratch when you call GetNextObjectInShape
                // (since those scripts may call their own GetFirstObjectInShape).
                // To work around this, put the death effect on a 0f delay.
                DelayCommand(0f, () =>
                {
                    AssignCommand(attacker, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), target));
                });
                ClearCurrentTarget(attacker);
            }
            else
            {
                if (GetIsPC(target))
                {
                    var targetPlayerId = GetObjectUUID(target);
                    var dbTargetPlayer = DB.Get<Player>(targetPlayerId);
                    var dbPlayerShip = DB.Get<PlayerShip>(dbTargetPlayer.ActiveShipId);

                    if (Property.TryGetLoadedInstance(dbPlayerShip.PropertyId, out var instance))
                    {
                        var location = Location(instance.Area, Vector3.Zero, 0.0f);
                        ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_ShakeScreen), location);
                    }

                    dbPlayerShip.Status.Shield = targetShipStatus.Shield;
                    dbPlayerShip.Status.Hull = targetShipStatus.Hull;

                    DB.Set(dbPlayerShip);
                    ExecuteScript("pc_shld_adjusted", target);
                    ExecuteScript("pc_hull_adjusted", target);
                }
                else
                {
                    // Update NPC cache.
                    _shipNPCs[target] = targetShipStatus;
                }
            }

            // Notify nearby players of damage taken by target.
            NotifyShipDamage(
                attacker,
                target,
                receiver => $"{PlayerName.GetDisplayName(receiver, attacker)} deals {amount} damage to {PlayerName.GetDisplayName(receiver, target)}.");

            if(GetIsPC(attacker))
                ExecuteScript("pc_target_upd", attacker);

            if (hullDamage > 0)
            {
                SpaceCombatEvents.RaiseHullDamaged(new SpaceHullEvent
                {
                    Ship = target,
                    Attacker = attacker,
                    HullBefore = hullBefore,
                    HullAfter = targetShipStatus.Hull,
                    MaxHull = targetShipStatus.MaxHull
                });
            }

            return (shieldDamage, hullDamage);
        }

        /// <summary>
        /// Applies damage to a ship target's armor only. This bypasses shields.
        /// When hull reaches zero, the ship will explode.
        /// </summary>
        /// <param name="attacker">The attacking ship</param>
        /// <param name="target">The defending, targeted ship</param>
        /// <param name="amount">The amount of damage to apply to the target.</param>
        public static void ApplyHullDamage(uint attacker, uint target, int amount)
        {
            if (amount < 0) return;

            var targetShipStatus = GetShipStatus(target);

            // Leashed ships neither take nor deal damage while they return home, like plot-flagged ground enemies.
            if (targetShipStatus == null || AI.IsLeashEvading(target) || AI.IsLeashEvading(attacker))
                return;

            // Player ships hurt each other only where the ground rules allow it, and the PvP damage ramp applies.
            if (!CanShipAttack(attacker, target))
                return;

            amount = ApplyPvpDamageRamp(attacker, target, amount);

            // Firing on a convoy or customs patrol turns its group hostile and costs lawful standing.
            Trade.OnShipAttacked(attacker, target);

            var hullBefore = targetShipStatus.Hull;
            if (targetShipStatus.Hull > 0)
            {
                targetShipStatus.Hull -= amount;
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Explosion), target);
            }

            // Safety clamping
            if (targetShipStatus.Hull < 0)
            targetShipStatus.Hull = 0;

            RecordIncursionDamage(attacker, target, hullBefore - targetShipStatus.Hull);

            // Apply death if hull has reached zero. Shields are reduced to 0 in this instance as well.
            if (targetShipStatus.Hull <= 0)
            {
                targetShipStatus.Shield = 0;
                RecordShipKiller(target, attacker);
                AssignCommand(attacker, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), target));
                ClearCurrentTarget(attacker);
            }
            else
            {
                if (GetIsPC(target))
                {
                    var targetPlayerId = GetObjectUUID(target);
                    var dbTargetPlayer = DB.Get<Player>(targetPlayerId);
                    var dbPlayerShip = DB.Get<PlayerShip>(dbTargetPlayer.ActiveShipId);

                    if (Property.TryGetLoadedInstance(dbPlayerShip.PropertyId, out var instance))
                    {
                        var location = Location(instance.Area, Vector3.Zero, 0.0f);
                        ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_ShakeScreen), location);
                    }

                    dbPlayerShip.Status.Shield = targetShipStatus.Shield;
                    dbPlayerShip.Status.Hull = targetShipStatus.Hull;

                    DB.Set(dbPlayerShip);
                    ExecuteScript("pc_shld_adjusted", target);
                    ExecuteScript("pc_hull_adjusted", target);
                }
                else
                {
                    // Update NPC cache.
                    _shipNPCs[target] = targetShipStatus;
                }
            }

            // Notify nearby players of damage taken by target.
            NotifyShipDamage(
                attacker,
                target,
                receiver => $"{PlayerName.GetDisplayName(receiver, attacker)} deals {amount} damage directly to hull of {PlayerName.GetDisplayName(receiver, target)}.");

            if (GetIsPC(attacker))
                ExecuteScript("pc_target_upd", attacker);

            if (targetShipStatus.Hull < hullBefore)
            {
                MarkShipCombat(attacker, target);
                NotifyCloakDamaged(target);
                SpaceCombatEvents.RaiseHullDamaged(new SpaceHullEvent
                {
                    Ship = target,
                    Attacker = attacker,
                    HullBefore = hullBefore,
                    HullAfter = targetShipStatus.Hull,
                    MaxHull = targetShipStatus.MaxHull
                });
            }
        }


        /// <summary>
        /// Every second, run through all known spaceship NPCs and process their AI.
        /// </summary>
        private static void ProcessSpaceNPCAI()
        {
            var now = DateTime.UtcNow;

            // Ships destroyed or killed without their death event firing must stop acting.
            var defunctShips = _shipNPCs.Keys
                .Where(creature => !GetIsObjectValid(creature) || GetIsDead(creature))
                .ToList();
            foreach (var creature in defunctShips)
            {
                _toggleWake.Remove(creature);
                _shipNPCs.Remove(creature);
                ClearNpcRole(creature);
            }

            foreach (var (creature, shipStatus) in _shipNPCs)
            {
                if (AI.IsLeashEvading(creature))
                    continue;

                ApplyAutoShipRecovery(creature, shipStatus);

                // Repairers switch off once their target is whole again, in or out of a fight.
                var role = GetNpcRole(creature);
                UpdateNpcRepairToggles(creature, shipStatus, false);
                RunNpcRoleUpkeep(creature, shipStatus, role);

                // Determine target. A cloaked ship keeps its enmity but is skipped until it can be seen again.
                var target = Enmity.GetHighestEnmityAttackTarget(creature);
                if (!GetIsObjectValid(target)) continue;

                // Ships follow the same combat leash as ground enemies.
                if (AI.TryStartCombatLeashEvade(creature, target))
                    continue;

                // The role may fight another target than the threat leader (plan section 11).
                var threatTarget = target;
                target = RunNpcRole(creature, shipStatus, role, threatTarget, now, out var roleHoldDistance);
                if (!GetIsObjectValid(target))
                    target = threatTarget;

                var targetShipStatus = GetShipStatus(target);
                var distance = GetDistanceBetween(creature, target);

                // Engage at the range of the shortest weapon that can hit this target so every such weapon gets used.
                var engagementDistance = shipStatus.HighPowerModules.Values
                    .Concat(shipStatus.LowPowerModules.Values)
                    .Concat(shipStatus.MidPowerModules.Values)
                    .Where(x => CanNPCFireShipModule(creature, shipStatus, target, targetShipStatus, x))
                    .Select(x => GetShipModuleMaxDistance(creature, shipStatus, target, targetShipStatus, x))
                    .DefaultIfEmpty(0f)
                    .Min();
                var holdDistance = roleHoldDistance > 0f
                    ? roleHoldDistance
                    : engagementDistance > 0f
                        ? Math.Min(NPCShipKeepDistance, engagementDistance * 0.75f)
                        : NPCShipKeepDistance;

                // Toggled modules switch on against the highest enmity target and then run themselves;
                // the auto-fire loop keeps offensive ones on whichever target the ship currently has.
                SetCurrentTarget(creature, target);

                // A Logistics ship's repairers lock onto allies (RunNpcRole), not only itself.
                UpdateNpcRepairToggles(creature, shipStatus, role != ShipNpcRole.Logistics);
                TurnOnNpcWeaponToggles(creature, shipStatus, target, targetShipStatus, distance);

                // One-press modules are still used from the AI tick.
                var featsToUse = new List<FeatType>();
                foreach (var (feat, shipModule) in GetNpcModuleSlots(shipStatus))
                {
                    var shipModuleDetail = _shipModules[shipModule.ItemTag];
                    if (shipModuleDetail.Activation != ShipModuleActivation.OnePress ||
                        !IsOffensiveShipModule(shipModuleDetail.Type))
                    {
                        continue;
                    }

                    var requiredCapacitor = shipModuleDetail.CalculateCapacitorAction?.Invoke(creature, shipStatus, 0) ?? 0;
                    if (shipModule.RecastTime > now ||
                        shipStatus.Capacitor < requiredCapacitor ||
                        shipStatus.GlobalRecast > now)
                    {
                        continue;
                    }

                    var maxDistance = GetShipModuleMaxDistance(creature, shipStatus, target, targetShipStatus, shipModule);
                    // Self-centered weapons hit enemies around the firing ship rather than the selected target.
                    var useModule = CanNPCFireShipModule(creature, shipStatus, target, targetShipStatus, shipModule) &&
                                    (shipModuleDetail.CanTargetSelf
                                        ? HasEnemyShipWithin(creature, maxDistance)
                                        : distance <= maxDistance);

                    if (useModule)
                    {
                        featsToUse.Add(feat);
                    }
                }

                // Fire from the current position first, then close in when beyond weapon range or keep distance.
                AssignCommand(creature, () =>
                {
                    ClearAllActions();
                    foreach (var feat in featsToUse)
                    {
                        ActionUseFeat(feat, target);
                    }

                    if (engagementDistance > 0f && distance > engagementDistance)
                        ActionMoveToObject(target, true, holdDistance);
                    else
                        ActionMoveAwayFromObject(target, true, holdDistance);
                });
            }
        }

        /// <summary>
        /// Whether a ship can keep firing a delayed salvo. Dead and leashed ships stop.
        /// </summary>
        public static bool CanShipFire(uint ship)
        {
            return !GetIsDead(ship) && !AI.IsLeashEvading(ship);
        }

        /// <summary>
        /// Leashed ships start and finish returning home at full strength, matching the full heal ground enemies receive.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureLeashStarted)]
        [NWNEventHandler(ScriptName.OnCreatureLeashEnded)]
        public static void RestoreLeashedShip()
        {
            if (!_shipNPCs.TryGetValue(OBJECT_SELF, out var shipStatus))
                return;

            shipStatus.Shield = shipStatus.MaxShield;
            shipStatus.Hull = shipStatus.MaxHull;

            // A reset carrier recalls its fighters and a minelayer's mines go; both start over.
            ClearNpcRole(OBJECT_SELF);
        }

        private static bool HasEnemyShipWithin(uint creature, float distance)
        {
            return ObjectSnapshot.InShape(Shape.Sphere, distance, GetLocation(creature), true, ObjectType.Creature)
                .Any(nearby => nearby != creature &&
                               GetIsObjectValid(nearby) &&
                               GetIsEnemy(nearby, creature) &&
                               GetShipStatus(nearby) != null);
        }

        /// <summary>
        /// Whether an NPC ship's offensive module passes its target validation against the given target.
        /// </summary>
        private static bool CanNPCFireShipModule(
            uint creature,
            ShipStatus shipStatus,
            uint target,
            ShipStatus targetShipStatus,
            ShipStatus.ShipStatusModule shipModule)
        {
            var shipModuleDetail = _shipModules[shipModule.ItemTag];
            if (!ShipToggleRules.TargetsEnemy(shipModuleDetail.Type) || targetShipStatus == null)
                return false;

            return string.IsNullOrWhiteSpace(shipModuleDetail.ModuleValidationAction?.Invoke(creature, shipStatus, target, targetShipStatus, shipModule.ModuleBonus));
        }

        /// <summary>
        /// Notifies players near the attacker, plus a targeted pilot who is outside that radius, of ship damage.
        /// </summary>
        private static void NotifyShipDamage(uint attacker, uint target, Messaging.BuildMessageDelegate buildMessage)
        {
            Messaging.SendMessageNearbyToPlayers(attacker, buildMessage, ShipDamageMessageRange);

            if (GetIsPC(target) &&
                target != attacker &&
                GetDistanceBetween(attacker, target) > ShipDamageMessageRange)
            {
                SendMessageToPC(target, buildMessage(target));
            }
        }

        private static bool IsOffensiveShipModule(ShipModuleType type)
        {
            return ShipToggleRules.IsOffensive(type);
        }

        private static float GetShipModuleMaxDistance(
            uint activator,
            ShipStatus activatorShipStatus,
            uint target,
            ShipStatus targetShipStatus,
            ShipStatus.ShipStatusModule shipModule)
        {
            var shipModuleDetail = _shipModules[shipModule.ItemTag];
            return shipModuleDetail.ModuleMaxDistanceAction == null
                ? 10f
                : shipModuleDetail.ModuleMaxDistanceAction(activator, activatorShipStatus, target, targetShipStatus, shipModule.ModuleBonus);
        }

        /// <summary>
        /// When a creature clicks on a space object, target that object.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpaceTarget)]
        public static void TargetSpaceObject()
        {
            var creature = GetPlaceableLastClickedBy();
            var self = OBJECT_SELF;

            var tag = GetTag(self);

            // Space object not registered with the system.
            if (!_spaceObjects.ContainsKey(tag)) return;

            // Register this instance into the cache.
            if (!_spaceObjectInstances.ContainsKey(self))
                _spaceObjectInstances[self] = new ShipStatus();

            var (target, _) = GetCurrentTarget(creature);
            AssignCommand(creature, () => ClearAllActions());

            // Targeted the same object - remove it.
            if (target == self)
            {
                ClearCurrentTarget(creature);
            }
            // Targeted something new. Remove existing target and pick the new one.
            else
            {
                ClearCurrentTarget(creature);
                SetCurrentTarget(creature, self);
            }
        }

        /// <summary>
        /// Performs an emergency exit on a ship.
        /// This will send the ship back to the last place it docked if there are no players in the property
        /// and no one is currently piloting the ship.
        /// </summary>
        /// <param name="instance">The area instance</param>
        public static void PerformEmergencyExit(uint instance)
        {
            var propertyId = Property.GetPropertyId(instance);
            var shipQuery = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false);
            var dbShip = DB.Search(shipQuery).FirstOrDefault();

            if (dbShip == null)
                return;

            var playerCount = AreaPlugin.GetNumberOfPlayersInArea(instance);
            var shipClone = _shipClones.ContainsKey(dbShip.Id)
                ? _shipClones[dbShip.Id]
                : OBJECT_INVALID;
            var isPiloted = !GetIsObjectValid(shipClone) ||
                            _pendingShipLaunches.Values.Any(launch => launch.ShipId == dbShip.Id);

            // Other players are in the instance or someone is piloting the ship.
            // No need to send the ship back to the dock.
            if (playerCount > 1 || isPiloted)
                return;

            // No one's in the ship and it's lost in space. Let's send it back to the last dock
            // and apply penalties.
            var dbProperty = DB.Get<WorldProperty>(propertyId);

            if (dbProperty.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
            {
                dbProperty.Positions.Remove(PropertyLocationType.CurrentPosition);

                dbShip.Status.Shield = 0;
                dbShip.Status.Hull = 1;

                DB.Set(dbProperty);
                DB.Set(dbShip);
            }

            if (_shipClones.ContainsKey(dbShip.Id))
            {
                _shipClones.Remove(dbShip.Id);
            }

            DestroyObject(shipClone);
        }

        /// <summary>
        /// Retrieves the module bonus item property off a given item.
        /// If the item property doesn't exist, 0 will be returned.
        /// </summary>
        /// <param name="item">The item to calculate.</param>
        /// <returns>The module bonus of an item or 0 if none are found.</returns>
        public static int GetModuleBonus(uint item)
        {
            var moduleBonus = 0;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.ModuleBonus)
                {
                    moduleBonus += GetItemPropertyCostTableValue(ip);
                }
            }

            return moduleBonus;
        }

        /// <summary>
        /// Reads all starship bonus properties off an item and returns their cumulative values.
        /// </summary>
        /// <param name="item">The item to read.</param>
        /// <returns>An object containing cumulative starship bonus information</returns>
        public static ShipBonuses GetShipBonuses(uint item)
        {
            var bonuses = new ShipBonuses();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.StarshipBonus)
                {
                    var type = GetItemPropertySubType(ip);
                    var amount = GetItemPropertyCostTableValue(ip);

                    switch (type)
                    {
                        case 46: // Starship Hull
                            bonuses.Hull += amount;
                            break;
                        case 47: // Starship Capacitor
                            bonuses.Capacitor += amount;
                            break;
                        case 48: // Starship Shield
                            bonuses.Shield += amount;
                            break;
                        case 49: // Starship Shield Recharge Rate
                            bonuses.ShieldRechargeRate += amount;
                            break;
                        case 50: // Starship EM Damage
                            bonuses.EMDamage += amount;
                            break;
                        case 51: // Starship Thermal Damage
                            bonuses.ThermalDamage += amount;
                            break;
                        case 52: // Starship Explosive Damage
                            bonuses.ExplosiveDamage += amount;
                            break;
                        case 53: // Starship Accuracy
                            bonuses.Accuracy += amount;
                            break;
                        case 54: // Starship Evasion
                            bonuses.Evasion += amount;
                            break;
                        case 55: // Starship Thermal Defense
                            bonuses.ThermalDefense += amount;
                            break;
                        case 56: // Starship Explosive Defense
                            bonuses.ExplosiveDefense += amount;
                            break;
                        case 57: // Starship EM Defense
                            bonuses.EMDefense += amount;
                            break;
                    }
                }
            }

            return bonuses;
        }

    }
}
