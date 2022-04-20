namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiAcceptsPriceChange
    {
        void ChangePrice(string recordId, int price);
    }
}
