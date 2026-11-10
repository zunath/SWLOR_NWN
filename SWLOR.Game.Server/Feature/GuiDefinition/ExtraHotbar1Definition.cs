using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    public class ExtraHotbar1Definition : ExtraHotbarDefinitionBase
    {
        protected override GuiWindowType WindowType => GuiWindowType.ExtraHotbar1;
        protected override float InitialY => 200f;
    }
}
