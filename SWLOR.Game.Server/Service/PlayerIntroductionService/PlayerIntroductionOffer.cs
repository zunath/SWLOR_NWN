namespace SWLOR.Game.Server.Service.PlayerIntroductionService
{
    public record PlayerIntroductionOffer(
        Guid Id,
        uint Presenter,
        string PresenterPlayerId,
        string IdentityKey,
        string DisplayName,
        string Name,
        DateTime ExpiresAt)
    {
        public string ValidateIdentityAndConsent(string playerId, string identityKey,
            string currentKnownName, string approvedKnownName)
        {
            if (PresenterPlayerId != playerId || IdentityKey != identityKey)
                return "This introduction is no longer available.";

            if (!string.Equals(currentKnownName ?? string.Empty, approvedKnownName ?? string.Empty,
                    StringComparison.Ordinal))
                return "Your private label changed. Review the introduction again before remembering it.";

            return string.Empty;
        }
    }
}
