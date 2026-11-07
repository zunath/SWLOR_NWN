using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>
    /// What a farmhand's pending catch-up holds and what a restart does with it, with no database or engine calls
    /// (<see cref="FarmhandCatchUpProcess"/> orders the writes and <see cref="Farmhands"/> reads and writes the records).
    /// </summary>
    public static class FarmhandCatchUpRules
    {
        /// <summary>
        /// The pending catch-up for a replay that has run: the farmhand and every record the replay changed, each as the replay left it.
        /// The records are the replay's own copies, which no stored record shares.
        /// </summary>
        /// <param name="farmhand">The farmhand as the replay left it.</param>
        /// <param name="fromUtc">The farmhand's last processed time before the replay.</param>
        public static FarmhandCatchUp Build(Farmhand farmhand, DateTime fromUtc, FarmhandReplayResult result)
        {
            var pending = new FarmhandCatchUp
            {
                Id = FarmhandCatchUp.KeyFor(farmhand.TargetId),
                TargetId = farmhand.TargetId,
                AssignmentId = farmhand.DroidAssignmentId,
                FromUtc = fromUtc,
                UntilUtc = farmhand.LastProcessedUtc,
                Farmhand = farmhand
            };
            pending.Animals.AddRange(result.Animals);
            pending.Plots.AddRange(result.Plots);
            pending.WanderedOff.AddRange(result.WanderedOff);
            pending.Withered.AddRange(result.Withered);
            return pending;
        }

        /// <summary>
        /// What to do with a pending catch-up. The farmhand is written after every other record in it, so a farmhand that holds the
        /// catch-up's time means every record holds it too. A farmhand that is gone, or a newer assignment's, is not the one the
        /// catch-up belongs to, and writing it would bring a released farmhand back.
        /// </summary>
        /// <param name="current">The farmhand now, or null when it is gone.</param>
        public static FarmhandCatchUpSettlement Settle(FarmhandCatchUp pending, Farmhand current)
        {
            if (current == null || current.DroidAssignmentId != pending.AssignmentId)
                return FarmhandCatchUpSettlement.Discard;

            return current.LastProcessedUtc >= pending.UntilUtc ? FarmhandCatchUpSettlement.Finished : FarmhandCatchUpSettlement.Apply;
        }
    }
}
