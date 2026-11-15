using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
{
    /// <summary>
    /// Raised when a player's set of equipped techniques changes, since equipping or unequipping
    /// one can change loadout-derived stats, resistances, or elemental resonance.
    /// </summary>
    public class TechniqueChangedRefreshEvent: IGuiRefreshEvent
    {
    }
}
