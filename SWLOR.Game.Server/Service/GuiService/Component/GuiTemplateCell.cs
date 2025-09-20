using System;
using System.Linq;
using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
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
