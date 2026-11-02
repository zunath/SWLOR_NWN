namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>The outcome of an official's attempt to decide a situation.</summary>
    public sealed class CityDecisionResult
    {
        private CityDecisionResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }

        /// <summary>What the official is told: the effects on success, or why the decision was refused.</summary>
        public string Message { get; }

        public static CityDecisionResult Success(string message)
        {
            return new CityDecisionResult(true, message);
        }

        public static CityDecisionResult Refused(string message)
        {
            return new CityDecisionResult(false, message);
        }
    }
}
