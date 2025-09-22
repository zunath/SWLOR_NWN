using SWLOR.Component.Crafting.Enums;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Component.Crafting.UI.Payload
{
    public class ResearchPayload: GuiPayloadBase
    {
        public string PropertyId { get; set; }
        public uint BlueprintItem { get; set; }
        public RecipeType SelectedRecipe { get; set; }

        public ResearchPayload(string propertyId, uint blueprintItem, RecipeType recipe)
        {
            PropertyId = propertyId;
            BlueprintItem = blueprintItem;
            SelectedRecipe = recipe;
        }
    }
}
