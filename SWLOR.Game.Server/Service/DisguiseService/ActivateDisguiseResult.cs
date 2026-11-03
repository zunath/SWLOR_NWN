namespace SWLOR.Game.Server.Service.DisguiseService
{
    public class ActivateDisguiseResult
    {
        public bool IsSuccessful { get; }
        public string ErrorMessage { get; }

        /// <summary>
        /// The activation was not made now: PvP combat holds it back, and it takes effect when the combat window ends.
        /// <see cref="ErrorMessage"/> says so, and <see cref="IsSuccessful"/> is false because the disguise is not on.
        /// </summary>
        public bool IsQueued { get; private init; }

        private ActivateDisguiseResult(bool isSuccessful, string errorMessage)
        {
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;
        }

        public static ActivateDisguiseResult Queued(string message)
        {
            return new ActivateDisguiseResult(false, message) { IsQueued = true };
        }

        public static ActivateDisguiseResult Success()
        {
            return new ActivateDisguiseResult(true, string.Empty);
        }

        public static ActivateDisguiseResult Failure(string errorMessage)
        {
            return new ActivateDisguiseResult(false, errorMessage);
        }
    }
}
