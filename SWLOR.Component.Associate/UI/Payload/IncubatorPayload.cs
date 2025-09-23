using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.Associate.UI.Payload
{
    public class IncubatorPayload: IGuiPayload
    {
        public string PropertyId { get; set; }
        public string IncubationJobId { get; set; }

        public IncubatorPayload(string propertyId, string incubationJobId)
        {
            PropertyId = propertyId;
            IncubationJobId = incubationJobId;
        }
    }
}
