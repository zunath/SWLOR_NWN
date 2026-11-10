using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
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
