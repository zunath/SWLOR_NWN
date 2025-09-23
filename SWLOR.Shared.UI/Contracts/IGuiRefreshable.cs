using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.UI.Contracts
{
    public interface IGuiRefreshable<in T>
        where T: IGuiRefreshEvent
    {
        void Refresh(T payload);
    }
}
