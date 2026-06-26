namespace SWLOR.Game.Server.Service.DisguiseService
{
    public class SaveDisguiseResult
    {
        public bool IsSuccessful { get; }
        public string ErrorMessage { get; }

        private SaveDisguiseResult(bool isSuccessful, string errorMessage)
        {
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;
        }

        public static SaveDisguiseResult Success()
        {
            return new SaveDisguiseResult(true, string.Empty);
        }

        public static SaveDisguiseResult Failure(string errorMessage)
        {
            return new SaveDisguiseResult(false, errorMessage);
        }
    }
}
