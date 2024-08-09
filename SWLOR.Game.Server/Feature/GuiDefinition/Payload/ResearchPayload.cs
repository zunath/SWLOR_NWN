using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
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
