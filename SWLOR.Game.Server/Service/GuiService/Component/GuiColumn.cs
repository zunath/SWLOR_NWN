using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiColumn: GuiWidget
    {
        private List<GuiRow> Rows { get; set; }

        public GuiColumn AddRow(Action<GuiRow> row)
        {
            var newRow = new GuiRow();
            Rows.Add(newRow);
            row(newRow);

            return this;
        }

        public GuiColumn()
        {
            Rows = new List<GuiRow>();
        }

        public override Json BuildElement()
        {
            var column = JsonArray();

            foreach (var row in Rows)
            {
                column = JsonArrayInsert(column, row.ToJson());
            }

            return Nui.Column(column);
        }
    }
}
