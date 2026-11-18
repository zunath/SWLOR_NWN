using System.Collections.Generic;
namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Holds the status effects of players who logged out and are still waiting for them to return. The status effects tracker asks the
    /// holders when a removal reaches every target by source, so what a logged-out player keeps is cleaned up with the rest.
    /// </summary>
    public interface ILoggedOutStatusEffectSource
    {
        /// <summary>The effects each logged-out player holds, one tracker per player.</summary>
        IEnumerable<CreatureStatusEffect> GetLoggedOutEffects();
    }
}
