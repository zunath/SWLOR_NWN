namespace SWLOR.Game.Server.Domain.UI
{
    public interface IGuiRefreshable<in T>
        where T: IGuiRefreshEvent
    {
        void Refresh(T payload);
    }
}
