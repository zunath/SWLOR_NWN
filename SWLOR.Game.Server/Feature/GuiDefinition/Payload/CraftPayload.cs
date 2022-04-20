using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
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
