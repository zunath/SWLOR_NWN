using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent
{
    /// <summary>
    /// Raised when a player's set of equipped techniques changes, since equipping or unequipping
    /// one grants/revokes feats and recomputes the technique set bonus.
    /// </summary>
    public class TechniqueChangedRefreshEvent: IGuiRefreshEvent
    {
    }
}
