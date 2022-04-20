﻿using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class TrashCan
    {
        /// <summary>
        /// When the trash can is opened, the player is notified anything placed inside will be destroyed.
        /// </summary>
        [NWNEventHandler("trash_opened")]
        public static void AlertPlayer()
        {
            var player = GetLastOpenedBy();
            FloatingTextStringOnCreature("Any item placed inside this trash can will be destroyed permanently.", player, false);
        }

        /// <summary>
        /// When the trash can is closed, any items inside will be destroyed and then the placeable will be destroyed.
        /// </summary>
        [NWNEventHandler("trash_closed")]
        public static void CleanUp()
        {
            var container = OBJECT_SELF;
            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            {
                DestroyObject(item);
            }

            DestroyObject(container);
        }

        /// <summary>
        /// When an item is added to the trash can, it will be destroyed.
        /// </summary>
        [NWNEventHandler("trash_disturbed")]
        public static void DestroyItem()
        {
            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();

            if (type != DisturbType.Added) return;

            DestroyObject(item);
        }
    }
}
