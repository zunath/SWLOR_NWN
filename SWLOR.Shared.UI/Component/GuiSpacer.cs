using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Nui;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Shared.UI.Component
{
    public class GuiSpacer<T> : GuiWidget<T, GuiSpacer<T>>
        where T: IGuiViewModel
    {
        /// <summary>
        /// Builds the Spacer element.
        /// </summary>
        /// <returns>Json representing the spacer element.</returns>
        public override Json BuildElement()
        {
            return Nui.Spacer();
        }
    }
}
