using SWLOR.Core.Beamdog;

namespace SWLOR.Core.Service.GuiService.Component
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
