using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    public enum CraftingTransactionPhase { Preparing, Committed, RewardsReady }
    public sealed record CraftingDebit(string ItemId, string Resref, int OriginalQuantity, int Quantity)
    {
        public int RemainingQuantity => OriginalQuantity - Quantity;
        public bool NeedsDebit(int? currentQuantity, bool markedConsumed = false)
        {
            if (Quantity <= 0 || OriginalQuantity < Quantity) throw new InvalidOperationException("Invalid crafting reservation.");
            if (markedConsumed && RemainingQuantity == 0 || currentQuantity == RemainingQuantity || currentQuantity == null && RemainingQuantity == 0) return false;
            if (currentQuantity == OriginalQuantity) return true;
            throw new InvalidOperationException("The reserved crafting stack changed unexpectedly. Recovery requires staff review.");
        }
    }
    public sealed class CraftingDelivery
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public bool Delivered { get; set; }
    }
    public sealed record CraftActionRecord(int Revision, CraftActionType Action, bool Succeeded,
        CraftCondition Condition, CraftCondition NextCondition, int CPSpent, int CPRestored, int DurabilitySpent, int DurabilityRestored,
        int Progress, int Quality);
    public sealed class CraftingTransaction
    {
        public Guid Id { get; set; }
        public RecipeType Recipe { get; set; }
        public CraftSession Session { get; set; }
        public List<CraftActionRecord> Actions { get; set; } = new();
        public CraftingTransactionPhase Phase { get; set; }
        public List<CraftingDebit> Debits { get; set; } = new();
        public List<string> Components { get; set; } = new();
        public List<string> Enhancements { get; set; } = new();
        public string BlueprintItemId { get; set; }
        public string BlueprintData { get; set; }
        public int LicensedRunsBefore { get; set; }
        public int GoldBefore { get; set; }
        public int CreditCost { get; set; }
        public List<CraftingDelivery> Deliveries { get; set; } = new();
        public int XP { get; set; }
        public bool FirstCraft { get; set; }
        public bool XPAwarded { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    }
}
