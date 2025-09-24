using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Crafting.Enums;

namespace SWLOR.Component.Crafting.UI.Payload
{
    public class CraftPayload: IGuiPayload
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
