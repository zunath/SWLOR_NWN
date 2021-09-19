using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiGroup<T> : GuiWidget<T, GuiGroup<T>>
        where T: IGuiDataModel
    {
        public IGuiWidget Child { get; set; }
        public bool ShowBorder { get; set; }
        public NuiScrollbars Scrollbars { get; set; }

        public GuiGroup()
        {
            ShowBorder = true;
            Scrollbars = NuiScrollbars.Auto;
        }

        public override Json BuildElement()
        {
            var child = Child.ToJson();

            return Nui.Group(child, ShowBorder, Scrollbars);
        }
    }
}
