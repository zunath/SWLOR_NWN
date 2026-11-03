using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Credits owed to players who may be offline: a tournament prize, or an entry fee returned when a tournament is
    /// cancelled or its city is removed. A debt is a <see cref="PendingCredit"/> record. It is paid at the player's next
    /// login, or at once when they are online. The credits are saved in the character file, with a mark naming the records
    /// they paid, before the records are deleted; a restart in between finds the mark at the next login and deletes those
    /// records instead of paying them again, as <see cref="OverflowStorage"/> does for items.
    /// </summary>
    public static class PendingCredits
    {
        private const string DeliveredVariable = "PENDING_CREDITS_DELIVERED";
        private const char IdSeparator = ',';

        /// <summary>The client needs a moment after entering before chat messages show.</summary>
        private const float DeliveryDelaySeconds = 5f;

        /// <summary>
        /// Records a debt. Writing the same id again leaves one record, so settling a tournament twice after a restart
        /// cannot owe twice.
        /// </summary>
        /// <returns>False when nothing is owed or the player is unknown.</returns>
        public static bool Add(string id, string playerId, long credits, string reason)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(playerId) || credits <= 0 || credits > int.MaxValue)
                return false;

            DB.Set(new PendingCredit
            {
                Id = id,
                PlayerId = playerId,
                Credits = (int)credits,
                Reason = reason ?? string.Empty,
                CreatedUtc = DateTime.UtcNow
            });
            return true;
        }

        public static long GetTotal(string playerId)
        {
            return Load(playerId).Sum(x => (long)x.Credits);
        }

        /// <summary>Pays a player's debts now when they are online. Offline players are paid at their next login.</summary>
        public static void DeliverIfOnline(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            var player = GetObjectByUUID(playerId);
            if (GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player))
                Deliver(player);
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void DeliverOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            // The records a restart left behind after their credits were already saved are dropped before anything is paid.
            DeleteDelivered(player);
            DelayCommand(DeliveryDelaySeconds, () =>
            {
                if (GetIsObjectValid(player))
                    Deliver(player);
            });
        }

        private static void Deliver(uint player)
        {
            var records = Load(GetObjectUUID(player));
            if (records.Count == 0)
                return;

            var total = records.Sum(x => (long)x.Credits);
            if (total > int.MaxValue)
            {
                // More than a character can be handed at once: pay what fits and leave the rest for the next login.
                var batch = new List<PendingCredit>();
                long running = 0;
                foreach (var record in records)
                {
                    if (running + record.Credits > int.MaxValue)
                        break;

                    batch.Add(record);
                    running += record.Credits;
                }

                records = batch;
                total = running;
                if (records.Count == 0)
                    return;
            }

            GiveGoldToCreature(player, (int)total);

            // The credits and the mark reach the character file before the records go, so a restart cannot pay twice.
            ObjectPlugin.SetString(player, DeliveredVariable, string.Join(IdSeparator, records.Select(x => x.Id)), true);
            CharacterCheckpoint.Save(player);

            foreach (var record in records)
            {
                DB.Delete<PendingCredit>(record.Id);
                Log.Write(LogGroup.Property, $"Pending credits paid: {PlayerName.GetAuditName(player)} - {record.Credits} credits - {record.Reason} ({record.Id})");
            }

            ObjectPlugin.DeleteString(player, DeliveredVariable);
            CharacterCheckpoint.Save(player);

            foreach (var record in records)
            {
                SendMessageToPC(player, ColorToken.Green($"{record.Reason} ({record.Credits} credits)"));
            }
        }

        /// <summary>
        /// Deletes the records the character file already paid, found by the mark the last delivery left, and clears the
        /// mark.
        /// </summary>
        private static void DeleteDelivered(uint player)
        {
            var mark = ObjectPlugin.GetString(player, DeliveredVariable);
            if (string.IsNullOrWhiteSpace(mark))
                return;

            foreach (var id in mark.Split(IdSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                DB.Delete<PendingCredit>(id);
            }

            ObjectPlugin.DeleteString(player, DeliveredVariable);
            CharacterCheckpoint.Save(player);
        }

        private static List<PendingCredit> Load(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return new List<PendingCredit>();

            var query = new DBQuery<PendingCredit>()
                .AddFieldSearch(nameof(PendingCredit.PlayerId), playerId, false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<PendingCredit>();

            return DB.Search(query.AddPaging(count, 0))
                .Where(x => x != null)
                .OrderBy(x => x.CreatedUtc)
                .ToList();
        }
    }
}
