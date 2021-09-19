using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiListTemplate<T>
        where T: IGuiDataModel
    {
        private List<GuiListTemplateCell<T>> Cells { get; set; }

        public GuiListTemplate()
        {
            Cells = new List<GuiListTemplateCell<T>>();
        }

        public GuiListTemplate<T> AddCell(Action<GuiListTemplateCell<T>> cell)
        {
            var newCell = new GuiListTemplateCell<T>();
            Cells.Add(newCell);
            cell(newCell);

            return this;
        }

        public Json ToJson()
        {
            var template = JsonArray();

            foreach (var cell in Cells)
            {
                template = JsonArrayInsert(template, cell.ToJson());
            }

            return template;
        }
    }
}
