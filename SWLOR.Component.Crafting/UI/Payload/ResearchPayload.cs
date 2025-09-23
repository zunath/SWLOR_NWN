using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Crafting.UI.Payload
{
    public class ResearchPayload: IGuiPayload
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
