using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiColumn<T> : GuiWidget<T, GuiColumn<T>>
        where T: IGuiViewModel
    {
        public List<GuiRow<T>> Rows { get; }

        public GuiColumn<T> AddRow(Action<GuiRow<T>> row)
        {
            var newRow = new GuiRow<T>();
            Rows.Add(newRow);
            row(newRow);

            return this;
        }

        public GuiColumn()
        {
            Rows = new List<GuiRow<T>>();
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
