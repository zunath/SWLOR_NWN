using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.DoorRule.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Area = SWLOR.Game.Server.Legacy.Data.Entity.Area;
using BaseStructureType = SWLOR.Game.Server.Legacy.Enumeration.BaseStructureType;
using BuildingType = SWLOR.Game.Server.Legacy.Enumeration.BuildingType;
using PCBaseType = SWLOR.Game.Server.Legacy.Enumeration.PCBaseType;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class BaseService
    {
        private static readonly Dictionary<uint, List<AreaStructure>> _areaBaseServiceStructures = new Dictionary<uint, List<AreaStructure>>();
        private static readonly Dictionary<string, IDoorRule> _doorRules = new Dictionary<string, IDoorRule>();

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleHeartbeat>(message => OnModuleHeartbeat());
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
            MessageHub.Instance.Subscribe<OnModuleUseFeat>(message => OnModuleUseFeat());
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
        }

        public static List<AreaStructure> GetAreaStructures(uint area)
        {
            return _areaBaseServiceStructures[area];
        }

        public static void RegisterAreaStructures(uint area)
        {
            if(!_areaBaseServiceStructures.ContainsKey(area))
                _areaBaseServiceStructures[area] = new List<AreaStructure>();
        }

        public static PCTempBaseData GetPlayerTempData(NWPlayer player)
        {
            if (!player.Data.ContainsKey("BASE_SERVICE_DATA"))
            {
                player.Data["BASE_SERVICE_DATA"] = new PCTempBaseData();
            }

            return player.Data["BASE_SERVICE_DATA"];
        }

        public static void ClearPlayerTempData(NWPlayer player)
        {
            if (player.Data.ContainsKey("BASE_SERVICE_DATA"))
            {
                PCTempBaseData data = player.Data["BASE_SERVICE_DATA"];
                if (data.StructurePreview != null && data.StructurePreview.IsValid)
                {
                    data.StructurePreview.Destroy();
                }

                player.Data.Remove("BASE_SERVICE_DATA");
            }
        }

        private static void OnModuleUseFeat()
        {
            NWPlayer player = (OBJECT_SELF);
            var featID = Convert.ToInt32(Events.GetEventData("FEAT_ID"));

            var positionX = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_X"));
            var positionY = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Y"));
            var positionZ = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Z"));
            var area = (uint)StringToObject(Events.GetEventData("AREA_OBJECT_ID"));
            var vector = Vector3(positionX, positionY, positionZ);

            var targetLocation = Location(area, vector, 0.0f);

            if (featID != (int)Feat.StructureTool) return;

            var data = GetPlayerTempData(player);
            data.TargetArea = area;
            data.TargetLocation = targetLocation;
            data.TargetObject = StringToObject(Events.GetEventData("TARGET_OBJECT_ID"));

            player.ClearAllActions();
            DialogService.StartConversation(player, player, "BaseManagementTool");
        }

        private static void OnModuleLoad()
        {
            RegisterDoorRules();
            foreach (var area in NWModule.Get().Areas)
            {
                var areaResref = GetResRef(area);
                if (!_areaBaseServiceStructures.ContainsKey(area))
                {
                    _areaBaseServiceStructures[area] = new List<AreaStructure>();
                }

                var pcBases = DataService.PCBase.GetAllNonApartmentPCBasesByAreaResref(areaResref);
                foreach (var @base in pcBases)
                {
                    // Migration code : ensure owner has all permissions.
                    var allPermissions = Enum.GetValues(typeof(BasePermission)).Cast<BasePermission>().ToArray();
                    BasePermissionService.GrantBasePermissions(@base.PlayerID, @base.ID, allPermissions);

                    var structures = DataService.PCBaseStructure.GetAllByPCBaseID(@base.ID);
                    foreach (var structure in structures)
                    {
                        if (structure.ParentPCBaseStructureID != null) continue; // Don't spawn any structures contained by buildings.
                        SpawnStructure(area, structure.ID);
                    }
                }

            }
        }

        private static void RegisterDoorRules()
        {
            // Use reflection to get all of SpawnRule implementations.
            var classes = Assembly.GetCallingAssembly().GetTypes()
                .Where(p => typeof(IDoorRule).IsAssignableFrom(p) && p.IsClass && !p.IsAbstract).ToArray();
            foreach (var type in classes)
            {
                var instance = Activator.CreateInstance(type) as IDoorRule;
                if (instance == null)
                {
                    throw new NullReferenceException("Unable to activate instance of type: " + type);
                }
                _doorRules.Add(type.Name, instance);
            }
        }


        public static IDoorRule GetDoorRule(string key)
        {
            if (!_doorRules.ContainsKey(key))
            {
                throw new KeyNotFoundException("Door rule '" + key + "' is not registered. Did you create a class for it?");
            }

            return _doorRules[key];
        }

        public static NWPlaceable SpawnStructure(uint area, Guid pcBaseStructureID)
        {
            var pcStructure = DataService.PCBaseStructure.GetByID(pcBaseStructureID);

            NWLocation location = Location(area,
                Vector3((float)pcStructure.LocationX, (float)pcStructure.LocationY, (float)pcStructure.LocationZ),
                (float)pcStructure.LocationOrientation);

            var baseStructure = DataService.BaseStructure.GetByID(pcStructure.BaseStructureID);
            var structureType = (BaseStructureType)baseStructure.BaseStructureTypeID;
            var resref = baseStructure.PlaceableResref;
            var exteriorStyle = pcStructure.ExteriorStyleID == null ? null : DataService.BuildingStyle.GetByID(Convert.ToInt32(pcStructure.ExteriorStyleID));

            List<AreaStructure> areaStructures = _areaBaseServiceStructures[area];
            if (exteriorStyle != null &&
                string.IsNullOrWhiteSpace(resref) &&
                structureType == BaseStructureType.Building)
            {
                resref = exteriorStyle.Resref;
            }

            NWPlaceable plc = (CreateObject(ObjectType.Placeable, resref, location));
            plc.SetLocalString("PC_BASE_STRUCTURE_ID", pcStructure.ID.ToString());
            plc.SetLocalInt("REQUIRES_BASE_POWER", baseStructure.RequiresBasePower ? 1 : 0);
            plc.SetLocalString("ORIGINAL_SCRIPT_CLOSED", GetEventScript(plc.Object, EventScript.Placeable_OnClosed));
            plc.SetLocalString("ORIGINAL_SCRIPT_DAMAGED", GetEventScript(plc.Object, EventScript.Placeable_OnDamaged));
            plc.SetLocalString("ORIGINAL_SCRIPT_DEATH", GetEventScript(plc.Object, EventScript.Placeable_OnDeath));
            plc.SetLocalString("ORIGINAL_SCRIPT_HEARTBEAT", GetEventScript(plc.Object, EventScript.Placeable_OnHeartbeat));
            plc.SetLocalString("ORIGINAL_SCRIPT_INVENTORYDISTURBED", GetEventScript(plc.Object, EventScript.Placeable_OnInventoryDisturbed));
            plc.SetLocalString("ORIGINAL_SCRIPT_LOCK", GetEventScript(plc.Object, EventScript.Placeable_OnLock));
            plc.SetLocalString("ORIGINAL_SCRIPT_MELEEATTACKED", GetEventScript(plc.Object, EventScript.Placeable_OnMeleeAttacked));
            plc.SetLocalString("ORIGINAL_SCRIPT_OPEN", GetEventScript(plc.Object, EventScript.Placeable_OnOpen));
            plc.SetLocalString("ORIGINAL_SCRIPT_SPELLCASTAT", GetEventScript(plc.Object, EventScript.Placeable_OnSpellCastAt));
            plc.SetLocalString("ORIGINAL_SCRIPT_UNLOCK", GetEventScript(plc.Object, EventScript.Placeable_OnUnlock));
            plc.SetLocalString("ORIGINAL_SCRIPT_USED", GetEventScript(plc.Object, EventScript.Placeable_OnUsed));
            plc.SetLocalString("ORIGINAL_SCRIPT_USER_DEFINED_EVENT", GetEventScript(plc.Object, EventScript.Placeable_OnUserDefined));
            plc.SetLocalString("ORIGINAL_SCRIPT_LEFT_CLICK", GetEventScript(plc.Object, EventScript.Placeable_OnLeftClick));
            plc.SetLocalString("ORIGINAL_SCRIPT_1", GetLocalString(plc.Object, "SCRIPT_1"));

            if (!string.IsNullOrWhiteSpace(pcStructure.CustomName))
            {
                plc.Name = pcStructure.CustomName;
            }

            NWPlaceable door = null;
            if (exteriorStyle != null && (structureType == BaseStructureType.Building || structureType == BaseStructureType.Starship))
            {
                door = SpawnBuildingDoor(exteriorStyle.DoorRule, plc);
                areaStructures.Add(new AreaStructure(pcStructure.PCBaseID, pcStructure.ID, door, false, null));
            }
            areaStructures.Add(new AreaStructure(pcStructure.PCBaseID, pcStructure.ID, plc, true, door));

            if (AreaService.IsAreaInstance(area) && !string.IsNullOrWhiteSpace(GetLocalString(area, "PC_BASE_STRUCTURE_ID")))
            {
                var pcBase = DataService.PCBase.GetByID(pcStructure.PCBaseID);
                if (DateTime.UtcNow > pcBase.DateFuelEnds && pcBase.Fuel <= 0)
                {
                    ToggleInstanceObjectPower(area, false);
                }
            }

            // Starship structures don't count as part of the base they are docked in.  Instead, the starship has its
            // own base entry, with a ShipLocation field that can be set to the structure ID of a docking bay in
            // someone else's base.
            if (structureType == BaseStructureType.StarshipProduction)
            {
                LoggingService.Trace(TraceComponent.Space, "Found starship dock.");

                // See whether any starship is docked here.
                var starship = DataService.PCBase.GetByShipLocationOrDefault(pcStructure.ID.ToString());

                if (starship != null)
                {
                    LoggingService.Trace(TraceComponent.Space, "Found a starship docked in this dock.");

                    // Find the PCBaseStructure in the starship base that has an exterior listed.  This will be the actual
                    // starship. 
                    var shipExterior = DataService.PCBaseStructure.GetStarshipInteriorByPCBaseIDOrDefault(starship.ID);

                    if (shipExterior == null)
                    {
                        // We have a starship PCBase with no accompanying PCBaseStructure.  This is a bug (and crashes NWNX
                        // if we try to continue). 
                        LoggingService.Trace(TraceComponent.Space, "Found PCBase " + starship.ID.ToString() + " with missing PCBaseStructure!");
                    }
                    else
                    {
                        LoggingService.Trace(TraceComponent.Space, "Spawning starship with ID: " + shipExterior.ID.ToString());
                        SpawnStructure(area, shipExterior.ID);
                        plc.SetLocalInt("DOCKED_STARSHIP", 1);
                    }
                }
            }

            return plc;
        }

        public static void ToggleInstanceObjectPower(uint area, bool isPoweredOn)
        {
            List<AreaStructure> areaStructures = _areaBaseServiceStructures[area];

            foreach (var record in areaStructures)
            {
                var structure = record.Structure;
                if (GetLocalBool(structure, "REQUIRES_BASE_POWER") == true)
                {
                    if (isPoweredOn)
                    {
                        SetEventScript(structure.Object, EventScript.Placeable_OnClosed, structure.GetLocalString("ORIGINAL_SCRIPT_CLOSED"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnDamaged, structure.GetLocalString("ORIGINAL_SCRIPT_DAMAGED"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnDeath, structure.GetLocalString("ORIGINAL_SCRIPT_DEATH"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnHeartbeat, structure.GetLocalString("ORIGINAL_SCRIPT_HEARTBEAT"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnInventoryDisturbed, structure.GetLocalString("ORIGINAL_SCRIPT_INVENTORYDISTURBED"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnLock, structure.GetLocalString("ORIGINAL_SCRIPT_LOCK"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnMeleeAttacked, structure.GetLocalString("ORIGINAL_SCRIPT_MELEEATTACKED"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnOpen, structure.GetLocalString("ORIGINAL_SCRIPT_OPEN"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnSpellCastAt, structure.GetLocalString("ORIGINAL_SCRIPT_SPELLCASTAT"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnUnlock, structure.GetLocalString("ORIGINAL_SCRIPT_UNLOCK"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnUsed, structure.GetLocalString("ORIGINAL_SCRIPT_USED"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnUserDefined, structure.GetLocalString("ORIGINAL_SCRIPT_USER_DEFINED_EVENT"));
                        SetEventScript(structure.Object, EventScript.Placeable_OnLeftClick, structure.GetLocalString("ORIGINAL_SCRIPT_LEFT_CLICK"));
                        structure.SetLocalString("SCRIPT_1", structure.GetLocalString("ORIGINAL_SCRIPT_1"));
                        structure.IsLocked = false;
                    }
                    else
                    {
                        SetEventScript(structure.Object, EventScript.Placeable_OnClosed, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnDamaged, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnDeath, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnHeartbeat, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnInventoryDisturbed, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnLock, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnMeleeAttacked, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnOpen, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnSpellCastAt, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnUnlock, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnUsed, "script_1");
                        SetEventScript(structure.Object, EventScript.Placeable_OnUserDefined, string.Empty);
                        SetEventScript(structure.Object, EventScript.Placeable_OnLeftClick, string.Empty);
                        structure.SetLocalString("SCRIPT_1", "Placeable.DisabledStructure.OnUsed");
                        structure.IsLocked = true;
                    }
                }
            }

        }

        public static NWPlaceable SpawnBuildingDoor(string spawnRule, NWPlaceable building, NWLocation locationOverride = null)
        {
            var area = building.Area;
            var location = locationOverride ?? building.Location;

            var pcBaseStructureID = building.GetLocalString("PC_BASE_STRUCTURE_ID");
            var doorRule = GetDoorRule(spawnRule);
            var door = doorRule.Run(area, location);
            door.SetLocalString("PC_BASE_STRUCTURE_ID", pcBaseStructureID);
            SetLocalBool(door, "IS_DOOR", true);

            return door;
        }

        public static void PurchaseArea(NWPlayer player, uint area, string sector)
        {
            if (sector != AreaSector.Northwest && sector != AreaSector.Northeast &&
                sector != AreaSector.Southwest && sector != AreaSector.Southeast)
                throw new ArgumentException(nameof(sector) + " must match one of the valid sector values: NE, NW, SE, SW");

            var width = GetAreaSize(Dimension.Width, area);
            var height = GetAreaSize(Dimension.Height, area);
            if (width < 32) throw new Exception("Area must be at least 32 tiles wide.");
            if (height < 32) throw new Exception("Area must be at least 32 tiles high.");

            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var dbArea = DataService.Area.GetByResref(areaResref);
            Guid? existingOwner = null;
            switch (sector)
            {
                case AreaSector.Northwest: existingOwner = dbArea.NorthwestOwner; break;
                case AreaSector.Northeast: existingOwner = dbArea.NortheastOwner; break;
                case AreaSector.Southwest: existingOwner = dbArea.SouthwestOwner; break;
                case AreaSector.Southeast: existingOwner = dbArea.SoutheastOwner; break;
            }

            if (existingOwner != null)
            {
                player.SendMessage("Another player already owns that sector.");
                return;
            }

            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var purchasePrice = dbArea.PurchasePrice + (int)(dbArea.PurchasePrice * (dbPlayer.LeaseRate * 0.01f));

            if (player.Gold < purchasePrice)
            {
                player.SendMessage("You do not have enough credits to purchase that sector.");
                return;
            }

            player.AssignCommand(() => TakeGoldFromCreature(purchasePrice, player.Object, true));

            switch (sector)
            {
                case AreaSector.Northwest: dbArea.NorthwestOwner = player.GlobalID; break;
                case AreaSector.Northeast: dbArea.NortheastOwner = player.GlobalID; break;
                case AreaSector.Southwest: dbArea.SouthwestOwner = player.GlobalID; break;
                case AreaSector.Southeast: dbArea.SoutheastOwner = player.GlobalID; break;
            }

            DataService.SubmitDataChange(dbArea, DatabaseActionType.Update);

            var pcBase = new PCBase
            {
                AreaResref = dbArea.Resref,
                PlayerID = player.GlobalID,
                DateInitialPurchase = DateTime.UtcNow,
                DateRentDue = DateTime.UtcNow.AddDays(7),
                DateFuelEnds = DateTime.UtcNow,
                Sector = sector,
                PCBaseTypeID = (int)PCBaseType.RegularBase,
                CustomName = string.Empty
            };
            DataService.SubmitDataChange(pcBase, DatabaseActionType.Insert);

            var permission = new PCBasePermission
            {
                PCBaseID = pcBase.ID,
                PlayerID = player.GlobalID
            };
            DataService.SubmitDataChange(permission, DatabaseActionType.Insert);

            // Grant all base permissions to owner.
            var allPermissions = Enum.GetValues(typeof(BasePermission)).Cast<BasePermission>().ToArray();
            BasePermissionService.GrantBasePermissions(player, pcBase.ID, allPermissions);

            player.FloatingText("You purchase " + areaName + " (" + sector + ") for " + purchasePrice + " credits.");

            MessageHub.Instance.Publish(new OnPurchaseLand(player, sector, areaName, areaTag, areaResref, PCBaseType.RegularBase));
        }

        private static void OnModuleHeartbeat()
        {
            var module = NWModule.Get();
            var ticks = module.GetLocalInt("BASE_SERVICE_TICKS") + 1;


            if (ticks >= 10)
            {
                var playerIDs = new List<Tuple<Guid, string>>();
                var pcBases = DataService.PCBase.GetAllWhereRentDue();

                foreach (var pcBase in pcBases)
                {
                    var dbArea = DataService.Area.GetByResref(pcBase.AreaResref);
                    playerIDs.Add(new Tuple<Guid, string>(pcBase.PlayerID, dbArea.Name + " (" + pcBase.Sector + ")"));
                    ClearPCBaseByID(pcBase.ID);
                    MessageHub.Instance.Publish(new OnBaseLeaseExpired(pcBase));
                }

                var players = module.Players.ToList();
                foreach (var removed in playerIDs)
                {
                    var existing = players.FirstOrDefault(x => x.GlobalID == removed.Item1);
                    existing?.FloatingText("Your lease on " + removed.Item2 + " has expired. All structures and items have been impounded by the planetary government. Speak with them to pay a fee and retrieve your goods.");
                }

                ticks = 0;
            }

            module.SetLocalInt("BASE_SERVICE_TICKS", ticks);
        }

        public static PCBaseStructure GetBaseControlTower(Guid pcBaseID)
        {
            // Note - if this is a starship base, then the "control tower" is the starship object. 
            var structures = DataService.PCBaseStructure.GetAllByPCBaseID(pcBaseID);

            return structures.SingleOrDefault(x =>
            {
                var baseStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);
                return (baseStructure.BaseStructureTypeID == (int)BaseStructureType.ControlTower || baseStructure.BaseStructureTypeID == (int)BaseStructureType.Starship);
            });
        }

        public static double GetPowerInUse(Guid pcBaseID)
        {
            return DataService.PCBaseStructure.GetPowerInUseByPCBaseID(pcBaseID);
        }

        public static double GetCPUInUse(Guid pcBaseID)
        {
            return DataService.PCBaseStructure.GetCPUInUseByPCBaseID(pcBaseID);
        }

        public static double GetMaxBaseCPU(Guid pcBaseID)
        {
            var tower = GetBaseControlTower(pcBaseID);

            if (tower == null) return 0.0d;

            var structure = DataService.BaseStructure.GetByID(tower.BaseStructureID);

            return structure.CPU + (tower.StructureBonus * 2);
        }

        public static double GetMaxBasePower(Guid pcBaseID)
        {
            var tower = GetBaseControlTower(pcBaseID);

            if (tower == null) return 0.0d;

            var structure = DataService.BaseStructure.GetByID(tower.BaseStructureID);

            return structure.Power + (tower.StructureBonus * 3);
        }

        public static string GetSectorOfLocation(NWLocation targetLocation)
        {
            var area = targetLocation.Area;
            var cellX = (int)(GetPositionFromLocation(targetLocation).X / 10);
            var cellY = (int)(GetPositionFromLocation(targetLocation).Y / 10);
            var pcBaseID = GetLocalString(area, "PC_BASE_ID");

            var sector = "INVALID";

            if (!string.IsNullOrWhiteSpace(pcBaseID))
            {
                sector = "AP";
            }
            // NWN location positions start at the bottom left, not the top left.
            else if (cellX >= 0 && cellX <= 15 &&
                cellY >= 0 && cellY <= 15)
            {
                sector = AreaSector.Southwest;
            }
            else if (cellX >= 16 && cellX <= 31 &&
                     cellY >= 16 && cellY <= 31)
            {
                sector = AreaSector.Northeast;
            }
            else if (cellX >= 0 && cellX <= 15 &&
                     cellY >= 16 && cellY <= 31)
            {
                sector = AreaSector.Northwest;
            }
            else if (cellX >= 16 && cellX <= 31 &&
                     cellY >= 0 && cellY <= 15)
            {
                sector = AreaSector.Southeast;
            }

            return sector;
        }

        public static string CanPlaceStructure(NWCreature user, NWItem structureItem, NWLocation targetLocation, int baseStructureID)
        {
            NWPlayer player = user.Object;
            var sector = GetSectorOfLocation(targetLocation);

            // Picked an invalid sector
            if (sector == "INVALID")
            {
                return "Invalid location selected.";
            }

            // Can't find the structure item for some reason.
            if (structureItem == null || !structureItem.IsValid || !Equals(structureItem.Possessor, user))
            {
                return "Unable to locate structure item.";
            }

            var area = GetAreaFromLocation(targetLocation);
            var buildingStructureID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");
            var buildingStructureGuid = string.IsNullOrWhiteSpace(buildingStructureID) ? Guid.Empty : new Guid(buildingStructureID);
            var pcBaseID = GetLocalString(area, "PC_BASE_ID");
            var pcBaseGUID = string.IsNullOrWhiteSpace(pcBaseID) ? Guid.Empty : new Guid(pcBaseID);

            // Identify the building type.
            BuildingType buildingType;
            if (!string.IsNullOrWhiteSpace(pcBaseID))
            {
                buildingType = BuildingType.Apartment;
            }
            else if (string.IsNullOrWhiteSpace(buildingStructureID))
            {
                buildingType = BuildingType.Exterior;
            }
            else
            {
                // Starship or building - check the variable to tell which. 
                var buildingTypeID = GetLocalInt(area, "BUILDING_TYPE");
                buildingType = (BuildingType)buildingTypeID;
            }

            var areaResref = GetResRef(area);
            var dbArea = DataService.Area.GetByResrefOrDefault(areaResref);

            // Can't build in this area.
            if (dbArea == null || !dbArea.IsBuildable) return "Structures cannot be placed in this area.";
            var pcBase = !string.IsNullOrWhiteSpace(pcBaseID) ?
                DataService.PCBase.GetByID(pcBaseGUID) :
                DataService.PCBase.GetByAreaResrefAndSectorOrDefault(areaResref, sector);

            // Check and see if the player has hit the structure limit.
            if (pcBase == null && buildingType == BuildingType.Interior)
            {
                var parentStructure = DataService.PCBaseStructure.GetByID(buildingStructureGuid);
                var parentBaseStructure = DataService.BaseStructure.GetByID(parentStructure.BaseStructureID);
                pcBase = DataService.PCBase.GetByID(parentStructure.PCBaseID);

                var buildingStructureCount = DataService.PCBaseStructure.GetAll().Count(x => x.ParentPCBaseStructureID == parentStructure.ID) + 1;
                if (buildingStructureCount > parentBaseStructure.Storage + parentStructure.StructureBonus)
                {
                    return "No more structures can be placed inside this building.";
                }
            }
            else if (pcBase == null && buildingType == BuildingType.Starship)
            {
                var parentStructure = DataService.PCBaseStructure.GetByID(buildingStructureGuid);
                var buildingStyle = DataService.BuildingStyle.GetByID(Convert.ToInt32(parentStructure.InteriorStyleID));
                pcBase = DataService.PCBase.GetByID(parentStructure.PCBaseID);

                var buildingStructureCount = DataService.PCBaseStructure.GetAll().Count(x => x.ParentPCBaseStructureID == parentStructure.ID) + 1;
                if (buildingStructureCount > buildingStyle.FurnitureLimit + parentStructure.StructureBonus)
                {
                    return "No more structures can be placed inside this starship.";
                }
            }
            else if (pcBase != null && buildingType == BuildingType.Apartment)
            {
                var buildingStyle = DataService.BuildingStyle.GetByID(Convert.ToInt32(pcBase.BuildingStyleID));
                var buildingStructureCount = DataService.PCBaseStructure.GetAllByPCBaseID(pcBase.ID).Count();
                if (buildingStructureCount > buildingStyle.FurnitureLimit)
                {
                    return "No more structures can be placed inside this building.";
                }
            }

            // Area is unclaimed but PC doesn't own it.
            if (pcBase == null)
                return "This area is unclaimed but not owned by you. You may purchase a lease on it from the planetary government by using your Base Management Tool (found under feats).";

            // Check whether player has permission to place or edit structures.
            var canPlaceOrEditStructures = buildingType == BuildingType.Apartment || buildingType == BuildingType.Exterior ?
                BasePermissionService.HasBasePermission(player, pcBase.ID, BasePermission.CanPlaceEditStructures) :                 // Bases
                BasePermissionService.HasStructurePermission(player, buildingStructureGuid, StructurePermission.CanPlaceEditStructures);    // Buildings

            var baseStructure = DataService.BaseStructure.GetByID(baseStructureID);
            var baseStructureType = DataService.BaseStructureType.GetByID(baseStructure.BaseStructureTypeID);

            if (baseStructureType.ID == (int)BaseStructureType.Starship)
            {
                canPlaceOrEditStructures = BasePermissionService.HasBasePermission(player, pcBase.ID, BasePermission.CanDockStarship);
            }

            // Don't have permission.
            if (!canPlaceOrEditStructures)
                return "You do not have permission to place or edit structures in this territory.";

            // Can only place this structure inside buildings and the player is currently outside.
            if (!baseStructureType.CanPlaceOutside && buildingType == BuildingType.Exterior)
            {
                return "That structure can only be placed inside buildings.";
            }

            // Can only place this structure outside
            if (!baseStructureType.CanPlaceInside && (buildingType == BuildingType.Interior || buildingType == BuildingType.Apartment || buildingType == BuildingType.Starship))
            {
                return "That structure can only be placed outside of buildings.";
            }

            // Check for control tower requirements.
            if (buildingType == BuildingType.Exterior)
            {
                var structures = DataService.PCBaseStructure.GetAllByPCBaseID(pcBase.ID);

                var hasControlTower = structures
                                           .SingleOrDefault(x =>
                                           {
                                               var bs = DataService.BaseStructure.GetByID(x.BaseStructureID);
                                               return bs.BaseStructureTypeID == (int)BaseStructureType.ControlTower;
                                           }) != null;

                if (!hasControlTower && baseStructureType.ID != (int)BaseStructureType.ControlTower)
                {
                    return "A control tower must be placed down in the sector first.";
                }

                if (hasControlTower && baseStructureType.ID == (int)BaseStructureType.ControlTower)
                {
                    return "Only one control tower can be placed down per sector.";
                }
            }

            // Crafting devices may only be placed inside buildings set to the 'Workshop' mode.
            if (baseStructureType.ID == (int)BaseStructureType.CraftingDevice)
            {
                if (buildingType == BuildingType.Interior)
                {
                    var parentBuilding = DataService.PCBaseStructure.GetByID(buildingStructureGuid);
                    var mode = (StructureModeType)parentBuilding.StructureModeID;

                    if (mode != StructureModeType.Workshop)
                    {
                        return "Crafting devices may only be placed inside buildings set to the 'Workshop' mode.";
                    }
                }
            }

            // Starships may only be placed on an empty bay.
            if (baseStructureType.ID == (int)BaseStructureType.Starship)
            {
                var nNth = 1;
                NWObject dock = GetNearestObjectToLocation(targetLocation, ObjectType.Placeable, nNth);

                while (dock.IsValid)
                {
                    // Not close enough. 
                    if (GetDistanceBetweenLocations(targetLocation, dock.Location) > 10.0f) break;

                    // Ship already docked here.
                    if (dock.GetLocalInt("DOCKED_STARSHIP") == 1)
                    {
                        nNth++;
                        dock = GetNearestObjectToLocation(targetLocation, ObjectType.Placeable, nNth);
                        continue;
                    }

                    var dockPCBaseStructureID = dock.GetLocalString("PC_BASE_STRUCTURE_ID");

                    if (!string.IsNullOrWhiteSpace(dockPCBaseStructureID))
                    {
                        var dockPCBaseStructureGuid = new Guid(dockPCBaseStructureID);
                        var dockStructure = DataService.PCBaseStructure.GetByID(dockPCBaseStructureGuid);
                        var dockBaseStructure = DataService.BaseStructure.GetByID(dockStructure.BaseStructureID);

                        if (dockBaseStructure.BaseStructureTypeID == (int)BaseStructureType.StarshipProduction)
                        {
                            // We've found a dock!
                            dock.SetLocalInt("DOCKED_STARSHIP", 1);

                            // Create a new base for the starship and mark its location as dockPCBaseStructureID 
                            var style = DataService.BuildingStyle.GetByBaseStructureIDAndBuildingType(baseStructureID, BuildingType.Starship);

                            var starkillerBase = new PCBase
                            {
                                PlayerID = player.GlobalID,
                                DateInitialPurchase = DateTime.UtcNow,
                                DateFuelEnds = DateTime.UtcNow,
                                DateRentDue = DateTime.UtcNow.AddDays(999),
                                PCBaseTypeID = (int)PCBaseType.Starship,
                                Sector = "SS",
                                BuildingStyleID = style.ID,
                                AreaResref = style.Resref,
                                CustomName = string.Empty,
                                ShipLocation = dockPCBaseStructureID
                            };
                            DataService.SubmitDataChange(starkillerBase, DatabaseActionType.Insert);

                            var permission = new PCBasePermission
                            {
                                PCBaseID = starkillerBase.ID,
                                PlayerID = player.GlobalID
                            };
                            DataService.SubmitDataChange(permission, DatabaseActionType.Insert);

                            // Grant all base permissions to owner.
                            var allPermissions = Enum.GetValues(typeof(BasePermission)).Cast<BasePermission>().ToArray();
                            BasePermissionService.GrantBasePermissions(player, starkillerBase.ID, allPermissions);
                            var position = GetPositionFromLocation(targetLocation);
                            var extStyle = DataService.BuildingStyle.GetByBaseStructureIDAndBuildingType(baseStructureID, BuildingType.Exterior);

                            // Create the PC base structure entry, and call SpawnStructure to manifest it.
                            var starshipStructure = new PCBaseStructure
                            {
                                BaseStructureID = baseStructureID,
                                Durability = DurabilityService.GetDurability(structureItem),
                                LocationOrientation = GetFacingFromLocation(targetLocation),
                                LocationX = position.X,
                                LocationY = position.Y,
                                LocationZ = position.Z,
                                PCBaseID = starkillerBase.ID,
                                InteriorStyleID = style.ID,
                                ExteriorStyleID = extStyle.ID,
                                CustomName = string.Empty,
                                StructureBonus = structureItem.StructureBonus,
                                StructureModeID = baseStructure.DefaultStructureModeID
                            };
                            DataService.SubmitDataChange(starshipStructure, DatabaseActionType.Insert);

                            SpawnStructure(area, starshipStructure.ID);

                            // Delete the item from the PC's inventory.
                            structureItem.Destroy();

                            return "Starship successfully docked.";
                        }
                    }

                    nNth++;
                    dock = GetNearestObjectToLocation(targetLocation, ObjectType.Placeable, nNth);
                }

                return "Unable to dock starship.  Starships must be docked on a vacant docking bay.";
            }

            return null;
        }

        public static NWItem ConvertStructureToItem(PCBaseStructure pcBaseStructure, NWObject target)
        {
            var baseStructure = DataService.BaseStructure.GetByID(pcBaseStructure.BaseStructureID);
            NWItem item = (CreateItemOnObject(baseStructure.ItemResref, target.Object));
            item.SetLocalInt("BASE_STRUCTURE_ID", pcBaseStructure.BaseStructureID);
            item.Name = baseStructure.Name;

            DurabilityService.SetMaxDurability(item, (float)pcBaseStructure.Durability);
            DurabilityService.SetDurability(item, (float)pcBaseStructure.Durability);
            item.StructureBonus = pcBaseStructure.StructureBonus;

            if (pcBaseStructure.InteriorStyleID != null && pcBaseStructure.ExteriorStyleID != null)
            {
                item.SetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID", (int)pcBaseStructure.InteriorStyleID);
                item.SetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID", (int)pcBaseStructure.ExteriorStyleID);
                SetLocalBool(item, "STRUCTURE_BUILDING_INITIALIZED", true);
            }

            return item;
        }

        public static void BootPlayersOutOfInstance(Guid pcBaseStructureID)
        {
            var areas = NWModule.Get().Areas;
            var instance = areas.SingleOrDefault(x => AreaService.IsAreaInstance(x) && GetLocalString(x, "PC_BASE_STRUCTURE_ID") == pcBaseStructureID.ToString());
            if (GetIsObjectValid(instance))
            {
                foreach (var player in NWModule.Get().Players)
                {
                    if (Equals(player.Area, instance))
                    {
                        DoPlayerExitBuildingInstance(player);
                    }
                }
            }
        }

        public static void ClearPCBaseByID(Guid pcBaseID, bool displayExplosion = false, bool impoundItems = true)
        {
            LoggingService.Trace(TraceComponent.Base, "Destroying base with base ID: " + pcBaseID.ToString());

            var pcBase = DataService.PCBase.GetByID(pcBaseID);

            // Pull back all structures associated with a PC's base. 
            // The order here is important because we want to remove child structures first otherwise we'll get foreign key issues when the DB persists.
            // For this reason, we want to order by ParentPCBaseStructureID which will give us those with an ID followed by those with no ID (i.e null).
            var structures = DataService
                .PCBaseStructure.GetAllByPCBaseID(pcBaseID)
                .OrderBy(o => o.ParentPCBaseStructureID)
                .ToList();

            var areas = NWModule.Get().Areas;
            var baseArea = areas.Single(x => GetResRef(x) == pcBase.AreaResref && !AreaService.IsAreaInstance(x));
            List<AreaStructure> areaStructures = _areaBaseServiceStructures[baseArea];
            areaStructures = areaStructures.Where(x => x.PCBaseID == pcBaseID).ToList();

            // Remove the primary resident of the base.
            var basePrimaryResident = DataService.Player.GetByPrimaryResidencePCBaseIDOrDefault(pcBaseID);
            if (basePrimaryResident != null)
            {
                basePrimaryResident.PrimaryResidencePCBaseID = null;
                DataService.SubmitDataChange(basePrimaryResident, DatabaseActionType.Update);
            }
            // Get all structures whose PCBaseID matches this one (I.E children of this base)
            // Then filter out the ones with a primary resident
            var childStructures = DataService.PCBaseStructure.GetAllByPCBaseID(pcBaseID);
            foreach (var child in childStructures)
            {
                var primaryResident = DataService.Player.GetByPrimaryResidencePCBaseStructureIDOrDefault(child.ID);

                // If we found a resident, mark their primary residence as null and update in the cache.
                if (primaryResident != null)
                {
                    primaryResident.PrimaryResidencePCBaseStructureID = null;
                    DataService.SubmitDataChange(primaryResident, DatabaseActionType.Update);
                }
            }

            for(var index = areaStructures.Count-1; index >= 0; index--)
            {
                var structure = _areaBaseServiceStructures[baseArea][index];
                BootPlayersOutOfInstance(structure.PCBaseStructureID);

                if (structure.Structure.GetLocalInt("DOCKED_STARSHIP") == 1)
                {
                    // This is a dock with a starship parked.  Clear the docked starship base entry as well.
                    var starkillerBase = DataService.PCBase.GetByShipLocationOrDefault(structure.PCBaseStructureID.ToString());

                    if (starkillerBase == null)
                    {
                        Console.WriteLine("Unable to locate base in BaseService -> ClearPCBaseByID. PCBaseStructureID = " + structure.PCBaseStructureID);
                        continue;
                    }

                    LoggingService.Trace(TraceComponent.Base, "Destroying child starship with base ID: " + starkillerBase.ID);
                    ClearPCBaseByID(starkillerBase.ID);
                }

                _areaBaseServiceStructures[baseArea].RemoveAt(index);
                structure.Structure.Destroy();
            }

            var rubbleContainers = new Dictionary<Guid, NWPlaceable>();
            for (var x = structures.Count - 1; x >= 0; x--)
            {
                var pcBaseStructure = structures.ElementAt(x);
                var baseStructure = DataService.BaseStructure.GetByID(pcBaseStructure.BaseStructureID);
                var items = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(pcBaseStructure.ID).ToList();
                NWPlaceable rubbleContainer = null;

                if (!impoundItems)
                {
                    // Pull the area structure from the cache and create a new container at the same location.
                    // This new placeable will be used later on to store items since the impoundItems flag has been set.
                    // 
                    // Note: We're iterating over both objects which are in the tower area as well as the objects contained inside buildings.
                    // In the case of contained items, we need to locate the parent structure (i.e the building) instead of the structure itself.
                    //
                    // Regarding the s.ChildStructure section: For buildings there are two placeables stored - one for the building placeable and another for the door.
                    // In this scenario we only want one of them so we ignore the structure which has a door. There's room for improvement here because this isn't intuitive.
                    var structureKey = (Guid)(
                        pcBaseStructure.ParentPCBaseStructureID == null ? // If the structure has a parent, it can be assumed to be inside of a building.
                            pcBaseStructure.ID : // "Outside" structures
                            pcBaseStructure.ParentPCBaseStructureID); // "Inside" structures (furniture, etc.)

                    if (rubbleContainers.ContainsKey(structureKey))
                    {
                        // Get the existing container.
                        rubbleContainer = rubbleContainers[structureKey];
                    }
                    else
                    {
                        // Container doesn't exist yet. Create a new one and add it to the dictionary.
                        var cachedStructure = areaStructures.Single(s => s.PCBaseStructureID == structureKey && s.ChildStructure == null);
                        rubbleContainer = CreateObject(ObjectType.Placeable, "structure_rubble", cachedStructure.Structure.Location);
                        rubbleContainers.Add(structureKey, rubbleContainer);
                    }
                }

                // If impoundItems is true, all structures and their contents will be transferred to the owner's item impound.
                // Otherwise, the items will drop to a placeable at the location of each structure.
                // Child items will drop to the parent's container.
                for (var i = items.Count - 1; i >= 0; i--)
                {
                    var item = items.ElementAt(i);

                    if (impoundItems)
                    {
                        ImpoundService.Impound(item);
                    }
                    else
                    {
                        SerializationService.DeserializeItem(item.ItemObject, rubbleContainer);
                    }
                    DataService.SubmitDataChange(item, DatabaseActionType.Delete);
                }

                // Clear structure permissions
                var structurePermissions = DataService.PCBaseStructurePermission.GetAllByPCBaseStructureID(pcBaseStructure.ID).ToList();
                for (var p = structurePermissions.Count - 1; p >= 0; p--)
                {
                    var permission = structurePermissions.ElementAt(p);
                    DataService.SubmitDataChange(permission, DatabaseActionType.Delete);
                }

                if (impoundItems)
                {
                    // Build the structure's item in-world and then impound it. Destroy the copy after we're done.
                    var tempStorage = (GetObjectByTag("TEMP_ITEM_STORAGE"));
                    var copy = ConvertStructureToItem(pcBaseStructure, tempStorage);
                    ImpoundService.Impound(pcBase.PlayerID, copy);
                    copy.Destroy();
                }
                else
                {
                    // Control Towers are assumed to be destroyed at this point and won't be dropped.
                    if (baseStructure.BaseStructureTypeID != (int)BaseStructureType.ControlTower)
                    {
                        // We aren't impounding items, so convert the structure into the world and leave it in the rubble container.
                        ConvertStructureToItem(pcBaseStructure, rubbleContainer);
                    }
                }


                DataService.SubmitDataChange(pcBaseStructure, DatabaseActionType.Delete);
            }

            // Clear base permissions
            var permissions = DataService.PCBasePermission.GetAllPermissionsByPCBaseID(pcBaseID).ToList();
            for (var p = permissions.Count - 1; p >= 0; p--)
            {
                var permission = permissions.ElementAt(p);
                DataService.SubmitDataChange(permission, DatabaseActionType.Delete);
            }

            DataService.SubmitDataChange(pcBase, DatabaseActionType.Delete);

            var dbArea = DataService.Area.GetByResref(pcBase.AreaResref);
            if (pcBase.Sector == AreaSector.Northeast) dbArea.NortheastOwner = null;
            else if (pcBase.Sector == AreaSector.Northwest) dbArea.NorthwestOwner = null;
            else if (pcBase.Sector == AreaSector.Southeast) dbArea.SoutheastOwner = null;
            else if (pcBase.Sector == AreaSector.Southwest) dbArea.SouthwestOwner = null;

            DataService.SubmitDataChange(dbArea, DatabaseActionType.Update);


            // Boot players from instances and remove all structures from the area's cache.
            for(var index = areaStructures.Count-1; index >= 0; index--)
            {
                var structure = areaStructures[index];
                BootPlayersOutOfInstance(structure.PCBaseStructureID);

                _areaBaseServiceStructures[baseArea].RemoveAt(index);
                structure.Structure.Destroy();

                // Display explosion on each structure if necessary.
                if (displayExplosion)
                {
                    var location = structure.Structure.Location;
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), location);
                }
            }
        }

        public static void ApplyCraftedItemLocalVariables(NWItem item, BaseStructure structure)
        {
            // Structure items need an additional local variable and their name set on creation.
            if (structure != null)
            {
                item.SetLocalInt("BASE_STRUCTURE_ID", structure.ID);
                item.Name = structure.Name;

                if (structure.BaseStructureTypeID == (int)BaseStructureType.Building)
                {
                    var defaultInterior = DataService.BuildingStyle.GetDefaultInteriorByBaseStructureID(structure.ID).ID;
                    var defaultExterior = DataService.BuildingStyle.GetDefaultExteriorByBaseStructureID(structure.ID).ID;

                    item.SetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID", defaultInterior);
                    item.SetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID", defaultExterior);
                }

            }
        }

        public static Guid? GetPlayerIDOwnerOfSector(Area dbArea, string sector)
        {
            switch (sector)
            {
                case AreaSector.Northeast: return dbArea.NortheastOwner;
                case AreaSector.Northwest: return dbArea.NorthwestOwner;
                case AreaSector.Southeast: return dbArea.SoutheastOwner;
                case AreaSector.Southwest: return dbArea.SouthwestOwner;
            }

            throw new Exception(nameof(GetPlayerIDOwnerOfSector) + ": invalid sector: " + sector);
        }


        public static void JumpPCToBuildingInterior(NWPlayer player, uint area, int apartmentBuildingID = -1)
        {
            NWObject exit = null;

            // Loop through the area to find the building exit placeable.
            NWObject @object = (GetFirstObjectInArea(area));
            while (@object.IsValid)
            {
                if (@object.Tag == "building_exit")
                {
                    exit = @object;
                }

                @object = (GetNextObjectInArea(area));
            }

            // Couldn't find an exit. Simply send error message to player.
            if (exit == null)
            {
                player.FloatingText("ERROR: Couldn't find the building interior's exit. Inform an admin of this issue.");
                return;
            }

            // Assign some local variables to the exit object, for later use.
            exit.SetLocalLocation("PLAYER_HOME_EXIT_LOCATION", player.Location);
            exit.SetLocalInt("IS_BUILDING_DOOR", 1);

            // Assign apartment building ID to the exit only if we're working with an actual apartment.
            if (apartmentBuildingID > 0)
            {
                exit.SetLocalInt("APARTMENT_BUILDING_ID", apartmentBuildingID);
            }

            // Got everything set up. Port the player to the area.
            var location = GetLocalLocation(area, "INSTANCE_ENTRANCE");
            player.AssignCommand(() =>
            {
                ActionJumpToLocation(location);
                ActionDoCommand(() => { PlayerService.SaveLocation(player); });
            });
        }

        public static void DoPlayerExitBuildingInstance(NWPlayer player, NWPlaceable door = null)
        {
            var area = GetArea(player);
            if (!AreaService.IsAreaInstance(area)) return;

            if (door == null)
            {
                NWObject obj = (GetFirstObjectInArea(area));
                while (obj.IsValid)
                {
                    if (obj.Tag == "building_exit")
                    {
                        door = (obj.Object);
                        break;
                    }
                    obj = (GetNextObjectInArea(area));
                }
            }

            if (door == null)
            {
                player.SendMessage("Could not find exit. Either you have entered the module in an expired lease building area. If this is not the case log a /bug and report where you were.");
                LoggingService.Trace(TraceComponent.Space, "Could not find exit. Either you have entered the module in an expired lease building area. If this is not the case log a /bug and report where you were.");
                NWObject waypoint = GetObjectByTag("MN_StarchaserHomes");
                player.AssignCommand(() => ActionJumpToObject(waypoint));
                return;
            }

            NWLocation location = door.GetLocalLocation("PLAYER_HOME_EXIT_LOCATION");

            var structureID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");
            if (!string.IsNullOrWhiteSpace(structureID))
            {
                var structureGuid = new Guid(structureID);
                var baseStructure = DataService.PCBaseStructure.GetByIDOrDefault(structureGuid);
                if (baseStructure != null)
                {
                    var pcBase = DataService.PCBase.GetByIDOrDefault(baseStructure.PCBaseID);
                    if (pcBase != null && pcBase.PCBaseTypeID == (int)PCBaseType.Starship)
                    {
                        // This is a starship.  Exit should be based on location, not based on the door variable.
                        if (SpaceService.IsLocationPublicStarport(pcBase.ShipLocation))
                        {
                            // Retrieve the dock waypoint and jump to it.  
                            var shipLocationGuid = new Guid(pcBase.ShipLocation);
                            var starport = DataService.Starport.GetByStarportID(shipLocationGuid);

                            NWObject waypoint = GetWaypointByTag(starport.WaypointTag);

                            if (!waypoint.IsValid)
                            {
                                player.SendMessage("Could not find waypoint " + starport.WaypointTag + ". This is a bug, please report it.");
                                LoggingService.Trace(TraceComponent.Space, "Failed to find waypoint: " + starport.WaypointTag);
                                return;
                            }

                            player.AssignCommand(() => ActionJumpToObject(waypoint));

                        }
                        else if (SpaceService.IsLocationSpace(pcBase.ShipLocation))
                        {
                            // Cannot exit!
                            // TODO - allow moving between starships that have docked to each other. 
                            player.SendMessage("You are in space! You cannot leave now.");
                        }
                        else
                        {
                            // This is a PC dock.  Find it. 
                            var dock = FindPlaceableFromStructureID(pcBase.ShipLocation);

                            if (!dock.IsValid)
                            {
                                player.SendMessage("Could not find dock " + pcBase.ShipLocation + ". This is a bug, please report it.");
                                LoggingService.Trace(TraceComponent.Space, "Failed to find dock: " + pcBase.ShipLocation);
                                return;
                            }

                            player.AssignCommand(() => ActionJumpToObject(dock));
                        }

                        return;
                    }
                }
            }

            if (!GetIsObjectValid(GetAreaFromLocation(location)))
            {
                // This is a building structure or apartment, but we don't have a stored location to exit from.  
                // This is probably because we logged in in this instance, so the entrance isn't connected.
                // Fix that now.
                if (!String.IsNullOrWhiteSpace(structureID))
                {
                    // Building
                    // Find the door placeable and get its location.  It will have the same ID as the actual 
                    // building, but will have the DOOR variable set.  
                    var structureGuid = new Guid(structureID);
                    var pcbs = DataService.PCBaseStructure.GetByID(structureGuid);
                    var pcBase = DataService.PCBase.GetByID(pcbs.PCBaseID);

                    var areas = NWModule.Get().Areas;
                    var baseArea = GetFirstArea();

                    foreach (var checkArea in areas)
                    {
                        if (GetResRef(checkArea) == pcBase.AreaResref)
                        {
                            baseArea = checkArea;
                        }
                    }

                    List<AreaStructure> areaStructures = _areaBaseServiceStructures[baseArea];
                    foreach (var plc in areaStructures)
                    {
                        LoggingService.Trace(TraceComponent.Space, "Found area structure in " + GetName(GetAreaFromLocation(plc.Structure.Location)) + " with name " + GetName(plc.Structure) + " and pcbs " + plc.PCBaseStructureID.ToString() + " and door " + plc.Structure.GetLocalInt("IS_DOOR").ToString());
                        if (plc.PCBaseStructureID == pcbs.ID && plc.Structure.GetLocalInt("IS_DOOR") == 1)
                        {
                            location = plc.Structure.Location;
                            break;
                        }
                    }

                    if (!GetIsObjectValid(GetAreaFromLocation(location)))
                    {
                        LoggingService.Trace(TraceComponent.None, "Player tried to exit from building, but we couldn't find its door placeable.");
                        player.SendMessage("Sorry, we can't find the exit to this building.  Please report this as a bug, thank you.");
                    }
                }
                else
                {
                    structureID = GetLocalString(area, "PC_BASE_ID");
                    if (!String.IsNullOrWhiteSpace(structureID))
                    {
                        // Apartment.  
                        // Apartment entrances have the tag apartment_ent and the int variable APARTMENT_BUILDING_ID that matches 
                        // pcBase.ApartmentBuildingID
                        var structureGuid = new Guid(structureID);
                        var pcBase = DataService.PCBase.GetByID(structureGuid);
                        var nNth = 0;
                        NWObject entrance = GetObjectByTag("apartment_ent", nNth);

                        while (entrance.IsValid)
                        {
                            LoggingService.Trace(TraceComponent.Space, "Found apartment entrance in " + GetName(GetAreaFromLocation(entrance.Location)) + " with ID " + entrance.GetLocalInt("APARTMENT_BUILDING_ID").ToString());
                            if (entrance.GetLocalInt("APARTMENT_BUILDING_ID") == pcBase.ApartmentBuildingID)
                            {
                                // Found it!
                                location = entrance.Location;
                                break;
                            }

                            nNth++;
                            entrance = GetObjectByTag("apartment_ent", nNth);
                        }

                        if (!GetIsObjectValid(GetAreaFromLocation(location)))
                        {
                            LoggingService.Trace(TraceComponent.None, "Player tried to exit from apartment, but we couldn't find its door placeable.");
                            player.SendMessage("Sorry, we can't find the exit to this apartment.  Please report this as a bug, thank you.");
                        }
                    }
                    else
                    {
                        // Unknown type!
                        LoggingService.Trace(TraceComponent.None, "Player tried to exit from an instance that has neither PC_BASE_ID nor PC_BASE_STRUCTURE_ID defined!");
                        player.SendMessage("Sorry, we don't know where this door goes to.  Please report this as a bug, thank you.");
                        return;
                    }
                }
            }

            player.AssignCommand(() => ActionJumpToLocation(location));

            DelayCommand(1.0f, () =>
            {
                player = (GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, area)) return;
                    player = (GetNextPC());
                }

                AreaService.DestroyAreaInstance(area);
            });
        }

        public static NWPlaceable FindPlaceableFromStructureID(string pcBaseStructureID)
        {
            // Find the placeable and get its location.
            var pcBaseStructureGuid = new Guid(pcBaseStructureID);
            var pcbs = DataService.PCBaseStructure.GetByID(pcBaseStructureGuid);
            var pcBase = DataService.PCBase.GetByID(pcbs.PCBaseID);

            var areas = NWModule.Get().Areas;
            var baseArea = GetFirstArea();

            foreach (var area in areas)
            {
                if (GetResRef(area) == pcBase.AreaResref)
                {
                    baseArea = area;
                }
            }

            List<AreaStructure> areaStructures = _areaBaseServiceStructures[baseArea];
            foreach (var plc in areaStructures)
            {
                if (plc.PCBaseStructureID == pcbs.ID)
                {
                    return plc.Structure;
                }
            }

            return null;
        }

        public static bool CanHandleChat(NWObject sender)
        {
            var validTarget = sender.IsPlayer || sender.IsDM;
            return validTarget && GetLocalBool(sender, "LISTENING_FOR_NEW_CONTAINER_NAME") == true;
        }

        private static void OnModuleNWNXChat()
        {
            NWPlayer sender = OBJECT_SELF;
            var text = Chat.GetMessage().Trim();

            if (!CanHandleChat(sender))
            {
                return;
            }

            Chat.SkipMessage();

            if (text.Length > 32)
            {
                sender.FloatingText("Container names must be 32 characters or less.");
                return;
            }

            sender.SetLocalString("NEW_CONTAINER_NAME", text);
            sender.SendMessage("New container name received. Please press the 'Next' button in the conversation window.");
        }

        public static int CalculateMaxShieldHP(PCBaseStructure controlTower)
        {
            if (controlTower == null) return 0;

            return (int)(controlTower.Durability * 300);
        }

        public static int CalculateMaxFuel(Guid pcBaseID)
        {
            const int siloType = (int)BaseStructureType.FuelSilo;
            var tower = GetBaseControlTower(pcBaseID);

            if (tower == null)
            {
                Console.WriteLine("Could not find tower in BaseService -> CalculateMaxFuel. PCBaseID = " + pcBaseID);
                return 0;
            }

            var towerStructure = DataService.BaseStructure.GetByID(tower.BaseStructureID);

            var siloBonus = DataService.PCBaseStructure.GetAllByPCBaseID(pcBaseID)
                                  .Where(x =>
                                  {
                                      var baseStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);
                                      return x.PCBaseID == pcBaseID && baseStructure.BaseStructureTypeID == siloType;
                                  })
                                  .DefaultIfEmpty()
                                  .Sum(x =>
                                  {
                                      if (x == null) return 0;
                                      var baseStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);
                                      return baseStructure.Storage + x.StructureBonus;
                                  }) * 0.01f;

            var fuelMax = towerStructure.Storage;

            return (int)(fuelMax + fuelMax * (siloBonus * 2));
        }

        public static int CalculateMaxReinforcedFuel(Guid pcBaseID)
        {
            const int siloType = (int)BaseStructureType.StronidiumSilo;
            var tower = GetBaseControlTower(pcBaseID);

            if (tower == null)
            {
                Console.WriteLine("Could not find tower in BaseService -> CalculateMaxReinforcedFuel. PCBaseID = " + pcBaseID);
                return 0;
            }

            var towerBaseStructure = DataService.BaseStructure.GetByID(tower.BaseStructureID);
            var siloBonus = DataService.PCBaseStructure.GetAllByPCBaseID(pcBaseID)
                                  .Where(x =>
                                  {
                                      var baseStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);
                                      return x.PCBaseID == pcBaseID &&
                                             baseStructure.BaseStructureTypeID == siloType;
                                  })
                                  .DefaultIfEmpty()
                                  .Sum(x =>
                                  {
                                      if (x == null) return 0;
                                      var baseStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);

                                      return baseStructure.Storage + x.StructureBonus;
                                  }) * 0.01f;

            var fuelMax = towerBaseStructure.ReinforcedStorage;

            return (int)(fuelMax + fuelMax * (siloBonus * 2));
        }

        public static int CalculateResourceCapacity(Guid pcBaseID)
        {
            const int siloType = (int)BaseStructureType.ResourceSilo;
            var tower = GetBaseControlTower(pcBaseID);

            if (tower == null)
            {
                Console.WriteLine("Could not find tower in BaseService -> CalculateResourceCapacity. PCBaseID = " + pcBaseID);
                return 0;
            }

            var towerBaseStructure = DataService.BaseStructure.GetByID(tower.BaseStructureID);
            var siloBonus = DataService.PCBaseStructure.GetAllByPCBaseID(pcBaseID)
                                  .Where(x =>
                                  {
                                      var baseStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);

                                      return x.PCBaseID == pcBaseID &&
                                             baseStructure.BaseStructureTypeID == siloType;
                                  })
                                  .DefaultIfEmpty()
                                  .Sum(x =>
                                  {
                                      if (x == null) return 0;
                                      var baseStructure = DataService.BaseStructure.GetByID(x.BaseStructureID);

                                      return baseStructure.Storage + x.StructureBonus;
                                  }) * 0.01f;

            var resourceMax = towerBaseStructure.ResourceStorage;

            return (int)(resourceMax + resourceMax * (siloBonus * 2));
        }

        public static string UpgradeControlTower(NWCreature user, NWItem item, NWObject target)
        {
            //--------------------------------------------------------------------------
            // Called when a PC uses a structure item on a placed object.
            // Returns "" if either the item or the target are not control towers.
            // Returns a success or failure message otherwise.
            //--------------------------------------------------------------------------
            NWPlayer player = user.Object;

            //--------------------------------------------------------------------------
            // Check that the item is a control tower.
            //--------------------------------------------------------------------------
            var newTowerStructureID = item.GetLocalInt("BASE_STRUCTURE_ID");
            var newTower = DataService.BaseStructure.GetByID(newTowerStructureID);
            if (newTower.BaseStructureTypeID != (int)BaseStructureType.ControlTower)
            {
                return "";
            }

            //--------------------------------------------------------------------------
            // Check that the target is a control tower.
            //--------------------------------------------------------------------------
            var sTowerID = target.GetLocalString("PC_BASE_STRUCTURE_ID");
            if (string.IsNullOrWhiteSpace(sTowerID)) return "";
            Guid towerGuid;

            try
            {
                towerGuid = new Guid(sTowerID);
            }
            catch (Exception e)
            {
                LoggingService.LogError(e, "Failed to convert GUID: " + sTowerID);
                return "System error - target had invalid GUID.  Please report this error.";
            }

            var towerStructure = DataService.PCBaseStructure.GetByID(towerGuid);
            var oldTower = DataService.BaseStructure.GetByID(towerStructure.BaseStructureID);
            if (oldTower.BaseStructureTypeID != (int)BaseStructureType.ControlTower)
            {
                return "";
            }

            //--------------------------------------------------------------------------
            // Check that the PC has permission to manage structures. 
            // Check whether player has permission to place or edit structures.
            //--------------------------------------------------------------------------
            var canPlaceOrEditStructures = BasePermissionService.HasBasePermission(player, towerStructure.PCBaseID, BasePermission.CanPlaceEditStructures);

            // Don't have permission.
            if (!canPlaceOrEditStructures)
                return "You do not have permission to place or edit structures in this territory.";

            //--------------------------------------------------------------------------
            // Check that the current CPU and power usage of the base is not more than
            // the new tower can handle.
            //--------------------------------------------------------------------------
            var powerInUse = GetPowerInUse(towerStructure.PCBaseID);
            var cpuInUse = GetCPUInUse(towerStructure.PCBaseID);

            var towerPower = newTower.Power + (item.StructureBonus * 3);
            var towerCPU = newTower.CPU + (item.StructureBonus * 2);

            if (towerPower < powerInUse) return "The new tower does not have enough power to handle this base.";
            if (towerCPU < cpuInUse) return "The new tower does not have enough CPU to handle this base.";

            //--------------------------------------------------------------------------
            // Change the old tower for the new tower.
            // Create the old tower as as item on the PC. 
            // Update the tower model and bonus to the new values, and save the change.
            //--------------------------------------------------------------------------
            ConvertStructureToItem(towerStructure, user);
            towerStructure.BaseStructureID = newTowerStructureID;
            towerStructure.StructureBonus = item.StructureBonus;

            DataService.SubmitDataChange(towerStructure, DatabaseActionType.Update);

            //--------------------------------------------------------------------------
            // Actually create/destroy the NWN objects.
            //--------------------------------------------------------------------------
            var area = (GetAreaFromLocation(GetLocation(target)));
            SpawnStructure(area, towerStructure.ID);
            target.Destroy();
            item.Destroy();

            //--------------------------------------------------------------------------
            // Note - we may have more fuel or resources than the new tower allows.  
            // That's okay - means we can't add more but won't break anything.
            //--------------------------------------------------------------------------
            return "Control tower upgraded.";
        }

        public static uint GetAreaInstance(Guid instanceID, bool isBase)
        {
            var instance = OBJECT_INVALID;
            var varName = isBase ? "PC_BASE_ID" : "PC_BASE_STRUCTURE_ID";
            foreach (var area in NWModule.Get().Areas)
            {
                if (GetLocalString(area, varName) == instanceID.ToString())
                {
                    instance = area;
                    break;
                }
            }

            return instance;
        }

        public static uint CreateAreaInstance(NWPlayer player, Guid instanceID, bool isBase)
        {
            PCBase pcBase;
            BuildingStyle style;
            List<PCBaseStructure> furnitureStructures;
            var name = "";
            var type = 0;
            int mainLight1;
            int mainLight2;
            int sourceLight1;
            int sourceLight2;

            if (isBase)
            {
                pcBase = DataService.PCBase.GetByID(instanceID);
                furnitureStructures = DataService.PCBaseStructure.GetAllByPCBaseID(pcBase.ID).ToList();
                style = DataService.BuildingStyle.GetByID(Convert.ToInt32(pcBase.BuildingStyleID));
                type = (int)BuildingType.Apartment;
                name = pcBase.CustomName;

                if (string.IsNullOrWhiteSpace(name))
                {
                    var owner = DataService.Player.GetByID(pcBase.PlayerID);
                    name = owner.CharacterName + "'s Apartment";
                }

                mainLight1 = pcBase.TileMainLight1Color;
                mainLight2 = pcBase.TileMainLight2Color;
                sourceLight1 = pcBase.TileSourceLight1Color;
                sourceLight2 = pcBase.TileSourceLight2Color;
            }
            else
            {
                var structure = DataService.PCBaseStructure.GetByID(instanceID);
                pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
                furnitureStructures = DataService.PCBaseStructure.GetAllByParentPCBaseStructureID(structure.ID).ToList();
                style = DataService.BuildingStyle.GetByID(Convert.ToInt32(structure.InteriorStyleID));
                name = structure.CustomName;

                var starship = pcBase.PCBaseTypeID == 3;
                type = starship ? (int)BuildingType.Starship : (int)BuildingType.Interior;

                if (string.IsNullOrWhiteSpace(name))
                {
                    var owner = PlayerService.GetPlayerEntity(pcBase.PlayerID);
                    name = owner.CharacterName + (starship ? "'s Starship" : "'s Building");
                }

                mainLight1 = structure.TileMainLight1Color;
                mainLight2 = structure.TileMainLight2Color;
                sourceLight1 = structure.TileSourceLight1Color;
                sourceLight2 = structure.TileSourceLight2Color;
            }

            // Create the area instance, assign the building type, and then assign local variables to the exit placeable for later use.
            var instance = AreaService.CreateAreaInstance(player, style.Resref, name, "PLAYER_HOME_ENTRANCE");
            SetLocalInt(instance, "BUILDING_TYPE", type);

            // Store the base ID or the structure ID as a local variable.
            if (isBase) SetLocalString(instance, "PC_BASE_ID", instanceID.ToString());
            else SetLocalString(instance, "PC_BASE_STRUCTURE_ID", instanceID.ToString());

            // Spawn the furniture.
            foreach (var furniture in furnitureStructures)
            {
                SpawnStructure(instance, furniture.ID);
            }

            // Set lighting if changed
            var width = GetAreaSize(Dimension.Width, instance);
            var height = GetAreaSize(Dimension.Height, instance);
            Vector3 vPos;
            vPos.X = 0.0f;
            vPos.Y = 0.0f;
            vPos.Z = 0.0f;
            for (var i = 0; i <= height; i++)
            {
                vPos.X = (float)i;
                for (var j = 0; j <= width; j++)
                {
                    vPos.Y = (float)j;

                    var location = Location(instance, vPos, 0.0f);

                    //Console.WriteLine("Setting Tile Color: X = " + vPos.X + " Y = " + vPos.Y);
                    if (mainLight1 >= 0)
                    {
                        SetTileMainLightColor(location, mainLight1, GetTileMainLight2Color(location));
                    }
                    if (mainLight2 >= 0)
                    {
                        SetTileMainLightColor(location, GetTileMainLight1Color(location), mainLight2);
                    }
                    if (sourceLight1 >= 0)
                    {
                        SetTileSourceLightColor(location, sourceLight1, GetTileSourceLight2Color(location));
                    }
                    if (sourceLight2 >= 0)
                    {
                        SetTileSourceLightColor(location, GetTileSourceLight1Color(location), sourceLight2);
                    }
                }
            }
            RecomputeStaticLighting(instance);

            LoggingService.Trace(TraceComponent.Space, "Created instance with ID " + instanceID.ToString() + ", name " + GetName(instance));

            return instance;
        }

        public static void OnAreaEnter()
        {
            //--------------------------------------------------------------------------------
            // Code moved from the PlayerService once respawning in bases was implemented, as
            // it created a circular dependency between the two libraries.
            //--------------------------------------------------------------------------------
            NWPlayer player = (GetEnteringObject());
            if (!player.IsPlayer) return;

            var area = GetArea(player);
            var areaTag = GetTag(area);
            var areaName = GetName(area);
            if (areaTag == "ooc_area" || (areaName.StartsWith("Space - ") && player.GetLocalInt("IS_SHIP") == 0))
            {
                var entity = PlayerService.GetPlayerEntity(player.GlobalID);

                //--------------------------------------------------------------------------
                // Check for instances.
                //--------------------------------------------------------------------------
                var locationInstanceID = entity.LocationInstanceID;
                if (locationInstanceID != null)
                {
                    LoggingService.Trace(TraceComponent.None, "Player logging in to an instance, ID " + locationInstanceID.ToString());

                    //--------------------------------------------------------------------------
                    // Find out whether this instance is an area, a base, or neither.
                    //--------------------------------------------------------------------------
                    if (DataService.PCBase.GetByIDOrDefault((Guid)locationInstanceID) != null)
                    {
                        //--------------------------------------------------------------------------
                        // This is a base (i.e. apartment).
                        //--------------------------------------------------------------------------
                        LoggingService.Trace(TraceComponent.None, "Player logging in to an apartment.");

                        area = GetAreaInstance((Guid)locationInstanceID, true);
                        if (!GetIsObjectValid(area)) area = CreateAreaInstance(player, (Guid)locationInstanceID, true);
                    }
                    else if (DataService.PCBaseStructure.GetByIDOrDefault((Guid)locationInstanceID) != null)
                    {
                        //--------------------------------------------------------------------------
                        // Not a base - building or starship.
                        //--------------------------------------------------------------------------
                        LoggingService.Trace(TraceComponent.None, "Player logging in to a building or starship.");
                        area = GetAreaInstance((Guid)locationInstanceID, false);
                        if (!GetIsObjectValid(area)) area = CreateAreaInstance(player, (Guid)locationInstanceID, false);
                    }
                    else
                    {
                        //--------------------------------------------------------------------------
                        // ID specified, but not present
                        // This probably means we were on a destroyed starship.  Do respawn. 
                        //--------------------------------------------------------------------------
                        player.SendMessage("The ship you were on was destroyed.");
                        ExecuteScript("OnModuleRespawn", player);
                    }
                }

                if (!GetIsObjectValid(area)) area = NWModule.Get().Areas.SingleOrDefault(x => GetResRef(x) == entity.LocationAreaResref);
                if (!GetIsObjectValid(area)) return;

                var position = Vector3((float)entity.LocationX, (float)entity.LocationY, (float)entity.LocationZ);
                var location = Location(area,
                    position,
                    (float)entity.LocationOrientation);

                player.AssignCommand(() => ActionJumpToLocation(location));
            }
        }
    }

}
