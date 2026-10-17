using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CraftService
{
    public static class CraftedFoodDeliveryService
    {
        /// <summary>Stores a fully configured food batch before any portion reaches the player.</summary>
        public static string CommitCompletedBatch(uint player, IReadOnlyList<uint> configuredOutputItems) =>
            CommitCompletedBatch(player, configuredOutputItems, () => SaveCharacter(player), DB.Set);

        private static string CommitCompletedBatch(uint player, IReadOnlyList<uint> configuredOutputItems,
            Action saveCharacter, Action<CraftedFoodDelivery> saveDelivery)
        {
            ArgumentNullException.ThrowIfNull(configuredOutputItems);
            if (configuredOutputItems.Count == 0 || configuredOutputItems.Distinct().Count() != configuredOutputItems.Count)
                throw new InvalidOperationException("A crafted food batch must contain distinct portions.");
            if (LoadRecovery(player) != null)
                throw new InvalidOperationException("Collect your finished food before crafting another batch.");

            var delivery = new CraftedFoodDelivery { PlayerId = GetObjectUUID(player) };
            foreach (var item in configuredOutputItems)
            {
                if (!GetIsObjectValid(item) || GetTag(item) != "FOOD" || GetItemStackSize(item) != 1)
                    throw new InvalidOperationException("A crafted food portion is not valid.");
                var possessor = GetItemPossessor(item);
                if (!GetIsObjectValid(possessor) || GetObjectType(possessor) != ObjectType.Placeable)
                    throw new InvalidOperationException("A crafted food portion is not in its assembly storage.");

                CraftedFoodSerialization.Prepare(item);
                var data = ObjectPlugin.Serialize(item);
                if (string.IsNullOrWhiteSpace(data))
                    throw new InvalidOperationException("A crafted food portion could not be saved.");
                delivery.Items.Add(data);
            }

            List<CraftedFoodDelivery> existing = null;
            try
            {
                existing = ReadDeliveries(delivery.PlayerId);
            }
            catch (Exception exception)
            {
                // The start-craft guard rejects an unavailable database. Preserve a batch which
                // finished while its database became unavailable instead of dropping it.
                Log.WriteError(exception, "Crafted food delivery lookup failed for completed batch {DeliveryId}", delivery.Id);
            }
            if (existing != null && existing.Any(pending => pending.Items.Count > 0))
                throw new InvalidOperationException("Collect your finished food before crafting another batch.");

            SetLocalString(player, CraftedFoodDelivery.RecoveryVariable, JsonConvert.SerializeObject(delivery));
            try
            {
                // The consumed inputs and this entire batch must reach the same character
                // file before Redis can preserve the output independently of those inputs.
                saveCharacter();
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Crafted food batch {DeliveryId} character recovery save could not be confirmed for {PlayerId}",
                    delivery.Id, delivery.PlayerId);
                SendMessageToPC(player, "Your finished food is held for recovery, but its save could not be confirmed. Please try again before disconnecting.");
                foreach (var item in configuredOutputItems)
                    DestroyObject(item);
                return delivery.Id;
            }

            try
            {
                saveDelivery(delivery);
                DeleteLocalString(player, CraftedFoodDelivery.RecoveryVariable);
            }
            catch (Exception exception)
            {
                // The saved character already holds the depleted inputs and full recovery
                // batch. Retain its stable ID if the database write or acknowledgement fails.
                Log.WriteError(exception, "Crafted food batch {DeliveryId} database save could not be confirmed for {PlayerId}",
                    delivery.Id, delivery.PlayerId);
            }

            foreach (var item in configuredOutputItems)
                DestroyObject(item);
            return delivery.Id;
        }

        public static bool HasPending(uint player) => GetPendingCount(player) > 0;

        public static int GetPendingCount(uint player)
        {
            var recovery = LoadRecovery(player);
            try
            {
                var deliveries = ReadDeliveries(GetObjectUUID(player));
                var count = deliveries.Sum(delivery => delivery.Items.Count);
                if (recovery != null && deliveries.All(delivery => delivery.Id != recovery.Id))
                {
                    var existing = DB.Get<CraftedFoodDelivery>(recovery.Id);
                    if (existing != null && existing.PlayerId != recovery.PlayerId)
                        throw new InvalidOperationException("The crafted food recovery belongs to another player.");
                    count = checked(count + (existing?.Items.Count ?? recovery.Items.Count));
                }
                return count;
            }
            catch (Exception exception) when (recovery != null)
            {
                Log.WriteError(exception, "Crafted food delivery lookup failed; recovery batch {DeliveryId} remains pending", recovery.Id);
                return recovery.Items.Count;
            }
        }

        public static void ClaimPending(uint player) => ClaimPending(player, () => SaveCharacter(player), DB.Set);

        private static void ClaimPending(uint player, Action saveCharacter) =>
            ClaimPending(player, saveCharacter, DB.Set);

        private static void ClaimPending(uint player, Action saveCharacter,
            Action<CraftedFoodDelivery> saveDelivery)
        {
            var playerId = GetObjectUUID(player);
            try
            {
                var recovery = LoadRecovery(player);
                CraftedFoodDelivery recoveredDelivery = null;
                if (recovery != null)
                {
                    var existing = DB.Get<CraftedFoodDelivery>(recovery.Id);
                    if (existing != null && existing.PlayerId != playerId)
                        throw new InvalidOperationException("The crafted food recovery belongs to another player.");
                    if (existing == null)
                    {
                        // This may be a live batch whose initial character save failed.
                        // Never publish or acquire it until the inputs and recovery are saved.
                        saveCharacter();
                        saveDelivery(recovery);
                    }
                    recoveredDelivery = existing ?? recovery;
                    DeleteLocalString(player, CraftedFoodDelivery.RecoveryVariable);
                }

                var deliveries = ReadDeliveries(playerId);
                if (recoveredDelivery != null && deliveries.All(delivery => delivery.Id != recoveredDelivery.Id))
                    deliveries.Insert(0, recoveredDelivery);
                var checkpoint = LoadCheckpoint(player);
                if (checkpoint != null)
                {
                    var checkpointDelivery = DB.Get<CraftedFoodDelivery>(checkpoint.DeliveryId);
                    if (checkpointDelivery == null)
                    {
                        DeleteLocalString(player, CraftedFoodClaim.CheckpointVariable);
                    }
                    else
                    {
                        if (checkpointDelivery.PlayerId != playerId)
                            throw new InvalidOperationException("The crafted food receipt belongs to another player.");
                        deliveries.RemoveAll(delivery => delivery.Id == checkpointDelivery.Id);
                        deliveries.Insert(0, checkpointDelivery);
                    }
                }

                foreach (var delivery in deliveries)
                {
                    var remaining = CraftedFoodClaim.Claim(delivery,
                        () => LoadCheckpoint(player),
                        receipt => SetLocalString(player, CraftedFoodClaim.CheckpointVariable, JsonConvert.SerializeObject(receipt)),
                        data => AcquirePortion(player, data), saveCharacter, saveDelivery);
                    if (remaining.Items.Count == 0)
                        DB.Delete<CraftedFoodDelivery>(remaining.Id);
                    DeleteLocalString(player, CraftedFoodClaim.CheckpointVariable);
                }

                var count = GetPendingCount(player);
                if (count > 0)
                    SendMessageToPC(player, $"{count} finished food portion(s) are waiting for you. Make room and reopen crafting to collect them.");
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Crafted food delivery remains pending for {PlayerId}", playerId);
                SendMessageToPC(player, "Your finished food could not be delivered. Reopen crafting to try again.");
            }
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverPendingFood()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;
            ClaimPending(player);
        }

        private static bool AcquirePortion(uint player, string data)
        {
            var item = ObjectPlugin.Deserialize(data);
            if (GetIsObjectValid(item) && GetTag(item) == "FOOD" && GetItemStackSize(item) == 1 &&
                ItemPlugin.MoveTo(item, player, true) &&
                GetIsObjectValid(item) && GetItemPossessor(item) == player)
                return true;
            if (GetIsObjectValid(item))
                DestroyObject(item);
            return false;
        }

        private static CraftedFoodClaim LoadCheckpoint(uint player)
        {
            var data = GetLocalString(player, CraftedFoodClaim.CheckpointVariable);
            return string.IsNullOrWhiteSpace(data) ? null : JsonConvert.DeserializeObject<CraftedFoodClaim>(data);
        }

        private static CraftedFoodDelivery LoadRecovery(uint player)
        {
            var data = GetLocalString(player, CraftedFoodDelivery.RecoveryVariable);
            if (string.IsNullOrWhiteSpace(data))
                return null;
            var recovery = JsonConvert.DeserializeObject<CraftedFoodDelivery>(data);
            if (recovery == null || string.IsNullOrWhiteSpace(recovery.Id) ||
                recovery.PlayerId != GetObjectUUID(player) || recovery.Items == null || recovery.Items.Count == 0 ||
                recovery.Items.Any(string.IsNullOrWhiteSpace) || recovery.ClaimRevision != 0)
                throw new InvalidOperationException("The crafted food recovery batch is invalid.");
            return recovery;
        }

        private static void SaveCharacter(uint player)
        {
            var client = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByObjectId(player);
            if (client == null || client.SaveServerCharacter() == 0)
                throw new InvalidOperationException("The crafted food character receipt could not be saved.");
        }

        private static List<CraftedFoodDelivery> ReadDeliveries(string playerId)
        {
            var query = new DBQuery<CraftedFoodDelivery>()
                .AddFieldSearch(nameof(CraftedFoodDelivery.PlayerId), playerId, false);
            var count = checked((int)DB.SearchCount(query));
            var deliveries = new List<CraftedFoodDelivery>();
            if (count == 0)
                return deliveries;
            foreach (var data in DB.SearchRawJson(query.AddPaging(count, 0)))
            {
                if (string.IsNullOrWhiteSpace(data))
                    continue;
                var token = JToken.Parse(data);
                if (token is JArray results)
                {
                    if (results.Count == 0)
                        continue;
                    if (results.Count != 1)
                        throw new InvalidOperationException("A crafted food record returned multiple JSON values.");
                    token = results[0];
                }
                if (token.Type == JTokenType.Null)
                    continue;
                var delivery = token.ToObject<CraftedFoodDelivery>();
                if (delivery != null && delivery.PlayerId == playerId)
                    deliveries.Add(delivery);
            }
            return deliveries.OrderBy(delivery => delivery.DateCreated).ToList();
        }
    }
}
