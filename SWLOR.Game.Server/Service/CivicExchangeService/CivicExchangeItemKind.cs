namespace SWLOR.Game.Server.Service.CivicExchangeService
{
    /// <summary>What a Civic Exchange item gives the citizen who buys it.</summary>
    public enum CivicExchangeItemKind
    {
        Invalid = 0,

        /// <summary>Random crafting materials of one tier, delivered on purchase. Limited to 3 a week per CD key.</summary>
        MaterialsCrate = 1,

        /// <summary>A long buff, as strong as a top-tier food buff, that replaces food buffs and is replaced by them.</summary>
        Buff = 2
    }
}
