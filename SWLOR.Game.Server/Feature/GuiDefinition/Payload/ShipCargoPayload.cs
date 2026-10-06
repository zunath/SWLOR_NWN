using SWLOR.Game.Server.Service.GuiService;
namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public sealed class ShipCargoPayload : GuiPayloadBase
    {
        public string ShipId { get; }
        public ShipCargoPayload(string shipId) => ShipId = shipId;
    }
}
