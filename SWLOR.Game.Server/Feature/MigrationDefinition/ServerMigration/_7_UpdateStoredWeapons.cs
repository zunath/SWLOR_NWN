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
    public class _7_UpdateStoredWeapons: ServerMigrationBase, IServerMigration
    {
        public int Version => 7;

        private readonly Dictionary<string, (int, int)> _itemReplace = new()
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
                { "trn_saberstaff_1", (6, 8) },
                { "twin_elec_1", (6, 8) },
                { "tit_twinblade", (10, 13) },
                { "trn_saberstaff_2", (10, 13) },
                { "twin_elec_2", (10, 13) },
                { "sith_twinblade", (11, 15) },
                { "cap_twinblade", (13, 15) },
                { "cap_saberstaff", (13, 15) },
                { "mando_twinblade", (11, 13) },
                { "mando_sabstaff", (11, 13) },
                { "h_twinblade_2", (11, 16) },
                { "h_twinelec_2", (11, 16) },
                { "del_twinblade", (15, 18) },
                { "trn_saberstaff_3", (15, 18) },
                { "twin_elec_3", (15, 18) },
                { "byysk_twinblade", (17, 19) },
                { "poach_twinblade", (16, 19) },
                { "h_twinblade_3", (16, 20) },
                { "h_twinelec_3", (16, 20) },
                { "proto_twinblade", (19, 22) },
                { "trn_saberstaff_4", (19, 22) },
                { "twin_elec_4", (19, 22) },
                { "raider_twinblade", (20, 24) },
                { "h_twinblade_4", (20, 25) },
                { "h_twinelec_4", (20, 25) },
                { "oph_twinblade", (24, 27) },
                { "trn_saberstaff_5", (24, 27) },
                { "twin_elec_5", (24, 27) },
                { "h_twinblade_5", (25, 28) },
                { "h_twinelec_5", (25, 28) }
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
            var baseItem = GetBaseItemType(item);
            if (!Item.RifleBaseItemTypes.Contains(baseItem) && !Item.SaberstaffBaseItemTypes.Contains(baseItem) && !Item.TwinBladeBaseItemTypes.Contains(baseItem))
                return;

            var itemResRef = GetResRef(item);
            int oldDmg = 0;
            int newDmg = 0;

            if (_itemReplace.ContainsKey(itemResRef))
            {
                oldDmg = _itemReplace[itemResRef].Item1;
                newDmg = _itemReplace[itemResRef].Item2;
            }
            else if (baseItem == BaseItem.Saberstaff) { newDmg = 3; } // Actual saberstaves won't be in the list, so we're just bumping their DMG directly

            var wpnDmg = newDmg - oldDmg;
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

            var newDmgProperty = ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, wpnDmg);
            BiowareXP2.IPSafeAddItemProperty(item, newDmgProperty, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
        }

    }
}
