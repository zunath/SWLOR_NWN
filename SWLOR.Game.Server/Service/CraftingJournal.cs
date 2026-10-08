using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class CraftingJournal
    {
        public const string ConsumedItemVariable = "CRAFTING_CONSUMED";
        private const string CommitVariable = "CRAFTING_COMMIT";
        private const string DeliveryVariable = "CRAFTING_DELIVERY";
        public static CraftingTransaction Get(uint player) => DB.Get<Player>(GetObjectUUID(player))?.PendingCraft;
        public static List<uint> Inventory(uint player)
        {
            var result = new List<uint>();
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player)) result.Add(item);
            return result;
        }
        public static void RemoveConsumedItems(uint player)
        {
            foreach (var item in Inventory(player))
                if (!string.IsNullOrWhiteSpace(GetLocalString(item, ConsumedItemVariable))) DestroyObject(item);
        }
        public static void Save(uint player, CraftingTransaction transaction)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer.PendingCraft != null && dbPlayer.PendingCraft.Id != transaction.Id)
                throw new InvalidOperationException("A previous crafting transaction still needs recovery.");
            dbPlayer.PendingCraft = transaction;
            DB.Set(dbPlayer);
        }
        public static string SerializeQuantity(uint item, int quantity)
        {
            // NWN CopyItem can merge with the source stack. Deserialize creates an isolated object outside the world.
            var copy = ObjectPlugin.Deserialize(ObjectPlugin.Serialize(item));
            if (!GetIsObjectValid(copy)) throw new InvalidOperationException("Unable to preserve the crafting item.");
            SetItemStackSize(copy, quantity);
            try { return ObjectPlugin.Serialize(copy); }
            finally { DestroyObject(copy); }
        }
        public static CraftingTransaction Begin(uint player, RecipeType recipe, CraftSession session,
            IReadOnlyList<CraftComponentReservation> reservations, IEnumerable<string> components, IEnumerable<string> enhancements, uint blueprint)
        {
            if (Get(player) != null) throw new InvalidOperationException("Recover the previous craft before starting another.");
            var hasBlueprint = GetIsObjectValid(blueprint) && Craft.GetBlueprintDetails(blueprint).Recipe != RecipeType.Invalid;
            if (hasBlueprint && (GetItemPossessor(blueprint) != player || Craft.GetBlueprintDetails(blueprint).Recipe != recipe ||
                Craft.GetBlueprintDetails(blueprint).LicensedRuns <= 0 || GetGold(player) < Craft.CalculateBlueprintCraftCreditCost(blueprint)))
                throw new InvalidOperationException("The selected blueprint or its crafting payment is unavailable.");
            var transaction = new CraftingTransaction
            {
                Id = session.Id, Recipe = recipe, Session = session, RecipeRewards = CraftRecipeRewards.Capture(Craft.GetRecipe(recipe), session.SkillRank),
                Components = components.ToList(), Enhancements = enhancements.ToList(),
                Debits = reservations.GroupBy(entry => entry.Item).Select(group => new CraftingDebit(
                    GetObjectUUID(group.Key), GetResRef(group.Key), GetItemStackSize(group.Key), group.Sum(entry => entry.Quantity))).ToList(),
                GoldBefore = GetGold(player), CreditCost = hasBlueprint ? Craft.CalculateBlueprintCraftCreditCost(blueprint) : 0,
                BlueprintItemId = hasBlueprint ? GetObjectUUID(blueprint) : null,
                BlueprintData = hasBlueprint ? ObjectPlugin.Serialize(blueprint) : null,
                LicensedRunsBefore = hasBlueprint ? Craft.GetBlueprintDetails(blueprint).LicensedRuns : 0
            };
            // Save stable inventory UUIDs before publishing a write-ahead receipt.
            ExportSingleCharacter(player);
            Save(player, transaction);
            CompletePreparation(player, transaction);
            return transaction;
        }
        public static void CompletePreparation(uint player, CraftingTransaction transaction)
        {
            if (transaction.Phase == CraftingTransactionPhase.RewardsReady) return;
            if (transaction.Phase == CraftingTransactionPhase.Committed && GetLocalString(player, CommitVariable) == transaction.Id.ToString("N"))
            { RemoveConsumedItems(player); return; }
            var inventory = Inventory(player).ToDictionary(GetObjectUUID);
            // Validate every stack and payment first; recovery never guesses about unexpected inventory.
            foreach (var debit in transaction.Debits)
            {
                inventory.TryGetValue(debit.ItemId, out var item);
                var exists = item != 0 && GetIsObjectValid(item);
                if (exists && GetResRef(item) != debit.Resref) throw new InvalidOperationException("Reserved crafting item identity changed.");
                debit.NeedsDebit(exists ? GetItemStackSize(item) : null, exists && GetLocalString(item, ConsumedItemVariable) == transaction.Id.ToString("N"));
            }
            uint blueprint = OBJECT_INVALID;
            if (!string.IsNullOrWhiteSpace(transaction.BlueprintItemId))
            {
                if (!inventory.TryGetValue(transaction.BlueprintItemId, out blueprint))
                {
                    if (transaction.LicensedRunsBefore != 1) throw new InvalidOperationException("Reserved blueprint is unavailable.");
                    blueprint = OBJECT_INVALID;
                }
                else if (GetLocalString(blueprint, ConsumedItemVariable) != transaction.Id.ToString("N"))
                {
                    var runs = Craft.GetBlueprintDetails(blueprint).LicensedRuns;
                    if (runs != transaction.LicensedRunsBefore && runs != transaction.LicensedRunsBefore - 1) throw new InvalidOperationException("Reserved blueprint license changed unexpectedly.");
                }
            }
            if (transaction.CreditCost > 0 && GetGold(player) != transaction.GoldBefore && GetGold(player) != transaction.GoldBefore - transaction.CreditCost)
                throw new InvalidOperationException("Crafting credit payment needs staff review.");
            foreach (var debit in transaction.Debits)
            {
                inventory.TryGetValue(debit.ItemId, out var item);
                var exists = item != 0 && GetIsObjectValid(item);
                if (!debit.NeedsDebit(exists ? GetItemStackSize(item) : null, exists && GetLocalString(item, ConsumedItemVariable) == transaction.Id.ToString("N")))
                {
                    if (exists && debit.RemainingQuantity == 0) DestroyObject(item);
                    continue;
                }
                if (debit.RemainingQuantity > 0) SetItemStackSize(item, debit.RemainingQuantity);
                else
                {
                    // DestroyObject can complete after this script. Its exported marker prevents a vault rollback from returning spent material.
                    SetLocalString(item, ConsumedItemVariable, transaction.Id.ToString("N"));
                    DestroyObject(item);
                }
            }
            if (transaction.CreditCost > 0 && GetGold(player) == transaction.GoldBefore) TakeGoldFromCreature(transaction.CreditCost, player, true);
            if (GetIsObjectValid(blueprint))
            {
                var detail = Craft.GetBlueprintDetails(blueprint);
                if (GetLocalString(blueprint, ConsumedItemVariable) == transaction.Id.ToString("N")) DestroyObject(blueprint);
                else if (detail.LicensedRuns == transaction.LicensedRunsBefore)
                {
                    if (detail.LicensedRuns == 1) SetLocalString(blueprint, ConsumedItemVariable, transaction.Id.ToString("N"));
                    detail.LicensedRuns--; Craft.SetBlueprintDetails(blueprint, detail);
                }
            }
            SetLocalString(player, CommitVariable, transaction.Id.ToString("N"));
            ExportSingleCharacter(player);
            transaction.Phase = CraftingTransactionPhase.Committed;
            Save(player, transaction);
        }
        public static void SaveSession(uint player, CraftSession session, CraftActionRecord action = null)
        {
            var transaction = Get(player);
            if (transaction == null || transaction.Id != session.Id) throw new InvalidOperationException("Crafting session receipt is missing.");
            if (action != null) transaction.Actions.Add(action);
            transaction.Session = session; Save(player, transaction);
        }
        public static void PrepareRewards(uint player, CraftingTransaction transaction, IEnumerable<string> data, int xp, bool firstCraft)
        {
            if (transaction.Phase == CraftingTransactionPhase.RewardsReady) return;
            transaction.Deliveries = data.Select((serialized, index) => new CraftingDelivery
                { Id = $"{transaction.Id:N}:{index}", Data = serialized }).ToList();
            transaction.XP = xp; transaction.FirstCraft = firstCraft;
            transaction.Phase = CraftingTransactionPhase.RewardsReady;
            Save(player, transaction);
        }
        public static bool DeliverRewards(uint player, CraftingTransaction transaction)
        {
            if (transaction.Phase != CraftingTransactionPhase.RewardsReady) throw new InvalidOperationException("Crafting rewards are not ready.");
            foreach (var delivery in transaction.Deliveries.Where(entry => !entry.Delivered))
            {
                var existing = Inventory(player).FirstOrDefault(item => GetLocalString(item, DeliveryVariable) == delivery.Id);
                if (existing == 0)
                {
                    var item = ObjectPlugin.Deserialize(delivery.Data);
                    if (!GetIsObjectValid(item)) throw new InvalidOperationException("Unable to restore a crafting reward.");
                    SetLocalString(item, DeliveryVariable, delivery.Id);
                    if (!ObjectPlugin.AcquireItem(player, item))
                    { DestroyObject(item); SendMessageToPC(player, "Crafting rewards are saved. Make room in your inventory, then reopen crafting to collect them."); return false; }
                }
                // Vault delivery is saved before the database acknowledgement. A replay finds the item marker.
                ExportSingleCharacter(player);
                delivery.Delivered = true; Save(player, transaction);
            }
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (transaction.FirstCraft && !dbPlayer.CraftedRecipes.ContainsKey(transaction.Recipe))
            { dbPlayer.CraftedRecipes[transaction.Recipe] = DateTime.UtcNow; DB.Set(dbPlayer); }
            if (!transaction.XPAwarded)
            {
                var awardId = $"craft:{transaction.Id:N}";
                if (transaction.XP > 0) Skill.GiveSkillXP(player, (transaction.RecipeRewards?.Skill ?? Craft.GetRecipe(transaction.Recipe).Skill), transaction.XP, false, false, awardId);
                dbPlayer = DB.Get<Player>(GetObjectUUID(player));
                transaction.XPAwarded = transaction.XP <= 0 || dbPlayer.SkillXPAwards.Contains(awardId);
                Save(player, transaction);
            }
            if (!transaction.XPAwarded) return false;
            dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            dbPlayer.PendingCraft = null; DB.Set(dbPlayer);
            Log.WriteStructured(LogGroup.Crafting,
                "Crafting settled: PlayerId={PlayerId} SessionId={SessionId} Recipe={Recipe} Rules={Rules} Profile={Profile} Status={Status} Actions={Actions} Quality={Quality} MaxQuality={MaxQuality} XP={XP} Seconds={Seconds} Trace={Trace} PerkTriggers={PerkTriggers}",
                dbPlayer.Id, transaction.Id, transaction.Recipe, transaction.Session.RulesVersion, transaction.Session.Profile,
                transaction.Session.Status, transaction.Session.ActionCount, transaction.Session.Quality, transaction.Session.MaxQuality, transaction.XP,
                (DateTime.UtcNow - transaction.StartedAt).TotalSeconds, Newtonsoft.Json.JsonConvert.SerializeObject(transaction.Actions),
                Newtonsoft.Json.JsonConvert.SerializeObject(transaction.Session.TriggerCounts));
            return true;
        }
    }
}
