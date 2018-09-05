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
        private readonly ISerializationService _serialization;

        public BaseService(INWScript script,
            INWNXEvents nwnxEvents,
            IDialogService dialog,
            IDataContext db,
            ISerializationService serialization)
        {
            _ = script;
            _nwnxEvents = nwnxEvents;
            _dialog = dialog;
            _db = db;
            _serialization = serialization;


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
                List<AreaStructure> areaStructures = new List<AreaStructure>();
                if (area.Data.ContainsKey("BASE_SERVICE_STRUCTURES"))
                {
                    areaStructures = area.Data["BASE_SERVICE_STRUCTURES"];
                }

                var pcBases = _db.PCBases.Where(x => x.AreaResref == area.Resref).ToList();
                foreach (var @base in pcBases)
                {

                    foreach (var structure in @base.PCBaseStructures)
                    {
                        Location location = _.Location(area.Object,
                            _.Vector((float)structure.LocationX, (float)structure.LocationY, (float)structure.LocationZ),
                            (float)structure.LocationOrientation);

                        var plc = NWPlaceable.Wrap(_.CreateObject(OBJECT_TYPE_PLACEABLE, structure.BaseStructure.PlaceableResref, location));
                        plc.SetLocalInt("PC_BASE_STRUCTURE_ID", structure.PCBaseStructureID);

                        areaStructures.Add(new AreaStructure(@base.PCBaseID, structure.PCBaseStructureID, plc));
                    }
                }

                area.Data["BASE_SERVICE_STRUCTURES"] = areaStructures;
            }
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
            _db.SaveChanges();

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
            Area dbArea = _db.Areas.SingleOrDefault(x => x.Resref == area.Resref);

            if (dbArea == null || !dbArea.IsBuildable) return "Structures cannot be placed in this area.";
            PCBase pcBase = _db.PCBases.SingleOrDefault(x => x.AreaResref == area.Resref && x.Sector == sector);

            if (pcBase == null)
                return "This area is unclaimed but not owned by you. You may purchase a lease on it from the planetary government by using your Base Management Tool (found under feats).";

            if (pcBase.PlayerID != user.GlobalID)
                return "You do not have permission to place structures in this territory.";

            int controlTowerTypeID = (int)BaseStructureType.ControlTower;
            
            var structure = _db.BaseStructures.Single(x => x.BaseStructureID == structureID);
            int structureTypeID = structure.BaseStructureTypeID;

            bool hasControlTower = pcBase.PCBaseStructures
                                       .SingleOrDefault(x => x.BaseStructure.BaseStructureTypeID == controlTowerTypeID) != null;

            if (!hasControlTower && structureTypeID != controlTowerTypeID)
            {
                return "A control tower must be placed down in the sector first.";
            }

            if (hasControlTower && structureTypeID == controlTowerTypeID)
            {
                return "Only one control tower can be placed down per sector.";
            }

            return null;
        }

        public void ClearPCBaseByID(int pcBaseID, bool doSave = true)
        {
            var pcBase = _db.PCBases.Single(x => x.PCBaseID == pcBaseID);
            var area = NWModule.Get().Areas.Single(x => x.Resref == pcBase.AreaResref);
            List<AreaStructure> areaStructures = area.Data["BASE_SERVICE_STRUCTURES"];
            areaStructures = areaStructures.Where(x => x.PCBaseID == pcBaseID).ToList();
            
            foreach (var structure in areaStructures)
            {
                ((List<AreaStructure>) area.Data["BASE_SERVICE_STRUCTURES"]).Remove(structure);
                structure.Structure.Destroy();
            }
            
            for(int x = pcBase.PCBaseStructures.Count-1; x >= 0; x--)
            {
                var pcBaseStructure = pcBase.PCBaseStructures.ElementAt(x);
                for (int i = pcBaseStructure.PCBaseStructureItems.Count - 1; i >= 0; i--)
                {
                    var item = pcBaseStructure.PCBaseStructureItems.ElementAt(x);
                    var impoundItem = new PCImpoundedItem
                    {
                        DateImpounded = DateTime.UtcNow,
                        ItemName = item.ItemName,
                        ItemResref = item.ItemResref,
                        ItemObject = item.ItemObject,
                        ItemTag = item.ItemTag,
                        PlayerID = pcBase.PlayerID
                    };

                    _db.PCImpoundedItems.Add(impoundItem);
                    _db.PCBaseStructureItems.Remove(item);
                }

                // Convert structure back to an item.
                var tempStorage = NWPlaceable.Wrap(_.GetObjectByTag("TEMP_ITEM_STORAGE"));
                NWItem copy = NWItem.Wrap(_.CreateItemOnObject(pcBaseStructure.BaseStructure.ItemResref, tempStorage.Object));
                copy.SetLocalInt("BASE_STRUCTURE_ID", pcBaseStructure.BaseStructureID);
                copy.Name = pcBaseStructure.BaseStructure.Name;
                copy.MaxDurability = (float)pcBaseStructure.Durability;
                copy.Durability = (float)pcBaseStructure.Durability;

                PCImpoundedItem structureImpoundedItem = new PCImpoundedItem
                {
                    DateImpounded = DateTime.UtcNow,
                    PlayerID = pcBase.PlayerID,
                    ItemObject = _serialization.Serialize(copy),
                    ItemTag = copy.Tag,
                    ItemResref = copy.Resref,
                    ItemName = copy.Name
                };

                copy.Destroy();
                _db.PCImpoundedItems.Add(structureImpoundedItem);
                _db.PCBaseStructures.Remove(pcBaseStructure);
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
        
    }
}
