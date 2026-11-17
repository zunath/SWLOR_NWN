using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// The currency balance read the lightsaber workbench makes. Items owns the interface and <c>ICurrencyService</c> extends it
    /// (supply rule 2 of MaintainabilityRefactorDomainOrder.md; P5-15a).
    /// </summary>
    public interface ICurrencyBalance
    {
        /// <summary>
        /// Retrieves the amount of a specific currency held by a player.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of currency to look for</param>
        /// <returns>The amount of the currency held.</returns>
        int GetCurrency(uint player, CurrencyType type);
    }
}
