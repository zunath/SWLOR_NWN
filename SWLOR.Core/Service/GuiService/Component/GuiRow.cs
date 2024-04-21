using SWLOR.Core.Beamdog;

namespace SWLOR.Core.Service.GuiService.Component
{
    public class GuiRow<T> : GuiExpandableComponent<T>
        where T: IGuiViewModel
    {
        /// <summary>
        /// Builds the GuiRow element.
        /// </summary>
        /// <returns>Json representing the row element.</returns>
        public override Json BuildElement()
        {
            var row = JsonArray();

            foreach (var element in Elements)
            {
                row = JsonArrayInsert(row, element.ToJson());
            }

            return Nui.Row(row);
        }
    }
}
