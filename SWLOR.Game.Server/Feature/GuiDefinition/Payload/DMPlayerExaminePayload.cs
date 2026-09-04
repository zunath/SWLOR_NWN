using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.Payload
{
    public class DMPlayerExaminePayload: GuiPayloadBase
    {
        public uint Target { get; set; }

        /// <summary>
        /// Optional tab to open directly to, bypassing the default Details tab - e.g.
        /// DMPlayerExamineViewModel.MasteriesView when opened via the mastery review
        /// queue's "Open Full Profile" button. Null/empty opens Details as normal.
        /// </summary>
        public string InitialView { get; set; }

        public DMPlayerExaminePayload(uint target, string initialView = null)
        {
            Target = target;
            InitialView = initialView;
        }
    }
}
