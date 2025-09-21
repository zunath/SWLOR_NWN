using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.UI.Model;

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
