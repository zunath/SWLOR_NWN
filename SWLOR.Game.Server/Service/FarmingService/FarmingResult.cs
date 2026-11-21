namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>What a farming action did: a refusal, or success with the message the player reads.</summary>
    public class FarmingResult
    {
        public FarmingRefusal Refusal { get; }

        public string Message { get; }

        public bool Succeeded => Refusal == FarmingRefusal.None;

        private FarmingResult(FarmingRefusal refusal, string message)
        {
            Refusal = refusal;
            Message = message;
        }

        public static FarmingResult Refused(FarmingRefusal refusal) => new(refusal, FarmingRules.Describe(refusal));

        public static FarmingResult Success(string message) => new(FarmingRefusal.None, message);
    }
}
