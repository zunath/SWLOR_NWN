using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TransferService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Moves value between a character's inventory and a database record without a restart duplicating or losing it (Player City Revamp,
    /// farming and the Civic Exchange). The inventory is in the character file, which saves about once a minute, and the record is in the
    /// database, which saves at once, so a server stop between the two leaves the items in both places or neither. A transfer is journaled
    /// the way a City Project donation is (<see cref="CityProjectDonations"/>): an entry (<see cref="TransferEntry"/>) is written, the
    /// character file is saved with a mark naming it, the database side applies the change with a receipt, and the entry and the mark
    /// go. The order of the steps and the decision for each restart point are <see cref="TransferJournalProcess"/> and
    /// <see cref="TransferJournalRules"/>; the feature supplies a <see cref="TransferHandler"/> for the database side; this class reads
    /// and writes the entries, the inventory and the mark.
    /// <para>
    /// A restart is settled at the character's next login: an entry with no mark never left (or never entered) the character file and is
    /// dropped; one the database already holds is cleared; one a deposit's target has not yet taken is applied, or returned to the
    /// character's overflow store when the target cannot take it; one a withdrawal's database already made and the file did not save is
    /// delivered to overflow; a mark with no entry is cleared. The entries of characters who are offline wait for their login, since a
    /// character file can only be read once the character is loaded.
    /// </para>
    /// <para>
    /// Where the database side only consumes what the character gives (a meal, a seed) and losing it in the instant between the save and
    /// the write is harmless, <see cref="TakeAndSave"/> is enough: the items are taken and the character file is saved before the
    /// database is written, so a restart can lose them but never copy them.
    /// </para>
    /// </summary>
    public static class TransferJournal
    {
        /// <summary>The character-file local naming the entries whose items the file has already given up or received, as comma-separated entry ids.</summary>
        private const string MarkVariable = "CITY_TRANSFERS";

        /// <summary>The client needs a moment after entering before chat messages show.</summary>
        private const float MessageDelaySeconds = 6f;

        private static Dictionary<string, TransferHandler> _handlers;

        #region Handlers

        /// <summary>Every feature's handler, by kind.</summary>
        private static Dictionary<string, TransferHandler> Handlers => _handlers ??= new[]
        {
            Farmhands.FeedLoadTransfer,
            Farmhands.HopperUnloadTransfer,
            Farmhands.ReleaseTransfer,
            Corrals.PlaceTransfer,
            Corrals.CollectTransfer,
            Corrals.RemoveAnimalTransfer,
            Farming.RemoveCropTransfer,
            FermentationVats.StartTransfer,
            FermentationVats.CollectTransfer,
            CivicExchange.CrateTransfer
        }.ToDictionary(handler => handler.Kind);

        private static TransferHandler GetHandler(string kind)
        {
            return Handlers.GetValueOrDefault(kind);
        }

        #endregion

        #region Transfers

        /// <summary>
        /// A deposit: takes the entry's items from the player and has the feature's handler give them to the database record. The
        /// player's overflow store gets them instead, with a message, when the record can no longer take them.
        /// </summary>
        /// <param name="heldItems">The objects to take for each <see cref="TransferItem"/> that carries <c>Data</c>, in order.</param>
        /// <returns><see cref="TransferSettlement.Declined"/> when nothing was changed because the character file could not be saved.</returns>
        public static TransferSettlement Deposit(uint player, TransferEntry entry, params uint[] heldItems)
        {
            var settlement = TransferJournalProcess.Deposit(new Store(player, heldItems), entry);
            Report(player, entry, settlement);
            return settlement;
        }

        /// <summary>
        /// A withdrawal: has the feature's handler take the change out of the database record and gives the entry's items to the player.
        /// What does not fit in their inventory goes to their overflow store.
        /// </summary>
        public static TransferSettlement Withdraw(uint player, TransferEntry entry)
        {
            return Withdraw(player, entry, out _);
        }

        /// <param name="overflowByItem">For each of the entry's items, in order, how many went to the player's overflow store instead of their inventory.</param>
        public static TransferSettlement Withdraw(uint player, TransferEntry entry, out int[] overflowByItem)
        {
            var store = new Store(player, new uint[0], entry.Items.Count);
            var settlement = TransferJournalProcess.Withdraw(store, entry);
            overflowByItem = settlement == TransferSettlement.ToOverflow ? entry.Items.Select(item => item.Count).ToArray() : store.OverflowByItem;
            Report(player, entry, settlement);
            return settlement;
        }

        /// <summary>
        /// Takes items from the player and saves the character file, for a database side that only consumes them. False, having taken
        /// nothing, when the player does not carry them all or the file could not be saved.
        /// </summary>
        public static bool TakeAndSave(uint player, params (string Resref, int Count)[] items)
        {
            var needed = items.Where(item => item.Count > 0 && !string.IsNullOrWhiteSpace(item.Resref)).ToList();
            foreach (var group in needed.GroupBy(item => item.Resref))
            {
                if (FarmItems.Count(player, group.Key) < group.Sum(item => item.Count))
                    return false;
            }

            foreach (var (resref, count) in needed)
            {
                FarmItems.Take(player, resref, count);
            }

            if (CharacterCheckpoint.Save(player))
                return true;

            var location = GetLocation(player);
            var playerId = GetObjectUUID(player);
            foreach (var (resref, count) in needed)
            {
                FarmItems.Give(player, playerId, resref, count, location);
            }

            return false;
        }

        private static void Report(uint player, TransferEntry entry, TransferSettlement settlement)
        {
            if (settlement != TransferSettlement.ToOverflow || !GetIsObjectValid(player))
                return;

            SendMessageToPC(player, ColorToken.Red(TransferJournalRules.DescribeOverflow(entry)));
        }

        #endregion

        #region Login

        /// <summary>Settles the transfers a restart interrupted, before anything else the character does.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var marks = TransferJournalRules.ParseMarks(ObjectPlugin.GetString(player, MarkVariable));
            var entries = Load(GetObjectUUID(player));
            if (marks.Count == 0 && entries.Count == 0)
                return;

            try
            {
                var outcomes = TransferJournalProcess.SettleAtLogin(new Store(player, new uint[0]), marks, entries);
                foreach (var (entry, settlement) in outcomes)
                {
                    Tell(player, entry, settlement);
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Transfers of player [{GetObjectUUID(player)}] could not be settled at login: {ex}");
            }
        }

        private static void Tell(uint player, TransferEntry entry, TransferSettlement settlement)
        {
            if (settlement != TransferSettlement.ToOverflow)
                return;

            var text = ColorToken.Red(TransferJournalRules.DescribeOverflow(entry));
            DelayCommand(MessageDelaySeconds, () =>
            {
                if (GetIsObjectValid(player))
                    SendMessageToPC(player, text);
            });
        }

        #endregion

        #region Records

        /// <summary>A character's entries that a handler exists for, oldest first. An entry of a kind no handler knows is left alone and logged.</summary>
        private static List<TransferEntry> Load(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return new List<TransferEntry>();

            var query = new DBQuery<TransferEntry>().AddFieldSearch(nameof(TransferEntry.PlayerId), playerId, false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return new List<TransferEntry>();

            var entries = DB.Search(query.AddPaging(count, 0)).Where(x => x != null).OrderBy(x => x.CreatedUtc).ToList();
            foreach (var unknown in entries.Where(entry => GetHandler(entry.Kind) == null).ToList())
            {
                Log.Write(LogGroup.Error, $"Transfer entry {unknown.Id} of player [{playerId}] has the unknown kind '{unknown.Kind}', so it is left as it is.");
                entries.Remove(unknown);
            }

            return entries;
        }

        #endregion

        /// <summary>The entries, the character and the feature's database side behind the steps <see cref="TransferJournalProcess"/> orders.</summary>
        private sealed class Store : ITransferStore
        {
            private readonly uint _player;
            private readonly Queue<uint> _held;

            /// <summary>For each item of a withdrawal, how many went into the overflow store instead of the inventory.</summary>
            public int[] OverflowByItem { get; }

            public Store(uint player, IEnumerable<uint> heldItems, int itemCount = 0)
            {
                _player = player;
                _held = new Queue<uint>(heldItems);
                OverflowByItem = new int[itemCount];
            }

            public void SavePending(TransferEntry entry)
            {
                DB.Set(entry);
            }

            public void DeletePending(string entryId)
            {
                DB.Delete<TransferEntry>(entryId);
            }

            public bool TakeAndMark(TransferEntry entry)
            {
                if (!GetIsObjectValid(_player))
                    return false;

                // Everything is checked before anything is taken, so a refusal changes nothing.
                foreach (var group in entry.Items.Where(item => string.IsNullOrEmpty(item.Data)).GroupBy(item => item.Resref))
                {
                    if (FarmItems.Count(_player, group.Key) < group.Sum(item => item.Count))
                        return false;
                }

                var held = new List<uint>();
                foreach (var _ in entry.Items.Where(item => !string.IsNullOrEmpty(item.Data)))
                {
                    var item = _held.Count > 0 ? _held.Dequeue() : OBJECT_INVALID;
                    if (!GetIsObjectValid(item) || GetItemPossessor(item) != _player)
                        return false;

                    held.Add(item);
                }

                foreach (var item in entry.Items.Where(item => string.IsNullOrEmpty(item.Data)))
                {
                    FarmItems.Take(_player, item.Resref, item.Count);
                }

                foreach (var item in held)
                {
                    UseUpOne(item);
                }

                ObjectPlugin.SetString(_player, MarkVariable, TransferJournalRules.AddMark(ObjectPlugin.GetString(_player, MarkVariable), entry.Id), true);
                if (CharacterCheckpoint.Save(_player))
                    return true;

                // The file could not be saved: everything is put back and unmarked, so the action simply did not happen.
                SetMark(TransferJournalRules.RemoveMark(ObjectPlugin.GetString(_player, MarkVariable), entry.Id));
                Restore(entry);
                return false;
            }

            public bool GiveAndMark(TransferEntry entry)
            {
                var placedPlain = new List<(string Resref, int Count)>();
                var placedHeld = new List<uint>();
                for (var index = 0; index < entry.Items.Count; index++)
                {
                    var item = entry.Items[index];
                    if (!string.IsNullOrEmpty(item.Data))
                    {
                        var created = ObjectPlugin.Deserialize(item.Data);
                        if (GetIsObjectValid(created) && Item.TryAcquireItem(_player, created))
                            placedHeld.Add(created);
                        else
                        {
                            StoreHeldInOverflow(entry, index);
                            OverflowByItem[index]++;
                        }

                        continue;
                    }

                    // The part that does not fit is written to overflow before the file is saved, so a restart between the two
                    // can only make the settling write the same records again.
                    var placed = FarmItems.GiveToInventory(_player, item.Resref, item.Count);
                    if (placed > 0)
                        placedPlain.Add((item.Resref, placed));
                    if (placed < item.Count)
                    {
                        WriteOverflow(entry, index, item.Count - placed);
                        OverflowByItem[index] += item.Count - placed;
                    }
                }

                ObjectPlugin.SetString(_player, MarkVariable, TransferJournalRules.AddMark(ObjectPlugin.GetString(_player, MarkVariable), entry.Id), true);
                if (CharacterCheckpoint.Save(_player))
                    return true;

                // The file could not be saved: what was given comes out again, and everything goes to overflow instead.
                SetMark(TransferJournalRules.RemoveMark(ObjectPlugin.GetString(_player, MarkVariable), entry.Id));
                foreach (var (resref, count) in placedPlain)
                {
                    FarmItems.Take(_player, resref, count);
                }

                foreach (var item in placedHeld.Where(GetIsObjectValid))
                {
                    DestroyObject(item);
                }

                return false;
            }

            public void ClearMark(string entryId)
            {
                // The next save carries the cleared mark. A restart before then leaves a mark with no entry, which the next login clears.
                SetMark(TransferJournalRules.RemoveMark(ObjectPlugin.GetString(_player, MarkVariable), entryId));
            }

            public void MarkApplied(TransferEntry entry)
            {
                entry.AppliedUtc = DateTime.UtcNow;
                DB.Set(entry);
            }

            public bool IsApplied(TransferEntry entry)
            {
                return GetHandler(entry.Kind).IsApplied(entry);
            }

            public bool CanApply(TransferEntry entry)
            {
                return GetHandler(entry.Kind).CanApply(entry);
            }

            public void Apply(TransferEntry entry)
            {
                GetHandler(entry.Kind).Apply(entry);
                Log.Write(LogGroup.Property, $"Transfer {entry.Id} ({entry.Kind}, {entry.Direction}) of player {entry.PlayerId} applied to {entry.TargetId}: {Describe(entry)}.");
            }

            public void ToOverflow(TransferEntry entry)
            {
                for (var index = 0; index < entry.Items.Count; index++)
                {
                    var item = entry.Items[index];
                    if (!string.IsNullOrEmpty(item.Data))
                        StoreHeldInOverflow(entry, index);
                    else
                        WriteOverflow(entry, index, item.Count);
                }

                Log.Write(LogGroup.Property, $"Transfer {entry.Id} ({entry.Kind}, {entry.Direction}) of player {entry.PlayerId} went to overflow storage: {Describe(entry)}.");
            }

            private void SetMark(string mark)
            {
                if (string.IsNullOrEmpty(mark))
                    ObjectPlugin.DeleteString(_player, MarkVariable);
                else
                    ObjectPlugin.SetString(_player, MarkVariable, mark, true);
            }

            /// <summary>Gives the entry's items back to the player after a failed save, into the inventory and then overflow.</summary>
            private void Restore(TransferEntry entry)
            {
                var location = GetLocation(_player);
                for (var index = 0; index < entry.Items.Count; index++)
                {
                    var item = entry.Items[index];
                    if (!string.IsNullOrEmpty(item.Data))
                    {
                        var created = ObjectPlugin.Deserialize(item.Data);
                        if (GetIsObjectValid(created) && !Item.TryAcquireItem(_player, created))
                            StoreHeldInOverflow(entry, index);
                    }
                    else
                    {
                        FarmItems.Give(_player, entry.PlayerId, item.Resref, item.Count, location);
                    }
                }
            }

            /// <summary>Writes <paramref name="count"/> of a plain item to overflow under the ids the item always has, so a repeat writes the same records.</summary>
            private static void WriteOverflow(TransferEntry entry, int index, int count)
            {
                CityProjects.WriteOverflow(
                    entry.PlayerId,
                    entry.Items[index].Resref,
                    count,
                    chunk => TransferJournalRules.GetOverflowRecordId(entry.Id, index, chunk),
                    $"Transfer {entry.Id}");
            }

            private static void StoreHeldInOverflow(TransferEntry entry, int index)
            {
                var created = ObjectPlugin.Deserialize(entry.Items[index].Data);
                if (!GetIsObjectValid(created))
                {
                    Log.Write(LogGroup.Error, $"Transfer {entry.Id}: item {entry.Items[index].Resref} could not be restored, so it is lost.");
                    return;
                }

                FarmItems.StoreInOverflow(entry.PlayerId, created, TransferJournalRules.GetOverflowRecordId(entry.Id, index, 0));
            }

            private static void UseUpOne(uint item)
            {
                var size = GetItemStackSize(item);
                if (size > 1)
                    SetItemStackSize(item, size - 1);
                else
                    DestroyObject(item);
            }

            private static string Describe(TransferEntry entry)
            {
                return string.Join(", ", entry.Items.Select(item => $"{item.Count} x {item.Resref}"));
            }
        }
    }
}
