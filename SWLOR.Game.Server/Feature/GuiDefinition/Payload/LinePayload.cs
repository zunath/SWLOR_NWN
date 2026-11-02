using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    /// <summary>Opens the Line window on the fight a bite started.</summary>
    public class LinePayload : GuiPayloadBase
    {
        public FishingFight Fight { get; }

        public LinePayload(FishingFight fight)
        {
            Fight = fight;
        }
    }
}
