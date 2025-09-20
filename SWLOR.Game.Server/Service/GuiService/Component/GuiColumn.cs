using System;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiColumn<T> : GuiWidget<T, GuiColumn<T>>
        where T: IGuiViewModel
    {
        /// <summary>
        /// Adds a row to this column.
        /// </summary>
        /// <param name="row">An action which is used to construct the new row.</param>
        public GuiColumn<T> AddRow(Action<GuiRow<T>> row)
        {
            var newRow = new GuiRow<T>();
            Elements.Add(newRow);
            row(newRow);

            return this;
        }

        /// <summary>
        /// Builds the GuiColumn element.
        /// </summary>
        /// <returns>Json representing the column element.</returns>
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
