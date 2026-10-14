namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// One character found by the DM player search. Staff tools show canonical identity.
    /// </summary>
    public class DMPlayerSearchResult
    {
        public string PlayerId { get; }
        public bool IsOnline { get; }
        public string Name { get; }
        public string Descriptor { get; }
        public string AccountName { get; }
        public string CDKey { get; }
        public string LocationName { get; }
        public DateTime? LastLoginDate { get; }

        public DMPlayerSearchResult(
            string playerId,
            bool isOnline,
            string name,
            string descriptor,
            string accountName,
            string cdKey,
            string locationName,
            DateTime? lastLoginDate)
        {
            PlayerId = playerId;
            IsOnline = isOnline;
            Name = name;
            Descriptor = descriptor;
            AccountName = accountName;
            CDKey = cdKey;
            LocationName = locationName;
            LastLoginDate = lastLoginDate;
        }
    }
}
