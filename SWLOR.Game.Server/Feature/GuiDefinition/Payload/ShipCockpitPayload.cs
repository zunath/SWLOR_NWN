using SWLOR.Game.Server.Service.GuiService;
namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public sealed class ShipCockpitPayload : GuiPayloadBase
    {
        public string ShipId { get; }
        public ShipCockpitPayload(string shipId) => ShipId = shipId;
    }
}
