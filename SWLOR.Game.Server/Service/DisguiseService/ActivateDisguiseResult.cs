namespace SWLOR.Game.Server.Service.DisguiseService
{
    public class ActivateDisguiseResult
    {
        public bool IsSuccessful { get; }
        public string ErrorMessage { get; }

        private ActivateDisguiseResult(bool isSuccessful, string errorMessage)
        {
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;
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
