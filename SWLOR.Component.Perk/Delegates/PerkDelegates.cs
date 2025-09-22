using SWLOR.Shared.Core.Enums;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Shared.Core.Delegates
{
    public delegate string PerkPurchaseRequirementAction(uint player);
    public delegate string PerkRefundRequirementAction(uint player);

    public delegate void PerkTriggerEquippedAction(uint player, uint item, InventorySlot slot, PerkType perkType, int effectivePerkLevel);
    public delegate void PerkTriggerUnequippedAction(uint player, uint item, InventorySlot slot, PerkType perkType, int effectivePerkLevel);
    public delegate void PerkTriggerPurchasedRefundedAction(uint player);

}
