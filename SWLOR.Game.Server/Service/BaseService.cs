using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using static NWN.NWScript;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class BaseService : IBaseService
    {
        private readonly INWScript _;
        private readonly INWNXEvents _nwnxEvents;
        private readonly IDialogService _dialog;
        private readonly IDataContext _db;
        private readonly IPlayerService _player;
        private readonly IImpoundService _impound;
        private readonly IBasePermissionService _perm;
        private readonly INWNXChat _nwnxChat;

        public BaseService(INWScript script,
            INWNXEvents nwnxEvents,
            IDialogService dialog,
            IDataContext db,
            IPlayerService player,
            IImpoundService impound,
            IBasePermissionService perm,
            INWNXChat nwnxChat)
        {
            _ = script;
            _nwnxEvents = nwnxEvents;
            _dialog = dialog;
            _db = db;
            _player = player;
            _impound = impound;
            _perm = perm;
            _nwnxChat = nwnxChat;
        }

        public PCTempBaseData GetPlayerTempData(NWPlayer player)
        {
            if (!player.Data.ContainsKey("BASE_SERVICE_DATA"))
            {
                player.Data["BASE_SERVICE_DATA"] = new PCTempBaseData();
            }

            return player.Data["BASE_SERVICE_DATA"];
        }

        public void ClearPlayerTempData(NWPlayer player)
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

        public void OnModuleUseFeat()
        {
            NWPlayer player = NWPlayer.Wrap(Object.OBJECT_SELF);
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();
            Location targetLocation = _nwnxEvents.OnFeatUsed_GetTargetLocation();
            NWArea targetArea = NWArea.Wrap(_.GetAreaFromLocation(targetLocation));

            if (featID != (int)CustomFeatType.BaseManagementTool) return;

            var data = GetPlayerTempData(player);
            data.TargetArea = targetArea;
            data.TargetLocation = targetLocation;
            data.TargetObject = _nwnxEvents.OnItemUsed_GetTarget();

            player.ClearAllActions();
            _dialog.StartConversation(player, player, "BaseManagementTool");
        }

        public void OnModuleLoad()
        {
            foreach (var area in NWModule.Get().Areas)
            {
                if (!area.Data.ContainsKey("BASE_SERVICE_STRUCTURES"))
                {
                    area.Data["BASE_SERVICE_STRUCTURES"] = new List<AreaStructure>();
                }

                var pcBases = _db.PCBases.Where(x => x.AreaResref == area.Resref).ToList();
                foreach (var @base in pcBases)
                {
                    foreach (var structure in @base.PCBaseStructures)
                    {
                        if (structure.ParentPCBaseStructureID != null) continue; // Don't spawn any structures contained by buildings.
                        SpawnStructure(area, structure.PCBaseStructureID);
                    }
                }

            }
        }

        public NWPlaceable SpawnStructure(NWArea area, int pcBaseStructureID)
        {
            PCBaseStructure structure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == pcBaseStructureID);

            Location location = _.Location(area.Object,
                _.Vector((float)structure.LocationX, (float)structure.LocationY, (float)structure.LocationZ),
                (float)structure.LocationOrientation);

            BaseStructureType structureType = (BaseStructureType)structure.BaseStructure.BaseStructureTypeID;
            string resref = structure.BaseStructure.PlaceableResref;

            List<AreaStructure> areaStructures = area.Data["BASE_SERVICE_STRUCTURES"];
            if (string.IsNullOrWhiteSpace(resref) &&
                structureType == BaseStructureType.Building)
            {
                resref = structure.ExteriorStyle.Resref;
            }

            var plc = NWPlaceable.Wrap(_.CreateObject(OBJECT_TYPE_PLACEABLE, resref, location));
            plc.SetLocalInt("PC_BASE_STRUCTURE_ID", structure.PCBaseStructureID);

            NWPlaceable door = null;
            if (structureType == BaseStructureType.Building)
            {
                door = SpawnBuildingDoor(structure.ExteriorStyle.DoorSpawnProcedure, plc);
                areaStructures.Add(new AreaStructure(structure.PCBaseID, structure.PCBaseStructureID, door, false, null));
            }
            areaStructures.Add(new AreaStructure(structure.PCBaseID, structure.PCBaseStructureID, plc, true, door));

            return plc;
        }

        public NWPlaceable SpawnBuildingDoor(int doorSpawnProcedure, NWPlaceable building, Location locationOverride = null)
        {
            NWArea area = building.Area;
            Location location = locationOverride ?? building.Location;
            Vector doorPosition = _.GetPositionFromLocation(location);
            float fOrient = _.GetFacingFromLocation(location);
            int pcBaseStructureID = building.GetLocalInt("PC_BASE_STRUCTURE_ID");
            Location doorLocation = null;

            switch (doorSpawnProcedure)
            {
                case 1: // House, 2 Story
                    fOrient = fOrient + 146.31f;
                    if (fOrient > 360.0) fOrient = fOrient - 360.0f;

                    float fMod = _.sqrt(13.0f) * _.sin(fOrient);
                    doorPosition.m_X = doorPosition.m_X + fMod;

                    fMod = _.sqrt(13.0f) * _.cos(fOrient);
                    doorPosition.m_Y = doorPosition.m_Y - fMod;
                    doorLocation = _.Location(area.Object, doorPosition, _.GetFacingFromLocation(location));
                    break;
            }

            if (doorLocation == null)
            {
                throw new Exception("Unable to find Door Spawn Procedure #" + doorSpawnProcedure + ". Door was not spawned.");
            }
           
            var door = NWPlaceable.Wrap(_.CreateObject(OBJECT_TYPE_PLACEABLE, "building_door", doorLocation));
            door.SetLocalInt("PC_BASE_STRUCTURE_ID", pcBaseStructureID);
            door.SetLocalInt("IS_DOOR", TRUE);

            return door;
        }

        public void PurchaseArea(NWPlayer player, NWArea area, string sector)
        {
            if (sector != AreaSector.Northwest && sector != AreaSector.Northeast &&
                sector != AreaSector.Southwest && sector != AreaSector.Southeast)
                throw new ArgumentException(nameof(sector) + " must match one of the valid sector values: NE, NW, SE, SW");

            if (area.Width < 32) throw new Exception("Area must be at least 32 tiles wide.");
            if (area.Height < 32) throw new Exception("Area must be at least 32 tiles high.");


            var dbArea = _db.Areas.Single(x => x.Resref == area.Resref);
            string existingOwner = string.Empty;
            switch (sector)
            {
                case AreaSector.Northwest: existingOwner = dbArea.NorthwestOwner; break;
                case AreaSector.Northeast: existingOwner = dbArea.NortheastOwner; break;
                case AreaSector.Southwest: existingOwner = dbArea.SouthwestOwner; break;
                case AreaSector.Southeast: existingOwner = dbArea.SoutheastOwner; break;
            }

            if (!string.IsNullOrWhiteSpace(existingOwner))
            {
                player.SendMessage("Another player already owns that sector.");
                return;
            }

            if (player.Gold < dbArea.PurchasePrice)
            {
                player.SendMessage("You do not have enough credits to purchase that sector.");
                return;
            }

            player.AssignCommand(() => _.TakeGoldFromCreature(dbArea.PurchasePrice, player.Object, TRUE));

            switch (sector)
            {
                case AreaSector.Northwest: dbArea.NorthwestOwner = player.GlobalID; break;
                case AreaSector.Northeast: dbArea.NortheastOwner = player.GlobalID; break;
                case AreaSector.Southwest: dbArea.SouthwestOwner = player.GlobalID; break;
                case AreaSector.Southeast: dbArea.SoutheastOwner = player.GlobalID; break;
            }

            PCBase pcBase = new PCBase
            {
                AreaResref = dbArea.Resref,
                PlayerID = player.GlobalID,
                DateInitialPurchase = DateTime.UtcNow,
                DateRentDue = DateTime.UtcNow.AddDays(7),
                Sector = sector
            };
            _db.PCBases.Add(pcBase);

            PCBasePermission permission = new PCBasePermission
            {
                PCBase = pcBase,
                PlayerID = player.GlobalID
            };
            _db.PCBasePermissions.Add(permission);
            _db.SaveChanges();

            // Grant all base permissions to owner.
            var allPermissions = Enum.GetValues(typeof(BasePermission)).Cast<BasePermission>().ToArray();
            _perm.GrantBasePermissions(player, pcBase.PCBaseID, allPermissions);

            player.FloatingText("You purchase " + area.Name + " (" + sector + ") for " + dbArea.PurchasePrice + " credits.");
        }

        public void OnModuleHeartbeat()
        {
            NWModule module = NWModule.Get();
            int ticks = module.GetLocalInt("BASE_SERVICE_TICKS") + 1;


            if (ticks >= 10)
            {
                List<Tuple<string, string>> playerIDs = new List<Tuple<string, string>>();
                var pcBases = _db.PCBases.Where(x => x.DateRentDue <= DateTime.UtcNow).ToList();
                
                foreach (var pcBase in pcBases)
                {
                    Area dbArea = _db.Areas.Single(x => x.Resref == pcBase.AreaResref);
                    playerIDs.Add(new Tuple<string, string>(pcBase.PlayerID, dbArea.Name + " (" + pcBase.Sector + ")"));
                    ClearPCBaseByID(pcBase.PCBaseID, false);
                }

                var players = module.Players;
                foreach(var removed in playerIDs)
                {
                    var existing = players.FirstOrDefault(x => x.GlobalID == removed.Item1);
                    existing?.FloatingText("Your lease on " + removed.Item2 + " has expired. All structures and items have been impounded by the planetary government. Speak with them to pay a fee and retrieve your goods.");
                }

                _db.SaveChanges();
                ticks = 0;
            }

            module.SetLocalInt("BASE_SERVICE_TICKS", ticks);
        }

        public PCBaseStructure GetBaseControlTower(int pcBaseID)
        {
            var pcBase = _db.PCBases.Single(x => x.PCBaseID == pcBaseID);
            return pcBase.PCBaseStructures.SingleOrDefault(x =>
                x.BaseStructure.BaseStructureTypeID == (int) BaseStructureType.ControlTower);
        }

        public double GetPowerInUse(int pcBaseID)
        {
            int controlTowerID = (int)BaseStructureType.ControlTower;
            return _db.PCBaseStructures
                .Where(x => x.PCBaseID == pcBaseID && x.BaseStructure.BaseStructureTypeID != controlTowerID)
                .DefaultIfEmpty()
                .Sum(s => s == null || s.BaseStructure == null ? 0 : s.BaseStructure.Power);
        }

        public double GetCPUInUse(int pcBaseID)
        {
            int controlTowerID = (int)BaseStructureType.ControlTower;
            return _db.PCBaseStructures
                .Where(x => x.PCBaseID == pcBaseID && x != null && x.BaseStructure != null && x.BaseStructure.BaseStructureTypeID != controlTowerID)
                .DefaultIfEmpty()
                .Sum(s => s == null || s.BaseStructure == null ? 0 : s.BaseStructure.CPU);
        }

        public string GetSectorOfLocation(Location targetLocation)
        {
            int cellX = (int)(_.GetPositionFromLocation(targetLocation).m_X / 10);
            int cellY = (int)(_.GetPositionFromLocation(targetLocation).m_Y / 10);

            string sector = "INVALID";
            // NWN location positions start at the bottom left, not the top left.
            if (cellX >= 0 && cellX <= 15 &&
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

        public string CanPlaceStructure(NWCreature user, NWItem structureItem, Location targetLocation, int structureID)
        {
            NWPlayer player = NWPlayer.Wrap(user.Object);
            string sector = GetSectorOfLocation(targetLocation);
            if (sector == "INVALID")
            {
                return "Invalid location selected.";
            }

            if (structureItem == null || !structureItem.IsValid || !Equals(structureItem.Possessor, user))
            {
                return "Unable to locate structure item.";
            }

            NWArea area = NWArea.Wrap(_.GetAreaFromLocation(targetLocation));
            int buildingStructureID = area.GetLocalInt("PC_BASE_STRUCTURE_ID");
            bool isOutside = buildingStructureID <= 0;
            Area dbArea = _db.Areas.SingleOrDefault(x => x.Resref == area.Resref);

            if (dbArea == null || !dbArea.IsBuildable ) return "Structures cannot be placed in this area.";
            PCBase pcBase = _db.PCBases.SingleOrDefault(x => x.AreaResref == area.Resref && x.Sector == sector);
            if (pcBase == null && !isOutside)
            {
                var parentStructure = _db.PCBaseStructures.Single(x => x.PCBaseStructureID == buildingStructureID);
                pcBase = parentStructure.PCBase;

                int buildingStructureCount = _db.PCBaseStructures.Count(x => x.ParentPCBaseStructureID == parentStructure.PCBaseStructureID) + 1;
                if(buildingStructureCount > parentStructure.BaseStructure.Storage)
                {
                    return "No more structures can be placed inside this building.";
                }

            }

            if (pcBase == null)
                return "This area is unclaimed but not owned by you. You may purchase a lease on it from the planetary government by using your Base Management Tool (found under feats).";

            var canPlaceOrEditStructures = isOutside ? 
                _perm.HasBasePermission(player, pcBase.PCBaseID, BasePermission.CanPlaceEditStructures) :                 // Bases
                _perm.HasStructurePermission(player, buildingStructureID, StructurePermission.CanPlaceEditStructures);    // Buildings

            if (!canPlaceOrEditStructures)
                return "You do not have permission to place or edit structures in this territory.";
            
            var structure = _db.BaseStructures.Single(x => x.BaseStructureID == structureID);
            BaseStructureType structureType = (BaseStructureType)structure.BaseStructureTypeID;
            BaseStructureType[] allowedOutside =
            {
                BaseStructureType.ControlTower,
                BaseStructureType.Drill,
                BaseStructureType.Silo,
                BaseStructureType.Turret,
                BaseStructureType.Building,
                BaseStructureType.MassProduction,
                BaseStructureType.StarshipProduction
            };

            BaseStructureType[] allowedInside =
            {
                BaseStructureType.Furniture
            };
            
            if (!allowedOutside.Contains(structureType) && isOutside)
            {
                return "That structure can only be placed inside buildings.";
            }

            if (!allowedInside.Contains(structureType) && !isOutside)
            {
                return "That structure can only be placed outside of buildings.";
            }

            if (isOutside)
            {
                bool hasControlTower = pcBase.PCBaseStructures
                                           .SingleOrDefault(x => x.BaseStructure.BaseStructureTypeID == (int)BaseStructureType.ControlTower) != null;

                if (!hasControlTower && structureType != BaseStructureType.ControlTower)
                {
                    return "A control tower must be placed down in the sector first.";
                }

                if (hasControlTower && structureType == BaseStructureType.ControlTower)
                {
                    return "Only one control tower can be placed down per sector.";
                }

            }
            
            return null;
        }

        public NWItem ConvertStructureToItem(PCBaseStructure pcBaseStructure, NWObject target)
        {
            NWItem item = NWItem.Wrap(_.CreateItemOnObject(pcBaseStructure.BaseStructure.ItemResref, target.Object));
            item.SetLocalInt("BASE_STRUCTURE_ID", pcBaseStructure.BaseStructureID);
            item.Name = pcBaseStructure.BaseStructure.Name;
            item.MaxDurability = (float)pcBaseStructure.Durability;
            item.Durability = (float)pcBaseStructure.Durability;
            item.StructureBonus = pcBaseStructure.StructureBonus;

            if (pcBaseStructure.InteriorStyleID != null && pcBaseStructure.ExteriorStyleID != null)
            {
                item.SetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID", (int)pcBaseStructure.InteriorStyleID);
                item.SetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID", (int)pcBaseStructure.ExteriorStyleID);
                item.SetLocalInt("STRUCTURE_BUILDING_INITIALIZED", TRUE);
            }

            return item;
        }
        


        public void ClearPCBaseByID(int pcBaseID, bool doSave = true)
        {
            var pcBase = _db.PCBases.Single(x => x.PCBaseID == pcBaseID);
            var areas = NWModule.Get().Areas;
            var baseArea = areas.Single(x => x.Resref == pcBase.AreaResref);
            List<AreaStructure> areaStructures = baseArea.Data["BASE_SERVICE_STRUCTURES"];
            areaStructures = areaStructures.Where(x => x.PCBaseID == pcBaseID).ToList();
            
            foreach (var structure in areaStructures)
            {
                var instance = areas.SingleOrDefault(x => x.IsInstance && x.GetLocalInt("PC_BASE_STRUCTURE_ID") == structure.PCBaseStructureID);
                if (instance != null)
                {
                    foreach (var player in NWModule.Get().Players)
                    {
                        if (Equals(player.Area, instance))
                        {
                            DoPlayerExitBuildingInstance(player);
                        }
                    }
                }

                ((List<AreaStructure>) baseArea.Data["BASE_SERVICE_STRUCTURES"]).Remove(structure);
                structure.Structure.Destroy();
            }

            for (int x = pcBase.PCBaseStructures.Count-1; x >= 0; x--)
            {
                // Impound item storage
                var pcBaseStructure = pcBase.PCBaseStructures.ElementAt(x);
                for (int i = pcBaseStructure.PCBaseStructureItems.Count - 1; i >= 0; i--)
                {
                    var item = pcBaseStructure.PCBaseStructureItems.ElementAt(x);
                    _impound.Impound(item);
                    _db.PCBaseStructureItems.Remove(item);
                }

                // Clear structure permissions
                for (int p = pcBaseStructure.PCBaseStructurePermissions.Count - 1; p >= 0; p--)
                {
                    var permission = pcBaseStructure.PCBaseStructurePermissions.ElementAt(p);
                    _db.PCBaseStructurePermissions.Remove(permission);
                }

                var tempStorage = NWPlaceable.Wrap(_.GetObjectByTag("TEMP_ITEM_STORAGE"));
                NWItem copy = ConvertStructureToItem(pcBaseStructure, tempStorage);
                _impound.Impound(pcBase.PlayerID, copy);
                copy.Destroy();

                _db.PCBaseStructures.Remove(pcBaseStructure);
            }

            // Clear base permissions
            for (int p = pcBase.PCBasePermissions.Count - 1; p >= 0; p--)
            {
                var permission = pcBase.PCBasePermissions.ElementAt(p);
                _db.PCBasePermissions.Remove(permission);
            }

            _db.PCBases.Remove(pcBase);
            
            Area dbArea = _db.Areas.Single(x => x.Resref == pcBase.AreaResref);
            if (pcBase.Sector == AreaSector.Northeast) dbArea.NortheastOwner = null;
            else if (pcBase.Sector == AreaSector.Northwest) dbArea.NorthwestOwner = null;
            else if (pcBase.Sector == AreaSector.Southeast) dbArea.SoutheastOwner = null;
            else if (pcBase.Sector == AreaSector.Southwest) dbArea.SouthwestOwner = null;
            
            if (doSave)
            {
                _db.SaveChanges();
            }
        }

        public void ApplyCraftedItemLocalVariables(NWItem item, BaseStructure structure)
        {
            // Structure items need an additional local variable and their name set on creation.
            if (structure != null)
            {
                item.SetLocalInt("BASE_STRUCTURE_ID", structure.BaseStructureID);
                item.Name = structure.Name;

                if (structure.BaseStructureTypeID == (int)BaseStructureType.Building)
                {
                    var defaultInterior = _db.BuildingStyles.Single(x => x.BaseStructureID == structure.BaseStructureID && x.IsDefault && x.IsInterior).BuildingStyleID;
                    var defaultExterior = _db.BuildingStyles.Single(x => x.BaseStructureID == structure.BaseStructureID && x.IsDefault && !x.IsInterior).BuildingStyleID;

                    item.SetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID", defaultInterior);
                    item.SetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID", defaultExterior);
                }

            }
        }

        public string GetPlayerIDOwnerOfSector(Area dbArea, string sector)
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


        public void JumpPCToBuildingInterior(NWPlayer player, NWArea area)
        {
            NWObject waypoint = null;
            NWObject exit = null;

            NWObject @object = NWObject.Wrap(_.GetFirstObjectInArea(area.Object));
            while (@object.IsValid)
            {
                if (@object.Tag == "PLAYER_HOME_ENTRANCE")
                {
                    waypoint = @object;
                }
                else if (@object.Tag == "building_exit")
                {
                    exit = @object;
                }

                @object = NWObject.Wrap(_.GetNextObjectInArea(area.Object));
            }

            if (waypoint == null)
            {
                player.FloatingText("ERROR: Couldn't find the building interior's entrance. Inform an admin of this issue.");
                return;
            }

            if (exit == null)
            {
                player.FloatingText("ERROR: Couldn't find the building interior's exit. Inform an admin of this issue.");
                return;
            }

            _player.SaveLocation(player);

            exit.SetLocalLocation("PLAYER_HOME_EXIT_LOCATION", player.Location);
            exit.SetLocalInt("IS_BUILDING_DOOR", 1);

            Location location = waypoint.Location;
            player.AssignCommand(() =>
            {
                _.ActionJumpToLocation(location);
            });
        }

        public void DoPlayerExitBuildingInstance(NWPlayer player, NWPlaceable door = null)
        {
            NWArea area = player.Area;
            if (!area.IsInstance) return;

            if (door == null)
            {
                NWObject obj = NWObject.Wrap(_.GetFirstObjectInArea(area.Object));
                while (obj.IsValid)
                {
                    if (obj.Tag == "building_exit")
                    {
                        door = NWPlaceable.Wrap(obj.Object);
                        break;
                    }
                    obj = NWObject.Wrap(_.GetNextObjectInArea(area.Object));
                }
            }

            if (door == null)
            {
                return;
            }

            Location location = door.GetLocalLocation("PLAYER_HOME_EXIT_LOCATION");
            player.AssignCommand(() => _.ActionJumpToLocation(location));

            player.DelayCommand(() =>
            {
                player = NWPlayer.Wrap(_.GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, area)) return;
                    player = NWPlayer.Wrap(_.GetNextPC());
                }

                _.DestroyArea(area.Object);
            }, 1.0f);
        }

        public void OnModuleNWNXChat(NWPlayer sender)
        {
            if (sender.GetLocalInt("LISTENING_FOR_NEW_CONTAINER_NAME") != 1) return;
            if (!sender.IsPlayer && !sender.IsDM) return;

            _nwnxChat.SkipMessage();
            string text = _nwnxChat.GetMessage().Trim();
            if (text.Length > 32)
            {
                sender.FloatingText("Container names must be 32 characters or less.");
                return;
            }

            sender.SetLocalString("NEW_CONTAINER_NAME", text);
            sender.SendMessage("New container name received. Please press the 'Next' button in the conversation window.");
        }

        public int CalculateMaxShieldHP(PCBaseStructure controlTower)
        {
            return (int)(controlTower.Durability * 300);
        }
    }
}
