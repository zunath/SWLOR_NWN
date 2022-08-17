using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    public class _5_UpdateStoredWeapons: IServerMigration
    {
        public int Version => 5;

        private readonly Dictionary<string, (int, int)> itemReplace = new()
            {
                { "tit_rifle", (12, 15) },
                { "cap_rifle", (15, 20) },
                { "mando_rifle", (13, 17) },
                { "h_rifle_2", (14, 24) },
                { "del_rifle", (17, 27) },
                { "poach_rifle", (18, 29) },
                { "byysk_rifle", (18, 29) },
                { "h_rifle_3", (19, 32) },
                { "proto_rifle", (21, 34) },
                { "raider_rifle", (22, 36) },
                { "h_rifle_4", (23, 38) },
                { "oph_rifle", (26, 39) },
                { "h_rifle_5", (28, 42) },
                { "b_twinblade", (6, 8) },
                { "tit_twinblade", (10, 13) },
                { "sith_twinblade", (11, 15) },
                { "cap_twinblade", (13, 15) },
                { "mando_twinblade", (11, 13) },
                { "h_twinblade_2", (11, 16) },
                { "del_twinblade", (15, 18) },
                { "byysk_twinblade", (17, 19) },
                { "poach_twinblade", (16, 19) },
                { "h_twinblade_3", (16, 20) },
                { "proto_twinblade", (19, 22) },
                { "raider_twinblade", (20, 24) },
                { "h_twinblade_4", (20, 25) },
                { "oph_twinblade", (24, 27) },
                { "h_twinblade_5", (25, 28) },
            };

        public void Migrate()
        {
            UpdatePersistentStorageWeapons();
        }

        private void UpdatePersistentStorageWeapons()
        {
            var query = new DBQuery<InventoryItem>();
            var itemCount = (int)DB.SearchCount(query);
            var items = DB.Search(query.AddPaging(itemCount, 0)).ToList();

            foreach (var item in items)
            {
                var deserialized = ObjectPlugin.Deserialize(item.Data);
                if (!GetIsObjectValid(deserialized))
                    continue;

                UpdateWeapon(deserialized);

                item.Data = ObjectPlugin.Serialize(deserialized);
                DB.Set(item);

                DestroyObject(deserialized);
            }
        }

        private void UpdateWeapon(uint item)
        {
            BaseItem baseItem = GetBaseItemType(item);
            if (!Item.RifleBaseItemTypes.Contains(baseItem) && !Item.TwinBladeBaseItemTypes.Contains(baseItem))
                return;

            string itemResRef = GetResRef(item);

            if (!itemReplace.ContainsKey(itemResRef))
            {
                return;
            }

            int oldDmg = itemReplace[itemResRef].Item1;
            int newDmg = itemReplace[itemResRef].Item2;

            int wpnDmg = newDmg - oldDmg;
            if (wpnDmg <= 0) { return; }

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.DMG &&
                    (GetItemPropertySubType(ip) == (int)CombatDamageType.Physical))
                {
                    wpnDmg += GetItemPropertyCostTableValue(ip);
                    RemoveItemProperty(item, ip);
                }
            }

            ItemProperty newDmgProperty = ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, wpnDmg);
            BiowareXP2.IPSafeAddItemProperty(item, newDmgProperty, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
        }

    }
}
