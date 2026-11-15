namespace SWLOR.Game.Server.Domain.UI
{
    public class ManageApartmentPayload: GuiPayloadBase
    {
        public string SpecificPropertyId { get; set; }

        public ManageApartmentPayload(string specificPropertyId)
        {
            SpecificPropertyId = specificPropertyId;
        }
    }
}
