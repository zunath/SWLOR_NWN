namespace SWLOR.Core.Service.GuiService
{
    public interface IGuiAcceptsPriceChange
    {
        void ChangePrice(string recordId, int price);
    }
}
