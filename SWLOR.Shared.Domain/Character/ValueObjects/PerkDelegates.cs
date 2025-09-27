using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Shared.Domain.Character.ValueObjects
{
    public delegate string PerkPurchaseRequirementAction(uint player);
    public delegate string PerkRefundRequirementAction(uint player);

    public delegate void PerkTriggerEquippedAction(uint player, uint item, InventorySlotType slot, PerkType perkType, int effectivePerkLevel);
    public delegate void PerkTriggerUnequippedAction(uint player, uint item, InventorySlotType slot, PerkType perkType, int effectivePerkLevel);
    public delegate void PerkTriggerPurchasedRefundedAction(uint player);

}
