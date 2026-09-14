using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds an inventory item requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementItem : IAbilityActivationRequirement
    {
        public string ItemResref { get; }
        public int Quantity { get; }
        public StatType PreserveChanceStatType { get; }

        public AbilityRequirementItem(
            string itemResref,
            int quantity = 1,
            StatType preserveChanceStatType = StatType.Invalid)
        {
            ItemResref = itemResref;
            Quantity = Math.Max(1, quantity);
            PreserveChanceStatType = preserveChanceStatType;
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
            if (PreserveChanceStatType == StatType.Invalid)
                return false;

            var chance = Stat.GetStatAdjustment(player, PreserveChanceStatType);
            return chance > 0 && Random.D100(1) <= chance;
        }
    }
}
