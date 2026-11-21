using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.ClerkRecoveryService
{
    /// <summary>
    /// The steps of a clerk action, in the order that makes a restart safe. The mark goes onto the crop or animal first, carrying
    /// everything the action gives. The overflow records come second, under ids that never change. The crop or animal is deleted or
    /// updated last, which also clears the mark. A restart after the first step leaves the mark, and <see cref="Recover"/> repeats
    /// the second and third steps: rewriting an overflow record under the same id changes nothing, so nothing is given twice, and
    /// nothing is lost because the mark still holds what was owed. The overflow records are always written before the crop or
    /// animal is deleted or updated.
    /// </summary>
    public static class ClerkRecoveryProcess
    {
        /// <summary>Takes the action: marks the crop or animal, then finishes it.</summary>
        public static void Run(IClerkRecoveryStore store, ClerkRecoveryMark mark)
        {
            store.WriteMark(mark);
            Complete(store, mark);
        }

        /// <summary>
        /// Finishes an action a restart or an error interrupted. Returns false when the crop or animal carries no mark.
        /// </summary>
        public static bool Recover(IClerkRecoveryStore store)
        {
            var mark = store.ReadMark();
            if (mark == null)
                return false;

            Complete(store, mark);
            return true;
        }

        private static void Complete(IClerkRecoveryStore store, ClerkRecoveryMark mark)
        {
            var index = 0;
            foreach (var item in ClerkRecoveryRules.GetItems(mark))
                store.WriteOverflowItems(mark.PlayerId, ClerkRecoveryRules.GetOverflowRecordPrefix(mark.Id, index++), item.Key, item.Value);

            if (mark.Crate != null)
                store.WriteOverflowCrate(mark.PlayerId, ClerkRecoveryRules.GetOverflowRecordPrefix(mark.Id, index), mark.Crate);

            store.FinishSource(mark);
        }
    }
}
