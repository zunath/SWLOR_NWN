using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>
    /// Keeps a Prospecting payout recoverable until the character file holds it. The node is saved depleted and the Gathering XP is
    /// saved on the player record at once, but the units go to the inventory, which reaches the character file only on its next export.
    /// Each payout is written to <see cref="Player.PendingHarvestReturns"/> with the next craft file sequence before its units are
    /// delivered, so a crash before the export cannot keep the node's depletion and the XP while losing the units: a login whose file
    /// predates the entry gives them again, and a file at or above it drops the entry. Entries an export has covered are also dropped
    /// while playing.
    /// </summary>
    public static class HarvestReturnJournal
    {
        /// <summary>
        /// Adds a payout at <paramref name="sequence"/>, the craft file sequence just advanced for it, first forgetting the entries the
        /// last export (<paramref name="exported"/>) has put in the character file. The caller saves the record before delivering.
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

        /// <summary>
        /// At login: empties the journal and returns the units of every payout the loaded character file (<paramref name="loaded"/>)
        /// predates, which the player must be given again. Returns null when the journal was already empty, so nothing needs saving.
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
