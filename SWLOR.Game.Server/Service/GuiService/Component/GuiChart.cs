using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiChart: GuiWidget
    {
        private List<GuiChartSlot> Slots { get; set; }

        public GuiChart AddSlot(Action<GuiChartSlot> slot)
        {
            var newSlot = new GuiChartSlot();
            Slots.Add(newSlot);
            slot(newSlot);

            return this;
        }

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
