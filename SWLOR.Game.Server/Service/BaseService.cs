using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.DoorRule.Contracts;
using static NWN.NWScript;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class BaseService : IBaseService
    {
        private readonly INWScript _;
        private readonly INWNXEvents _nwnxEvents;
        private readonly IDialogService _dialog;
        private readonly IDataService _data;
        private readonly IPlayerService _player;
        private readonly IImpoundService _impound;
        private readonly IBasePermissionService _perm;
        private readonly INWNXChat _nwnxChat;
        private readonly IDurabilityService _durability;
        private readonly IAreaService _area;

        public BaseService(
            INWScript script,
            INWNXEvents nwnxEvents,
            IDialogService dialog,
            IDataService data,
            IPlayerService player,
            IImpoundService impound,
            IBasePermissionService perm,
            INWNXChat nwnxChat,
            IDurabilityService durability,
            IAreaService area)
        {
            _ = script;
            _nwnxEvents = nwnxEvents;
            _dialog = dialog;
            _data = data;
            _player = player;
            _impound = impound;
            _perm = perm;
            _nwnxChat = nwnxChat;
            _durability = durability;
            _area = area;
            _data = data;
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
            NWPlayer player = (Object.OBJECT_SELF);
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();
            NWLocation targetLocation = _nwnxEvents.OnFeatUsed_GetTargetLocation();
            NWArea targetArea = (_.GetAreaFromLocation(targetLocation));

            if (featID != (int)CustomFeatType.StructureManagementTool) return;

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

                var pcBases = _data.Where<PCBase>(x => x.AreaResref == area.Resref && x.ApartmentBuildingID == null).ToList();
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
            PCBaseStructure pcStructure = _data.Get<PCBaseStructure>(pcBaseStructureID);
            PCBase pcBase = _data.Get<PCBase>(pcStructure.PCBaseID);
            BaseStructure baseStructure = _data.Get<BaseStructure>(pcStructure.BaseStructureID);
            var exteriorStyle = _data.Get<BuildingStyle>(pcStructure.ExteriorStyleID);

            NWLocation location = _.Location(area.Object,
                _.Vector((float)pcStructure.LocationX, (float)pcStructure.LocationY, (float)pcStructure.LocationZ),
                (float)pcStructure.LocationOrientation);

            BaseStructureType structureType = (BaseStructureType)baseStructure.BaseStructureTypeID;
            string resref = baseStructure.PlaceableResref;

            List<AreaStructure> areaStructures = area.Data["BASE_SERVICE_STRUCTURES"];
            if (string.IsNullOrWhiteSpace(resref) &&
                structureType == BaseStructureType.Building)
            {
                resref = exteriorStyle.Resref;
            }
            
            NWPlaceable plc = (_.CreateObject(OBJECT_TYPE_PLACEABLE, resref, location));
            plc.SetLocalInt("PC_BASE_STRUCTURE_ID", pcStructure.PCBaseStructureID);
            plc.SetLocalInt("REQUIRES_BASE_POWER", baseStructure.RequiresBasePower ? 1 : 0);
            plc.SetLocalString("ORIGINAL_SCRIPT_CLOSED", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_CLOSED));
            plc.SetLocalString("ORIGINAL_SCRIPT_DAMAGED", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_DAMAGED));
            plc.SetLocalString("ORIGINAL_SCRIPT_DEATH", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_DEATH));
            plc.SetLocalString("ORIGINAL_SCRIPT_HEARTBEAT", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_HEARTBEAT));
            plc.SetLocalString("ORIGINAL_SCRIPT_INVENTORYDISTURBED", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED));
            plc.SetLocalString("ORIGINAL_SCRIPT_LOCK", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_LOCK));
            plc.SetLocalString("ORIGINAL_SCRIPT_MELEEATTACKED", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_MELEEATTACKED));
            plc.SetLocalString("ORIGINAL_SCRIPT_OPEN", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_OPEN));
            plc.SetLocalString("ORIGINAL_SCRIPT_SPELLCASTAT", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_SPELLCASTAT));
            plc.SetLocalString("ORIGINAL_SCRIPT_UNLOCK", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_UNLOCK));
            plc.SetLocalString("ORIGINAL_SCRIPT_USED", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_USED));
            plc.SetLocalString("ORIGINAL_SCRIPT_USER_DEFINED_EVENT", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_USER_DEFINED_EVENT));
            plc.SetLocalString("ORIGINAL_SCRIPT_LEFT_CLICK", _.GetEventScript(plc.Object, EVENT_SCRIPT_PLACEABLE_ON_LEFT_CLICK));
            plc.SetLocalString("ORIGINAL_JAVA_SCRIPT_1", _.GetLocalString(plc.Object, "JAVA_SCRIPT_1"));

            NWPlaceable door = null;
            if (structureType == BaseStructureType.Building)
            {
                door = SpawnBuildingDoor(exteriorStyle.DoorRule, plc);
                areaStructures.Add(new AreaStructure(pcStructure.PCBaseID, pcStructure.PCBaseStructureID, door, false, null));
            }
            areaStructures.Add(new AreaStructure(pcStructure.PCBaseID, pcStructure.PCBaseStructureID, plc, true, door));

            if (area.IsInstance && area.GetLocalInt("PC_BASE_STRUCTURE_ID") > 0)
            {
                if (DateTime.UtcNow > pcBase.DateFuelEnds && pcBase.Fuel <= 0)
                {
                    ToggleInstanceObjectPower(area, false);
                }
            }

            return plc;
        }

        public void ToggleInstanceObjectPower(NWArea area, bool isPoweredOn)
        {
            List<AreaStructure> areaStructures = area.Data["BASE_SERVICE_STRUCTURES"];

            foreach (var record in areaStructures)
            {
                var structure = record.Structure;
                if (structure.GetLocalInt("REQUIRES_BASE_POWER") == TRUE)
                {
                    if (isPoweredOn)
                    {
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_CLOSED, structure.GetLocalString("ORIGINAL_SCRIPT_CLOSED"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_DAMAGED, structure.GetLocalString("ORIGINAL_SCRIPT_DAMAGED"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_DEATH, structure.GetLocalString("ORIGINAL_SCRIPT_DEATH"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_HEARTBEAT, structure.GetLocalString("ORIGINAL_SCRIPT_HEARTBEAT"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, structure.GetLocalString("ORIGINAL_SCRIPT_INVENTORYDISTURBED"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_LOCK, structure.GetLocalString("ORIGINAL_SCRIPT_LOCK"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_MELEEATTACKED, structure.GetLocalString("ORIGINAL_SCRIPT_MELEEATTACKED"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_OPEN, structure.GetLocalString("ORIGINAL_SCRIPT_OPEN"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_SPELLCASTAT, structure.GetLocalString("ORIGINAL_SCRIPT_SPELLCASTAT"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_UNLOCK, structure.GetLocalString("ORIGINAL_SCRIPT_UNLOCK"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_USED, structure.GetLocalString("ORIGINAL_SCRIPT_USED"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_USER_DEFINED_EVENT, structure.GetLocalString("ORIGINAL_SCRIPT_USER_DEFINED_EVENT"));
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_LEFT_CLICK, structure.GetLocalString("ORIGINAL_SCRIPT_LEFT_CLICK"));
                        structure.SetLocalString("JAVA_SCRIPT_1", structure.GetLocalString("ORIGINAL_JAVA_SCRIPT_1"));
                        structure.IsLocked = false;
                    }
                    else
                    {
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_CLOSED, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_DAMAGED, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_DEATH, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_HEARTBEAT, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_INVENTORYDISTURBED, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_LOCK, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_MELEEATTACKED, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_OPEN, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_SPELLCASTAT, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_UNLOCK, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_USED, "jvm_script_1");
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_USER_DEFINED_EVENT, string.Empty);
                        _.SetEventScript(structure.Object, EVENT_SCRIPT_PLACEABLE_ON_LEFT_CLICK, string.Empty);
                        structure.SetLocalString("JAVA_SCRIPT_1", "Placeable.DisabledStructure.OnUsed");
                        structure.IsLocked = true;
                    }
                }
            }

        }

        public NWPlaceable SpawnBuildingDoor(string spawnRule, NWPlaceable building, NWLocation locationOverride = null)
        {
            NWArea area = building.Area;
            NWLocation location = locationOverride ?? building.Location;
            
            int pcBaseStructureID = building.GetLocalInt("PC_BASE_STRUCTURE_ID");
            NWPlaceable door = App.ResolveByInterface<IDoorRule, NWPlaceable>("DoorRule." + spawnRule, rule => rule.Run(area, location));
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


            var dbArea = _data.Single<Area>(x => x.Resref == area.Resref);
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
                Sector = sector,
                PCBaseTypeID = (int)Enumeration.PCBaseType.RegularBase,
                CustomName = string.Empty
            };
            _data.SubmitDataChange(pcBase, DatabaseActionType.Insert);
            
            // TODO: Add PCBaseID (won't be auto-gened in new code)

            PCBasePermission permission = new PCBasePermission
            {
                PCBaseID = pcBase.PCBaseID,
                PlayerID = player.GlobalID
            };
            _data.SubmitDataChange(permission, DatabaseActionType.Insert);
            
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
                var pcBases = _data.Where<PCBase>(x => x.DateRentDue <= DateTime.UtcNow).ToList();
                
                foreach (var pcBase in pcBases)
                { 
                    Area dbArea = _data.Single<Area>(x => x.Resref == pcBase.AreaResref);
                    playerIDs.Add(new Tuple<string, string>(pcBase.PlayerID, dbArea.Name + " (" + pcBase.Sector + ")"));
                    ClearPCBaseByID(pcBase.PCBaseID);
                }

                var players = module.Players.ToList();
                foreach(var removed in playerIDs)
                {
                    var existing = players.FirstOrDefault(x => x.GlobalID == removed.Item1);
                    existing?.FloatingText("Your lease on " + removed.Item2 + " has expired. All structures and items have been impounded by the planetary government. Speak with them to pay a fee and retrieve your goods.");
                }

                ticks = 0;
            }

            module.SetLocalInt("BASE_SERVICE_TICKS", ticks);
        }

        public PCBaseStructure GetBaseControlTower(int pcBaseID)
        {
            var pcBase = _data.Single<PCBase>(x => x.PCBaseID == pcBaseID);
            

            return pcBase.PCBaseStructures.SingleOrDefault(x =>
                x.BaseStructure.BaseStructureTypeID == (int) BaseStructureType.ControlTower);
        }

        public double GetPowerInUse(int pcBaseID)
        {
            int controlTowerID = (int)BaseStructureType.ControlTower;
            return _data.PCBaseStructures
                .Where(x => x.PCBaseID == pcBaseID && x.BaseStructure.BaseStructureTypeID != controlTowerID)
                .DefaultIfEmpty()
                .Sum(s => s == null || s.BaseStructure == null ? 0 : s.BaseStructure.Power);
        }

        public double GetCPUInUse(int pcBaseID)
        {
            int controlTowerID = (int)BaseStructureType.ControlTower;
            return _data.PCBaseStructures
                .Where(x => x.PCBaseID == pcBaseID && x != null && x.BaseStructure != null && x.BaseStructure.BaseStructureTypeID != controlTowerID)
                .DefaultIfEmpty()
                .Sum(s => s == null || s.BaseStructure == null ? 0 : s.BaseStructure.CPU);
        }

        public string GetSectorOfLocation(NWLocation targetLocation)
        {
            NWArea area = targetLocation.Area;
            int cellX = (int)(_.GetPositionFromLocation(targetLocation).m_X / 10);
            int cellY = (int)(_.GetPositionFromLocation(targetLocation).m_Y / 10);
            int pcBaseID = area.GetLocalInt("PC_BASE_ID");

            string sector = "INVALID";

            if (pcBaseID > 0)
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

        public string CanPlaceStructure(NWCreature user, NWItem structureItem, NWLocation targetLocation, int structureID)
        {
            NWPlayer player = user.Object;
            string sector = GetSectorOfLocation(targetLocation);
            if (sector == "INVALID")
            {
                return "Invalid location selected.";
            }

            if (structureItem == null || !structureItem.IsValid || !Equals(structureItem.Possessor, user))
            {
                return "Unable to locate structure item.";
            }

            NWArea area = _.GetAreaFromLocation(targetLocation);
            int buildingStructureID = area.GetLocalInt("PC_BASE_STRUCTURE_ID");
            int pcBaseID = area.GetLocalInt("PC_BASE_ID");
            BuildingType buildingType;
            if (pcBaseID > 0)
            {
                buildingType = BuildingType.Apartment;
            }
            else if (buildingStructureID <= 0)
            {
                buildingType = BuildingType.Exterior;
            }
            else
            {
                buildingType = BuildingType.Interior;
            }

            Area dbArea = _data.Areas.SingleOrDefault(x => x.Resref == area.Resref);

            if (dbArea == null || !dbArea.IsBuildable ) return "Structures cannot be placed in this area.";
            PCBase pcBase = pcBaseID > 0 ?
                _data.PCBases.Single(x => x.PCBaseID == pcBaseID) :
                _data.PCBases.SingleOrDefault(x => x.AreaResref == area.Resref && x.Sector == sector);
            if (pcBase == null && buildingType == BuildingType.Interior)
            {
                var parentStructure = _data.PCBaseStructures.Single(x => x.PCBaseStructureID == buildingStructureID);
                pcBase = parentStructure.PCBase;

                int buildingStructureCount = _data.PCBaseStructures.Count(x => x.ParentPCBaseStructureID == parentStructure.PCBaseStructureID) + 1;
                if(buildingStructureCount > parentStructure.BaseStructure.Storage)
                {
                    return "No more structures can be placed inside this building.";
                }

            }

            if (pcBase == null)
                return "This area is unclaimed but not owned by you. You may purchase a lease on it from the planetary government by using your Base Management Tool (found under feats).";

            var canPlaceOrEditStructures = buildingType == BuildingType.Apartment || buildingType == BuildingType.Exterior ? 
                _perm.HasBasePermission(player, pcBase.PCBaseID, BasePermission.CanPlaceEditStructures) :                 // Bases
                _perm.HasStructurePermission(player, buildingStructureID, StructurePermission.CanPlaceEditStructures);    // Buildings

            if (!canPlaceOrEditStructures)
                return "You do not have permission to place or edit structures in this territory.";
            
            var structure = _data.BaseStructures.Single(x => x.BaseStructureID == structureID);
            var structureType = _data.Get<Data.Entity.BaseStructureType>(structure.BaseStructureTypeID);
            
            if (!structureType.CanPlaceOutside && buildingType == BuildingType.Exterior)
            {
                return "That structure can only be placed inside buildings.";
            }

            if (!structureType.CanPlaceInside && (buildingType == BuildingType.Interior || buildingType == BuildingType.Apartment))
            {
                return "That structure can only be placed outside of buildings.";
            }

            if (buildingType == BuildingType.Exterior)
            {
                bool hasControlTower = pcBase.PCBaseStructures
                                           .SingleOrDefault(x => x.BaseStructure.BaseStructureTypeID == (int)BaseStructureType.ControlTower) != null;

                if (!hasControlTower && structureType.BaseStructureTypeID != (int)BaseStructureType.ControlTower)
                {
                    return "A control tower must be placed down in the sector first.";
                }

                if (hasControlTower && structureType.BaseStructureTypeID == (int)BaseStructureType.ControlTower)
                {
                    return "Only one control tower can be placed down per sector.";
                }

            }
            
            return null;
        }

        public NWItem ConvertStructureToItem(PCBaseStructure pcBaseStructure, NWObject target)
        {
            NWItem item = (_.CreateItemOnObject(pcBaseStructure.BaseStructure.ItemResref, target.Object));
            item.SetLocalInt("BASE_STRUCTURE_ID", pcBaseStructure.BaseStructureID);
            item.Name = pcBaseStructure.BaseStructure.Name;
            
            _durability.SetMaxDurability(item, (float)pcBaseStructure.Durability);
            _durability.SetDurability(item, (float)pcBaseStructure.Durability);
            item.StructureBonus = pcBaseStructure.StructureBonus;

            if (pcBaseStructure.InteriorStyleID != null && pcBaseStructure.ExteriorStyleID != null)
            {
                item.SetLocalInt("STRUCTURE_BUILDING_INTERIOR_ID", (int)pcBaseStructure.InteriorStyleID);
                item.SetLocalInt("STRUCTURE_BUILDING_EXTERIOR_ID", (int)pcBaseStructure.ExteriorStyleID);
                item.SetLocalInt("STRUCTURE_BUILDING_INITIALIZED", TRUE);
            }

            return item;
        }

        public void BootPlayersOutOfInstance(int pcBaseStructureID)
        {
            var areas = NWModule.Get().Areas;
            var instance = areas.SingleOrDefault(x => x.IsInstance && x.GetLocalInt("PC_BASE_STRUCTURE_ID") == pcBaseStructureID);
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
        }


        public void ClearPCBaseByID(int pcBaseID)
        {
            var pcBase = _data.PCBases.Single(x => x.PCBaseID == pcBaseID);
            var areas = NWModule.Get().Areas;
            var baseArea = areas.Single(x => x.Resref == pcBase.AreaResref);
            List<AreaStructure> areaStructures = baseArea.Data["BASE_SERVICE_STRUCTURES"];
            areaStructures = areaStructures.Where(x => x.PCBaseID == pcBaseID).ToList();
            
            // Remove all players who are a resident of any structure in this base or the base itself.
            var residents = _data.PlayerCharacters.Where(x => x.PrimaryResidencePCBaseID == pcBaseID || (x.PrimaryResidencePCBaseStructure != null && x.PrimaryResidencePCBaseStructure.PCBaseID == pcBaseID)).ToList();
            foreach (var resident in residents)
            {
                resident.PrimaryResidencePCBaseID = null;
                resident.PrimaryResidencePCBaseStructureID = null;
            }

            foreach (var structure in areaStructures)
            {
                BootPlayersOutOfInstance(structure.PCBaseStructureID);

                ((List<AreaStructure>) baseArea.Data["BASE_SERVICE_STRUCTURES"]).Remove(structure);
                structure.Structure.Destroy();
            }

            for (int x = pcBase.PCBaseStructures.Count-1; x >= 0; x--)
            {
                // Impound item storage
                var pcBaseStructure = pcBase.PCBaseStructures.ElementAt(x);
                for (int i = pcBaseStructure.PCBaseStructureItems.Count - 1; i >= 0; i--)
                {
                    var item = pcBaseStructure.PCBaseStructureItems.ElementAt(i);
                    _impound.Impound(item);
                    _data.PCBaseStructureItems.Remove(item);
                }

                // Clear structure permissions
                for (int p = pcBaseStructure.PCBaseStructurePermissions.Count - 1; p >= 0; p--)
                {
                    var permission = pcBaseStructure.PCBaseStructurePermissions.ElementAt(p);
                    _data.PCBaseStructurePermissions.Remove(permission);
                }

                var tempStorage = (_.GetObjectByTag("TEMP_ITEM_STORAGE"));
                NWItem copy = ConvertStructureToItem(pcBaseStructure, tempStorage);
                _impound.Impound(pcBase.PlayerID, copy);
                copy.Destroy();

                _data.PCBaseStructures.Remove(pcBaseStructure);
            }

            // Clear base permissions
            for (int p = pcBase.PCBasePermissions.Count - 1; p >= 0; p--)
            {
                var permission = pcBase.PCBasePermissions.ElementAt(p);
                _data.PCBasePermissions.Remove(permission);
            }

            _data.PCBases.Remove(pcBase);
            
            Area dbArea = _data.Areas.Single(x => x.Resref == pcBase.AreaResref);
            if (pcBase.Sector == AreaSector.Northeast) dbArea.NortheastOwner = null;
            else if (pcBase.Sector == AreaSector.Northwest) dbArea.NorthwestOwner = null;
            else if (pcBase.Sector == AreaSector.Southeast) dbArea.SoutheastOwner = null;
            else if (pcBase.Sector == AreaSector.Southwest) dbArea.SouthwestOwner = null;

            

            if (doSave)
            {
                _data.SaveChanges();
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
                    var defaultInterior = _data.BuildingStyles.Single(x => x.BaseStructureID == structure.BaseStructureID && x.IsDefault && x.BuildingTypeID == (int)BuildingType.Interior && x.IsActive).BuildingStyleID;
                    var defaultExterior = _data.BuildingStyles.Single(x => x.BaseStructureID == structure.BaseStructureID && x.IsDefault && x.BuildingTypeID == (int)BuildingType.Exterior && x.IsActive).BuildingStyleID;

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
            NWObject exit = null;

            NWObject @object = (_.GetFirstObjectInArea(area.Object));
            while (@object.IsValid)
            {
                if (@object.Tag == "building_exit")
                {
                    exit = @object;
                }

                @object = (_.GetNextObjectInArea(area.Object));
            }
            
            if (exit == null)
            {
                player.FloatingText("ERROR: Couldn't find the building interior's exit. Inform an admin of this issue.");
                return;
            }

            _player.SaveLocation(player);

            exit.SetLocalLocation("PLAYER_HOME_EXIT_LOCATION", player.Location);
            exit.SetLocalInt("IS_BUILDING_DOOR", 1);

            Location location = area.GetLocalLocation("INSTANCE_ENTRANCE");
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
                NWObject obj = (_.GetFirstObjectInArea(area.Object));
                while (obj.IsValid)
                {
                    if (obj.Tag == "building_exit")
                    {
                        door = (obj.Object);
                        break;
                    }
                    obj = (_.GetNextObjectInArea(area.Object));
                }
            }

            if (door == null)
            {
                return;
            }

            Location location = door.GetLocalLocation("PLAYER_HOME_EXIT_LOCATION");
            player.AssignCommand(() => _.ActionJumpToLocation(location));

            _.DelayCommand(1.0f, () =>
            {
                player = (_.GetFirstPC());
                while (player.IsValid)
                {
                    if (Equals(player.Area, area)) return;
                    player = (_.GetNextPC());
                }

                _area.DestroyAreaInstance(area);
            });
        }

        public static bool CanHandleChat(NWObject sender, string message)
        {
            bool validTarget = sender.IsPlayer || sender.IsDM;
            return validTarget && sender.GetLocalInt("LISTENING_FOR_NEW_CONTAINER_NAME") == TRUE;
        }

        public void OnModuleNWNXChat(NWPlayer sender)
        {
            string text = _nwnxChat.GetMessage().Trim();

            if (!CanHandleChat(sender, text))
            {
                return;
            }

            _nwnxChat.SkipMessage();

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
            if (controlTower == null) return 0;

            return (int)(controlTower.Durability * 300);
        }

        public int CalculateMaxFuel(PCBase pcBase)
        {
            const int siloType = (int)BaseStructureType.FuelSilo;
            PCBaseStructure tower = GetBaseControlTower(pcBase.PCBaseID);
            float siloBonus = _data.PCBaseStructures
                                  .Where(x => x.PCBaseID == pcBase.PCBaseID &&
                                              x.BaseStructure.BaseStructureTypeID == siloType)
                                  .DefaultIfEmpty()
                                  .Sum(x => x == null ? 0 : x.BaseStructure.Storage + x.StructureBonus) * 0.01f;

            var fuelMax = tower.BaseStructure.Storage;

            return (int)(fuelMax + fuelMax * siloBonus);
        }

        public int CalculateMaxReinforcedFuel(PCBase pcBase)
        {
            const int siloType = (int) BaseStructureType.StronidiumSilo;
            PCBaseStructure tower = GetBaseControlTower(pcBase.PCBaseID);
            float siloBonus = _data.PCBaseStructures
                                  .Where(x => x.PCBaseID == pcBase.PCBaseID &&
                                              x.BaseStructure.BaseStructureTypeID == siloType)
                                  .DefaultIfEmpty()
                                  .Sum(x => x == null ? 0 : x.BaseStructure.Storage + x.StructureBonus) * 0.01f;

            var fuelMax = tower.BaseStructure.ReinforcedStorage;

            return (int)(fuelMax + fuelMax * siloBonus);
        }

        public int CalculateResourceCapacity(PCBase pcBase)
        {
            const int siloType = (int) BaseStructureType.ResourceSilo;
            PCBaseStructure tower = GetBaseControlTower(pcBase.PCBaseID);
            float siloBonus = _data.PCBaseStructures
                                  .Where(x => x.PCBaseID == pcBase.PCBaseID &&
                                              x.BaseStructure.BaseStructureTypeID == siloType)
                                  .DefaultIfEmpty()
                                  .Sum(x => x == null ? 0 : x.BaseStructure.Storage + x.StructureBonus) * 0.01f;

            var resourceMax = tower.BaseStructure.ResourceStorage;

            return (int) (resourceMax + resourceMax * siloBonus);
        }


    }
}
