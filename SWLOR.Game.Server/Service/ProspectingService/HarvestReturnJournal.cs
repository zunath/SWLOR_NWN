using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// Keeps a Prospecting payout recoverable until the character file holds it. The node is saved depleted at once, but the units go to
    /// the inventory, which reaches the character file only on its next export. Each payout's units are written to
    /// <see cref="Player.PendingHarvestReturns"/> with the next craft file sequence, together with its Gathering XP as waiting craft XP, in one
    /// record save made before the node is reduced. A stop at any point after it cannot lose the units or the XP: a login whose file
    /// predates the entry gives the units again, a file at or above it drops the entry, and the waiting XP is given at login like any
    /// craft XP. A unit with no room in the inventory is taken off the entry before it is placed at the player's feet, since no file holds it either way. Entries an
    /// export has covered are also dropped while playing.
    /// </summary>
    public static class HarvestReturnJournal
    {
        /// <summary>
        /// Adds a payout at <paramref name="sequence"/>, the craft file sequence just advanced for it, first forgetting the entries the
        /// last export (<paramref name="exported"/>) has put in the character file. The caller saves the record before reducing the node.
        /// </summary>
        public static void Record(Player dbPlayer, int exported, int sequence, IEnumerable<string> resrefs)
        {
            dbPlayer.PendingHarvestReturns ??= new List<PendingHarvestReturn>();
            dbPlayer.PendingHarvestReturns.RemoveAll(entry => entry.Sequence <= exported);

            var items = resrefs
                .GroupBy(resref => resref)
                .ToDictionary(group => group.Key, group => group.Count());
            if (items.Count > 0)
                dbPlayer.PendingHarvestReturns.Add(new PendingHarvestReturn { Sequence = sequence, Items = items });
        }

        /// <summary>Takes units that did not reach the inventory off the entry at <paramref name="sequence"/>, dropping the entry once it is empty.</summary>
        public static void Remove(Player dbPlayer, int sequence, IEnumerable<string> resrefs)
        {
            var entry = dbPlayer.PendingHarvestReturns?.FirstOrDefault(pending => pending.Sequence == sequence);
            if (entry == null)
                return;

            foreach (var resref in resrefs)
            {
                if (!entry.Items.TryGetValue(resref, out var quantity))
                    continue;

                if (quantity > 1)
                    entry.Items[resref] = quantity - 1;
                else
                    entry.Items.Remove(resref);
            }

            if (entry.Items.Count == 0)
                dbPlayer.PendingHarvestReturns.Remove(entry);
        }

        /// <summary>
        /// At login: empties the journal and returns the units of every payout the loaded character file (<paramref name="loaded"/>)
        /// predates, which the player must be given again. The caller journals the units it gives again in the same record save that keeps
        /// this clearing, so no save leaves them unprotected. Returns null when the journal was already empty, so nothing needs saving.
        /// </summary>
        public static Dictionary<string, int> TakeAtLogin(Player dbPlayer, int loaded)
        {
            var pending = dbPlayer.PendingHarvestReturns;
            if (pending == null || pending.Count == 0)
                return null;

            var redo = new Dictionary<string, int>();
            foreach (var (resref, quantity) in pending.Where(entry => entry.Sequence > loaded).SelectMany(entry => entry.Items))
                redo[resref] = redo.GetValueOrDefault(resref) + quantity;

            pending.Clear();
            return redo;
        }
    }
}
