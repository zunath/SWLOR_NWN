using SWLOR.Core.Service.CraftService;
using SWLOR.Core.Service.GuiService;

namespace SWLOR.Core.Feature.GuiDefinition.Payload
{
    public class CraftPayload: GuiPayloadBase
    {
        public RecipeType Recipe { get; set; }

        public CraftPayload(RecipeType recipe)
        {
            Recipe = recipe;
        }
    }
}
