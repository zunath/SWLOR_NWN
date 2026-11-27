using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.EquipmentStatActions
{
    /// <summary>
    /// Writes a stat onto an NPC's creature skin, the way an NPC keeps equipment stats.
    /// </summary>
    public static class NpcSkinStatWriter
    {
        public static void ReapplyNPCStat(uint npc, ItemPropertyType ipType, int amount, bool isAdding)
        {
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, npc);
            var value = 0;

            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                var type = GetItemPropertyType(ip);
                if (type == ipType)
                {
                    value += GetItemPropertyCostTableValue(ip);
                }
            }

            if (isAdding)
            {
                value += amount;
            }
            else
            {
                value -= amount;
            }

            if (value <= 0)
            {
                BiowareXP2.IPRemoveMatchingItemProperties(skin, ipType, DurationType.Invalid, -1);
            }
            else
            {
                var itemProperty = ItemPropertyCustom(ipType, -1, value);
                BiowareXP2.IPSafeAddItemProperty(skin, itemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
            }
        }
    }
}
