using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiChart: GuiWidget
    {
        public List<GuiChartSlot> Slots { get; set; }

        public GuiChart()
        {
            Slots = new List<GuiChartSlot>();
        }

        public override Json BuildElement()
        {
            var slots = JsonArray();

            foreach (var slot in Slots)
            {
                slots = JsonArrayInsert(slots, slot.ToJson());
            }

            return Nui.Chart(slots);
        }
    }
}
