namespace SWLOR.Game.Server.Service.ClerkRecoveryService
{
    /// <summary>What a clerk action did: whether it worked, and the message the player reads.</summary>
    public sealed class ClerkRecoveryResult
    {
        public ClerkRecoveryResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; }

        public string Message { get; }

        public static ClerkRecoveryResult Refused(string message) => new(false, message);

        public static ClerkRecoveryResult Success(string message) => new(true, message);
    }
}
