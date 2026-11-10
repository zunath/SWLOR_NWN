using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ExtraHotbar3Definition : ExtraHotbarDefinitionBase
    {
        protected override GuiWindowType WindowType => GuiWindowType.ExtraHotbar3;
        protected override float InitialY => 320f;
    }
}
