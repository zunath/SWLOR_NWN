using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ExtraHotbar2Definition : ExtraHotbarDefinitionBase
    {
        protected override GuiWindowType WindowType => GuiWindowType.ExtraHotbar2;
        protected override float InitialY => 260f;
    }
}
