using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.NWN.API.NWNX;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    /// <summary>Reserves owned inventory space before native unequipping can drop an item.</summary>
    internal static unsafe class PlayerEquipmentStorage
    {
        private static CNWSItem NativeItem(uint item) =>
            NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(item)?.AsNWSItem()
            ?? throw new InvalidOperationException("An inventory item could not be resolved.");

        private static bool Fits(CItemRepository repository, uint item)
        {
            byte x = 0, y = 0;
            return repository != null && repository.FindPosition(NativeItem(item), &x, &y, 0) != 0;
        }

        private static (int Width, int Height) Size(uint item)
        {
            var row = (int)GetBaseItemType(item);
            if (!int.TryParse(Get2DAString("baseitems", "InvSlotWidth", row), out var width) ||
                !int.TryParse(Get2DAString("baseitems", "InvSlotHeight", row), out var height) || width <= 0 || height <= 0)
                throw new InvalidOperationException("Inventory dimensions are unavailable.");
            return (width, height);
        }

        private static void Move(uint item, uint target)
        {
            if (!ItemPlugin.MoveTo(item, target, true) || GetItemPossessor(item, true) != target)
                throw new InvalidOperationException("An inventory transfer could not be completed.");
        }

        public static void Unequip(uint player, uint item, InventorySlot slot)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
            var inventory = creature.m_pcItemRepository;
            var destination = OBJECT_INVALID;
            if (!Fits(inventory, item))
            {
                var carried = new List<uint>();
                for (var current = GetFirstItemInInventory(player); GetIsObjectValid(current); current = GetNextItemInInventory(player))
                    if (GetItemPossessor(current, true) == player) carried.Add(current);
                destination = carried.FirstOrDefault(candidate => GetHasInventory(candidate) &&
                    Fits(NativeItem(candidate).m_pItemRepository, item), OBJECT_INVALID);
                if (destination == OBJECT_INVALID)
                    destination = CreateRecoveryBag(player, item, inventory, carried);
            }

            if (destination == OBJECT_INVALID)
                CreaturePlugin.RunUnequip(player, item);
            else
            {
                // Match NWNX_Creature's event context while explicitly selecting the reserved bag.
                var script = NWNXLib.g_pVirtualMachine.m_pVirtualMachineScript[0];
                var previousEvent = script.m_nScriptEventID;
                try
                {
                    script.m_nScriptEventID = 3016;
                    creature.RunUnequip(item, destination, 0xff, 0xff, 0, OBJECT_INVALID);
                }
                finally { script.m_nScriptEventID = previousEvent; }
            }
            if (GetItemInSlot(slot, player) == item || GetItemPossessor(item) != player ||
                inventory.GetItemInRepository(NativeItem(item), 1) == 0)
                throw new InvalidOperationException($"Unable to preserve equipment from slot {slot} during player migration.");
        }

        private static uint CreateRecoveryBag(uint player, uint item, CItemRepository inventory, List<uint> carried)
        {
            var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
            if (!GetIsObjectValid(storage))
                throw new InvalidOperationException("Migration item storage is unavailable.");
            var bag = CreateItemOnObject("bag_b", storage);
            if (!GetIsObjectValid(bag))
                throw new InvalidOperationException("A recovery bag could not be created.");
            var donor = OBJECT_INVALID;
            try
            {
                SetName(bag, "Recovered Equipment");
                var bagInventory = NativeItem(bag).m_pItemRepository;
                if (!Fits(bagInventory, item))
                    throw new InvalidOperationException("The equipped item cannot fit in a recovery bag.");
                if (!Fits(inventory, bag))
                {
                    var bagSize = Size(bag);
                    var itemSize = Size(item);
                    donor = carried.FirstOrDefault(candidate =>
                    {
                        if (GetHasInventory(candidate) || GetItemStackSize(candidate) != 1) return false;
                        var size = Size(candidate);
                        return size.Width >= bagSize.Width && size.Height >= bagSize.Height &&
                            ((size.Width + itemSize.Width <= bagInventory.m_nWidth && Math.Max(size.Height, itemSize.Height) <= bagInventory.m_nHeight) ||
                             (size.Height + itemSize.Height <= bagInventory.m_nHeight && Math.Max(size.Width, itemSize.Width) <= bagInventory.m_nWidth));
                    }, OBJECT_INVALID);
                    if (donor == OBJECT_INVALID)
                        throw new InvalidOperationException("The inventory has no safe space for recovered equipment. Free inventory space before reconnecting.");
                    Move(donor, bag);
                }
                Move(bag, player);
                return bag;
            }
            catch
            {
                // The original equipped item has not moved; return the donor before disposing of its temporary bag.
                if (GetIsObjectValid(donor) && GetItemPossessor(donor, true) == bag)
                    Move(donor, player);
                if (GetItemPossessor(bag, true) != player) DestroyObject(bag);
                throw;
            }
        }
    }
}
