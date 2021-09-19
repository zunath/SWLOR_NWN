namespace SWLOR.Game.Server.Service.GuiService
{
    public delegate void GuiEventDelegate<TDataModel>(
        TDataModel viewModel, 
        uint player, 
        int windowToken, 
        string windowId, 
        int arrayIndex = -1)
        where TDataModel: IGuiViewModel;

    public delegate GuiPlayerWindow CreatePlayerWindowDelegate(uint player);
}
