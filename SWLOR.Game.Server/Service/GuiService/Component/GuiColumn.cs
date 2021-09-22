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
        public GuiColumn<T> AddRow(Action<GuiRow<T>> row)
        {
            var newRow = new GuiRow<T>();
            Elements.Add(newRow);
            row(newRow);

            return this;
        }

        public override Json BuildElement()
        {
            var column = JsonArray();

            foreach (var row in Elements)
            {
                column = JsonArrayInsert(column, row.ToJson());
            }

            return Nui.Column(column);
        }
    }
}
