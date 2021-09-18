using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService
{
    public class GuiListTemplateCell: GuiWidget
    {
        public GuiWidget Element { get; set; }
        public float CellWidth { get; set; }
        public bool IsStatic { get; set; }

        public GuiListTemplateCell()
        {
            CellWidth = 0f;
            IsStatic = false;
        }

        public override Json BuildElement()
        {
            return Nui.ListTemplateCell(Element.ToJson(), CellWidth, IsStatic);
        }
    }
}
