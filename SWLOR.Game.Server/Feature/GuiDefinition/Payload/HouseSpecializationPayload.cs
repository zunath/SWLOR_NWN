using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class HouseSpecializationPayload : GuiPayloadBase
    {
        /// <summary>The property id of the house whose specialization the window shows.</summary>
        public string HouseId { get; set; }

        public HouseSpecializationPayload(string houseId)
        {
            HouseId = houseId;
        }
    }
}
