using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiGroup<T> : GuiExpandableComponent<T>
        where T: IGuiViewModel
    {
        public bool ShowBorder { get; private set; }
        public NuiScrollbars Scrollbars { get; private set; }

        public GuiGroup()
        {
            ShowBorder = true;
            Scrollbars = NuiScrollbars.Auto;
        }

        public GuiGroup<T> SetShowBorder(bool showBorder)
        {
            ShowBorder = showBorder;

            return this;
        }

        public GuiGroup<T> SetScrollbars(NuiScrollbars scrollBars)
        {
            Scrollbars = scrollBars;

            return this;
        }


        public override Json BuildElement()
        {
            var child = Elements.ElementAt(0).ToJson();
            return Nui.Group(child, ShowBorder, Scrollbars);
        }
    }
}
