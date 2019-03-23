using NWN;
using SWLOR.Game.Server.GameObject;

using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class CreatureCorpseService
    {
        public static void OnCreatureDeath()
        {
            _.SetIsDestroyable(FALSE);
            
            NWObject self = Object.OBJECT_SELF;
            if (self.Tag == "spaceship_copy") return;

            Vector lootPosition = _.Vector(self.Position.m_X, self.Position.m_Y, self.Position.m_Z - 0.11f);
            Location spawnLocation = _.Location(self.Area, lootPosition, self.Facing);

            NWPlaceable container = _.CreateObject(OBJECT_TYPE_PLACEABLE, "corpse", spawnLocation);
            container.SetLocalObject("CORPSE_BODY", self);
            container.Name = self.Name + "'s Corpse";

            container.AssignCommand(() =>
            {
                _.TakeGoldFromCreature(self.Gold, self);
            });

            // Dump equipped items in container
            for (int slot = 0; slot < NUM_INVENTORY_SLOTS; slot++)
            {
                if (slot == INVENTORY_SLOT_CARMOUR ||
                    slot == INVENTORY_SLOT_CWEAPON_B ||
                    slot == INVENTORY_SLOT_CWEAPON_L ||
                    slot == INVENTORY_SLOT_CWEAPON_R)
                    continue;

                NWItem item = _.GetItemInSlot(slot, self);
                if (item.IsValid && !item.IsCursed && item.IsDroppable)
                {
                    NWItem copy = _.CopyItem(item, container, TRUE);

                    if (slot == INVENTORY_SLOT_HEAD ||
                        slot == INVENTORY_SLOT_CHEST)
                    {
                        copy.SetLocalObject("CORPSE_ITEM_COPY", item);
                    }
                    else
                    {
                        item.Destroy();
                    }
                }
            }

            foreach (var item in self.InventoryItems)
            {
                _.CopyItem(item, container, TRUE);
                item.Destroy();
            }

            _.DelayCommand(360.0f, () =>
            {
                if (!container.IsValid) return;

                NWObject body = container.GetLocalObject("CORPSE_BODY");
                body.AssignCommand(() => _.SetIsDestroyable(TRUE));
                body.DestroyAllInventoryItems();
                body.Destroy();

                container.DestroyAllInventoryItems();
                container.Destroy();
            });

        }

    }
}
