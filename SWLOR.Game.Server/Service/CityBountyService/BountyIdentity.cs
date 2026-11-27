namespace SWLOR.Game.Server.Service.CityBountyService
{
    /// <summary>
    /// The identity a bounty is on: a player's own character, or one of their disguises. A disguise keeps its own flags,
    /// notice and weekly limit, so bounties are counted per identity and never link a disguise to its owner.
    /// </summary>
    public readonly record struct BountyIdentity
    {
        public BountyIdentity(string playerId, string disguiseId)
        {
            PlayerId = playerId ?? string.Empty;
            DisguiseId = disguiseId ?? string.Empty;
        }

        public string PlayerId { get; }

        /// <summary>The disguise's id, or empty for the character itself.</summary>
        public string DisguiseId { get; }

        public bool IsDisguise => DisguiseId.Length > 0;
    }
}
