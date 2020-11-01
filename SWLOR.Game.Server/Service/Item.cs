namespace SWLOR.Game.Server.Service
{
    public static class Item
    {
        public static void ReturnItem(uint target, uint item)
        {
            if (GetHasInventory(item))
            {
                var possessor = GetItemPossessor(item);
                AssignCommand(possessor, () =>
                {
                    ActionGiveItem(item, target);
                });
            }
            else
            {
                CopyItem(item, target, true);
                DestroyObject(item);
            }
        }

        /// <summary>
        /// Returns the number of items in an object's inventory.
        /// Returns -1 if target does not have an inventory
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>-1 if obj doesn't have an inventory, otherwise returns the number of items in the inventory</returns>
        public static int GetInventoryItemCount(uint obj)
        {
            if (!GetHasInventory(obj)) return -1;

            int count = 0;
            var item = GetFirstItemInInventory(obj);
            while (GetIsObjectValid(item))
            {
                count++;
                item = GetNextItemInInventory(obj);
            }

            return count;
        }
    }
}
