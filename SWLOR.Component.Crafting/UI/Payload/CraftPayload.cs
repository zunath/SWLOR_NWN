using SWLOR.Component.Crafting.Enums;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Crafting.UI.Payload
{
    public class CraftPayload: GuiPayloadBase
    {
        public RecipeType Recipe { get; set; }
        public uint BlueprintItem { get; set; }

        public CraftPayload(RecipeType recipe, uint blueprintItem)
        {
            Recipe = recipe;
            BlueprintItem = blueprintItem;
        }
    }
}
