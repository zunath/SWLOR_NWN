namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// A single earned tier within a PlayerMasteryLevel's history. Source is retained so
    /// MasteryRules.Abandon can recompute the correct retrain-credit tier and Quick Slot
    /// refund if this tier is later abandoned.
    /// </summary>
    public class MasteryTierRecord
    {
        public int Tier { get; set; }
        public DateTime DateEarned { get; set; }
        public MasteryTrainingSource Source { get; set; }
    }
}
