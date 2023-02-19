using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.PerkService
{
    public delegate string PerkPurchaseRequirementAction(uint player, PerkType perkType, int effectivePerkLevel);
    public delegate string PerkRefundRequirementAction(uint player, PerkType perkType, int effectivePerkLevel);

    public delegate void PerkTriggerEquippedAction(uint player, uint item, InventorySlot slot, PerkType perkType, int effectivePerkLevel);
    public delegate void PerkTriggerUnequippedAction(uint player, uint item, InventorySlot slot, PerkType perkType, int effectivePerkLevel);
    public delegate void PerkTriggerPurchasedRefundedAction(uint player, PerkType perkType, int effectivePerkLevel);


    public class PerkDetail
    {
        public PerkCategoryType Category { get; set; }
        public PerkType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string IconResref { get; set; }
        public PerkGroupType GroupType { get; set; }

        public PerkPurchaseRequirementAction PurchaseRequirement { get; set; }
        public PerkRefundRequirementAction RefundRequirement { get; set; }
        public Dictionary<int, PerkLevel> PerkLevels { get; set; }
        public List<PerkTriggerEquippedAction> EquippedTriggers { get; set; }
        public List<PerkTriggerUnequippedAction> UnequippedTriggers { get; set; }
        public List<PerkTriggerPurchasedRefundedAction> PurchasedTriggers { get; set; }
        public List<PerkTriggerPurchasedRefundedAction> RefundedTriggers { get; set; }

        public PerkDetail()
        {
            GroupType = PerkGroupType.Player;
            PerkLevels = new Dictionary<int, PerkLevel>();

            EquippedTriggers = new List<PerkTriggerEquippedAction>();
            UnequippedTriggers = new List<PerkTriggerUnequippedAction>();
            PurchasedTriggers = new List<PerkTriggerPurchasedRefundedAction>();
            RefundedTriggers = new List<PerkTriggerPurchasedRefundedAction>();
        }
    }
}
