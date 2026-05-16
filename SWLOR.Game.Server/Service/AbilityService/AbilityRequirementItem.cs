using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds an inventory item requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementItem : IAbilityActivationRequirement
    {
        public string ItemResref { get; }
        public int Quantity { get; }
        public PerkType PreservePerkType { get; }
        public int PreserveChancePerLevel { get; }

        public AbilityRequirementItem(
            string itemResref,
            int quantity = 1,
            PerkType preservePerkType = PerkType.Invalid,
            int preserveChancePerLevel = 0)
        {
            ItemResref = itemResref;
            Quantity = Math.Max(1, quantity);
            PreservePerkType = preservePerkType;
            PreserveChancePerLevel = Math.Max(0, preserveChancePerLevel);
        }

        public string CheckRequirements(uint player, AbilityDetail ability = null)
        {
            if (GetIsDM(player) || !GetIsPC(player))
                return string.Empty;

            var item = GetItemPossessedBy(player, ItemResref);
            if (GetIsObjectValid(item) && GetItemStackSize(item) >= Quantity)
                return string.Empty;

            var itemName = Cache.GetItemNameByResref(ItemResref);
            return Quantity == 1
                ? $"Requires {itemName}."
                : $"Requires {Quantity} {itemName}.";
        }

        public void AfterActivationAction(uint player, AbilityDetail ability = null)
        {
            if (GetIsDM(player) || !GetIsPC(player))
                return;

            if (ShouldPreserveItem(player))
                return;

            var item = GetItemPossessedBy(player, ItemResref);
            if (GetIsObjectValid(item))
                Item.ReduceItemStack(item, Quantity);
        }

        private bool ShouldPreserveItem(uint player)
        {
            if (PreservePerkType == PerkType.Invalid || PreserveChancePerLevel <= 0)
                return false;

            var chance = Perk.GetPerkLevel(player, PreservePerkType) * PreserveChancePerLevel;
            return chance > 0 && Random.D100(1) <= chance;
        }
    }
}
