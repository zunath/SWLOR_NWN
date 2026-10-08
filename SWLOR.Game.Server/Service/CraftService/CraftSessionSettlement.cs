namespace SWLOR.Game.Server.Service.CraftService
{
    // One settlement claim per committed craft. Claim before inventory or XP callbacks.
    public sealed class CraftSessionSettlement
    {
        private Guid? _claimedSession;

        public bool IsClaimed => _claimedSession.HasValue;

        public bool TryClaim(CraftSession session)
        {
            if (session == null || session.Status == CraftSessionStatus.Active || _claimedSession.HasValue)
                return false;
            _claimedSession = session.Id;
            return true;
        }
    }
}
