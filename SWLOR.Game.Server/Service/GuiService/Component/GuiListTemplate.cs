using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiListTemplate
    {
        private List<GuiListTemplateCell> Cells { get; set; }

        public GuiListTemplate()
        {
            Cells = new List<GuiListTemplateCell>();
        }

        public GuiListTemplate AddCell(Action<GuiListTemplateCell> cell)
        {
            var newCell = new GuiListTemplateCell();
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
