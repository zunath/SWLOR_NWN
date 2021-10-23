using System;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiTemplateCell<T>: GuiExpandableComponent<T>
        where T: IGuiViewModel
    {
        /// <summary>
        /// If zero, the cell will grow/shrink automatically.
        /// If any number greater than zero, that pixel width will be used.
        /// If any number less than zero, this will be set to zero.
        /// </summary>
        public float Width { get; set; } = 0;

        /// <summary>
        /// If true, the cell can grow if space is available; otherwise it will be static
        /// </summary>
        public bool IsVariable { get; set; } = true;

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
