using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class MigrationService: IMigrationService
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IColorTokenService _color;
        private readonly ISerializationService _serialization;
        private readonly IItemService _item;

        private const int PlayerVersionNumber = 1;

        public MigrationService(INWScript script,
            IDataContext db,
            IColorTokenService color,
            ISerializationService serialization,
            IItemService item)
        {
            _ = script;
            _db = db;
            _color = color;
            _serialization = serialization;
            _item = item;
        }

        public void OnAreaEnter()
        {
            NWPlayer oPC = (_.GetEnteringObject());

            if (!oPC.IsPlayer ||
                oPC.GetLocalInt("MIGRATION_SYSTEM_LOGGED_IN_ONCE") == 1 ||
                oPC.Area.Tag == "ooc_area") return;
            PerformMigration(oPC);
        }


        private void PerformMigration(NWPlayer oPC)
        {
            PlayerCharacter entity = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);

            for (int version = entity.VersionNumber + 1; version <= PlayerVersionNumber; version++)
            {
                Dictionary<string, PCMigrationItem> itemMap = new Dictionary<string, PCMigrationItem>();
                List<int> stripItemList = new List<int>();
                PCMigration migration = _db.PCMigrations.Single(x => x.PCMigrationID == version);

                foreach (PCMigrationItem item in migration.PCMigrationItems)
                {
                    if (!string.IsNullOrWhiteSpace(item.CurrentResref))
                    {
                        itemMap[item.CurrentResref] = item;
                    }
                    else if (item.BaseItemTypeID > -1 && item.StripItemProperties)
                    {
                        stripItemList.Add(item.BaseItemTypeID);
                    }
                }

                for (int slot = 0; slot < NWScript.NUM_INVENTORY_SLOTS; slot++)
                {
                    NWItem item = (_.GetItemInSlot(slot, oPC.Object));
                    ProcessItem(oPC, item, itemMap, stripItemList);
                }

                foreach (NWItem item in oPC.InventoryItems)
                {
                    ProcessItem(oPC, item, itemMap, stripItemList);
                }

                RunCustomMigrationProcess(oPC, version);
                entity = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);
                entity.VersionNumber = version;
                _db.SaveChanges();
                oPC.SetLocalInt("MIGRATION_SYSTEM_LOGGED_IN_ONCE", NWScript.TRUE);

                long overflowCount = _db.PCOverflowItems.LongCount(x => x.PlayerID == oPC.GlobalID);
                string message = _color.Green("Your character has been updated!" +
                                              (overflowCount > 0 ? " Items which could not be created have been placed into overflow inventory. You can access this from the rest menu." : ""));

                _.DelayCommand(8.0f, () =>
                {
                    oPC.FloatingText(message);
                });
            }
        }

        private void ProcessItem(NWPlayer oPC, NWItem item, Dictionary<string, PCMigrationItem> itemMap, List<int> stripItemList)
        {
            string resref = item.Resref;
            int quantity = item.StackSize;
            int baseItemTypeID = item.BaseItemType;
            PCMigrationItem migrationItem = itemMap[resref];

            if (itemMap.ContainsKey(resref))
            {
                item.Destroy();
                if (!string.IsNullOrWhiteSpace(migrationItem.NewResref))
                {
                    NWItem newItem = (_.CreateItemOnObject(migrationItem.NewResref, oPC.Object, quantity));
                    if (!newItem.Possessor.IsValid)
                    {
                        PCOverflowItem overflow = new PCOverflowItem
                        {
                            ItemResref = newItem.Resref,
                            ItemTag = newItem.Tag,
                            ItemName = newItem.Name,
                            ItemObject = _serialization.Serialize(newItem),
                            PlayerID = oPC.GlobalID
                        };
                        _db.PCOverflowItems.Add(overflow);
                        _db.SaveChanges();

                        newItem.Destroy();
                    }
                }
            }
            else if (stripItemList.Contains(baseItemTypeID))
            {
                _item.StripAllItemProperties(item);
            }
        }

        private static void RunCustomMigrationProcess(NWObject oPC, int versionNumber)
        {
            switch (versionNumber)
            {
                default:
                    break;
            }
        }
    }
}
