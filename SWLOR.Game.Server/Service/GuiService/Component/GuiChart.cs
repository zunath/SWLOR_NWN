using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiChart<T> : GuiWidget<T, GuiChart<T>>
        where T: IGuiDataModel
    {
        private List<GuiChartSlot<T>> Slots { get; set; }

        public GuiChart<T> AddSlot(Action<GuiChartSlot<T>> slot)
        {
            var newSlot = new GuiChartSlot<T>();
            Slots.Add(newSlot);
            slot(newSlot);

            return this;
        }

        public GuiChart()
        {
            Slots = new List<GuiChartSlot<T>>();
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
