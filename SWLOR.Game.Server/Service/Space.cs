using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service
{
    public static class Space
    {
        public const int MaxRegisteredShips = 10;

        private static readonly Dictionary<string, ShipDetail> _shipTypes = new();
        private static readonly Dictionary<string, ShipModuleDetail> _shipModules = new();
        private static readonly Dictionary<string, SpaceObjectDetail> _spaceObjects = new();
        
        private static readonly Dictionary<uint, ShipStatus> _shipNPCs = new();
        private static readonly Dictionary<uint, ShipStatus> _spaceObjectInstances = new();

        public static Dictionary<FeatType, ShipModuleFeat> ShipModuleFeats { get; } = ShipModuleFeat.GetAll();

        private static readonly HashSet<string> _shipItemResrefs = new();
        private static readonly HashSet<string> _shipModuleItemTags = new();

        private static readonly Dictionary<string, uint> _shipClones = new();

        private static readonly Dictionary<PlanetType, Dictionary<string, ShipDockPoint>> _dockPoints = new();

        private static readonly HashSet<uint> _playersInSpace = new();

        /// <summary>
        /// When the module loads, cache all space data into memory.
        /// </summary>
        [NWNEventHandler("mod_cache")]
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
        }

        [NWNEventHandler("mod_enter")]
        public static void EnterServer()
        {
            var player = GetEnteringObject();
            ReloadPlayerTlkStrings();
            WarpPlayerInsideShip(player);
        }

        [NWNEventHandler("mod_exit")]
        public static void ExitServer()
        {
            var player = GetExitingObject();
            if (!IsPlayerInSpaceMode(player))
                return;

            CloneShip(player);

            if (_playersInSpace.Contains(player))
                _playersInSpace.Remove(player);
        }

        /// <summary>
        /// When the module loads, 
        /// </summary>
        [NWNEventHandler("mod_load")]
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
            if (!string.IsNullOrWhiteSpace(dockPointId))
            {
                return;
            }

            var planet = Planet.GetPlanetType(area);

            // Only waypoints in recognized planets are tracked.
            if (planet == PlanetType.Invalid)
                return;

            if (!_dockPoints.ContainsKey(planet))
                _dockPoints[planet] = new Dictionary<string, ShipDockPoint>();

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
        [NWNEventHandler("input_atk_bef")]
        public static void SelectTarget()
        {
            var player = OBJECT_SELF;
            var position = GetPosition(player);

            if (!IsPlayerInSpaceMode(player)) return;
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
        [NWNEventHandler("area_enter")]
        public static void UpdateSpacePosition()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (!IsPlayerInSpaceMode(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
            var dbProperty = DB.Get<WorldProperty>(dbShip.PropertyId);
            var position = GetPosition(player);
            var areaResref = GetResRef(OBJECT_SELF);
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
        [NWNEventHandler("area_exit")]
        public static void ClearTargetOnAreaExit()
        {
            var player = GetExitingObject();
            if (GetIsDM(player)) return;

            ClearCurrentTarget(player);
        }

        /// <summary>
        /// When the ship computer is used, check the user's permissions.
        /// If player has permission and the ship isn't currently being controlled by another player,
        /// send the player into space mode.
        /// </summary>
        [NWNEventHandler("ship_computer")]
        public static void UseShipComputer()
        {
            var area = GetArea(OBJECT_SELF);
            var player = GetLastUsedBy();
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

            if (_shipClones.ContainsKey(dbShip.Id) &&
                !GetIsObjectValid(_shipClones[dbShip.Id]))
            {
                SendMessageToPC(player, ColorToken.Red("This ship's controls are in use."));
                return;
            }

            if (!CanPlayerUseShip(player, dbShip.Status))
            {
                SendMessageToPC(player, ColorToken.Red("You do not have the ability to pilot this ship."));
                return;
            }

            SetLocalLocation(player, "SPACE_INSTANCE_LOCATION", GetLocation(player));
            SetLocalBool(player, "SPACE_INSTANCE_LOCATION_SET", true);
            EnterSpaceMode(player, dbShip.Id);

            var dbProperty = DB.Get<WorldProperty>(propertyId);

            // The existence of a current location means the ship is currently in space.
            // Warp the player to the ship's location.
            // Otherwise the player is docked. Warp the player to the space location of this dock.
            var propertyLocation = dbProperty.Positions.ContainsKey(PropertyLocationType.CurrentPosition) 
                ? dbProperty.Positions[PropertyLocationType.CurrentPosition]
                : dbProperty.Positions[PropertyLocationType.SpacePosition];

            var spaceArea = Area.GetAreaByResref(propertyLocation.AreaResref);
            var spacePosition = Vector3(propertyLocation.X, propertyLocation.Y, propertyLocation.Z);
            var location = Location(spaceArea, spacePosition, propertyLocation.Orientation);

            AssignCommand(player, () => ClearAllActions());
            AssignCommand(player, () => ActionJumpToLocation(location));
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
        public static void EnterSpaceMode(uint player, string shipId)
        {
            // Ground effects must be removed when entering space mode.
            // Otherwise players could buff on the ground, then get those same bonuses while in space.
            StatusEffect.RemoveAll(player);
            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                RemoveEffect(player, effect);
            }

            ClonePlayerAndSit(player);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbPlayerShip = DB.Get<PlayerShip>(shipId);
            var shipDetail = _shipTypes[dbPlayerShip.Status.ItemTag];

            // Update player appearance to match that of the ship.
            SetCreatureAppearanceType(player, shipDetail.Appearance);
            CreaturePlugin.SetMovementRate(player, MovementRate.PC);

            // Set active ship Id and serialize the player's hot bar.
            dbPlayer.SerializedHotBar = CreaturePlugin.SerializeQuickbar(player);
            dbPlayer.ActiveShipId = shipId;

            foreach(var (slot, shipModule) in dbPlayerShip.Status.HighPowerModules)
            {
                var feat = HighSlotToFeat(slot);
                var shipModuleDetail = _shipModules[shipModule.ItemTag];

                // Passive modules shouldn't be converted to feats.
                if (shipModuleDetail.Type == ShipModuleType.Passive) continue;

                // Convert current ship module to feat.
                CreaturePlugin.AddFeat(player, feat);

                // Rename the feat to match the configured name on the ship module.
                ApplyShipModuleFeat(player, shipModuleDetail, feat);
            }

            foreach (var (slot, shipModule) in dbPlayerShip.Status.LowPowerModules)
            {
                var feat = LowSlotToFeat(slot);
                var shipModuleDetail = _shipModules[shipModule.ItemTag];

                // Passive modules shouldn't be converted to feats.
                if (shipModuleDetail.Type == ShipModuleType.Passive) continue;

                // Convert current ship module to feat.
                CreaturePlugin.AddFeat(player, feat);

                // Rename the feat to match the configured name on the ship module.
                ApplyShipModuleFeat(player, shipModuleDetail, feat);
            }

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
                foreach (var (slot, shipModule) in dbPlayerShip.Status.HighPowerModules)
                {
                    var feat = HighSlotToFeat(slot);
                    var shipModuleDetail = _shipModules[shipModule.ItemTag];
                    if (shipModuleDetail.Type != ShipModuleType.Passive)
                    {
                        PlayerPlugin.SetQuickBarSlot(player, currentSlot, PlayerQuickBarSlot.UseFeat(feat));

                        currentSlot++;
                    }
                }
                foreach (var (slot, shipModule) in dbPlayerShip.Status.LowPowerModules)
                {
                    var feat = LowSlotToFeat(slot);
                    var shipModuleDetail = _shipModules[shipModule.ItemTag];
                    if (shipModuleDetail.Type != ShipModuleType.Passive)
                    {
                        PlayerPlugin.SetQuickBarSlot(player, currentSlot, PlayerQuickBarSlot.UseFeat(feat));

                        currentSlot++;
                    }
                }

                dbPlayerShip.PlayerHotBars[playerId] = CreaturePlugin.SerializeQuickbar(player);
            }

            DB.Set(dbPlayer);
            DB.Set(dbPlayerShip);

            // If the ship is in the "actively piloted" list, it means it's in space.
            // Destroy the NPC clone that's associated with this ship since the player is taking over the controls.
            if (_shipClones.ContainsKey(dbPlayerShip.Id))
            {
                var clone = _shipClones[dbPlayerShip.Id];
                if (GetIsObjectValid(clone))
                {
                    DestroyObject(clone);
                }
            }
            // Otherwise add the ship to the list and associate an invalid object to its clone.
            else
            {
                _shipClones[dbPlayerShip.Id] = OBJECT_INVALID;
            }

            if(!_playersInSpace.Contains(player))
                _playersInSpace.Add(player);

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

        private static void ClonePlayerAndSit(uint player)
        {
            var chair = GetNearestObjectByTag("pilot_chair", player);
            var location = GetLocation(player);
            var copy = CopyObject(player, location, OBJECT_INVALID, "spaceship_copy");
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
        /// Makes the player exit space mode which reverts the player's appearance, loads the character's hot bar, etc.
        /// </summary>
        /// <param name="player">The player exiting space mode.</param>
        public static void ExitSpaceMode(uint player)
        {
            if (!IsPlayerInSpaceMode(player))
                return;

            CloneShip(player);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var shipId = dbPlayer.ActiveShipId;
            var dbShip = DB.Get<PlayerShip>(shipId);

            ClearCurrentTarget(player);
            SetCreatureAppearanceType(player, dbPlayer.OriginalAppearanceType);
            CreaturePlugin.SetMovementRate(player, MovementRate.PC);
            Enmity.RemoveCreatureEnmity(player);

            // Save the ship's hot bar and unassign the active ship Id.
            dbShip.PlayerHotBars[playerId] = CreaturePlugin.SerializeQuickbar(player);
            dbPlayer.ActiveShipId = Guid.Empty.ToString();

            // Remove all module feats from the player.
            foreach (var (feat, _) in ShipModuleFeats)
            {
                CreaturePlugin.RemoveFeat(player, feat);
            }

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
                    SetCreatureAppearanceType(clone, shipDetail.Appearance);
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
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            
            var shipDetails = _shipTypes[playerShip.ItemTag];

            // Check ship requirements
            foreach (var (perkType, requiredLevel) in shipDetails.RequiredPerks)
            {
                if (!dbPlayer.Perks.ContainsKey(perkType)) return false;

                if (dbPlayer.Perks[perkType] < requiredLevel) return false;
            }

            foreach (var (_, shipModule) in playerShip.HighPowerModules)
            {
                if (!CanPlayerUseShipModule(player, shipModule.ItemTag)) return false;
            }

            foreach (var (_, shipModule) in playerShip.LowPowerModules)
            {
                if (!CanPlayerUseShipModule(player, shipModule.ItemTag)) return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if player can use a ship module by its tag.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="itemTag">The ship module item tag</param>
        /// <returns>true if player can use the module, false otherwise</returns>
        public static bool CanPlayerUseShipModule(uint player, string itemTag)
        {
            if (!_shipModules.ContainsKey(itemTag)) return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var shipModule = _shipModules[itemTag];

            foreach (var (perkType, requiredLevel) in shipModule.RequiredPerks)
            {
                if (!dbPlayer.Perks.ContainsKey(perkType)) return false;

                if (dbPlayer.Perks[perkType] < requiredLevel) return false;
            }

            return true;
        }

        /// <summary>
        /// When a ship module item is examined,
        /// append the configured description to the item's description and add prerequisite perk item properties.
        /// </summary>
        [NWNEventHandler("examine_bef")]
        public static void ExamineShipModuleItem()
        {
            var item = StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID"));

            // Must be an item
            if (GetObjectType(item) != ObjectType.Item) return;

            // Tag must be registered with the system.
            var itemTag = GetTag(item);
            if (!_shipModules.ContainsKey(itemTag)) return;

            var moduleDetail = GetShipModuleDetailByItemTag(itemTag);

            // A description must have been set.
            if (string.IsNullOrWhiteSpace(moduleDetail.Description)) return;

            // Append the ship module's description to the item's description.
            var description = GetDescription(item, true);
            description += moduleDetail.Description + "\n";
            SetDescription(item, description);

            // Apply item properties defined in code.
            foreach (var (perkType, level) in moduleDetail.RequiredPerks)
            {
                var ip = ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, (int)perkType, level);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            }
        }

        /// <summary>
        /// When a ship item is examined, add the prerequisite perk item properties.
        /// </summary>
        [NWNEventHandler("examine_bef")]
        public static void ExamineShipItem()
        {
            var item = StringToObject(EventsPlugin.GetEventData("EXAMINEE_OBJECT_ID"));

            // Must be an item
            if (GetObjectType(item) != ObjectType.Item) return;

            // Tag must be registered with the system.
            var itemTag = GetTag(item);
            if (!_shipTypes.ContainsKey(itemTag)) return;
            var shipDetail = _shipTypes[itemTag];

            foreach (var (perkType, level) in shipDetail.RequiredPerks)
            {
                var ip = ItemPropertyCustom(ItemPropertyType.UseLimitationPerk, (int)perkType, level);
                BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            }
        }
        
        /// <summary>
        /// When a ship module's feat is used, execute the currently equipped module's custom code.
        /// </summary>
        [NWNEventHandler("feat_use_bef")]
        public static void HandleShipModuleFeats()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));

            if (!ShipModuleFeats.ContainsKey(feat)) return;
            
            var activator = OBJECT_SELF;
            var activatorShipStatus = GetShipStatus(activator);
            var slotNumber = GetFeatSlotNumber(feat);
            ShipStatus.ShipStatusModule shipModule;

            // Slot numbers between 1-10 are high powered slots
            if (slotNumber <= 10)
            {
                shipModule = activatorShipStatus.HighPowerModules[slotNumber];
            }
            // Slot Numbers between 10-20 are low powered slots.
            else if (slotNumber <= 20)
            {
                shipModule = activatorShipStatus.LowPowerModules[slotNumber-10];
            }
            else
            {
                Log.Write(LogGroup.Error, $"Failed to locate matching ship module by its feat for player {GetName(activator)}");
                SendMessageToPC(activator, "Unable to use that module.");
                return;
            }

            // Found the ship module. Run validation checks.
            var shipModuleDetails = _shipModules[shipModule.ItemTag];

            // Check capacitor requirements
            var requiredCapacitor = shipModuleDetails.CalculateCapacitorAction?.Invoke(activator, activatorShipStatus, shipModule.ModuleBonus) ?? 0;

            // Perk bonuses
            var capacitorReduction = 1.0f - Perk.GetPerkLevel(activator, PerkType.EnergyManagement) * 0.2f;
            requiredCapacitor = (int)(requiredCapacitor * capacitorReduction);

            if (activatorShipStatus.Capacitor < requiredCapacitor)
            {
                SendMessageToPC(activator, $"Your ship does not have enough capacitor to use that module. (Required: {requiredCapacitor})");
                return;
            }

            // Check recast requirements
            var now = DateTime.UtcNow;
            if (shipModule.RecastTime > now)
            {
                SendMessageToPC(activator, "That module is not ready.");
                return;
            }

            // Check global recast requirements
            if (GetShipStatus(activator).GlobalRecast > now)
            {
                return;
            }

            var (target, targetShipStatus) = GetCurrentTarget(activator);
            // Check for valid object type if the ship module requires it.
            if (shipModuleDetails.ValidTargetTypes.Count > 0 &&
                !shipModuleDetails.ValidTargetTypes.Contains(GetObjectType(target)) &&
                !(shipModuleDetails.CanTargetSelf && !GetIsObjectValid(target)))
            {
                SendMessageToPC(activator, "This module cannot be used on that target type.");
                return;
            }

            // Check for a selected target that doesn't have a ship status.
            if (GetObjectType(target) != ObjectType.Placeable && targetShipStatus == null && !shipModuleDetails.CanTargetSelf)
            {
                SendMessageToPC(activator, "Invalid target.");
                return;
            }
            
            // Check to ensure activator is within maximum distance.
            var maxDistance = shipModuleDetails.ModuleMaxDistanceAction == null ? 10f : shipModuleDetails.ModuleMaxDistanceAction(activator, activatorShipStatus, target, targetShipStatus, shipModule.ModuleBonus);
            if (GetIsPC(activator) && GetDistanceBetween(activator, target) > maxDistance)
            {
                SendMessageToPC(activator, $"Target is too far away. Maximum distance: {maxDistance} meters.");
                return;
            }

            // Run any custom validation specific to the ship module.
            if (shipModuleDetails.ModuleValidationAction != null)
            {
                var result = shipModuleDetails.ModuleValidationAction(activator, activatorShipStatus, target, targetShipStatus, shipModule.ModuleBonus);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    SendMessageToPC(activator, result);
                    return;
                }
            }

            // Validation succeeded, run the module-specific code now.
            shipModuleDetails.ModuleActivatedAction?.Invoke(activator, activatorShipStatus, target, targetShipStatus, shipModule.ModuleBonus);
            
            // Update the recast and global recast timer.
            if (shipModuleDetails.CalculateRecastAction != null)
            {
                var recastSeconds = shipModuleDetails.CalculateRecastAction(activator, activatorShipStatus, shipModule.ModuleBonus);
                var recastTimer = now.AddSeconds(recastSeconds);
                shipModule.RecastTime = recastTimer;
                activatorShipStatus.GlobalRecast = now.AddSeconds(2f);
            }

            // Reduce capacitor
            if (requiredCapacitor > 0)
            {
                activatorShipStatus.Capacitor -= requiredCapacitor;
            }

            // Update changes for players. No need to update NPCs as they are already stored in memory cache.
            if (GetIsPC(activator))
            {
                var playerId = GetObjectUUID(activator);
                var dbPlayer = DB.Get<Player>(playerId);
                var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
                dbShip.Status = activatorShipStatus;
                
                DB.Set(dbShip);
                ExecuteScript("pc_cap_adjusted", activator);
            }

            if (GetIsPC(target))
            {
                var playerId = GetObjectUUID(target);
                var dbPlayer = DB.Get<Player>(playerId);
                var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
                dbShip.Status = targetShipStatus;

                DB.Set(dbShip);
            }
        }

        private static void ApplyAutoShipRecovery(uint player, ShipStatus shipStatus)
        {
            // Shield recovery
            shipStatus.ShieldCycle++;
            var rechargeRate = shipStatus.ShieldRechargeRate;
            if (rechargeRate <= 0)
                rechargeRate = 1;

            if (shipStatus.ShieldCycle >= rechargeRate)
            {
                RestoreShield(player, shipStatus, 1);
                shipStatus.ShieldCycle = 0;
            }

            // Capacitor recovery
            RestoreCapacitor(player, shipStatus, 1);

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
            shipStatus.Shield += amount;
            if (shipStatus.Shield > shipStatus.MaxShield)
                shipStatus.Shield = shipStatus.MaxShield;

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
        [NWNEventHandler("crea_spawn_bef")]
        public static void CreatureSpawn()
        {
            var creature = OBJECT_SELF;
            var creatureTag = GetTag(creature);

            // Not registered with the space system. Exit early.
            if (!_spaceObjects.ContainsKey(creatureTag)) return;

            var registeredEnemyType = _spaceObjects[creatureTag];
            var shipDetail = _shipTypes[registeredEnemyType.ShipItemTag];

            var shipStatus = new ShipStatus
            {
                ItemTag = registeredEnemyType.ShipItemTag,
                Shield = shipDetail.MaxShield,
                MaxShield = shipDetail.MaxShield,
                Hull = shipDetail.MaxHull,
                MaxHull = shipDetail.MaxHull,
                Capacitor = shipDetail.MaxCapacitor,
                MaxCapacitor = shipDetail.MaxCapacitor,
                EMDefense = shipDetail.EMDefense,
                ExplosiveDefense = shipDetail.ExplosiveDefense,
                ThermalDefense = shipDetail.ThermalDefense,
                Accuracy = shipDetail.Accuracy,
                Evasion = shipDetail.Evasion,
                ShieldRechargeRate = shipDetail.ShieldRechargeRate
            };

            // Attach the modules to the ship status.
            var featCount = 0;
            foreach (var itemTag in registeredEnemyType.HighPoweredModules)
            {
                var feat = ShipModuleFeats.ElementAt(featCount).Key;
                var slot = HighFeatToSlot(feat);
                var shipModule = _shipModules[itemTag];
                shipStatus.HighPowerModules.Add(slot, new ShipStatus.ShipStatusModule
                {
                    ItemTag = itemTag,
                    RecastTime = DateTime.UtcNow
                });

                if (shipModule.Type != ShipModuleType.Passive)
                {
                    shipStatus.ActiveModules.Add(slot);
                }

                shipModule.ModuleEquippedAction?.Invoke(creature, shipStatus, 0);

                featCount++;
            }
            foreach (var itemTag in registeredEnemyType.LowPowerModules)
            {
                var feat = ShipModuleFeats.ElementAt(featCount).Key;
                var slot = LowFeatToSlot(feat);
                var shipModule = _shipModules[itemTag];
                shipStatus.LowPowerModules.Add(slot, new ShipStatus.ShipStatusModule
                {
                    ItemTag = itemTag,
                    RecastTime = DateTime.UtcNow
                });

                if (shipModule.Type != ShipModuleType.Passive)
                {
                    shipStatus.ActiveModules.Add(slot);
                }

                shipModule.ModuleEquippedAction?.Invoke(creature, shipStatus, 0);

                featCount++;
            }

            _shipNPCs[creature] = shipStatus;
        }

        /// <summary>
        /// When a creature dies, remove it from the cache.
        /// </summary>
        [NWNEventHandler("crea_death_aft")]
        public static void CreatureDeath()
        {
            var creature = OBJECT_SELF;

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
        /// Calculates attacker's chance to hit target.
        /// </summary>
        /// <param name="attacker">The creature attacking.</param>
        /// <param name="defender">The creature being targeted.</param>
        public static int CalculateChanceToHit(uint attacker, uint defender)
        {
            var attackerShipStatus = GetShipStatus(attacker);
            var defenderShipStatus = GetShipStatus(defender);

            if (attackerShipStatus == null || defenderShipStatus == null)
                return 0;

            var attackerAccuracy = Stat.GetAccuracy(attacker, OBJECT_INVALID, AbilityType.Agility, SkillType.Piloting) + attackerShipStatus.Accuracy;
            var defenderEvasion = Stat.GetEvasion(defender, SkillType.Piloting) + defenderShipStatus.Evasion;

            return Combat.CalculateHitRate(attackerAccuracy, defenderEvasion, 0);
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
            if (amount < 0) return;

            var targetShipStatus = GetShipStatus(target);

            if (targetShipStatus == null)
                return;

            var remainingDamage = amount;
            // First deal damage to target's shields.
            if (remainingDamage <= targetShipStatus.Shield)
            {
                // Shields have enough to cover the attack.
                targetShipStatus.Shield -= remainingDamage;
                remainingDamage = 0;
                ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue), target, 1.0f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Deflect), target);
            }
            else
            {
                remainingDamage -= targetShipStatus.Shield;
                targetShipStatus.Shield = 0;
            }


            // If damage is remaining, deal it to the hull.
            if (remainingDamage > 0)
            {
                targetShipStatus.Hull -= remainingDamage;
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Explosion), target);
            }

            // Safety clamping
            if (targetShipStatus.Shield < 0)
                targetShipStatus.Shield = 0;
            if (targetShipStatus.Hull < 0)
                targetShipStatus.Hull = 0;

            // Apply death if shield and hull have reached zero.
            if (targetShipStatus.Shield <= 0 && targetShipStatus.Hull <= 0)
            {
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
                    var instance = Property.GetRegisteredInstance(dbPlayerShip.PropertyId);
                    var location = Location(instance.Area, Vector3.Zero, 0.0f);

                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_ShakeScreen), location);

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
            Messaging.SendMessageNearbyToPlayers(attacker, $"{GetName(attacker)} deals {amount} damage to {GetName(target)}.");
            
            if(GetIsPC(attacker))
                ExecuteScript("pc_target_upd", attacker);

        }

        /// <summary>
        /// Applies death to a creature.
        /// If this is a PC:
        ///     - The ship modules will either drop or be destroyed.
        ///     - The ship will require repairs
        ///     - The pilot will be killed (inflicting default death system penalties)
        ///     - Everyone inside the ship instance will be killed (inflicting default death system penalties)
        ///     - The ship will relocate back to the last dock it was at
        /// If this is an NPC, they will be killed and explode in spectacular fashion.
        /// </summary>
        [NWNEventHandler("mod_death")]
        public static void ApplyDeath()
        {
            var creature = GetLastPlayerDied();

            if (!IsPlayerInSpaceMode(creature))
                return;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), creature);

            // When a player dies, they have a chance to drop every module installed on their ship.
            if (GetIsPC(creature))
            {
                const int ChanceToDropModule = 65;
                var deathLocation = GetLocation(creature);
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);
                var dbPlayerShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
                var dbProperty = DB.Get<WorldProperty>(dbPlayerShip.PropertyId);
                var instance = Property.GetRegisteredInstance(dbPlayerShip.PropertyId);

                // Give a chance to drop each installed module.
                foreach (var (_, shipModule) in dbPlayerShip.Status.HighPowerModules)
                {
                    if (Random.D100(1) <= ChanceToDropModule)
                    {
                        var deserialized = ObjectPlugin.Deserialize(shipModule.SerializedItem);
                        CopyObject(deserialized, deathLocation);
                        DestroyObject(deserialized);
                    }

                    var moduleDetails = GetShipModuleDetailByItemTag(shipModule.ItemTag);
                    moduleDetails.ModuleUnequippedAction?.Invoke(creature, dbPlayerShip.Status, shipModule.ModuleBonus);

                }

                foreach (var (_, shipModule) in dbPlayerShip.Status.LowPowerModules)
                {
                    if (Random.D100(1) <= ChanceToDropModule)
                    {
                        var deserialized = ObjectPlugin.Deserialize(shipModule.SerializedItem);
                        CopyObject(deserialized, deathLocation);
                        DestroyObject(deserialized);
                    }

                    var moduleDetails = GetShipModuleDetailByItemTag(shipModule.ItemTag);
                    moduleDetails.ModuleUnequippedAction?.Invoke(creature, dbPlayerShip.Status, shipModule.ModuleBonus);
                }

                // Player always loses all modules regardless if they actually dropped.
                dbPlayerShip.Status.HighPowerModules.Clear();
                dbPlayerShip.Status.LowPowerModules.Clear();
                dbPlayerShip.PlayerHotBars.Clear();
                dbPlayerShip.Status.Hull = 1;
                dbPlayerShip.Status.Shield = 0;

                // Exit space mode
                ClearCurrentTarget(creature);
                SetCreatureAppearanceType(creature, dbPlayer.OriginalAppearanceType);
                CreaturePlugin.SetMovementRate(creature, MovementRate.PC);
                Enmity.RemoveCreatureEnmity(creature);
                
                // Remove all module feats from the player.
                foreach (var (feat, _) in ShipModuleFeats)
                {
                    CreaturePlugin.RemoveFeat(creature, feat);
                }

                // Load the player's hot bar.
                if (string.IsNullOrWhiteSpace(dbPlayer.SerializedHotBar) ||
                    !CreaturePlugin.DeserializeQuickbar(creature, dbPlayer.SerializedHotBar))
                {
                    // Deserialization failed. Clear out the player's hot bar and start fresh.
                    for (var slot = 0; slot <= 35; slot++)
                    {
                        PlayerPlugin.SetQuickBarSlot(creature, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                    }

                    dbPlayer.SerializedHotBar = CreaturePlugin.SerializeQuickbar(creature);
                }

                _shipClones.Remove(dbPlayer.ActiveShipId);
                dbPlayer.ActiveShipId = Guid.Empty.ToString();

                // Removing the current position of the ship will automatically send it back to the last dock it was at.
                if (dbProperty.Positions.ContainsKey(PropertyLocationType.CurrentPosition))
                {
                    dbProperty.Positions.Remove(PropertyLocationType.CurrentPosition);
                }

                // Update the changes
                DB.Set(dbProperty);
                DB.Set(dbPlayerShip);
                DB.Set(dbPlayer);

                // Murder everyone inside the ship's instance.
                foreach (var player in instance.Players)
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), player);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), player);

                    FloatingTextStringOnCreature(ColorToken.Red("The ship has exploded!"), player, false);
                }

                DestroyPilotClone(creature);
            }
        }

        /// <summary>
        /// Every second, run through all known spaceship NPCs and process their AI.
        /// </summary>
        private static void ProcessSpaceNPCAI()
        {
            var now = DateTime.UtcNow;

            foreach (var (creature, shipStatus) in _shipNPCs)
            {
                ApplyAutoShipRecovery(creature, shipStatus);

                // Determine target
                var target = Enmity.GetHighestEnmityTarget(creature);
                if (!GetIsObjectValid(target)) continue;

                // Determine which modules are available.
                var highModules = shipStatus.HighPowerModules.Where(x =>
                {
                    var shipModuleDetail = _shipModules[x.Value.ItemTag];
                    var requiredCapacitor = shipModuleDetail.CalculateCapacitorAction?.Invoke(creature, shipStatus, 0) ?? 0;

                    return x.Value.RecastTime <= now &&
                           shipStatus.Capacitor >= requiredCapacitor &&
                           shipStatus.GlobalRecast <= now;
                })
                    .Select(s => new Tuple<FeatType, ShipStatus.ShipStatusModule>(HighSlotToFeat(s.Key), s.Value));

                var lowModules = shipStatus.LowPowerModules.Where(x =>
                {
                    var shipModuleDetail = _shipModules[x.Value.ItemTag];
                    var requiredCapacitor = shipModuleDetail.CalculateCapacitorAction?.Invoke(creature, shipStatus, 0) ?? 0;

                    return x.Value.RecastTime <= now &&
                           shipStatus.Capacitor >= requiredCapacitor &&
                           shipStatus.GlobalRecast <= now;
                })
                    .Select(s => new Tuple<FeatType, ShipStatus.ShipStatusModule>(LowSlotToFeat(s.Key), s.Value));

                var availableModules = highModules.Concat(lowModules);
                // Keep distance from target.
                AssignCommand(creature, () =>
                {
                    ClearAllActions();
                    ActionMoveAwayFromObject(target, true, 10f);
                });

                // Determine which module(s) to activate
                SetCurrentTarget(creature, target);
                foreach (var (feat, shipModule) in availableModules)
                {
                    var shipModuleDetail = _shipModules[shipModule.ItemTag];
                    var useModule = false;
                    if (shipModuleDetail.Type == ShipModuleType.ShieldRepairer)
                    {
                        var shieldPointsLost = shipStatus.MaxShield - shipStatus.Shield;
                        if (shieldPointsLost >= 8)
                        {
                            useModule = true;
                        }
                    }
                    else if(shipModuleDetail.Type == ShipModuleType.HullRepairer)
                    {
                        var hullPointsLost = shipStatus.MaxHull - shipStatus.Hull;
                        if (hullPointsLost >= 6)
                        {
                            useModule = true;
                        }
                    }
                    else if (shipModuleDetail.Type == ShipModuleType.CombatLaser ||
                             shipModuleDetail.Type == ShipModuleType.IonCannon ||
                             shipModuleDetail.Type == ShipModuleType.Missile ||
                             shipModuleDetail.Type == ShipModuleType.CapitalWeapons)
                    {
                        useModule = true;
                    }

                    if (useModule)
                    {
                        AssignCommand(creature, () =>
                        {
                            ActionUseFeat(feat, target);
                        });
                    }
                }
            }
        }

        /// <summary>
        /// When a creature clicks on a space object, target that object.
        /// </summary>
        [NWNEventHandler("spc_target")]
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
            var isPiloted = !GetIsObjectValid(shipClone);

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

        /// <summary>
        /// When a player attempts to stealth while in space mode,
        /// exit the stealth mode and send an error message.
        /// </summary>
        [NWNEventHandler("stlent_add_bef")]
        public static void PreventSpaceStealth()
        {
            var creature = OBJECT_SELF;

            if (!IsPlayerInSpaceMode(creature))
                return;

            AssignCommand(creature, () =>
            {
                SetActionMode(creature, ActionMode.Stealth, false);
            });

            SendMessageToPC(creature, ColorToken.Red($"You cannot enter stealth mode in space."));
        }

    }
}
