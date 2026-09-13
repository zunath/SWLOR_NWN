namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// A queued or active training entry inside PlayerMasteryProfile.TrainingQueue.
    /// Index 0 is always the active entry. Finish = StartDate + DurationDays - ReductionDays.
    /// </summary>
    public class MasteryTrainingEntry
    {
        public MasteryTrainingEntry()
        {
            MasteryId = string.Empty;
            RequestId = string.Empty;
        }

        public string MasteryId { get; set; }
        public int TargetTier { get; set; }
        public DateTime StartDate { get; set; }
        public int DurationDays { get; set; }

        /// <summary>
        /// Cumulative staff time reductions applied to this entry (event participation, etc).
        /// </summary>
        public int ReductionDays { get; set; }

        public MasteryTrainingSource Source { get; set; }

        /// <summary>The MasteryRequest.Id this entry was created from, if any.</summary>
        public string RequestId { get; set; }
    }
}
