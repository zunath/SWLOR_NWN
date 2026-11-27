namespace SWLOR.Game.Server.Domain.UI
{
    public interface IGuiAcceptsPriceChange
    {
        void ChangePrice(string recordId, int price);
    }
}
