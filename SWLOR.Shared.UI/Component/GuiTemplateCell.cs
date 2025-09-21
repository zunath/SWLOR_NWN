using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Nui;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Shared.UI.Component
{
    public class GuiTemplateCell<T>: GuiExpandableComponent<T>
        where T: IGuiViewModel
    {
        private bool IsVariable { get; set; } = true;
        
        /// <summary>
        /// Sets whether the cell is variable in size.
        /// </summary>
        /// <param name="isVariable">If true, cell will grow in size. If false, it will remain static.</param>
        public void SetIsVariable(bool isVariable)
        {
            IsVariable = isVariable;
        }

        public override Json BuildElement()
        {
            throw new NotSupportedException();
        }

        public override Json ToJson()
        {
            var width = Width <= 0 ? 0 : Width;
            var element = Elements.ElementAt(0).ToJson();

            return Nui.ListTemplateCell(element, width, IsVariable);
        }
    }
}
