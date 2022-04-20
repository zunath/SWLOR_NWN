namespace SWLOR.Game.Server.Service.GuiService
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
