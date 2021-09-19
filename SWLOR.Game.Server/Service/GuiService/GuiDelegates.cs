namespace SWLOR.Game.Server.Service.GuiService
{
    public delegate void GuiEventDelegate(IGuiDataModel viewModel, uint player, int windowToken, string windowId, int arrayIndex = -1);
    public delegate GuiPlayerWindow CreatePlayerWindowDelegate(uint player);
}
