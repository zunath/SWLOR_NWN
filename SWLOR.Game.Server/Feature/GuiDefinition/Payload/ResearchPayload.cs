using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class ResearchPayload: GuiPayloadBase
    {
        public uint BlueprintItem { get; set; }
        public RecipeType SelectedRecipe { get; set; }

        public ResearchPayload(uint blueprintItem, RecipeType recipe)
        {
            BlueprintItem = blueprintItem;
            SelectedRecipe = recipe;
        }
    }
}
