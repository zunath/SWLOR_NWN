using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Shared.UI.Model
{
    public class GuiPlayerWindow
    {
        public int WindowToken { get; set; }
        public IGuiViewModel ViewModel { get; set; }

        public GuiPlayerWindow(IGuiViewModel viewModel)
        {
            ViewModel = viewModel;
        }

    }
}
