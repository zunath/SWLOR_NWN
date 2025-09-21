namespace SWLOR.Shared.UI.Contracts
{
    public interface IGuiAcceptsPriceChange
    {
        void ChangePrice(string recordId, int price);
    }
}
