using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class LootCorpses
    {
        /// <summary>
        /// Handles creating a corpse placeable on a creature's death, copying its inventory to the placeable,
        /// and changing the name of the placeable to match the creature.
        /// </summary>
        [NWNEventHandler("crea_death")]
        public static void ProcessCorpse()
        {
            var self = OBJECT_SELF;
            SetIsDestroyable(false);

            var area = GetArea(self);
            var position = GetPosition(self);
            var facing = GetFacing(self);
            var lootPosition = Vector3(position.X, position.Y, position.Z - 0.11f);
            Location spawnLocation = Location(area, lootPosition, facing);

            var container = CreateObject(ObjectType.Placeable, "corpse", spawnLocation);
            SetLocalObject(container, "CORPSE_BODY", self);
            SetName(container, $"{GetName(self)}'s Corpse");
            
            AssignCommand(container, () =>
            {
                var gold = GetGold(self);
                TakeGoldFromCreature(gold, self);
            });

            // Dump equipped items in container
            for (int slot = 0; slot < NumberOfInventorySlots; slot++)
            {
                if (slot == (int)InventorySlot.CreatureArmor ||
                    slot == (int)InventorySlot.CreatureBite ||
                    slot == (int)InventorySlot.CreatureLeft ||
                    slot == (int)InventorySlot.CreatureRight)
                    continue;

                var item = GetItemInSlot((InventorySlot)slot, self);
                if (GetIsObjectValid(item) && !GetItemCursedFlag(item) && GetDroppableFlag(item))
                {
                    var copy = CopyItem(item, container, true);

                    if (slot == (int)InventorySlot.Head ||
                        slot == (int)InventorySlot.Chest)
                    {
                        SetLocalObject(copy, "CORPSE_ITEM_COPY", item);
                    }
                    else
                    {
                        DestroyObject(item);
                    }
                }
            }

            for(var item = GetFirstItemInInventory(self); GetIsObjectValid(item); item = GetNextItemInInventory(self))
            {
                if (GetIsObjectValid(item) && !GetItemCursedFlag(item) && GetDroppableFlag(item))
                {
                    CopyItem(item, container, true);
                    DestroyObject(item);
                }
            }

            DelayCommand(360.0f, () =>
            {
                if (!GetIsObjectValid(container)) return;

                var body = GetLocalObject(container, "CORPSE_BODY");
                AssignCommand(body, () => SetIsDestroyable());

                for (var item = GetFirstItemInInventory(body); GetIsObjectValid(item); item = GetNextItemInInventory(body))
                {
                    DestroyObject(item);
                }
                DestroyObject(body);

                for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
                {
                    DestroyObject(item);
                }
                DestroyObject(container);
            });
        }

        [NWNEventHandler("corpse_closed")]
        public static void CloseCorpseContainer()
        {
            var container = OBJECT_SELF;
            var firstItem = GetFirstItemInInventory(container);
            var corpseOwner = GetLocalObject(container, "CORPSE_BODY");

            if (!GetIsObjectValid(firstItem))
            {
                DestroyObject(container);
            }

            AssignCommand(corpseOwner, () =>
            {
                SetIsDestroyable();
            });
        }

        [NWNEventHandler("corpse_disturbed")]
        public static void DisturbCorpseContainer()
        {
            var looter = GetLastDisturbed();
            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();

            AssignCommand(looter, () =>
            {
                ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, 1.0f);
            });

            if (type == DisturbType.Added)
            {
                Item.ReturnItem(looter, item);
                SendMessageToPC(looter, "You cannot place items inside of corpses.");
            }
            else if (type == DisturbType.Removed)
            {
                var copy = GetLocalObject(item, "CORPSE_ITEM_COPY");

                if (GetIsObjectValid(copy))
                {
                    DestroyObject(copy);
                }

                DeleteLocalObject(item, "CORPSE_ITEM_COPY");
            }
        }
    }
}
