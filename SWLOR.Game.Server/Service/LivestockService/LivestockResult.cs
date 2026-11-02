namespace SWLOR.Game.Server.Service.LivestockService
{
    /// <summary>What a livestock action did: a refusal, or success with the message the player reads.</summary>
    public class LivestockResult
    {
        public LivestockRefusal Refusal { get; }

        public string Message { get; }

        public bool Succeeded => Refusal == LivestockRefusal.None;

        private LivestockResult(LivestockRefusal refusal, string message)
        {
            Refusal = refusal;
            Message = message;
        }

        public static LivestockResult Refused(LivestockRefusal refusal) => new(refusal, LivestockRules.Describe(refusal));

        /// <summary>A refusal whose message needs a time or a name, built by the caller.</summary>
        public static LivestockResult Refused(LivestockRefusal refusal, string message) => new(refusal, message);

        public static LivestockResult Success(string message) => new(LivestockRefusal.None, message);
    }
}
