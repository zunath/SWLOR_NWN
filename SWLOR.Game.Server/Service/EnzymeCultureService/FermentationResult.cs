namespace SWLOR.Game.Server.Service.EnzymeCultureService
{
    /// <summary>What a Fermentation Vat action did: a refusal, or success with the message the player reads.</summary>
    public class FermentationResult
    {
        public FermentationRefusal Refusal { get; }

        public string Message { get; }

        public bool Succeeded => Refusal == FermentationRefusal.None;

        private FermentationResult(FermentationRefusal refusal, string message)
        {
            Refusal = refusal;
            Message = message;
        }

        public static FermentationResult Refused(FermentationRefusal refusal) => new(refusal, EnzymeCultureRules.Describe(refusal));

        /// <summary>A refusal whose message needs a name or a time, built by the caller.</summary>
        public static FermentationResult Refused(FermentationRefusal refusal, string message) => new(refusal, message);

        public static FermentationResult Success(string message) => new(FermentationRefusal.None, message);
    }
}
