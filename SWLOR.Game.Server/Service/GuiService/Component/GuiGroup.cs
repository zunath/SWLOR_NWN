using System.Linq;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.GuiService.Component
{
    public class GuiGroup<T> : GuiExpandableComponent<T>, IGuiScrollable
        where T: IGuiViewModel
    {
        private bool ShowBorder { get; set; }

        /// <summary>
        /// The scroll mode of the group. Readable so layout validation can treat
        /// scrollable groups as decoupling their content from parent constraints.
        /// </summary>
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
            if (Elements.Count <= 0)
            {
                Elements.Add(new GuiSpacer<T>());
            }

            var child = Elements.ElementAt(0).ToJson();
            return Nui.Group(child, ShowBorder, Scrollbars);
        }
    }
}
