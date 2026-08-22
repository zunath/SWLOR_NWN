using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// An incubation field note registered for a single mutation target beast.
    /// </summary>
    public class FieldNoteDetail
    {
        public BeastType Target { get; }
        public KeyItemType Note { get; }
        public FieldNoteAcquisitionType Acquisition { get; }

        public FieldNoteDetail(BeastType target, KeyItemType note, FieldNoteAcquisitionType acquisition)
        {
            Target = target;
            Note = note;
            Acquisition = acquisition;
        }
    }
}
